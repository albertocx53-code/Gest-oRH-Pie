using Aurora.Core;
using Aurora.Dados;
using Aurora.WinForms;

namespace Aurora.Tests;

// TesteDoPonto - Clóvis (ponto e frequência)
//
// Testes da minha parte: consulta por período, resumo, lançamento de falta e de atraso.
// Cada cenário pede um repositório novo à função "criar", então um cenário nunca atrapalha o outro.
// Os mesmos cenários rodam duas vezes: com a lista em memória (DemoRh) e com o banco SQLite,
// cada cenário num banco temporário novo, que não mexe no aurora.db do sistema.
// A parte da tela (botões e janela do período) está no TestProgram.cs.
// Se algum teste falhar, o método lança uma exceção com o nome do teste.
internal static class TesteDoPonto
{
    // "Hoje" da demonstração, igual ao DemoRh.Today.
    private static readonly DateTime Hoje = new(2026, 11, 13);
    private static readonly DateTime Segunda = new(2026, 11, 9);
    private const string Senha = "Aurora.demo26";

    public static void Rodar()
    {
        var total = Executar("lista em memória", () => new DemoRh());
        total += TestarComOBanco();
        Console.WriteLine($"Ponto e frequência (Clóvis): {total} testes passaram.");
    }

    // Cada chamada da função cria um banco novo numa pasta temporária. As pastas são apagadas no fim.
    private static int TestarComOBanco()
    {
        var scripts = Path.Combine(AppContext.BaseDirectory, "BancoDeDados");
        var pastas = new List<string>();
        string NovoBanco()
        {
            var pasta = Path.Combine(Path.GetTempPath(), "aurora-clovis-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(pasta);
            pastas.Add(pasta);
            var banco = Path.Combine(pasta, "aurora.db");
            CriadorDoBanco.CriarSeNaoExistir(banco, scripts);
            return banco;
        }

        try
        {
            var total = Executar("banco SQLite", () => new RepositorioSqlite(NovoBanco()));

            // Abrindo o banco de novo, a falta e o atraso lançados continuam gravados
            var caminho = NovoBanco();
            var rh = Entrar(new RepositorioSqlite(caminho), "rh");
            rh.LancarFalta(101, Segunda.AddDays(-1), "Consulta médica", Hoje);
            rh.LancarAtraso(102, Segunda.AddDays(-1), "08:20", "17:00", "", Hoje);
            var reaberto = Entrar(new RepositorioSqlite(caminho), "rh");
            var dia = reaberto.ConsultarPeriodo(Segunda.AddDays(-1), Segunda.AddDays(-1));
            Verdade(dia.Count == 2 && dia.Any(item => item.Person == 101 && item.Status == "Falta" && item.In == "")
                && dia.Any(item => item.Person == 102 && item.Status == "Atraso" && item.In == "08:20"),
                "[Ponto · banco SQLite] falta e atraso continuam salvos depois de reabrir o banco");
            return total + 1;
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            foreach (var pasta in pastas)
                try { Directory.Delete(pasta, true); } catch { /* pasta temporária: se não der para apagar, ignora */ }
        }
    }

    private static int Executar(string origem, Func<IRhRepository> criar)
    {
        var testes = 0;
        void Teste(string nome, Action corpo)
        {
            try { corpo(); }
            catch (Exception erro) { throw new InvalidOperationException($"[Ponto · {origem}] {nome}: {erro.Message}", erro); }
            testes++;
        }

        Teste("a semana inteira tem 30 registros, 5 atrasos, 4 faltas e 21 presenças", () =>
        {
            var rh = Entrar(criar(), "rh");
            var resumo = rh.ResumoDoPeriodo(Segunda, Hoje);
            Igual(30, resumo.Registros); Igual(21, resumo.Presencas); Igual(5, resumo.Atrasos); Igual(4, resumo.Faltas);
            Igual(rh.ConsultarPeriodo(Segunda, Hoje).Count, resumo.Registros);
        });

        Teste("horas do período: 21 presenças de 9 h e 5 atrasos de 8 h 46 min", () =>
        {
            var resumo = Entrar(criar(), "rh").ResumoDoPeriodo(Segunda, Hoje);
            Igual("232 h 50 min", FrequenciaDePonto.FormatarHoras(resumo.Horas));
            Igual(26.0 / 30.0, resumo.Comparecimento);
        });

        Teste("resumo por funcionário: Marina só com atrasos e Otávio com 3 faltas", () =>
        {
            var resumo = Entrar(criar(), "rh").ResumoDoPeriodo(Segunda, Hoje);
            Igual(6, resumo.PorFuncionario.Count);
            var marina = resumo.PorFuncionario.Single(item => item.Matricula == 101);
            Igual(0, marina.Presencas); Igual(5, marina.Atrasos); Igual(0, marina.Faltas);
            Igual("43 h 50 min", FrequenciaDePonto.FormatarHoras(marina.Horas));
            var otavio = resumo.PorFuncionario.Single(item => item.Matricula == 108);
            Igual(2, otavio.Presencas); Igual(0, otavio.Atrasos); Igual(3, otavio.Faltas);
            Igual("Beatriz Lemos", resumo.PorFuncionario[0].Nome);
        });

        Teste("filtros por funcionário e por ocorrência", () =>
        {
            var rh = Entrar(criar(), "rh");
            Igual(5, rh.ConsultarPeriodo(Segunda, Hoje, 108).Count);
            Igual(4, rh.ConsultarPeriodo(Segunda, Hoje, null, "Falta").Count);
            Igual(3, rh.ConsultarPeriodo(Segunda, Hoje, 108, "Falta").Count);
            Igual(0, rh.ConsultarPeriodo(Segunda, Hoje, 104).Count);
            var resumo = rh.ResumoDoPeriodo(Segunda, Hoje, null, "Falta");
            Igual(4, resumo.Registros); Igual(0, resumo.Presencas); Igual(TimeSpan.Zero, resumo.Horas);
        });

        Teste("registros em ordem de data e, no mesmo dia, de nome", () =>
        {
            var lista = Entrar(criar(), "rh").ConsultarPeriodo(Segunda, Hoje);
            for (var i = 1; i < lista.Count; i++) Verdade(lista[i - 1].Day <= lista[i].Day, "datas fora de ordem");
            var dia = lista.Where(item => item.Day == Hoje).ToArray();
            Igual(6, dia.Length);
            Igual(105, dia[0].Person); // Beatriz Lemos vem primeiro em ordem alfabética
        });

        Teste("um dia só devolve só aquele dia", () =>
        {
            var rh = Entrar(criar(), "rh");
            Igual(6, rh.ConsultarPeriodo(Hoje, Hoje).Count);
            Igual(0, rh.ConsultarPeriodo(Hoje.AddDays(-30), Hoje.AddDays(-20)).Count);
            Igual(0, rh.ResumoDoPeriodo(Hoje.AddDays(-30), Hoje.AddDays(-20)).Registros);
        });

        Teste("bloqueios da consulta: datas invertidas, período longo, matrícula e ocorrência inválidas", () =>
        {
            var rh = Entrar(criar(), "rh");
            Recusa(() => rh.ConsultarPeriodo(Hoje, Segunda), "posterior");
            Recusa(() => rh.ConsultarPeriodo(Hoje.AddDays(-366), Hoje), "366 dias");
            Igual(0, rh.ConsultarPeriodo(Hoje.AddDays(-365), Hoje.AddDays(-300)).Count);
            Recusa(() => rh.ConsultarPeriodo(Segunda, Hoje, 999), "não encontrado");
            Recusa(() => rh.ConsultarPeriodo(Segunda, Hoje, null, "Folga"), "ocorrência válida");
        });

        Teste("lançar falta: grava sem horário e entra no resumo", () =>
        {
            var rh = Entrar(criar(), "rh");
            var domingo = Segunda.AddDays(-1);
            rh.LancarFalta(101, domingo, "  Consulta médica comunicada  ", Hoje);
            var registro = rh.ConsultarPeriodo(domingo, domingo).Single();
            Igual("Falta", registro.Status); Igual("", registro.In); Igual("", registro.Out);
            Igual("Consulta médica comunicada", registro.Reason);
            var resumo = rh.ResumoDoPeriodo(domingo, domingo);
            Igual(1, resumo.Faltas); Igual(0.0, resumo.Comparecimento);
            Igual(31, rh.ResumoDoPeriodo(domingo, Hoje).Registros);
        });

        Teste("lançar falta duas vezes no mesmo dia é recusado", () =>
        {
            var rh = Entrar(criar(), "rh");
            rh.LancarFalta(102, Segunda.AddDays(-2), "", Hoje);
            Recusa(() => rh.LancarFalta(102, Segunda.AddDays(-2), "", Hoje), "Já existe ponto");
            Recusa(() => rh.LancarFalta(101, Hoje, "", Hoje), "Já existe ponto");
        });

        Teste("lançar atraso: grava os horários e conta como comparecimento", () =>
        {
            var rh = Entrar(criar(), "rh");
            var dia = Segunda.AddDays(-3);
            rh.LancarAtraso(102, dia, "08:20", "17:00", "Trânsito", Hoje);
            var registro = rh.ConsultarPeriodo(dia, dia).Single();
            Igual("Atraso", registro.Status); Igual("08:20", registro.In); Igual("17:00", registro.Out);
            var resumo = rh.ResumoDoPeriodo(dia, dia);
            Igual(1, resumo.Atrasos); Igual(1.0, resumo.Comparecimento);
            Igual("8 h 40 min", FrequenciaDePonto.FormatarHoras(resumo.Horas));
        });

        Teste("atraso exige entrada depois das 08:00 e saída depois da entrada", () =>
        {
            var rh = Entrar(criar(), "rh");
            var dia = Segunda.AddDays(-3);
            Recusa(() => rh.LancarAtraso(102, dia, "08:00", "17:00", "", Hoje), "depois das 08:00");
            Recusa(() => rh.LancarAtraso(102, dia, "07:45", "17:00", "", Hoje), "depois das 08:00");
            Recusa(() => rh.LancarAtraso(102, dia, "08:20", "08:10", "", Hoje), "horários válidos");
            Recusa(() => rh.LancarAtraso(102, dia, "8h20", "17:00", "", Hoje), "horários válidos");
            Igual(0, rh.ConsultarPeriodo(dia, dia).Count);
        });

        Teste("não se lança falta nem atraso no futuro nem para quem não está ativo", () =>
        {
            var rh = Entrar(criar(), "rh");
            Recusa(() => rh.LancarFalta(101, Hoje.AddDays(1), "", Hoje), "data futura");
            Recusa(() => rh.LancarAtraso(101, Hoje.AddDays(1), "08:20", "17:00", "", Hoje), "data futura");
            Recusa(() => rh.LancarFalta(104, Segunda.AddDays(-1), "", Hoje), "funcionário ativo");
            Recusa(() => rh.LancarFalta(999, Segunda.AddDays(-1), "", Hoje), "funcionário ativo");
            Recusa(() => rh.LancarFalta(101, Segunda.AddDays(-1), new string('x', 241), Hoje), "240 caracteres");
        });

        Teste("quem ainda não tem ponto no dia", () =>
        {
            var rh = Entrar(criar(), "rh");
            Igual(0, rh.SemRegistro(Hoje).Count);
            var domingo = Segunda.AddDays(-1);
            Igual(6, rh.SemRegistro(domingo).Count);
            Verdade(rh.SemRegistro(domingo).All(item => item.Status == "Ativo"), "só funcionários ativos");
            rh.LancarFalta(101, domingo, "", Hoje);
            Igual(5, rh.SemRegistro(domingo).Count);
            Verdade(rh.SemRegistro(domingo).All(item => item.Id != 101), "101 já tem registro");
        });

        Teste("Gestor consulta, mas não lança", () =>
        {
            var rh = Entrar(criar(), "gestor");
            Igual(30, rh.ResumoDoPeriodo(Segunda, Hoje).Registros);
            Recusa(() => rh.LancarFalta(101, Segunda.AddDays(-1), "", Hoje), "sem editar");
            Recusa(() => rh.LancarAtraso(101, Segunda.AddDays(-1), "08:20", "17:00", "", Hoje), "sem editar");
            Recusa(() => rh.LancarAtraso(101, Segunda.AddDays(-1), "07:00", "17:00", "", Hoje.AddDays(-30)), "sem editar");
            Igual(0, rh.ConsultarPeriodo(Segunda.AddDays(-1), Segunda.AddDays(-1)).Count);
        });

        Teste("sessão encerrada bloqueia consulta e lançamento", () =>
        {
            var repositorio = criar();
            var sessao = new Authentication(new DemoAccounts()).SignIn("rh", Senha)!;
            var rh = new RhService(repositorio, sessao);
            sessao.SignOut();
            Recusa(() => rh.ConsultarPeriodo(Segunda, Hoje), "Entre novamente");
            Recusa(() => rh.ResumoDoPeriodo(Segunda, Hoje), "Entre novamente");
            Recusa(() => rh.LancarFalta(101, Segunda.AddDays(-1), "", Hoje), "Entre novamente");
        });

        return testes;
    }

    private static RhService Entrar(IRhRepository repositorio, string usuario) =>
        new(repositorio, new Authentication(new DemoAccounts()).SignIn(usuario, Senha) ?? throw new InvalidOperationException("login falhou"));

    private static void Igual<T>(T esperado, T atual)
    {
        if (!EqualityComparer<T>.Default.Equals(esperado, atual))
            throw new InvalidOperationException($"esperado {esperado}, veio {atual}");
    }

    private static void Verdade(bool condicao, string mensagem)
    {
        if (!condicao) throw new InvalidOperationException(mensagem);
    }

    // A operação precisa lançar RhRuleException, e a mensagem precisa conter o trecho esperado.
    private static void Recusa(Action operacao, string trecho)
    {
        try { operacao(); }
        catch (RhRuleException erro)
        {
            if (!erro.Message.Contains(trecho, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"mensagem inesperada: \"{erro.Message}\" (esperava conter \"{trecho}\")");
            return;
        }
        throw new InvalidOperationException($"deveria ter sido recusado (\"{trecho}\")");
    }
}