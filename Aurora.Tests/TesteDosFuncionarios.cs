using Aurora.Core;
using Aurora.Dados;
using Aurora.WinForms;

namespace Aurora.Tests;

// TesteDosFuncionarios - Jefferson (cadastro de funcionários)
//
// Testa o que eu acrescentei: a ficha do funcionário, as sugestões de cargo,
// a mudança de situação (botão Inativar) e o cálculo do tempo de casa.
// Os mesmos casos rodam duas vezes: com a lista em memória (DadosDeExemplo) e com o
// banco SQLite, num banco temporário que não mexe no aurora.db do sistema.
// A parte da tela (abrir a ficha pelo botão) está no ProgramaDeTestes.cs.
internal static class TesteDosFuncionarios
{
    private const string Senha = "Aurora.demo26";
    private static readonly DateTime Hoje = new(2026, 11, 13);

    public static void Rodar()
    {
        TestarTempoDeCasa();
        TestarComOServico(new DadosDeExemplo(), new Autenticacao(new ContasDeExemplo()), "lista em memória");
        TestarComOBanco();
        Console.WriteLine("Cadastro de funcionários (Jefferson): todos os testes passaram.");
    }

    private static void TestarTempoDeCasa()
    {
        Conferir(CadastroDeFuncionarios.TempoDeCasa(new(2022, 3, 14), Hoje) == "4 anos e 7 meses", "cálculo", "anos e meses");
        Conferir(CadastroDeFuncionarios.TempoDeCasa(new(2025, 11, 13), Hoje) == "1 ano", "cálculo", "um ano certinho");
        Conferir(CadastroDeFuncionarios.TempoDeCasa(new(2026, 10, 13), Hoje) == "1 mês", "cálculo", "um mês certinho");
        Conferir(CadastroDeFuncionarios.TempoDeCasa(new(2026, 10, 14), Hoje) == "Menos de 1 mês", "cálculo", "mês que ainda não fechou");
        Conferir(CadastroDeFuncionarios.TempoDeCasa(new(2026, 12, 1), Hoje) == "Admissão futura", "cálculo", "admissão no futuro");
    }

