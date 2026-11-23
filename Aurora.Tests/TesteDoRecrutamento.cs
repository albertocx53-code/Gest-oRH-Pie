using Aurora.Core;
using Aurora.Dados;
using Aurora.WinForms;

namespace Aurora.Tests;

// TesteDoRecrutamento - testes da parte do Kerlon (Recrutamento.cs)
//
// Confere o funil de etapas, os candidatos da vaga, o cadastro sem nome repetido,
// o Avançar etapa, o Mudar de vaga e as permissões (o Gestor só consulta).
// Os mesmos casos rodam com a lista em memória (DemoRh) e com o banco SQLite,
// num banco temporário que não mexe no aurora.db do sistema.
internal static class TesteDoRecrutamento
{
    private const string Senha = "Aurora.demo26";

    public static void Rodar()
    {
        Testar(new DemoRh(), new Authentication(new DemoAccounts()), "lista em memória");
        TestarComOBanco();
        Console.WriteLine("Recrutamento (Kerlon): todos os testes passaram.");
    }

    private static void Testar(IRhRepository repositorio, Authentication login, string onde)
    {
        var rh = new RhService(repositorio, login.SignIn("rh", Senha)!);
        var gestor = new RhService(repositorio, login.SignIn("gestor", Senha)!);

        // Consultas: as vagas abertas vêm primeiro e o funil tem todas as etapas, mesmo as vazias
        var vagas = rh.ConsultarVagas();
        Conferir(vagas.Count == rh.Vacancies.Count, onde, "consulta traz todas as vagas");
        Conferir(rh.FunilDeEtapas().Count == RhOptions.Stages.Count, onde, "funil com todas as etapas");
        Conferir(rh.FunilDeEtapas().Sum(item => item.Candidatos) == rh.Candidates.Count, onde, "funil soma todos os candidatos");
        Conferir(rh.ResumoDasVagas().Count == vagas.Count, onde, "resumo com uma linha por vaga");
        Recusar(() => rh.ConsultarVagas("Fechada"), onde, "situação de vaga que não existe");

        var abertas = vagas.Where(item => item.Status == "Aberta").ToArray();
        Conferir(abertas.Length >= 2, onde, "dados de exemplo com duas vagas abertas");
        var primeira = abertas[0];
        var segunda = abertas[1];

        // Cadastro: começa na Triagem e o mesmo nome não entra duas vezes na mesma vaga
        rh.CadastrarCandidato("Candidata Teste", primeira.Id);
        var nova = rh.Candidates.Single(item => item.Name == "Candidata Teste");
        Conferir(nova.Stage == RhOptions.Stages[0], onde, "candidatura nova começa na primeira etapa");
        Conferir(rh.CandidatosDaVaga(primeira.Id).Any(item => item.Id == nova.Id), onde, "aparece nos candidatos da vaga");
        Recusar(() => rh.CadastrarCandidato("candidata teste", primeira.Id), onde, "nome repetido na mesma vaga");

        // Avançar etapa e Mudar de vaga (a etapa continua a mesma)
        rh.AvancarEtapa(nova.Id);
        Conferir(Atual(rh, nova.Id).Stage == RhOptions.Stages[1], onde, "avançou uma etapa");
        rh.VincularAVaga(nova.Id, segunda.Id);
        Conferir(Atual(rh, nova.Id).Vacancy == segunda.Id && Atual(rh, nova.Id).Stage == RhOptions.Stages[1], onde, "mudou de vaga mantendo a etapa");
        Recusar(() => rh.VincularAVaga(nova.Id, segunda.Id), onde, "mudar para a vaga em que já está");

        // O Gestor consulta, mas não altera
        Conferir(gestor.CandidatosDaVaga(segunda.Id).Any(item => item.Id == nova.Id), onde, "gestor consulta os candidatos da vaga");
        Recusar(() => gestor.AvancarEtapa(nova.Id), onde, "gestor não avança etapa");
        Recusar(() => gestor.CadastrarCandidato("Outra Pessoa", primeira.Id), onde, "gestor não cadastra candidato");

        // Na última etapa (Contratado) não tem como avançar
        while (Atual(rh, nova.Id).Stage != RhOptions.Stages[^1]) rh.AvancarEtapa(nova.Id);
        Recusar(() => rh.AvancarEtapa(nova.Id), onde, "avançar depois da última etapa");
    }

    // Mesmos testes com o banco SQLite. Cada execução cria um banco novo numa pasta temporária.
    private static void TestarComOBanco()
    {
        var pasta = Path.Combine(Path.GetTempPath(), "aurora-recrutamento-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(pasta);
        var banco = Path.Combine(pasta, "aurora.db");

        try
        {
            CriadorDoBanco.CriarSeNaoExistir(banco, Path.Combine(AppContext.BaseDirectory, "BancoDeDados"));
            Testar(new RepositorioSqlite(banco), new Authentication(new ContasDoBanco(banco)), "banco SQLite");

            // Abrindo o banco de novo, a candidatura continua gravada como Contratado
            IRhRepository reaberto = new RepositorioSqlite(banco);
            Conferir(reaberto.Candidates.Single(item => item.Name == "Candidata Teste").Stage == RhOptions.Stages[^1], "banco SQLite", "etapa continua salva");
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { Directory.Delete(pasta, true); } catch { /* pasta temporária: se não der para apagar, ignora */ }
        }
    }

    private static Candidate Atual(RhService rh, int id) => rh.Candidates.Single(item => item.Id == id);

    private static void Conferir(bool ok, string onde, string oQue)
    {
        if (!ok) throw new InvalidOperationException($"Falhou no teste do recrutamento ({onde}): {oQue}");
    }

    private static void Recusar(Action acao, string onde, string oQue)
    {
        try
        {
            acao();
        }
        catch (RhRuleException)
        {
            return; // o erro era esperado
        }
        throw new InvalidOperationException($"Era para ter dado erro ({onde}): {oQue}");
    }
}
