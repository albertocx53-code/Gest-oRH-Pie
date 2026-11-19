using System.Globalization;

namespace Aurora.Core;

// FrequenciaDePonto - Clóvis (ponto e frequência)
//
// Acrescenta ao RhService as consultas e lançamentos da área de ponto:
//   ConsultarPeriodo -> registros de ponto de um intervalo de datas, com filtro por funcionário e ocorrência
//   ResumoDoPeriodo  -> totais do intervalo (presenças, atrasos, faltas, horas) e totais por funcionário
//   LancarFalta      -> lança uma falta (sem horário) em um dia
//   LancarAtraso     -> lança um atraso (entrada depois do horário padrão) em um dia
//   SemRegistro      -> funcionários ativos que ainda não têm ponto em um dia
//   HorasTrabalhadas -> horas entre a entrada e a saída de um registro
//
// Estes são "métodos de extensão" (repare no "this RhService rh"), o mesmo caminho usado em
// CadastroDeFuncionarios. Na tela a chamada fica data.ConsultarPeriodo(...), como se o método
// fosse do próprio RhService, mas ele mora neste arquivo. Assim o RhService não precisou mudar.
//
// Tudo aqui passa pelos métodos públicos do RhService (Employees, AttendanceBetween e
// SaveAttendance). Por isso as regras de sessão, de perfil e de validação continuam valendo:
// o Gestor consulta o ponto, mas não lança nada.
public static class FrequenciaDePonto
{
    // Entrada esperada. Entrar depois disso é atraso; é o mesmo horário que a demonstração usa.
    public static readonly TimeOnly HorarioPadrao = new(8, 0);

    // Maior intervalo aceito em uma consulta, para a tabela continuar legível.
    public const int MaximoDeDias = 366;

    // Registros de ponto entre duas datas (inclusive), do dia mais antigo para o mais novo e,
    // no mesmo dia, em ordem de nome. "matricula" e "ocorrencia" são filtros opcionais:
    // null quer dizer todos os funcionários / todas as ocorrências (Presença, Atraso ou Falta).
    public static IReadOnlyList<Attendance> ConsultarPeriodo(this RhService rh, DateTime de, DateTime ate, int? matricula = null, string? ocorrencia = null)
    {
        // A ordem das datas é conferida pelo AttendanceBetween do RhService. Aqui só limitamos o tamanho.
        if ((ate.Date - de.Date).TotalDays > MaximoDeDias - 1)
            throw new RhRuleException($"O período pode ter no máximo {MaximoDeDias} dias.");
        if (matricula is { } id && !rh.Employees.Any(item => item.Id == id))
            throw new RhRuleException("Funcionário não encontrado.");
        if (ocorrencia is not null && !RhOptions.AttendanceStatuses.Contains(ocorrencia))
            throw new RhRuleException("Selecione uma ocorrência válida.");

        var nomes = Nomes(rh);
        return rh.AttendanceBetween(de, ate)
            .Where(item => matricula is null || item.Person == matricula)
            .Where(item => ocorrencia is null || item.Status == ocorrencia)
            .OrderBy(item => item.Day)
            .ThenBy(item => nomes.GetValueOrDefault(item.Person, ""), StringComparer.CurrentCulture)
            .ThenBy(item => item.Person)
            .ToArray();
    }

    // Totais dos mesmos registros que ConsultarPeriodo devolve para os mesmos filtros.
    // Assim o resumo mostrado na tela sempre bate com a tabela que está embaixo dele.
    public static ResumoDeFrequencia ResumoDoPeriodo(this RhService rh, DateTime de, DateTime ate, int? matricula = null, string? ocorrencia = null)
    {
        var registros = rh.ConsultarPeriodo(de, ate, matricula, ocorrencia);
        var nomes = Nomes(rh);
        var porFuncionario = registros
            .GroupBy(item => item.Person)
            .Select(grupo => new FrequenciaDoFuncionario(
                grupo.Key,
                nomes.GetValueOrDefault(grupo.Key, $"Matrícula {grupo.Key}"),
                grupo.Count(item => item.Status == "Presença"),
                grupo.Count(item => item.Status == "Atraso"),
                grupo.Count(item => item.Status == "Falta"),
                Somar(grupo)))
            .OrderBy(item => item.Nome, StringComparer.CurrentCulture)
            .ToArray();

        return new ResumoDeFrequencia(
            de.Date,
            ate.Date,
            registros.Count,
            registros.Count(item => item.Status == "Presença"),
            registros.Count(item => item.Status == "Atraso"),
            registros.Count(item => item.Status == "Falta"),
            Somar(registros),
            porFuncionario);
    }

