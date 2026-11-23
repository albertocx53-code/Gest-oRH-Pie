using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Aurora.Core;

namespace Aurora.WinForms;

internal sealed partial class AuroraForm : Form
{
    // Estes nomes ligam o menu e os atalhos do painel à mesma função de navegação.
    private static readonly (string Id, string Label, string Icon)[] Pages =
    [
        ("home", "Painel", "home"), ("people", "Funcionários", "people"), ("time", "Ponto e frequência", "time"),
        ("hire", "Recrutamento", "hire"), ("calendar", "Calendário", "calendar"), ("pay", "Salários", "pay")
    ];
    private readonly RhService data;
    private readonly AuroraBackdrop root = new() { Dock = DockStyle.Fill };
    private readonly Panel content = new() { BackColor = Color.Transparent, AutoScroll = true };
    private readonly Dictionary<string, AuroraButton> nav = [];
    private readonly Label profileLabel;
    private readonly Label footer;
    private string page = "home";
    private DateTime punchDay = DemoRh.Today;
    private DateTime agendaDay = DemoRh.Today;
    private DateTime payrollMonth = DemoRh.Today;
    private bool CanViewPay => data.CanViewPay;
    internal bool ReturnToLogin { get; private set; }

    public AuroraForm(RhService data)
    {
        this.data = data;
        Text = "Aurora — Gestão de RH";
        ClientSize = new(1280, 820);
        MinimumSize = new(1040, 720);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = AuroraStyle.Font(10);
        Controls.Add(root);

        const int railLeft = 24;
        const int headerTop = 22;
        const int headerHeight = 47;
        var header = Glass(20);
        header.Bounds = new(236, headerTop, 1018, headerHeight);
        header.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        root.Controls.Add(header);
        AuroraStyle.Put(header, AuroraStyle.Label("Espaço de trabalho", 10, true), 15, 13);
        var switchProfile = Action("Sair da conta", "", 102, false, () => { ReturnToLogin = true; Close(); });
        switchProfile.Location = new(header.Width - 111, 6);
        switchProfile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        header.Controls.Add(switchProfile);

        var brand = new Surface(Color.FromArgb(48, 39, 100), Color.FromArgb(139, 126, 211), 15) { Size = new(41, 41) };
        brand.Paint += (_, e) => AuroraStyle.DrawBrand(e.Graphics, brand.ClientRectangle);
        AuroraStyle.Put(root, brand, railLeft, headerTop + (headerHeight - brand.Height) / 2);
        var wordmark = AuroraStyle.Label("Aurora", 15, true);
        var tagline = AuroraStyle.Label("Gestão de RH", 9, false, AuroraStyle.Muted);
        var brandText = new Panel { Size = new(129, headerHeight), BackColor = Color.Transparent };
        var textTop = (headerHeight - wordmark.PreferredHeight - tagline.PreferredHeight) / 2;
        AuroraStyle.Put(brandText, wordmark, 0, textTop);
        AuroraStyle.Put(brandText, tagline, 0, textTop + wordmark.PreferredHeight);
        AuroraStyle.Put(root, brandText, brand.Right + 11, headerTop);

        var rail = Glass(22);
        rail.Bounds = new(railLeft, 91, 191, 681);
        rail.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
        root.Controls.Add(rail);
        AuroraStyle.Put(rail, AuroraStyle.Label("ÁREAS DE RH", 8, true, AuroraStyle.Muted), 16, 19);
        for (var i = 0; i < Pages.Length; i++)
        {
            var item = Pages[i];
            var button = new AuroraButton(item.Label) { Glyph = item.Icon, Size = new(169, 43), Font = AuroraStyle.Font(9.5f, true), Navigation = true, TabIndex = i };
            button.Click += (_, _) => Navigate(item.Id);
            nav.Add(item.Id, button);
            AuroraStyle.Put(rail, button, 11, 46 + i * 48);
        }
        // Alberto: área "Sobre o projeto", com a equipe, o banco, os códigos e a POO (AuroraForm.Sobre.cs).
        var projetoTop = 46 + Pages.Length * 48 + 14;
        AuroraStyle.Put(rail, AuroraStyle.Label("PROJETO", 8, true, AuroraStyle.Muted), 16, projetoTop);
        var sobre = new AuroraButton("Sobre o projeto") { Glyph = "info", Size = new(169, 43), Font = AuroraStyle.Font(9.5f, true), Navigation = true, TabIndex = Pages.Length };
        sobre.Click += (_, _) => Navigate("about");
        nav.Add("about", sobre);
        AuroraStyle.Put(rail, sobre, 11, projetoTop + 24);
        var person = new Surface(Color.FromArgb(172, 36, 42, 65), Color.FromArgb(91, 142, 153, 192), 13) { Size = new(167, 59) };
        person.Location = new(12, rail.Height - 72);
        person.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        rail.Controls.Add(person);
        var mini = new Surface(AuroraStyle.Tint, AuroraStyle.Border, 9) { Size = new(33, 33) };
        mini.Paint += (_, e) => AuroraStyle.Icon(e.Graphics, "people", mini.Width / 2f, mini.Height / 2f, AuroraStyle.Accent);
        AuroraStyle.Put(person, mini, 8, 13);
        profileLabel = AuroraStyle.Label("", 9, true);
        AuroraStyle.Put(person, profileLabel, 47, 10);
        AuroraStyle.Put(person, AuroraStyle.Label("Sessão de demonstração", 7.5f, false, AuroraStyle.Muted), 47, 31);

        content.Bounds = new(236, 86, 1018, 686);
        content.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        root.Controls.Add(content);
        footer = AuroraStyle.Label("", 8.5f, false, AuroraStyle.Muted);
        footer.Location = new(240, 788);
        footer.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        root.Controls.Add(footer);
        Navigate(page);
    }

