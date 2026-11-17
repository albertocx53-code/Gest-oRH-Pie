using System.Globalization;
using Aurora.Core;
using Aurora.WinForms;
using Timer = System.Windows.Forms.Timer;

namespace Aurora.Tests;

internal static class TestProgram
{
    private const string DemoPassword = "Aurora.demo26";
    private static readonly DateTime Day = new(2026, 11, 13);

    [STAThread]
    private static int Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
        CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
        Application.EnableVisualStyles();
        Application.SetColorMode(SystemColorMode.Dark);
        try
        {
            CheckServices();
            TesteDoBanco.Rodar(); // testes do banco SQLite (Alberto)
            CheckWindows();
            Console.WriteLine("Serviços e fluxos do Windows Forms aprovados.");
            return 0;
        }
        catch (Exception failure)
        {
            Console.Error.WriteLine(failure);
            return 1;
        }
    }

    private static void CheckServices()
    {
        var auth = new Authentication(new DemoAccounts());
        var storage = new DemoRh();
        Check(auth.SignIn("rh", "senha incorreta") is null && auth.SignIn("inexistente", DemoPassword) is null, "credenciais inválidas");
        var administrator = auth.SignIn(" ADMIN ", DemoPassword)!;
        Check(administrator.Role == RhRole.Administrator, "perfil de administrador vindo da conta");
        var rhSession = auth.SignIn("rh", DemoPassword)!;
        var rh = new RhService(storage, rhSession);
        var manager = new RhService(storage, auth.SignIn("gestor", DemoPassword)!);
        Check(!manager.CanEdit && !manager.CanViewPay, "permissões do gestor");
        Check(manager.Employees.Count == 8 && manager.Agenda.All(item => item.Category != "Salários"), "consulta sem compromissos salariais");
        Reject(() => _ = manager.PayrollFor(Day), "consulta salarial bloqueada");
        Reject(() => manager.SavePayroll(new(101, Day, 1, 0, 0)), "gravação salarial bloqueada");
        Reject(() => manager.DeleteEmployee(101), "alteração bloqueada para gestor");
        Reject(() => rh.DeleteEmployee(101), "histórico impede exclusão");
        Reject(() => rh.SaveEmployee(new(0, " ", "Analista", "Pessoas", "Ativo", Day)), "nome obrigatório");
        Reject(() => _ = rh.AttendanceBetween(Day, Day.AddDays(-1)), "intervalo invertido");
        Check(rh.AttendanceBetween(Day.AddDays(-4), Day).Count == 30, "intervalo inclui as duas datas");
        Reject(() => rh.SaveAttendance(new(101, Day, "08:00", "17:00", "Atraso"), false), "ponto duplicado");
        Reject(() => rh.SaveAttendance(new(101, Day, "16:00", "08:00", "Atraso"), true), "horários invertidos");
        Reject(() => rh.SaveAttendance(new(101, Day, "08:00", "17:00", "Atraso", new string('A', 241)), true), "limite do motivo");
        rh.SaveAttendance(new(101, Day, "08:00", "17:00", "Falta", " Avisou a equipe. "), true);
        var absence = rh.AttendanceBetween(Day, Day).Single(item => item.Person == 101);
        Check(absence.In == "" && absence.Out == "" && absence.Reason == "Avisou a equipe.", "falta mantém motivo sem horários");
        rh.SaveAttendance(absence with { Status = "Presença", In = "08:00", Out = "17:00" }, true);
        Check(rh.AttendanceBetween(Day, Day).Single(item => item.Person == 101).Reason == "", "presença limpa motivo de ocorrência");
        Reject(() => rh.SavePayroll(new(101, Day, 100, 0, 101)), "desconto maior que os proventos");
        rh.SavePayroll(new(101, Day, 5_200, 300, 50));
        Check(rh.PayrollFor(Day).Single(item => item.Person == 101).Total == 5_450, "lançamento único por competência");
        rh.ToggleVacancy(1);
        Reject(() => rh.SaveCandidate(new(0, "Pessoa Teste", 1, "Triagem")), "vaga encerrada bloqueia novo candidato");
        rh.SaveCandidate(rh.Candidates.First() with { Stage = "Proposta" });
        Check(rh.Candidates.First().Stage == "Proposta", "candidato atual pode avançar na vaga encerrada");
        Reject(() => rh.AddAppointment("Reunião", Day, "25:00", "Funcionários"), "horário da agenda inválido");
        var oldList = rh.Employees;
        rh.SaveEmployee(new(0, "  Pessoa Teste  ", "Analista", "Pessoas", "Ativo", Day));
        Check(oldList.Count == 8 && rh.Employees.Count == 9, "lista entregue à tela não muda depois da gravação");
        var created = rh.Employees.Single(item => item.Name == "Pessoa Teste");
        rh.DeleteEmployee(created.Id);
        rh.SaveEmployee(new(0, "Outra Pessoa", "Analista", "Pessoas", "Ativo", Day));
        Check(rh.Employees.Single(item => item.Name == "Outra Pessoa").Id > created.Id, "matrícula excluída não é reutilizada");
        rhSession.SignOut();
        Reject(() => _ = rh.Employees, "sessão encerrada bloqueia leituras");
        Reject(() => rh.ToggleVacancy(1), "sessão encerrada bloqueia gravações");
        administrator.SignOut();
    }

    private static void CheckWindows()
    {
        // O mesmo fluxo usado pelo executável abre o login, libera o painel e volta ao login ao sair.
        using var timer = new Timer { Interval = 50 };
        var step = 0;
        var laps = 0;
        Exception? error = null;
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            try
            {
                if (++laps > 120) throw new InvalidOperationException("A janela esperada não abriu.");
                var windows = Application.OpenForms.Cast<Form>().ToArray();
                if (step is 0 or 2 && windows.OfType<LoginForm>().FirstOrDefault() is { } login)
                {
                    login.Opacity = 0;
                    if (step == 0)
                    {
                        Enter(login, "rh", "errada");
                        Check(login.Visible && login.Session is null && Descendants(login).Any(item => item.Text == "Usuário ou senha incorretos."), "login inválido mantém a janela aberta");
                    }
                    Enter(login, step == 0 ? "rh" : "gestor", DemoPassword);
                    step++;
                }
                else if (step is 1 or 3 && windows.OfType<AuroraForm>().FirstOrDefault() is { } main)
                {
                    main.Opacity = 0;
                    if (step == 1)
                    {
                        CheckRhScreens(main);
                        Click(main, "Sair da conta");
                    }
                    else
                    {
                        Check(!Descendants(main).OfType<Button>().Any(item => item.Text == "Salários" && item.Visible), "salários ausente para gestor");
                        Click(main, "Funcionários");
                        Check(!Button(main, "Novo funcionário").Enabled, "edição indisponível para gestor");
                        Check(Grid(main, "Cadastro de funcionários").Rows.Cast<DataGridViewRow>().Any(row => Equals(row.Cells[0].Value, "Rita Teste")), "troca de conta mantém dados da execução");
                        main.Close();
                    }
                    step++;
                }
            }
            catch (Exception failure)
            {
                error = failure;
                foreach (var window in Application.OpenForms.Cast<Form>().ToArray().Reverse()) window.Close();
            }
            if (error is null && step < 4) timer.Start();
        };
        timer.Start();
        Aurora.WinForms.Program.RunSessionLoop(new DemoRh(), new Authentication(new DemoAccounts()));
        if (error is not null) throw error;
        Check(step == 4, $"ciclo completo de login e troca de conta (etapa {step})");
    }

    private static void CheckRhScreens(Form main)
    {
        Click(main, "Funcionários");
        Input<TextBox>(main, "Buscar por nome ou cargo").Text = "operacoes";
        Check(Grid(main, "Cadastro de funcionários").RowCount == 1, "busca ignora acentos");
        Dialog(main, "Novo funcionário", "Novo funcionário", modal =>
        {
            Click(modal, "Confirmar");
            Check(modal.Visible, "cadastro em branco apresenta erro");
            Input<TextBox>(modal, "Nome").Text = "Rita Teste";
            Input<TextBox>(modal, "Cargo").Text = "Analista de RH";
            Click(modal, "Confirmar");
        });
        Check(Grid(main, "Cadastro de funcionários").RowCount == 9, "cadastro entrou na tabela");
        Click(main, "Ponto e frequência");
        Dialog(main, "Registrar ponto", "Registrar ponto", modal =>
        {
            var person = Input<ComboBox>(modal, "Funcionário");
            person.SelectedIndex = person.Items.Count - 1;
            Input<ComboBox>(modal, "Situação").SelectedItem = "Falta";
            Check(!Input<TextBox>(modal, "Entrada (HH:mm)").Enabled, "falta desativa horários");
            Input<TextBox>(modal, "Motivo da ocorrência (opcional)").Text = "Avisou o RH.";
            Click(modal, "Confirmar");
        });
        var punches = Grid(main, "Registros de ponto");
        var rita = punches.Rows.Cast<DataGridViewRow>().Single(row => Equals(row.Cells[0].Value, "Rita Teste"));
        Check(Equals(rita.Cells[5].Value, "Avisou o RH."), "motivo aparece na tabela");
        punches.CurrentCell = rita.Cells[0];
        Dialog(main, "Editar registro", "Editar registro de ponto", modal =>
        {
            Check(Input<TextBox>(modal, "Motivo da ocorrência (opcional)").Text == "Avisou o RH.", "motivo aparece na edição");
            Input<TextBox>(modal, "Motivo da ocorrência (opcional)").Text = "Motivo revisado.";
            Click(modal, "Confirmar");
        });
        Check(Grid(main, "Registros de ponto").Rows.Cast<DataGridViewRow>().Any(row => Equals(row.Cells[5].Value, "Motivo revisado.")), "edição atualizou o motivo");
        Click(main, "Recrutamento");
        Dialog(main, "Alterar etapa", "Editar candidatura", modal =>
        {
            Input<ComboBox>(modal, "Etapa").SelectedItem = "Proposta";
            Click(modal, "Confirmar");
        });
        Check(Equals(Grid(main, "Lista de candidaturas").Rows[0].Cells[2].Value, "Proposta"), "etapa mudou na tabela");
        Click(main, "Calendário");
        Dialog(main, "Novo compromisso", "Novo compromisso", modal =>
        {
            Input<TextBox>(modal, "Título").Text = "Entrevista final";
            Click(modal, "Confirmar");
        });
        Check(Grid(main, "Compromissos do dia").RowCount == 3, "agenda mostra o novo item");
        Click(main, "Salários");
        Dialog(main, "Novo lançamento", "Novo lançamento", modal =>
        {
            Input<NumericUpDown>(modal, "Salário (R$)").Value = 5_200;
            Input<NumericUpDown>(modal, "Bônus (R$)").Value = 300;
            Input<NumericUpDown>(modal, "Descontos (R$)").Value = 50;
            Click(modal, "Confirmar");
        });
        Check(Grid(main, "Lançamentos salariais").Rows.Cast<DataGridViewRow>().Any(row => row.Cells[4].Value?.ToString()?.Contains("5.450,00") == true), "folha mostra o líquido");
    }

    private static void Dialog(Form owner, string openingButton, string title, Action<Form> fill)
    {
        // O timer aciona os controles reais durante o ShowDialog sem depender de coordenadas na tela.
        using var timer = new Timer { Interval = 120 };
        Exception? error = null;
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            var modal = Application.OpenForms.Cast<Form>().FirstOrDefault(item => item.Text == title && item != owner);
            try
            {
                if (modal is null) throw new InvalidOperationException($"O diálogo {title} não abriu.");
                fill(modal);
                Check(!modal.Visible || modal.DialogResult == DialogResult.OK, $"O diálogo {title} não foi confirmado.");
            }
            catch (Exception failed) { error = failed; modal?.Close(); }
        };
        timer.Start();
        Click(owner, openingButton);
        if (error is not null) throw error;
    }

    private static void Enter(Form form, string user, string password)
    {
        Input<TextBox>(form, "Usuário").Text = user;
        Input<TextBox>(form, "Senha").Text = password;
        Click(form, "Entrar");
    }

    private static void Reject(Action test, string scenario)
    {
        try { test(); }
        catch (RhRuleException) { return; }
        throw new InvalidOperationException($"A regra deixou passar {scenario}.");
    }

    private static void Check(bool condition, string scenario)
    {
        if (!condition) throw new InvalidOperationException($"Falhou em {scenario}.");
    }

    private static void Click(Form form, string text) => Button(form, text).PerformClick();
    private static Button Button(Form form, string text) => Descendants(form).OfType<Button>().First(item => item.Visible && item.Text == text);
    private static DataGridView Grid(Form form, string name) => Input<DataGridView>(form, name);
    private static T Input<T>(Form form, string name) where T : Control => Descendants(form).OfType<T>().First(item => item.AccessibleName == name);

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
}
