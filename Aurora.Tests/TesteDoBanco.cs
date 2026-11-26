using Aurora.Core;
using Aurora.Dados;

namespace Aurora.Tests;

// Testes do banco de dados.
// Repete os mesmos testes da lista em memória, só que agora com o banco SQLite.
// Cada vez que roda, cria um banco novo numa pasta temporária
// (não mexe no aurora.db do sistema).
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
            Console.WriteLine("Banco de dados: todos os testes passaram.");
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
        IRepositorio repositorio = new RepositorioSqlite(banco);
        Conferir(repositorio.Funcionarios.Count == 8, "8 funcionários");
        Conferir(repositorio.Funcionarios[0].Nome == "Marina Teixeira" && repositorio.Funcionarios[0].Departamento == "Pessoas", "JOIN com departamento");
        Conferir(repositorio.Pontos.Count == 30, "30 registros de ponto");
        Conferir(repositorio.Pontos.Count(p => p.Situacao == "Falta" && p.Entrada == "" && p.Saida == "") == 4, "falta vem sem horário");
        Conferir(repositorio.Vagas.Count == 3 && repositorio.Candidatos.Count == 4, "vagas e candidatos");
        Conferir(repositorio.Folhas.Count == 12, "folha de outubro e novembro");
        Conferir(repositorio.Folhas.Single(p => p.Matricula == 101 && p.Competencia == new DateTime(2026, 11, 1)).Total == 3365m, "líquido da Marina");
        Conferir(repositorio.Agenda.Count == 3 && repositorio.Agenda[0].DataHora == Hoje.AddHours(10.5), "agenda com data e hora");
    }

    private static void TestarLogin(string banco)
    {
        var login = new Autenticacao(new ContasDoBanco(banco));
        Conferir(login.Entrar("rh", "senha errada") is null, "senha errada não entra");
        Conferir(login.Entrar("ninguem", Senha) is null, "usuário que não existe não entra");
        Conferir(login.Entrar(" ADMIN ", Senha)?.Perfil == PerfilDeAcesso.Administrador, "admin entra (maiúscula também)");
        Conferir(login.Entrar("rh", Senha)?.Perfil == PerfilDeAcesso.RecursosHumanos, "rh entra");
        Conferir(login.Entrar("gestor", Senha)?.Perfil == PerfilDeAcesso.Gestor, "gestor entra");
    }

    // Mesmos casos do teste do ServicoRh, agora com o banco
    private static void TestarRegrasComOServico(string banco)
    {
        IRepositorio repositorio = new RepositorioSqlite(banco);
        var login = new Autenticacao(new ContasDoBanco(banco));
        var rh = new ServicoRh(repositorio, login.Entrar("rh", Senha)!);
        var gestor = new ServicoRh(repositorio, login.Entrar("gestor", Senha)!);

        Recusar(() => gestor.ExcluirFuncionario(101), "gestor não pode apagar");
        Recusar(() => rh.ExcluirFuncionario(101), "funcionário com histórico não é apagado");
        Recusar(() => rh.SalvarPonto(new(101, Hoje, "08:00", "17:00", "Atraso"), false), "ponto duplicado");
        Conferir(rh.PontosEntre(Hoje.AddDays(-4), Hoje).Count == 30, "busca de ponto por período");

        // Edita o ponto: vira falta com motivo
        rh.SalvarPonto(new(101, Hoje, "08:00", "17:00", "Falta", " Avisou a equipe. "), true);
        var falta = rh.PontosEntre(Hoje, Hoje).Single(p => p.Matricula == 101);
        Conferir(falta.Entrada == "" && falta.Motivo == "Avisou a equipe.", "falta salva com motivo e sem horário");

        // Folha: salvar de novo no mesmo mês substitui, não duplica
        rh.SalvarFolha(new(101, Hoje, 5_200, 300, 50));
        Conferir(rh.FolhaDoMes(Hoje).Count(p => p.Matricula == 101) == 1, "um lançamento por mês");
        Conferir(rh.FolhaDoMes(Hoje).Single(p => p.Matricula == 101).Total == 5_450m, "valor novo da folha");
        rh.SalvarFolha(new(101, Hoje, 1234.56m, 0.1m, 0.2m));
        Conferir(rh.FolhaDoMes(Hoje).Single(p => p.Matricula == 101).Salario == 1234.56m, "centavos voltam certinho");

        // Vagas e candidatos
        rh.AbrirOuFecharVaga(1);
        Conferir(rh.Vagas.Single(v => v.Id == 1).Situacao == "Encerrada", "vaga encerrada");
        Recusar(() => rh.SalvarCandidato(new(0, "Pessoa Teste", 1, "Triagem")), "vaga encerrada não aceita candidato novo");
        rh.NovaVaga("Estagiário de dados", "Tecnologia");
        var novaVaga = rh.Vagas.Single(v => v.Titulo == "Estagiário de dados");
        Conferir(novaVaga.Id == 4 && novaVaga.Departamento == "Tecnologia", "vaga nova ganha id 4");
        rh.SalvarCandidato(new(0, "Pedro D'Ávila", novaVaga.Id, "Triagem"));
        Conferir(rh.Candidatos.Any(c => c.Nome == "Pedro D'Ávila" && c.Id == 5), "nome com apóstrofo salva normal");

        // Funcionário novo recebe a matrícula 109; depois de apagado, ela não é reutilizada
        rh.SalvarFuncionario(new(0, "Pessoa Teste", "Analista", "Pessoas", "Ativo", Hoje));
        var criado = rh.Funcionarios.Single(e => e.Nome == "Pessoa Teste");
        Conferir(criado.Id == 109, "matrícula 109");
        rh.SalvarFuncionario(criado with { Departamento = "Financeiro", Situacao = "Férias" });
        Conferir(rh.Funcionarios.Single(e => e.Id == 109).Departamento == "Financeiro", "edição troca o departamento");
        rh.ExcluirFuncionario(criado.Id);
        rh.SalvarFuncionario(new(0, "Outra Pessoa", "Analista", "Pessoas", "Ativo", Hoje));
        Conferir(rh.Funcionarios.Single(e => e.Nome == "Outra Pessoa").Id == 110, "matrícula apagada não é reaproveitada");

        rh.AdicionarCompromisso("Reunião com a equipe", Hoje, "09:15", "Funcionários");
        Conferir(rh.Agenda.Any(a => a.Titulo == "Reunião com a equipe" && a.DataHora == Hoje.AddHours(9.25)), "compromisso novo na agenda");
    }

    // Chama o repositório direto, sem o ServicoRh,
    // para confirmar que o próprio banco também bloqueia dados inválidos.
    private static void TestarChavesDoBanco(string banco)
    {
        IRepositorio repositorio = new RepositorioSqlite(banco);
        Recusar(() => repositorio.ExcluirFuncionario(102), "chave estrangeira segura o histórico");
        Recusar(() => repositorio.SalvarPonto(new(102, Hoje, "09:00", "18:00", "Presença"), false), "chave primária do ponto");
        Recusar(() => repositorio.SalvarPonto(new(999, Hoje, "09:00", "18:00", "Presença"), false), "ponto de matrícula que não existe");
        Recusar(() => repositorio.SalvarCandidato(new(0, "Fulano", 99, "Triagem")), "candidato em vaga que não existe");
        Recusar(() => repositorio.SalvarFuncionario(new(0, "Fulano", "Cargo", "Setor que não existe", "Ativo", Hoje)), "departamento inválido");
        Recusar(() => repositorio.SalvarFuncionario(new(0, "Fulano", "Cargo", "Pessoas", "Demitido", Hoje)), "CHECK da situação");
    }

    // Abre o banco de novo e confere se os dados continuam salvos
    private static void TestarSeOsDadosFicamSalvos(string banco, string scripts)
    {
        CriadorDoBanco.CriarSeNaoExistir(banco, scripts); // não pode recriar por cima
        IRepositorio repositorioNovo = new RepositorioSqlite(banco);
        Conferir(repositorioNovo.Funcionarios.Any(e => e.Nome == "Outra Pessoa"), "funcionário continua salvo");
        Conferir(repositorioNovo.Candidatos.Any(c => c.Nome == "Pedro D'Ávila"), "candidato continua salvo");
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
        catch (ErroDeRegra)
        {
            return; // o erro era esperado
        }
        throw new InvalidOperationException("Era para ter dado erro: " + oQue);
    }
}