    private static Surface Glass(int radius) => new(Color.FromArgb(123, 40, 47, 70), Color.FromArgb(126, 154, 168, 215), radius);
    private static Surface Card(int radius = 18) => new(AuroraStyle.Paper, AuroraStyle.Border, radius);

    private static AuroraButton Action(string label, string glyph, int width, bool primary, Action handler)
    {
        var button = new AuroraButton(label) { Glyph = glyph, Primary = primary, Size = new(width, 35) };
        button.Click += (_, _) => handler();
        return button;
    }

    private AuroraButton EditAction(string label, string glyph, int width, bool primary, Action handler)
    {
        // O botão sinaliza a permissão. RhService também verifica o perfil quando recebe a operação.
        var button = Action(label, glyph, width, primary, handler);
        button.Enabled = data.CanEdit;
        return button;
    }

    private void Navigate(string id)
    {
        if (id == "pay" && !CanViewPay) id = "home";
        page = id;
        foreach (var item in nav)
        {
            item.Value.Selected = item.Key == page;
            item.Value.Visible = item.Key != "pay" || CanViewPay;
            item.Value.AccessibleDescription = item.Value.Selected ? "Área atual" : "";
            item.Value.Invalidate();
        }
        profileLabel.Text = data.Profile;
        // Ao mudar de área, descartamos os controles antigos para não deixar filtros e eventos de outra tela ativos.
        content.SuspendLayout();
        foreach (var control in content.Controls.Cast<Control>().ToArray()) control.Dispose();
        content.AutoScrollPosition = Point.Empty;
        var view = page switch
        {
            "people" => PeoplePage(), "time" => TimePage(), "hire" => HirePage(), "calendar" => CalendarPage(), "pay" => PayPage(), "about" => AboutPage(), _ => Dashboard()
        };
        view.Dock = DockStyle.Top;
        content.Controls.Add(view);
        content.ResumeLayout(true);
        content.PerformLayout();
        view.PerformLayout();
        Status();
    }

    private void Status(string? message = null) => footer.Text = message is null
        ? "Dados fictícios · Alterações válidas enquanto o Aurora estiver aberto"
        : $"{message}  ·  Sessão de demonstração";

    private static Panel Page() => new() { BackColor = Color.Transparent, Height = 668 };