    // Os casos valem para a lista em memória e para o banco, porque os dois começam
    // com os mesmos funcionários, o mesmo ponto da semana e a mesma folha de novembro.
    private static void TestarComOServico(IRepositorio repositorio, Autenticacao login, string onde)
    {
        var sessaoDoRh = login.Entrar("rh", Senha)!;
        var rh = new ServicoRh(repositorio, sessaoDoRh);
        var gestor = new ServicoRh(repositorio, login.Entrar("gestor", Senha)!);

        // Ficha da Marina (101): atrasou nos 5 dias da semana e tem folha de novembro
        var marina = rh.ConsultarFicha(101, Hoje);
        Conferir(marina.Funcionario.Nome == "Marina Teixeira" && marina.Funcionario.Departamento == "Pessoas", onde, "ficha traz o cadastro");
        Conferir(marina.TempoDeCasa == "4 anos e 7 meses", onde, "tempo de casa na ficha");
        Conferir(marina.Presencas == 0 && marina.Atrasos == 5 && marina.Faltas == 0, onde, "ponto do mês na ficha");
        Conferir(marina.UltimoPonto is { Situacao: "Atraso" } ultimoDaMarina && ultimoDaMarina.Dia == Hoje, onde, "último registro de ponto");
        Conferir(marina.SalarioVisivel && marina.UltimaFolha?.Total == 3365m, onde, "último salário para o RH");

        // Larissa (107) faltou um dia. Otávio (108) faltou três, e o último registro dele é falta, sem horário
        var larissa = rh.ConsultarFicha(107, Hoje);
        Conferir(larissa.Presencas == 4 && larissa.Atrasos == 0 && larissa.Faltas == 1, onde, "presenças e falta da Larissa");
        var otavio = rh.ConsultarFicha(108, Hoje);
        Conferir(otavio.Presencas == 2 && otavio.Faltas == 3, onde, "faltas do Otávio");
        Conferir(otavio.UltimoPonto is { Situacao: "Falta", Entrada: "" } ultimoDoOtavio && ultimoDoOtavio.Dia == Hoje, onde, "falta é o último registro, sem horário");

        // Caio (104) está de férias: sem ponto e sem folha, a ficha abre do mesmo jeito
        var caio = rh.ConsultarFicha(104, Hoje);
        Conferir(caio.Presencas + caio.Atrasos + caio.Faltas == 0 && caio.UltimoPonto is null && caio.UltimaFolha is null, onde, "ficha sem histórico");
        Conferir(caio.TempoDeCasa == "3 anos e 10 meses", onde, "tempo de casa do Caio");

        // O gestor consulta a ficha, mas o salário nem é procurado
        var fichaDoGestor = gestor.ConsultarFicha(101, Hoje);
        Conferir(!fichaDoGestor.SalarioVisivel && fichaDoGestor.UltimaFolha is null && fichaDoGestor.Atrasos == 5, onde, "gestor vê a ficha sem salário");
        Recusar(() => _ = rh.ConsultarFicha(999, Hoje), onde, "ficha de matrícula que não existe");

        // Cargos: 8 diferentes nos dados fictícios, em ordem alfabética
        var cargos = rh.CargosCadastrados();
        Conferir(cargos.Count == 8 && cargos[0] == "Analista de RH" && cargos.Contains("Gerente comercial"), onde, "lista de cargos");
        // O mesmo cargo escrito com outras maiúsculas não vira uma sugestão nova
        rh.SalvarFuncionario(new(0, "Pessoa Cargo", "analista de rh", "Pessoas", "Ativo", Hoje));
        Conferir(rh.CargosCadastrados().Count == 8, onde, "cargo repetido não duplica a sugestão");

        // Rafael (102) tem ponto e folha: não pode ser excluído, mas pode ser inativado
        Recusar(() => rh.ExcluirFuncionario(102), onde, "exclusão bloqueada para quem tem histórico");
        rh.MudarSituacao(102, "Inativo");
        var rafael = rh.Funcionarios.Single(item => item.Id == 102);
        Conferir(rafael.Situacao == "Inativo" && rafael.Nome == "Rafael Monteiro" && rafael.Cargo == "Desenvolvedor" && rafael.Departamento == "Tecnologia", onde, "inativar muda só a situação");
        Conferir(rh.ConsultarFicha(102, Hoje).Presencas == 5, onde, "histórico de ponto continua depois de inativar");
        Recusar(() => rh.MudarSituacao(102, "Inativo"), onde, "situação repetida");
        Recusar(() => rh.MudarSituacao(103, "Demitido"), onde, "situação que não existe");
        Recusar(() => gestor.MudarSituacao(103, "Inativo"), onde, "gestor não muda situação");
        Recusar(() => rh.MudarSituacao(999, "Inativo"), onde, "matrícula que não existe");

        // Depois de sair da conta, nada é consultado
        sessaoDoRh.Sair();
        Recusar(() => _ = rh.ConsultarFicha(101, Hoje), onde, "sessão encerrada bloqueia a ficha");
    }

    // Mesmos testes com o banco SQLite. Cada execução cria um banco novo numa pasta temporária.
    private static void TestarComOBanco()
    {
        var pasta = Path.Combine(Path.GetTempPath(), "aurora-jefferson-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(pasta);
        var banco = Path.Combine(pasta, "aurora.db");

        try
        {
            CriadorDoBanco.CriarSeNaoExistir(banco, Path.Combine(AppContext.BaseDirectory, "BancoDeDados"));
            TestarComOServico(new RepositorioSqlite(banco), new Autenticacao(new ContasDoBanco(banco)), "banco SQLite");

            // Abrindo o banco de novo, a situação Inativo continua gravada
            IRepositorio reaberto = new RepositorioSqlite(banco);
            Conferir(reaberto.Funcionarios.Single(item => item.Id == 102).Situacao == "Inativo", "banco SQLite", "situação continua salva");
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { Directory.Delete(pasta, true); } catch { /* pasta temporária: se não der para apagar, ignora */ }
        }
    }

    private static void Conferir(bool ok, string onde, string oQue)
    {
        if (!ok) throw new InvalidOperationException($"Falhou no teste do cadastro de funcionários ({onde}): {oQue}");
    }

    private static void Recusar(Action acao, string onde, string oQue)
    {
        try
        {
            acao();
        }
        catch (ErroDeRegra)
        {
            return; // o erro era esperado
        }
        throw new InvalidOperationException($"Era para ter dado erro ({onde}): {oQue}");
    }
}
