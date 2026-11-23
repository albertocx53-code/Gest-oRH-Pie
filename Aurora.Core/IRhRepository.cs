namespace Aurora.Core;

// Contrato dos dados do sistema: DemoRh guarda em memória e RepositorioSqlite guarda no banco. O Program.cs escolhe qual usar.
// Id zero indica cadastro novo. O repositório atribui o Id definitivo para evitar colisões.
public interface IRhRepository
{
    IReadOnlyList<Employee> Employees { get; }
    IReadOnlyList<Attendance> Punches { get; }
    IReadOnlyList<Vacancy> Vacancies { get; }
    IReadOnlyList<Candidate> Candidates { get; }
    IReadOnlyList<Payroll> Pay { get; }
    IReadOnlyList<Appointment> Agenda { get; }

    void SaveEmployee(Employee employee);
    void DeleteEmployee(int id);
    void SaveAttendance(Attendance record, bool edit);
    void SaveVacancy(Vacancy vacancy);
    void SaveCandidate(Candidate candidate);
    void SavePayroll(Payroll payroll);
    void AddAppointment(Appointment appointment);
}