    // Lança uma falta. A falta não tem horário de entrada nem de saída (o RhService limpa os dois).
    // "hoje" é o dia usado como hoje (na demonstração, 13/11/2026): não se lança falta no futuro.
    // Se o funcionário já tem ponto nesse dia, o RhService recusa e manda usar Editar registro.
    public static void LancarFalta(this RhService rh, int matricula, DateTime dia, string motivo, DateTime hoje)
    {
        ConferirDia(rh, dia, hoje);
        rh.SaveAttendance(new Attendance(matricula, dia, "", "", "Falta", motivo), false);
    }

    // Lança um atraso. O atraso tem horário, e a entrada precisa ser depois do horário padrão.
    // O resto (funcionário ativo, horários válidos, saída depois da entrada, dia sem ponto)
    // é conferido pelo SaveAttendance do RhService.
    public static void LancarAtraso(this RhService rh, int matricula, DateTime dia, string entrada, string saida, string motivo, DateTime hoje)
    {
        ConferirDia(rh, dia, hoje);
        // Só conferimos o horário quando o perfil pode editar: para o Gestor a mensagem certa
        // é a do perfil, e ela vem do SaveAttendance.
        if (rh.CanEdit && TryHora(entrada.Trim(), out var hora) && hora <= HorarioPadrao)
            throw new RhRuleException($"Para lançar atraso, a entrada deve ser depois das {HorarioPadrao.ToString("HH:mm", CultureInfo.InvariantCulture)}.");
        rh.SaveAttendance(new Attendance(matricula, dia, entrada, saida, "Atraso", motivo), false);
    }

    // Funcionários ativos que não têm nenhum registro de ponto no dia.
    // A tela usa esta lista para já deixar selecionado quem ainda precisa de falta ou atraso.
    public static IReadOnlyList<Employee> SemRegistro(this RhService rh, DateTime dia)
    {
        var comRegistro = rh.AttendanceBetween(dia, dia).Select(item => item.Person).ToHashSet();
        return rh.Employees.Where(item => item.Status == "Ativo" && !comRegistro.Contains(item.Id)).ToArray();
    }

    // Horas entre a entrada e a saída (sem descontar intervalo). Falta ou horário inválido = zero.
    public static TimeSpan HorasTrabalhadas(Attendance registro) =>
        TryHora(registro.In, out var entrada) && TryHora(registro.Out, out var saida) && saida > entrada
            ? saida - entrada
            : TimeSpan.Zero;

    // Ex.: 232 horas e 50 minutos -> "232 h 50 min".
    public static string FormatarHoras(TimeSpan horas) => $"{(int)horas.TotalHours} h {horas.Minutes:00} min";

    private static void ConferirDia(RhService rh, DateTime dia, DateTime hoje)
    {
        if (rh.CanEdit && dia.Date > hoje.Date)
            throw new RhRuleException("Não é possível lançar falta ou atraso em uma data futura.");
    }

    private static TimeSpan Somar(IEnumerable<Attendance> registros) =>
        registros.Aggregate(TimeSpan.Zero, (soma, item) => soma + HorasTrabalhadas(item));

    private static Dictionary<int, string> Nomes(RhService rh) => rh.Employees.ToDictionary(item => item.Id, item => item.Name);

    private static bool TryHora(string valor, out TimeOnly hora) =>
        TimeOnly.TryParseExact(valor, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out hora);
}

// FrequenciaDoFuncionario - Clóvis
// Uma linha do resumo por funcionário: quantas presenças, atrasos e faltas e quantas horas no período.
public sealed record FrequenciaDoFuncionario(int Matricula, string Nome, int Presencas, int Atrasos, int Faltas, TimeSpan Horas);

// ResumoDeFrequencia - Clóvis
// O que a janela "Frequência por período" mostra em cima da tabela.
// Comparecimento = (presenças + atrasos) / registros: quem chegou atrasado compareceu.
public sealed record ResumoDeFrequencia(
    DateTime De,
    DateTime Ate,
    int Registros,
    int Presencas,
    int Atrasos,
    int Faltas,
    TimeSpan Horas,
    IReadOnlyList<FrequenciaDoFuncionario> PorFuncionario)
{
    public double Comparecimento => Registros == 0 ? 0 : (double)(Presencas + Atrasos) / Registros;
}