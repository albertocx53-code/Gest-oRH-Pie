using System.Globalization;
using Aurora.Core;
using Aurora.WinForms;
using Timer = System.Windows.Forms.Timer;

namespace Aurora.Tests;

internal static class ProgramaDeTestes
{
    private const string SenhaDeExemplo = "Aurora.demo26";
    private static readonly DateTime Dia = new(2026, 11, 13);

    [STAThread]
    private static int Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
        CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
        Application.EnableVisualStyles();
        Application.SetColorMode(SystemColorMode.Classic);
        try
        {
            TestarServicos();
            TesteDoBanco.Rodar(); // testes do banco SQLite
            TesteDoPonto.Rodar(); // testes do ponto e frequência (Clóvis)
            TesteSalarios.Rodar(); // folha de pagamento em memória e no banco (Diaz)
            TesteDosFuncionarios.Rodar(); // testes do cadastro de funcionários (Jefferson)
            TesteDoRecrutamento.Rodar(); // testes do recrutamento (Kerlon)
            TestarTelas();
            Console.WriteLine("Serviços e fluxos do Windows Forms aprovados.");
            return 0;
        }
        catch (Exception falha)
        {
            Console.Error.WriteLine(falha);
            return 1;
        }
    }

    private static void TestarServicos()
    {
        var autenticacao = new Autenticacao(new ContasDeExemplo());
        var armazenamento = new DadosDeExemplo();
        Verificar(autenticacao.Entrar("rh", "senha incorreta") is null && autenticacao.Entrar("inexistente", SenhaDeExemplo) is null, "credenciais inválidas");
        var administrador = autenticacao.Entrar(" ADMIN ", SenhaDeExemplo)!;
        Verificar(administrador.Perfil == PerfilDeAcesso.Administrador, "perfil de administrador vindo da conta");
        var sessaoDoRh = autenticacao.Entrar("rh", SenhaDeExemplo)!;
        var rh = new ServicoRh(armazenamento, sessaoDoRh);
        var gestor = new ServicoRh(armazenamento, autenticacao.Entrar("gestor", SenhaDeExemplo)!);
        Verificar(!gestor.PodeEditar && !gestor.PodeVerSalarios, "permissões do gestor");
        Verificar(gestor.Funcionarios.Count == 8 && gestor.Agenda.All(item => item.Categoria != "Salários"), "consulta sem compromissos salariais");
        DeveRecusar(() => _ = gestor.FolhaDoMes(Dia), "consulta salarial bloqueada");
        DeveRecusar(() => gestor.SalvarFolha(new(101, Dia, 1, 0, 0)), "gravação salarial bloqueada");
        DeveRecusar(() => gestor.ExcluirFuncionario(101), "alteração bloqueada para gestor");
        DeveRecusar(() => rh.ExcluirFuncionario(101), "histórico impede exclusão");
        DeveRecusar(() => rh.SalvarFuncionario(new(0, " ", "Analista", "Pessoas", "Ativo", Dia)), "nome obrigatório");
        DeveRecusar(() => _ = rh.PontosEntre(Dia, Dia.AddDays(-1)), "intervalo invertido");
        Verificar(rh.PontosEntre(Dia.AddDays(-4), Dia).Count == 30, "intervalo inclui as duas datas");
        DeveRecusar(() => rh.SalvarPonto(new(101, Dia, "08:00", "17:00", "Atraso"), false), "ponto duplicado");
        DeveRecusar(() => rh.SalvarPonto(new(101, Dia, "16:00", "08:00", "Atraso"), true), "horários invertidos");
        DeveRecusar(() => rh.SalvarPonto(new(101, Dia, "08:00", "17:00", "Atraso", new string('A', 241)), true), "limite do motivo");
        rh.SalvarPonto(new(101, Dia, "08:00", "17:00", "Falta", " Avisou a equipe. "), true);
        var ausencia = rh.PontosEntre(Dia, Dia).Single(item => item.Matricula == 101);
        Verificar(ausencia.Entrada == "" && ausencia.Saida == "" && ausencia.Motivo == "Avisou a equipe.", "falta mantém motivo sem horários");
        rh.SalvarPonto(ausencia with { Situacao = "Presença", Entrada = "08:00", Saida = "17:00" }, true);
        Verificar(rh.PontosEntre(Dia, Dia).Single(item => item.Matricula == 101).Motivo == "", "presença limpa motivo de ocorrência");
        DeveRecusar(() => rh.SalvarFolha(new(101, Dia, 100, 0, 101)), "desconto maior que os proventos");
        rh.SalvarFolha(new(101, Dia, 5_200, 300, 50));
        Verificar(rh.FolhaDoMes(Dia).Single(item => item.Matricula == 101).Total == 5_450, "lançamento único por competência");
        rh.AbrirOuFecharVaga(1);
        DeveRecusar(() => rh.SalvarCandidato(new(0, "Pessoa Teste", 1, "Triagem")), "vaga encerrada bloqueia novo candidato");
        rh.SalvarCandidato(rh.Candidatos.First() with { Etapa = "Proposta" });
        Verificar(rh.Candidatos.First().Etapa == "Proposta", "candidato atual pode avançar na vaga encerrada");
        DeveRecusar(() => rh.AdicionarCompromisso("Reunião", Dia, "25:00", "Funcionários"), "horário da agenda inválido");
        var listaAntiga = rh.Funcionarios;
        rh.SalvarFuncionario(new(0, "  Pessoa Teste  ", "Analista", "Pessoas", "Ativo", Dia));
        Verificar(listaAntiga.Count == 8 && rh.Funcionarios.Count == 9, "lista entregue à tela não muda depois da gravação");
        var criado = rh.Funcionarios.Single(item => item.Nome == "Pessoa Teste");
        rh.ExcluirFuncionario(criado.Id);
        rh.SalvarFuncionario(new(0, "Outra Pessoa", "Analista", "Pessoas", "Ativo", Dia));
        Verificar(rh.Funcionarios.Single(item => item.Nome == "Outra Pessoa").Id > criado.Id, "matrícula excluída não é reutilizada");
        sessaoDoRh.Sair();
        DeveRecusar(() => _ = rh.Funcionarios, "sessão encerrada bloqueia leituras");
        DeveRecusar(() => rh.AbrirOuFecharVaga(1), "sessão encerrada bloqueia gravações");
        administrador.Sair();
    }

    private static void TestarTelas()
    {
        // abre o login igual ao programa de verdade, entra, e depois sai da conta
        using var cronometro = new Timer { Interval = 50 };
        var passo = 0;
        var voltas = 0;
        Exception? erro = null;
        cronometro.Tick += (_, _) =>
        {
            cronometro.Stop();
            try
            {
                if (++voltas > 120) throw new InvalidOperationException("A janela esperada não abriu.");
                var janelas = Application.OpenForms.Cast<Form>().ToArray();
                if (passo is 0 or 2 && janelas.OfType<TelaDeLogin>().FirstOrDefault() is { } login)
                {
                    login.Opacity = 0;
                    if (passo == 0)
                    {
                        Digitar(login, "rh", "errada");
                        Verificar(login.Visible && login.SessaoAberta is null && TodosOsControles(login).Any(item => item.Text == "Usuário ou senha incorretos."), "login inválido mantém a janela aberta");
                    }
                    Digitar(login, passo == 0 ? "rh" : "gestor", SenhaDeExemplo);
                    passo++;
                }
                else if (passo is 1 or 3 && janelas.OfType<TelaPrincipal>().FirstOrDefault() is { } principal)
                {
                    principal.Opacity = 0;
                    if (passo == 1)
                    {
                        TestarTelasDoRh(principal);
                        Clicar(principal, "Sair da conta");
                    }
                    else
                    {
                        Verificar(!TodosOsControles(principal).OfType<Button>().Any(item => item.Text == "Salários" && item.Visible), "salários ausente para gestor");
                        Clicar(principal, "Funcionários");
                        Verificar(!AcharBotao(principal, "Novo funcionário").Enabled, "edição indisponível para gestor");
                        Verificar(AcharTabela(principal, "Cadastro de funcionários").Rows.Cast<DataGridViewRow>().Any(linha => Equals(linha.Cells[0].Value, "Rita Teste")), "troca de conta mantém dados da execução");
                        // Parte do Jefferson: o gestor consulta a ficha, mas não vê salário nem pode inativar.
                        Verificar(!AcharBotao(principal, "Inativar").Enabled, "inativar indisponível para gestor");
                        var lista = AcharTabela(principal, "Cadastro de funcionários");
                        lista.CurrentCell = lista.Rows[0].Cells[0];
                        AbrirJanela(principal, "Consultar ficha", "Ficha do funcionário", modal =>
                        {
                            Verificar(TodosOsControles(modal).OfType<Label>().Any(item => item.AccessibleName == "Salário" && item.Text == "Sem acesso aos salários neste perfil"), "ficha do gestor sem salário");
                            Clicar(modal, "Fechar");
                        });
                        principal.Close();
                    }
                    passo++;
                }
            }
            catch (Exception falha)
            {
                erro = falha;
                foreach (var janela in Application.OpenForms.Cast<Form>().ToArray().Reverse()) janela.Close();
            }
            if (erro is null && passo < 4) cronometro.Start();
        };
        cronometro.Start();
        Aurora.WinForms.Program.RodarSessoes(new DadosDeExemplo(), new Autenticacao(new ContasDeExemplo()));
        if (erro is not null) throw erro;
        Verificar(passo == 4, $"ciclo completo de login e troca de conta (etapa {passo})");
    }

    private static void TestarTelasDoRh(Form principal)
    {
        Clicar(principal, "Funcionários");
        Campo<TextBox>(principal, "Buscar por nome ou cargo").Text = "operacoes";
        Verificar(AcharTabela(principal, "Cadastro de funcionários").RowCount == 1, "busca ignora acentos");
        AbrirJanela(principal, "Novo funcionário", "Novo funcionário", modal =>
        {
            Clicar(modal, "Confirmar");
            Verificar(modal.Visible, "cadastro em branco apresenta erro");
            Campo<TextBox>(modal, "Nome").Text = "Rita Teste";
            Campo<TextBox>(modal, "Cargo").Text = "Analista de RH";
            Verificar(Campo<TextBox>(modal, "Cargo").AutoCompleteCustomSource.Contains("Analista de RH"), "cargo sugere os cargos já cadastrados"); // Parte do Jefferson
            Clicar(modal, "Confirmar");
        });
        Verificar(AcharTabela(principal, "Cadastro de funcionários").RowCount == 9, "cadastro entrou na tabela");
        // Parte do Jefferson: a ficha mostra a matrícula gerada no cadastro da Rita (109).
        var cadastro = AcharTabela(principal, "Cadastro de funcionários");
        cadastro.CurrentCell = cadastro.Rows.Cast<DataGridViewRow>().Single(linha => Equals(linha.Cells[0].Value, "Rita Teste")).Cells[0];
        AbrirJanela(principal, "Consultar ficha", "Ficha do funcionário", modal =>
        {
            Verificar(TodosOsControles(modal).OfType<Label>().Any(item => item.AccessibleName == "Matrícula" && item.Text == "109"), "ficha mostra a matrícula gerada");
            Clicar(modal, "Fechar");
        });
        Clicar(principal, "Ponto e frequência");
        AbrirJanela(principal, "Registrar ponto", "Registrar ponto", modal =>
        {
            var pessoa = Campo<ComboBox>(modal, "Funcionário");
            pessoa.SelectedIndex = pessoa.Items.Count - 1;
            Campo<ComboBox>(modal, "Situação").SelectedItem = "Falta";
            Verificar(!Campo<TextBox>(modal, "Entrada (HH:mm)").Enabled, "falta desativa horários");
            Campo<TextBox>(modal, "Motivo da ocorrência (opcional)").Text = "Avisou o RH.";
            Clicar(modal, "Confirmar");
        });
        var pontos = AcharTabela(principal, "Registros de ponto");
        var rita = pontos.Rows.Cast<DataGridViewRow>().Single(linha => Equals(linha.Cells[0].Value, "Rita Teste"));
        Verificar(Equals(rita.Cells[5].Value, "Avisou o RH."), "motivo aparece na tabela");
        pontos.CurrentCell = rita.Cells[0];
        AbrirJanela(principal, "Editar registro", "Editar registro de ponto", modal =>
        {
            Verificar(Campo<TextBox>(modal, "Motivo da ocorrência (opcional)").Text == "Avisou o RH.", "motivo aparece na edição");
            Campo<TextBox>(modal, "Motivo da ocorrência (opcional)").Text = "Motivo revisado.";
            Clicar(modal, "Confirmar");
        });
        Verificar(AcharTabela(principal, "Registros de ponto").Rows.Cast<DataGridViewRow>().Any(linha => Equals(linha.Cells[5].Value, "Motivo revisado.")), "edição atualizou o motivo");
        Clicar(principal, "Recrutamento");
        AbrirJanela(principal, "Alterar etapa", "Editar candidatura", modal =>
        {
            Campo<ComboBox>(modal, "Etapa").SelectedItem = "Proposta";
            Clicar(modal, "Confirmar");
        });
        Verificar(Equals(AcharTabela(principal, "Lista de candidaturas").Rows[0].Cells[2].Value, "Proposta"), "etapa mudou na tabela");
        // Parte do Kerlon: os botões novos da tela de Recrutamento ficam disponíveis para o RH.
        Verificar(AcharBotao(principal, "Avançar etapa").Enabled && AcharBotao(principal, "Mudar de vaga").Enabled, "botões do recrutamento habilitados");
        Clicar(principal, "Calendário");
        AbrirJanela(principal, "Novo compromisso", "Novo compromisso", modal =>
        {
            Campo<TextBox>(modal, "Título").Text = "Entrevista final";
            Clicar(modal, "Confirmar");
        });
        Verificar(AcharTabela(principal, "Compromissos do dia").RowCount == 3, "agenda mostra o novo item");
        Clicar(principal, "Salários");
        AbrirJanela(principal, "Novo lançamento", "Novo lançamento", modal =>
        {
            Campo<NumericUpDown>(modal, "Salário (R$)").Value = 5_200;
            Campo<NumericUpDown>(modal, "Bônus (R$)").Value = 300;
            Campo<NumericUpDown>(modal, "Descontos (R$)").Value = 50;
            Clicar(modal, "Confirmar");
        });
        Verificar(AcharTabela(principal, "Lançamentos salariais").Rows.Cast<DataGridViewRow>().Any(linha => linha.Cells[4].Value?.ToString()?.Contains("5.450,00") == true), "folha mostra o líquido");
        // Diaz: editar um lançamento existente pela grade (muda só os descontos: 5.200 + 300 - 150)
        var folha = AcharTabela(principal, "Lançamentos salariais");
        folha.CurrentCell = folha.Rows.Cast<DataGridViewRow>().First(linha => linha.Cells[4].Value?.ToString()?.Contains("5.450,00") == true).Cells[0];
        AbrirJanela(principal, "Editar lançamento", "Editar lançamento", modal =>
        {
            Campo<NumericUpDown>(modal, "Descontos (R$)").Value = 150;
            Clicar(modal, "Confirmar");
        });
        Verificar(AcharTabela(principal, "Lançamentos salariais").Rows.Cast<DataGridViewRow>().Any(linha => linha.Cells[4].Value?.ToString()?.Contains("5.350,00") == true), "edição do lançamento atualiza o líquido");
        Verificar(!TodosOsControles(principal).OfType<Button>().Any(item => item.Text == "Sobre o projeto"), "menu sem tela Sobre o projeto");
    }

    private static void AbrirJanela(Form dono, string botaoQueAbre, string titulo, Action<Form> preenchimento)
    {
        // o timer clica nos botões da janela enquanto ela está aberta
        using var cronometro = new Timer { Interval = 120 };
        Exception? erro = null;
        cronometro.Tick += (_, _) =>
        {
            cronometro.Stop();
            var modal = Application.OpenForms.Cast<Form>().FirstOrDefault(item => item.Text == titulo && item != dono);
            try
            {
                if (modal is null) throw new InvalidOperationException($"O diálogo {titulo} não abriu.");
                preenchimento(modal);
                Verificar(!modal.Visible || modal.DialogResult == DialogResult.OK, $"O diálogo {titulo} não foi confirmado.");
            }
            catch (Exception falhou) { erro = falhou; modal?.Close(); }
        };
        cronometro.Start();
        Clicar(dono, botaoQueAbre);
        if (erro is not null) throw erro;
    }

    private static void Digitar(Form janela, string usuario, string senha)
    {
        Campo<TextBox>(janela, "Usuário").Text = usuario;
        Campo<TextBox>(janela, "Senha").Text = senha;
        Clicar(janela, "Entrar");
    }

    private static void DeveRecusar(Action teste, string cenario)
    {
        try { teste(); }
        catch (ErroDeRegra) { return; }
        throw new InvalidOperationException($"A regra deixou passar {cenario}.");
    }

    private static void Verificar(bool condicao, string cenario)
    {
        if (!condicao) throw new InvalidOperationException($"Falhou em {cenario}.");
    }

    private static void Clicar(Form janela, string texto) => AcharBotao(janela, texto).PerformClick();
    private static Button AcharBotao(Form janela, string texto) => TodosOsControles(janela).OfType<Button>().First(item => item.Visible && item.Text == texto);
    private static DataGridView AcharTabela(Form janela, string nome) => Campo<DataGridView>(janela, nome);
    private static T Campo<T>(Form janela, string nome) where T : Control => TodosOsControles(janela).OfType<T>().First(item => item.AccessibleName == nome);

    private static IEnumerable<Control> TodosOsControles(Control pai)
    {
        foreach (Control filho in pai.Controls)
        {
            yield return filho;
            foreach (var filhos in TodosOsControles(filho)) yield return filhos;
        }
    }
}
