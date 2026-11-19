using System.Globalization;

namespace Aurora.Core;

public sealed class RhRuleException(string message) : Exception(message);

public sealed class RhService(IRhRepository repository, RhSession session)
{
    public string User => session.DisplayName;
    public string Profile => session.Role switch
    {
        RhRole.Administrator => "Administrador",
        RhRole.HumanResources => "Recursos Humanos",
        _ => "Gestor"
    };
    public bool CanEdit => session.IsActive && session.Role is RhRole.Administrator or RhRole.HumanResources;
    public bool CanViewPay => CanEdit;

    public IReadOnlyList<Employee> Employees => Read(() => repository.Employees);
    public IReadOnlyList<Vacancy> Vacancies => Read(() => repository.Vacancies);
    public IReadOnlyList<Candidate> Candidates => Read(() => repository.Candidates);
    public IReadOnlyList<Appointment> Agenda => Read(() => repository.Agenda.Where(item => CanViewPay || item.Category != "Salários").ToArray());

    public IReadOnlyList<Attendance> AttendanceBetween(DateTime from, DateTime through) => Read(() =>
    {
        Require(from.Date <= through.Date, "A data final deve ser igual ou posterior à inicial.");
        return repository.Punches.Where(row => row.Day >= from.Date && row.Day <= through.Date).ToArray();
    });

    public IReadOnlyList<Payroll> PayrollFor(DateTime month) => Read(() =>
    {
        Require(CanViewPay, "Seu perfil não tem acesso aos salários.");
        return repository.Pay.Where(item => item.Month == MonthStart(month)).ToArray();
    });

    public string Name(int id) => Employees.FirstOrDefault(item => item.Id == id)?.Name ?? $"Matrícula {id}";
    public string VacancyTitle(int id) => Vacancies.FirstOrDefault(item => item.Id == id)?.Title ?? "Vaga indisponível";
    public int Present(DateTime day) => AttendanceBetween(day, day).Count(item => item.In.Length > 0);

    // Jefferson: grava o cadastro de funcionário (novo ou edição). É aqui que os campos da tela
    // se ligam às regras: Write confere a sessão e o perfil (Gestor não grava), o departamento e
    // a situação precisam estar nas listas de RhOptions, a admissão fica entre 1900 e 2100, e
    // nome e cargo são obrigatórios (até 120 caracteres, sem espaços sobrando nas pontas).
    // Id 0 = funcionário novo; um Id diferente de zero precisa existir para ser editado.
    public void SaveEmployee(Employee item) => Write(() =>
    {
        if (item.Id != 0) Require(repository.Employees.Any(employee => employee.Id == item.Id), "Funcionário não encontrado.");
        Require(RhOptions.Teams.Contains(item.Team), "Selecione um departamento válido.");
        Require(RhOptions.EmployeeStatuses.Contains(item.Status), "Selecione uma situação válida.");
        CheckDate(item.Started);
        repository.SaveEmployee(item with { Name = Text(item.Name, "nome"), Role = Text(item.Role, "cargo"), Started = item.Started.Date });
    });

    // Jefferson: só exclui cadastro sem histórico. Quem já tem ponto ou salário é mantido,
    // e o caminho é mudar a situação para Inativo (botão Inativar, em CadastroDeFuncionarios).
    public void DeleteEmployee(int id) => Write(() =>
    {
        Require(repository.Employees.Any(item => item.Id == id), "Funcionário não encontrado.");
        // Ponto e folha usam a matrícula. Inativar mantém o histórico consultável.
        Require(!repository.Punches.Any(item => item.Person == id) && !repository.Pay.Any(item => item.Person == id),
            "Este cadastro tem registros de ponto ou salário. Você pode mudar a situação para Inativo.");
        repository.DeleteEmployee(id);
    });

    public void SaveAttendance(Attendance record, bool edit) => Write(() =>
    {
        CheckDate(record.Day);
        Require(repository.Employees.Any(item => item.Id == record.Person && (edit || item.Status == "Ativo")), "Selecione um funcionário ativo para registrar o ponto.");
        Require(RhOptions.AttendanceStatuses.Contains(record.Status), "Selecione uma ocorrência válida.");
        var day = record.Day.Date;
        var exists = repository.Punches.Any(item => item.Person == record.Person && item.Day == day);
        Require(exists == edit, edit ? "O registro de ponto não foi encontrado." : "Já existe ponto nesse dia. Use Editar registro para alterá-lo.");
        var entry = record.In.Trim();
        var exit = record.Out.Trim();
        if (record.Status == "Falta") (entry, exit) = ("", "");
        else Require(TryTime(entry, out var start) && TryTime(exit, out var end) && end > start,
            "Informe horários válidos. A saída deve ser posterior à entrada.");
        var reason = Attendance.AcceptsReason(record.Status) ? record.Reason.Trim() : "";
        Require(reason.Length <= 240, "O motivo pode ter até 240 caracteres.");
        repository.SaveAttendance(record with { Day = day, In = entry, Out = exit, Reason = reason }, edit);
    });

