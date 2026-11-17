namespace Aurora.Core;

// Registros imutáveis podem ir para a tela sem expor a coleção do repositório.
public sealed record Employee(int Id, string Name, string Role, string Team, string Status, DateTime Started);
public sealed record Attendance(int Person, DateTime Day, string In, string Out, string Status, string Reason = "")
{
    public static bool AcceptsReason(string status) => status is "Atraso" or "Falta";
}
public sealed record Vacancy(int Id, string Title, string Team, string Status);
public sealed record Candidate(int Id, string Name, int Vacancy, string Stage);
public sealed record Payroll(int Person, DateTime Month, decimal Base, decimal Bonus, decimal Discount)
{
    public decimal Total => Base + Bonus - Discount;
}
public sealed record Appointment(string Title, DateTime Time, string Category);

public static class RhOptions
{
    public static IReadOnlyList<string> Teams { get; } = Array.AsReadOnly(new[] { "Pessoas", "Tecnologia", "Financeiro", "Comercial", "Operações" });
    public static IReadOnlyList<string> EmployeeStatuses { get; } = Array.AsReadOnly(new[] { "Ativo", "Férias", "Afastado", "Inativo" });
    public static IReadOnlyList<string> AttendanceStatuses { get; } = Array.AsReadOnly(new[] { "Presença", "Atraso", "Falta" });
    public static IReadOnlyList<string> Stages { get; } = Array.AsReadOnly(new[] { "Triagem", "Entrevista", "Teste técnico", "Proposta", "Contratado" });
    public static IReadOnlyList<string> AgendaCategories { get; } = Array.AsReadOnly(new[] { "Funcionários", "Ponto", "Recrutamento", "Salários" });
}