    private static void Heading(Panel parent, string title, string subtitle, AuroraButton? action = null)
    {
        var header = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
        header.ColumnStyles.Add(new(SizeType.Percent, 100));
        header.ColumnStyles.Add(new(SizeType.Absolute, (action?.Width ?? 0) + 8));
        header.RowStyles.Add(new(SizeType.Percent, 100));
        PutWide(parent, header, 0, 91);
        var copy = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = Padding.Empty };
        header.Controls.Add(copy, 0, 0);
        var headline = AuroraStyle.Label(title, 30, true);
        headline.AutoSize = false;
        headline.AutoEllipsis = true;
        headline.Height = 53;
        AuroraStyle.Put(copy, headline, 3, 5);
        copy.SizeChanged += (_, _) => headline.Width = Math.Max(0, copy.Width - 8);
        headline.Width = Math.Max(0, copy.Width - 8);
        AuroraStyle.Put(copy, AuroraStyle.Label(subtitle, 11, false, AuroraStyle.Muted), 5, 63);
        if (action is null) return;
        action.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        action.Margin = new(0, 28, 2, 0);
        header.Controls.Add(action, 1, 0);
    }

    private Panel Dashboard()
    {
        // Os indicadores saem dos mesmos registros que os módulos exibem. Assim uma edição aparece no painel.
        var panel = Page();
        panel.Width = content.ClientSize.Width;
        var active = data.Employees.Count(p => p.Status == "Ativo");
        var today = data.Present(DemoRh.Today);
        var jobs = data.Vacancies.Count(job => job.Status == "Aberta");
        var month = CanViewPay ? data.PayrollFor(DemoRh.Today).Sum(p => p.Total) : 0m;
        Heading(panel, $"Olá, {data.User}", "Sexta-feira, 13 de novembro de 2026", EditAction("Novo funcionário", "plus", 169, true, () => EditEmployee(null)));

        var metrics = new TableLayoutPanel { ColumnCount = 4, RowCount = 1, BackColor = Color.Transparent };
        for (var i = 0; i < 4; i++) metrics.ColumnStyles.Add(new(SizeType.Percent, 25));
        metrics.RowStyles.Add(new(SizeType.Percent, 100));
        metrics.Bounds = new(0, 107, panel.Width, 121);
        metrics.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
        panel.Controls.Add(metrics);
        metrics.Controls.Add(Metric("people", "Funcionários ativos", active.ToString(), $"{data.Employees.Count} no cadastro", "people", 0), 0, 0);
        metrics.Controls.Add(Metric("time", "Presentes hoje", today.ToString(), $"de {active} funcionários ativos", "time", 1), 1, 0);
        metrics.Controls.Add(Metric("hire", "Vagas abertas", jobs.ToString(), $"{data.Candidates.Count} candidaturas", "hire", 2), 2, 0);
        metrics.Controls.Add(CanViewPay
            ? Metric("pay", "Folha de novembro", month.ToString("C0"), "Líquido simulado", "pay", 3)
            : Metric("calendar", "Compromissos hoje", data.Agenda.Count(a => a.Time.Date == DemoRh.Today).ToString(), "Agenda de RH", "calendar", 3), 3, 0);

        var row = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
        row.ColumnStyles.Add(new(SizeType.Percent, 56));
        row.ColumnStyles.Add(new(SizeType.Percent, 44));
        row.RowStyles.Add(new(SizeType.Percent, 100));
        row.Bounds = new(0, 245, panel.Width, 281);
        row.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
        panel.Controls.Add(row);
        row.Controls.Add(Chart(active), 0, 0);
        row.Controls.Add(RecentCandidates(), 1, 0);

        var agenda = Card();
        agenda.Bounds = new(0, 544, panel.Width, 107);
        agenda.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        panel.Controls.Add(agenda);
        AuroraStyle.Put(agenda, AuroraStyle.Label("Agenda de hoje", 13, true), 17, 12);
        var appointments = data.Agenda.Where(a => a.Time.Date == DemoRh.Today).OrderBy(a => a.Time).Take(2).ToArray();
        for (var i = 0; i < appointments.Length; i++)
        {
            var item = appointments[i];
            AuroraStyle.Put(agenda, AuroraStyle.Label(item.Time.ToString("HH:mm"), 9, true, AuroraStyle.Accent), 19, 47 + i * 24);
            var title = AuroraStyle.Label(item.Title, 10);
            title.AutoSize = false;
            title.AutoEllipsis = true;
            title.Size = new(agenda.Width - 234, 20);
            title.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            AuroraStyle.Put(agenda, title, 75, 45 + i * 24);
        }
        var seeAgenda = Action("Abrir calendário", "", 130, false, () => Navigate("calendar"));
        seeAgenda.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        AuroraStyle.Put(agenda, seeAgenda, agenda.Width - 146, 37);
        return panel;
    }

    private Surface Metric(string glyph, string name, string amount, string detail, string target, int index)
    {
        var tile = Card(15);
        tile.Dock = DockStyle.Fill;
        tile.Margin = new(index * 3, 0, (3 - index) * 3, 0);
        var icon = new Surface(AuroraStyle.Tint, AuroraStyle.Tint, 8) { Size = new(30, 30) };
        icon.Paint += (_, e) => AuroraStyle.Icon(e.Graphics, glyph, icon.Width / 2f, icon.Height / 2f, AuroraStyle.Accent, 18);
        AuroraStyle.Put(tile, icon, 13, 10);
        AuroraStyle.Put(tile, AuroraStyle.Label(name, 9, true, AuroraStyle.Muted), 13, 43);
        AuroraStyle.Put(tile, AuroraStyle.Label(amount, 22, true), 11, 59);
        AuroraStyle.Put(tile, AuroraStyle.Label(detail, 8.5f, false, AuroraStyle.Muted), 14, 98);
        var open = Action("", "next", 28, false, () => Navigate(target));
        open.AccessibleName = $"Abrir {name}";
        open.Height = 28;
        AuroraStyle.Put(tile, open, tile.Width - 41, 11);
        tile.SizeChanged += (_, _) => open.Left = tile.ClientSize.Width - open.Width - 13;
        return tile;
    }

    private Surface Chart(int active)
    {
        var box = Card();
        box.Dock = DockStyle.Fill;
        box.Margin = new(0, 0, 8, 0);
        AuroraStyle.Put(box, AuroraStyle.Label("Frequência da semana", 15, true), 16, 13);
        AuroraStyle.Put(box, AuroraStyle.Label("Pessoas com entrada registrada · 9 a 13 nov.", 9, false, AuroraStyle.Muted), 18, 45);
        box.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var width = (box.ClientSize.Width - 44) / 5f;
            using var line = new Pen(AuroraStyle.Border);
            using var small = AuroraStyle.Font(9);
            using var countFont = AuroraStyle.Font(11, true);
            for (var y = 107; y <= 227; y += 40) g.DrawLine(line, 22, y, box.Width - 22, y);
            var names = new[] { "Seg", "Ter", "Qua", "Qui", "Sex" };
            for (var i = 0; i < names.Length; i++)
            {
                var value = data.Present(DemoRh.Today.AddDays(i - 4));
                var h = Math.Max(2, 128f * value / Math.Max(1, Math.Max(active, value)));
                var mid = 22 + (i + .5f) * width;
                var bar = new RectangleF(mid - width * .29f, 227 - h, width * .58f, h);
                using var fill = new LinearGradientBrush(bar, i == 4 ? Color.FromArgb(126, 101, 221) : Color.FromArgb(150, 177, 249), i == 4 ? AuroraStyle.Accent : Color.FromArgb(115, 112, 216), 90);
                using var rounded = AuroraStyle.Shape(bar, Math.Min(9, h / 2));
                g.FillPath(fill, rounded);
                TextRenderer.DrawText(g, value.ToString(), countFont, new Rectangle((int)mid - 22, (int)bar.Top - 23, 44, 20), AuroraStyle.Ink, TextFormatFlags.HorizontalCenter);
                TextRenderer.DrawText(g, names[i], small, new Rectangle((int)mid - 24, 236, 48, 20), AuroraStyle.Muted, TextFormatFlags.HorizontalCenter);
            }
        };
        box.AccessibleName = "Frequência de segunda a sexta";
        box.AccessibleDescription = string.Join(", ", Enumerable.Range(0, 5).Select(i => $"{DemoRh.Today.AddDays(i - 4):dddd}: {data.Present(DemoRh.Today.AddDays(i - 4))}"));
        return box;
    }

    private Surface RecentCandidates()
    {
        var box = Card();
        box.Dock = DockStyle.Fill;
        box.Margin = new(8, 0, 0, 0);
        AuroraStyle.Put(box, AuroraStyle.Label("Candidaturas recentes", 15, true), 16, 13);
        AuroraStyle.Put(box, AuroraStyle.Label("Acompanhe as etapas da seleção", 9, false, AuroraStyle.Muted), 18, 45);
        var list = new TableLayoutPanel { ColumnCount = 3, RowCount = 4, BackColor = Color.Transparent };
        list.ColumnStyles.Add(new(SizeType.Absolute, 45));
        list.ColumnStyles.Add(new(SizeType.Percent, 100));
        list.ColumnStyles.Add(new(SizeType.Absolute, 101));
        list.Bounds = new(16, 72, box.Width - 32, 194);
        box.SizeChanged += (_, _) => list.Width = Math.Max(1, box.ClientSize.Width - 32);
        box.Controls.Add(list);
        var candidates = data.Candidates.TakeLast(4).Reverse().ToArray();
        for (var i = 0; i < candidates.Length; i++)
        {
            list.RowStyles.Add(new(SizeType.Absolute, 47));
            var candidate = candidates[i];
            var avatar = new Surface(i % 2 == 0 ? AuroraStyle.Tint : Color.FromArgb(41, 57, 86), Color.Transparent, 10) { Size = new(34, 34), Margin = new(0, 5, 0, 0) };
            var initials = string.Concat(candidate.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(word => char.ToUpper(word[0])));
            avatar.Controls.Add(new Label { Text = initials, Dock = DockStyle.Fill, BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleCenter, ForeColor = AuroraStyle.Accent, Font = AuroraStyle.Font(9, true) });
            list.Controls.Add(avatar, 0, i);
            var name = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = Padding.Empty };
            var label = AuroraStyle.Label(candidate.Name, 10, true);
            label.AutoSize = false; label.AutoEllipsis = true; label.Size = new(145, 20);
            AuroraStyle.Put(name, label, 0, 4);
            var job = AuroraStyle.Label(data.VacancyTitle(candidate.Vacancy), 8.5f, false, AuroraStyle.Muted);
            job.AutoSize = false; job.AutoEllipsis = true; job.Height = 18;
            AuroraStyle.Put(name, job, 0, 24);
            name.SizeChanged += (_, _) => label.Width = job.Width = Math.Max(1, name.Width - 3);
            list.Controls.Add(name, 1, i);
            var stage = EditAction(candidate.Stage, "", 100, false, () => EditCandidate(candidate));
            stage.Font = AuroraStyle.Font(8, true);
            stage.Height = 27;
            stage.Margin = new(0, 9, 0, 0);
            stage.AccessibleName = $"{candidate.Name}, {candidate.Stage}. Alterar etapa";
            list.Controls.Add(stage, 2, i);
        }
        return box;
    }

    public void ExportPreviews(string directory)
    {
        // Usa a janela real para que as imagens incluam o desenho e a disposição dos controles do Windows Forms.
        Directory.CreateDirectory(directory);
        ShowInTaskbar = false;
        Opacity = 0;
        Show();
        foreach (var item in Pages)
        {
            Navigate(item.Id);
            PerformLayout();
            Application.DoEvents();
            using var shot = new Bitmap(ClientSize.Width, ClientSize.Height);
            root.DrawToBitmap(shot, root.ClientRectangle);
            shot.Save(Path.Combine(directory, $"Aurora-{item.Id}.png"), ImageFormat.Png);
        }
    }
}