    public void AddVacancy(string title, string team) => Write(() =>
    {
        Require(RhOptions.Teams.Contains(team), "Selecione um departamento válido.");
        repository.SaveVacancy(new(0, Text(title, "título da vaga"), team, "Aberta"));
    });

    public void ToggleVacancy(int id) => Write(() =>
    {
        var vacancy = repository.Vacancies.FirstOrDefault(item => item.Id == id) ?? throw new RhRuleException("Vaga não encontrada.");
        repository.SaveVacancy(vacancy with { Status = vacancy.Status == "Aberta" ? "Encerrada" : "Aberta" });
    });

    public void SaveCandidate(Candidate item) => Write(() =>
    {
        var original = repository.Candidates.FirstOrDefault(existing => existing.Id == item.Id);
        Require(item.Id == 0 || original is not null, "Candidatura não encontrada.");
        // Uma vaga fechada mantém quem já estava nela, mas fica fora de novas inscrições.
        Require(repository.Vacancies.Any(job => job.Id == item.Vacancy && (job.Status == "Aberta" || original?.Vacancy == job.Id)),
            "Selecione uma vaga aberta.");
        Require(RhOptions.Stages.Contains(item.Stage), "Selecione uma etapa válida.");
        repository.SaveCandidate(item with { Name = Text(item.Name, "nome do candidato") });
    });

    public void AddAppointment(string title, DateTime day, string time, string category) => Write(() =>
    {
        CheckDate(day);
        Require(RhOptions.AgendaCategories.Contains(category), "Selecione uma categoria válida.");
        Require(TryTime(time.Trim(), out var hours), "Informe um horário no formato HH:mm.");
        repository.AddAppointment(new(Text(title, "título"), day.Date.Add(hours.ToTimeSpan()), category));
    });

    public void SavePayroll(Payroll item) => Write(() =>
    {
        Require(CanViewPay, "Seu perfil não tem acesso aos salários.");
        Require(repository.Employees.Any(employee => employee.Id == item.Person), "Funcionário não encontrado.");
        CheckDate(item.Month);
        Require(ValidAmount(item.Base) && ValidAmount(item.Bonus) && ValidAmount(item.Discount), "Informe valores entre R$ 0,00 e R$ 1.000.000,00, com até duas casas decimais.");
        Require(item.Discount <= item.Base + item.Bonus, "O desconto não pode superar salário e bônus.");
        // Uma competência por pessoa. Revisar o lançamento atualiza essa mesma chave.
        repository.SavePayroll(item with { Month = MonthStart(item.Month) });
    });

    private T Read<T>(Func<T> read)
    {
        lock (repository)
        {
            Require(session.IsActive, "Entre novamente para continuar.");
            return read();
        }
    }

    private void Write(Action write)
    {
        // A consulta usada na validação e a gravação ficam juntas dentro do mesmo processo.
        // Ao ligar um banco compartilhado, mantenha a integridade também na transação e nas chaves do banco.
        lock (repository)
        {
            Require(session.IsActive, "Entre novamente para continuar.");
            Require(CanEdit, "Seu perfil pode consultar os dados, sem editar registros.");
            write();
        }
    }

    private static string Text(string text, string field)
    {
        text = text.Trim();
        Require(text.Length is > 0 and <= 120, $"Preencha {field} com até 120 caracteres.");
        return text;
    }

    private static void Require(bool condition, string error)
    {
        if (!condition) throw new RhRuleException(error);
    }

    private static void CheckDate(DateTime value) => Require(value.Year is >= 1900 and <= 2100, "Informe uma data entre 1900 e 2100.");
    private static DateTime MonthStart(DateTime date) => new(date.Year, date.Month, 1);
    private static bool ValidAmount(decimal value) => value is >= 0 and <= 1_000_000 && value == decimal.Round(value, 2);
    private static bool TryTime(string value, out TimeOnly time) => TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
}
