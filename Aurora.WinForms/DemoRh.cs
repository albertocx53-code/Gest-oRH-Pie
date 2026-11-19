using Aurora.Core;

namespace Aurora.WinForms;

// Só o Program cria este repositório. As telas recebem RhService e passam pelas mesmas regras.
internal sealed class DemoRh : IRhRepository
{
    public static readonly DateTime Today = new(2026, 11, 13);
    private int nextEmployee = 109;
    private int nextVacancy = 4;
    private int nextCandidate = 5;

    private List<Employee> Employees { get; } =
    [
        new(101, "Marina Teixeira", "Analista de RH", "Pessoas", "Ativo", new(2022, 3, 14)),
        new(102, "Rafael Monteiro", "Desenvolvedor", "Tecnologia", "Ativo", new(2021, 8, 2)),
        new(103, "Helena Duarte", "Contadora", "Financeiro", "Ativo", new(2020, 5, 18)),
        new(104, "Caio Albuquerque", "Executivo de contas", "Comercial", "Férias", new(2023, 1, 9)),
        new(105, "Beatriz Lemos", "Coord. de operações", "Operações", "Ativo", new(2019, 11, 25)),
        new(106, "Tiago Ferreira", "Analista de suporte", "Tecnologia", "Afastado", new(2024, 2, 5)),
        new(107, "Larissa Campos", "Assistente financeiro", "Financeiro", "Ativo", new(2025, 6, 16)),
        new(108, "Otávio Prado", "Gerente comercial", "Comercial", "Ativo", new(2018, 9, 3))
    ];
    private List<Attendance> Punches { get; } = [];
    private List<Vacancy> Vacancies { get; } =
    [new(1, "Desenvolvedor back-end", "Tecnologia", "Aberta"), new(2, "Analista financeiro", "Financeiro", "Aberta"), new(3, "Assistente de RH", "Pessoas", "Aberta")];
    private List<Candidate> Candidates { get; } =
    [new(1, "Júlia Nogueira", 1, "Entrevista"), new(2, "Amanda Rocha", 1, "Teste técnico"), new(3, "Lucas Barreto", 2, "Triagem"), new(4, "Renata Siqueira", 2, "Proposta")];
    private List<Payroll> Pay { get; } = [];
    private List<Appointment> Agenda { get; } =
    [new("Entrevista · Júlia Nogueira", Today.AddHours(10.5), "Recrutamento"), new("Revisão das ocorrências de ponto", Today.AddHours(14), "Ponto"), new("Fechamento da folha de novembro", new(2026, 11, 27, 16, 0, 0), "Salários")];

    public DemoRh()
    {
        int[] counts = [5, 6, 4, 6, 5];
        var active = Employees.Where(person => person.Status == "Ativo").ToArray();
        for (var day = 0; day < counts.Length; day++)
        {
            var date = Today.AddDays(day - 4);
            for (var i = 0; i < active.Length; i++)
            {
                var status = i >= counts[day] ? "Falta" : i == 0 ? "Atraso" : "Presença";
                var reason = status switch
                {
                    "Atraso" => "Atraso no transporte público.",
                    "Falta" => "Compromisso pessoal comunicado ao RH.",
                    _ => ""
                };
                Punches.Add(new(active[i].Id, date, i < counts[day] ? (i == 0 ? "08:14" : "08:00") : "", i < counts[day] ? "17:00" : "", status, reason));
            }
        }
        foreach (var person in active)
            Pay.Add(new(person.Id, new(2026, 11, 1), 2900 + (person.Id - 100) * 410, 180, 125));
    }

    // Entregue cópias da lista para filtros e tabelas não alterarem os dados em memória.
    IReadOnlyList<Employee> IRhRepository.Employees => Employees.ToArray();
    IReadOnlyList<Attendance> IRhRepository.Punches => Punches.ToArray();
    IReadOnlyList<Vacancy> IRhRepository.Vacancies => Vacancies.ToArray();
    IReadOnlyList<Candidate> IRhRepository.Candidates => Candidates.ToArray();
    IReadOnlyList<Payroll> IRhRepository.Pay => Pay.ToArray();
    IReadOnlyList<Appointment> IRhRepository.Agenda => Agenda.ToArray();

    void IRhRepository.SaveEmployee(Employee employee)
    {
        if (employee.Id == 0) Employees.Add(employee with { Id = nextEmployee++ });
        else Employees[Employees.FindIndex(item => item.Id == employee.Id)] = employee;
    }

    void IRhRepository.DeleteEmployee(int id) => Employees.RemoveAll(item => item.Id == id);

    void IRhRepository.SaveAttendance(Attendance record, bool edit)
    {
        if (edit) Punches[Punches.FindIndex(item => item.Person == record.Person && item.Day == record.Day)] = record;
        else Punches.Add(record);
    }

    void IRhRepository.SaveVacancy(Vacancy vacancy)
    {
        if (vacancy.Id == 0) Vacancies.Add(vacancy with { Id = nextVacancy++ });
        else Vacancies[Vacancies.FindIndex(item => item.Id == vacancy.Id)] = vacancy;
    }

    void IRhRepository.SaveCandidate(Candidate candidate)
    {
        if (candidate.Id == 0) Candidates.Add(candidate with { Id = nextCandidate++ });
        else Candidates[Candidates.FindIndex(item => item.Id == candidate.Id)] = candidate;
    }

    void IRhRepository.SavePayroll(Payroll payroll)
    {
        var index = Pay.FindIndex(item => item.Person == payroll.Person && item.Month == payroll.Month);
        if (index < 0) Pay.Add(payroll);
        else Pay[index] = payroll;
    }

    void IRhRepository.AddAppointment(Appointment appointment) => Agenda.Add(appointment);
}
