using Aurora.Core;
using Aurora.Dados;
using Aurora.WinForms;

namespace Aurora.Tests;

// TesteSalarios - Diaz (salários e testes de integração)
//
// Roda os mesmos casos de folha de pagamento duas vezes:
//   1) com a lista em memória (DadosDeExemplo), que é a referência de comportamento;
//   2) com o banco SQLite (RepositorioSqlite), para provar que o banco se comporta igual.
// No final fecha e reabre o banco para conferir que os lançamentos continuam salvos.
internal static class TesteSalarios
{
    private const string Senha = "Aurora.demo26";
    private static readonly DateTime Novembro = new(2026, 11, 1);
    private static readonly DateTime Setembro = new(2026, 9, 1);

    public static void Rodar()
    {
        Testar(new DadosDeExemplo(), "memória");

        var pasta = Path.Combine(Path.GetTempPath(), "aurora-salarios-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(pasta);
        var banco = Path.Combine(pasta, "aurora.db");
        var scripts = Path.Combine(AppContext.BaseDirectory, "BancoDeDados");
        try
        {
            CriadorDoBanco.CriarSeNaoExistir(banco, scripts);
            Testar(new RepositorioSqlite(banco), "banco SQLite");

            // Abre o banco de novo, como se o sistema tivesse sido fechado e aberto outra vez.
            var rh = NovoServico(new RepositorioSqlite(banco), "rh");
            var salvo = rh.FolhaDoMes(Setembro).SingleOrDefault(item => item.Matricula == 101);
            Conferir(salvo is not null && salvo.Total == 4_000m, "banco SQLite", "lançamento continua salvo depois de reabrir");
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { Directory.Delete(pasta, true); } catch { /* pasta temporária: se não apagar, ignora */ }
        }
        Console.WriteLine("Salários (Diaz): todos os testes passaram.");
    }

    private static void Testar(IRepositorio repositorio, string origem)
    {
        var rh = NovoServico(repositorio, "rh");
        var gestor = NovoServico(repositorio, "gestor");

        // Dados fictícios de novembro: Marina (101) = 3310 + 180 - 125
        Conferir(rh.FolhaDoMes(Novembro).Single(item => item.Matricula == 101).Total == 3_365m, origem, "líquido da folha de novembro");

        // Novo lançamento: a competência é normalizada para o dia 1 e o líquido é base + bônus - desconto
        rh.SalvarFolha(new(101, new DateTime(2026, 9, 20), 4_000m, 500m, 300m));
        var criado = rh.FolhaDoMes(new DateTime(2026, 9, 5)).Single(item => item.Matricula == 101);
        Conferir(criado.Competencia == Setembro, origem, "competência salva como dia 1");
        Conferir(criado.Total == 4_200m, origem, "líquido = base + bônus - desconto");
        Conferir(rh.FolhaDoMes(new DateTime(2026, 8, 1)).All(item => item.Matricula != 101), origem, "outra competência não mostra o lançamento");

        // Salvar de novo na mesma competência atualiza, não duplica (é o que a edição faz)
        var listaAntiga = rh.FolhaDoMes(Setembro);
        rh.SalvarFolha(criado with { Bonus = 0m, Desconto = 0m });
        Conferir(rh.FolhaDoMes(Setembro).Count(item => item.Matricula == 101) == 1, origem, "um lançamento por funcionário e competência");
        Conferir(rh.FolhaDoMes(Setembro).Single(item => item.Matricula == 101).Total == 4_000m, origem, "edição atualiza o valor");
        Conferir(listaAntiga.Single(item => item.Matricula == 101).Total == 4_200m, origem, "lista entregue antes da gravação não muda");

        // Centavos voltam certinhos
        rh.SalvarFolha(new(101, Setembro, 1_234.56m, 0.10m, 0.20m));
        var centavos = rh.FolhaDoMes(Setembro).Single(item => item.Matricula == 101);
        Conferir(centavos.Salario == 1_234.56m && centavos.Bonus == 0.10m && centavos.Desconto == 0.20m, origem, "centavos preservados");
        rh.SalvarFolha(new(101, Setembro, 4_000m, 0m, 0m)); // deixa o valor que o teste final espera

        // Valores inválidos são recusados e nada é gravado
        Recusar(() => rh.SalvarFolha(new(101, Setembro, -1m, 0m, 0m)), origem, "salário negativo");
        Recusar(() => rh.SalvarFolha(new(101, Setembro, 1_000m, -1m, 0m)), origem, "bônus negativo");
        Recusar(() => rh.SalvarFolha(new(101, Setembro, 1_000m, 0m, -1m)), origem, "desconto negativo");
        Recusar(() => rh.SalvarFolha(new(101, Setembro, 1_000_000.01m, 0m, 0m)), origem, "acima do limite");
        Recusar(() => rh.SalvarFolha(new(101, Setembro, 10.005m, 0m, 0m)), origem, "três casas decimais");
        Recusar(() => rh.SalvarFolha(new(101, Setembro, 100m, 50m, 151m)), origem, "desconto maior que salário + bônus");
        Recusar(() => rh.SalvarFolha(new(999, Setembro, 1_000m, 0m, 0m)), origem, "funcionário inexistente");
        Recusar(() => rh.SalvarFolha(new(101, new DateTime(2101, 1, 1), 1_000m, 0m, 0m)), origem, "data fora do intervalo");
        Conferir(rh.FolhaDoMes(Setembro).Single(item => item.Matricula == 101).Total == 4_000m, origem, "recusas não alteram o lançamento");

        // Desconto exatamente igual a salário + bônus é permitido (líquido zero)
        rh.SalvarFolha(new(102, Setembro, 100m, 50m, 150m));
        Conferir(rh.FolhaDoMes(Setembro).Single(item => item.Matricula == 102).Total == 0m, origem, "líquido zero é permitido");

        // O gestor não consulta nem grava salários
        Recusar(() => _ = gestor.FolhaDoMes(Novembro), origem, "gestor não consulta folha");
        Recusar(() => gestor.SalvarFolha(new(101, Setembro, 1m, 0m, 0m)), origem, "gestor não grava folha");

        // Quem tem folha não pode ser excluído (o histórico fica)
        Recusar(() => rh.ExcluirFuncionario(101), origem, "funcionário com folha não é excluído");

        // Sessão encerrada não lê nem grava
        var sessao = NovoSessao("admin");
        var admin = new ServicoRh(repositorio, sessao);
        Conferir(admin.FolhaDoMes(Novembro).Count > 0, origem, "administrador consulta folha");
        sessao.Sair();
        Recusar(() => _ = admin.FolhaDoMes(Novembro), origem, "sessão encerrada não consulta");
        Recusar(() => admin.SalvarFolha(new(101, Setembro, 1m, 0m, 0m)), origem, "sessão encerrada não grava");
    }

    private static Sessao NovoSessao(string usuario) => new Autenticacao(new ContasDeExemplo()).Entrar(usuario, Senha)!;
    private static ServicoRh NovoServico(IRepositorio repositorio, string usuario) => new(repositorio, NovoSessao(usuario));

    private static void Conferir(bool ok, string origem, string oQue)
    {
        if (!ok) throw new InvalidOperationException($"Falhou no teste de salários ({origem}): {oQue}");
    }

    private static void Recusar(Action acao, string origem, string oQue)
    {
        try { acao(); }
        catch (ErroDeRegra) { return; } // o erro era esperado
        throw new InvalidOperationException($"Falhou no teste de salários ({origem}): deveria recusar - {oQue}");
    }
}
