using Aurora.Core;
using Aurora.Dados;

namespace Aurora.Tests;

// TesteDoBanco - Alberto (parte do banco de dados)
//
// Testes da camada de dados. Repete as regras testadas com a lista em memória,
// agora usando o banco SQLite.
//
// Cada execução cria um banco novo em uma pasta temporária,
// sem mexer no aurora.db do sistema.
internal static class TesteDoBanco
{
    private const string Senha = "Aurora.demo26";
    private static readonly DateTime Hoje = new(2026, 11, 13);

    public static void Rodar()
    {
        var pasta = Path.Combine(Path.GetTempPath(), "aurora-teste-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(pasta);
        var banco = Path.Combine(pasta, "aurora.db");
        var scripts = Path.Combine(AppContext.BaseDirectory, "BancoDeDados");

        try
        {
            CriadorDoBanco.CriarSeNaoExistir(banco, scripts);
            TestarDadosFicticios(banco);
            TestarLogin(banco);
            TestarRegrasComOServico(banco);
            TestarChavesDoBanco(banco);
            TestarSeOsDadosFicamSalvos(banco, scripts);
            Console.WriteLine("Banco de dados (Alberto): todos os testes passaram.");
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { Directory.Delete(pasta, true); } catch { /* se não conseguir apagar a pasta temporária, ignora */ }
        }
    }

    // Confere se o script 02 inseriu todos os dados
    private static void TestarDadosFicticios(string banco)
    {
        IRhRepository repo = new RepositorioSqlite(banco);
        Conferir(repo.Employees.Count == 8, "8 funcionários");
        Conferir(repo.Employees[0].Name == "Marina Teixeira" && repo.Employees[0].Team == "Pessoas", "JOIN com departamento");
        Conferir(repo.Punches.Count == 30, "30 registros de ponto");
        Conferir(repo.Punches.Count(p => p.Status == "Falta" && p.In == "" && p.Out == "") == 4, "falta vem sem horário");
        Conferir(repo.Vacancies.Count == 3 && repo.Candidates.Count == 4, "vagas e candidatos");
        Conferir(repo.Pay.Count == 12, "folha de outubro e novembro");
        Conferir(repo.Pay.Single(p => p.Person == 101 && p.Month == new DateTime(2026, 11, 1)).Total == 3365m, "líquido da Marina");
        Conferir(repo.Agenda.Count == 3 && repo.Agenda[0].Time == Hoje.AddHours(10.5), "agenda com data e hora");
    }

    private static void TestarLogin(string banco)
    {
        var login = new Authentication(new ContasDoBanco(banco));
        Conferir(login.SignIn("rh", "senha errada") is null, "senha errada não entra");
        Conferir(login.SignIn("ninguem", Senha) is null, "usuário que não existe não entra");
        Conferir(login.SignIn(" ADMIN ", Senha)?.Role == RhRole.Administrator, "admin entra (maiúscula também)");
        Conferir(login.SignIn("rh", Senha)?.Role == RhRole.HumanResources, "rh entra");
        Conferir(login.SignIn("gestor", Senha)?.Role == RhRole.Manager, "gestor entra");
    }

    // Mesmos casos do teste do RhService, agora com o banco
    private static void TestarRegrasComOServico(string banco)
    {
        IRhRepository repo = new RepositorioSqlite(banco);
        var login = new Authentication(new ContasDoBanco(banco));
        var rh = new RhService(repo, login.SignIn("rh", Senha)!);
        var gestor = new RhService(repo, login.SignIn("gestor", Senha)!);

        Recusar(() => gestor.DeleteEmployee(101), "gestor não pode apagar");
        Recusar(() => rh.DeleteEmployee(101), "funcionário com histórico não é apagado");
        Recusar(() => rh.SaveAttendance(new(101, Hoje, "08:00", "17:00", "Atraso"), false), "ponto duplicado");
        Conferir(rh.AttendanceBetween(Hoje.AddDays(-4), Hoje).Count == 30, "busca de ponto por período");

        // Edita o ponto: vira falta com motivo
        rh.SaveAttendance(new(101, Hoje, "08:00", "17:00", "Falta", " Avisou a equipe. "), true);
        var falta = rh.AttendanceBetween(Hoje, Hoje).Single(p => p.Person == 101);
        Conferir(falta.In == "" && falta.Reason == "Avisou a equipe.", "falta salva com motivo e sem horário");

        // Folha: salvar de novo no mesmo mês substitui, não duplica
        rh.SavePayroll(new(101, Hoje, 5_200, 300, 50));
        Conferir(rh.PayrollFor(Hoje).Count(p => p.Person == 101) == 1, "um lançamento por mês");
        Conferir(rh.PayrollFor(Hoje).Single(p => p.Person == 101).Total == 5_450m, "valor novo da folha");
        rh.SavePayroll(new(101, Hoje, 1234.56m, 0.1m, 0.2m));
        Conferir(rh.PayrollFor(Hoje).Single(p => p.Person == 101).Base == 1234.56m, "centavos voltam certinho");

        // Vagas e candidatos
        rh.ToggleVacancy(1);
        Conferir(rh.Vacancies.Single(v => v.Id == 1).Status == "Encerrada", "vaga encerrada");
        Recusar(() => rh.SaveCandidate(new(0, "Pessoa Teste", 1, "Triagem")), "vaga encerrada não aceita candidato novo");
        rh.AddVacancy("Estagiário de dados", "Tecnologia");
        var novaVaga = rh.Vacancies.Single(v => v.Title == "Estagiário de dados");
        Conferir(novaVaga.Id == 4 && novaVaga.Team == "Tecnologia", "vaga nova ganha id 4");
        rh.SaveCandidate(new(0, "Pedro D'Ávila", novaVaga.Id, "Triagem"));
        Conferir(rh.Candidates.Any(c => c.Name == "Pedro D'Ávila" && c.Id == 5), "nome com apóstrofo salva normal");

        // Funcionário novo recebe a matrícula 109; depois de apagado, ela não é reutilizada
        rh.SaveEmployee(new(0, "Pessoa Teste", "Analista", "Pessoas", "Ativo", Hoje));
        var criado = rh.Employees.Single(e => e.Name == "Pessoa Teste");
        Conferir(criado.Id == 109, "matrícula 109");
        rh.SaveEmployee(criado with { Team = "Financeiro", Status = "Férias" });
        Conferir(rh.Employees.Single(e => e.Id == 109).Team == "Financeiro", "edição troca o departamento");
        rh.DeleteEmployee(criado.Id);
        rh.SaveEmployee(new(0, "Outra Pessoa", "Analista", "Pessoas", "Ativo", Hoje));
        Conferir(rh.Employees.Single(e => e.Name == "Outra Pessoa").Id == 110, "matrícula apagada não é reaproveitada");

        rh.AddAppointment("Reunião com a equipe", Hoje, "09:15", "Funcionários");
        Conferir(rh.Agenda.Any(a => a.Title == "Reunião com a equipe" && a.Time == Hoje.AddHours(9.25)), "compromisso novo na agenda");
    }

    // Chama o repositório direto, sem o RhService,
    // para confirmar que o próprio banco também bloqueia dados inválidos.
    private static void TestarChavesDoBanco(string banco)
    {
        IRhRepository repo = new RepositorioSqlite(banco);
        Recusar(() => repo.DeleteEmployee(102), "chave estrangeira segura o histórico");
        Recusar(() => repo.SaveAttendance(new(102, Hoje, "09:00", "18:00", "Presença"), false), "chave primária do ponto");
        Recusar(() => repo.SaveAttendance(new(999, Hoje, "09:00", "18:00", "Presença"), false), "ponto de matrícula que não existe");
        Recusar(() => repo.SaveCandidate(new(0, "Fulano", 99, "Triagem")), "candidato em vaga que não existe");
        Recusar(() => repo.SaveEmployee(new(0, "Fulano", "Cargo", "Setor que não existe", "Ativo", Hoje)), "departamento inválido");
        Recusar(() => repo.SaveEmployee(new(0, "Fulano", "Cargo", "Pessoas", "Demitido", Hoje)), "CHECK da situação");
    }

    // Abre o banco de novo e confere se os dados continuam salvos
    private static void TestarSeOsDadosFicamSalvos(string banco, string scripts)
    {
        CriadorDoBanco.CriarSeNaoExistir(banco, scripts); // não pode recriar por cima
        IRhRepository repoNovo = new RepositorioSqlite(banco);
        Conferir(repoNovo.Employees.Any(e => e.Name == "Outra Pessoa"), "funcionário continua salvo");
        Conferir(repoNovo.Candidates.Any(c => c.Name == "Pedro D'Ávila"), "candidato continua salvo");
    }

    private static void Conferir(bool ok, string oQue)
    {
        if (!ok) throw new InvalidOperationException("Falhou no teste do banco: " + oQue);
    }

    private static void Recusar(Action acao, string oQue)
    {
        try
        {
            acao();
        }
        catch (RhRuleException)
        {
            return; // o erro era esperado
        }
        throw new InvalidOperationException("Era para ter dado erro: " + oQue);
    }
}
