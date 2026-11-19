using System.Globalization;
using Aurora.Core;

namespace Aurora.WinForms;

internal sealed partial class AuroraForm
{
    private Panel Module(string title, string description, AuroraButton? action = null)
    {
        var panel = Page();
        panel.Width = content.ClientSize.Width;
        Heading(panel, title, description, action);
        return panel;
    }

    private static void PutWide(Panel parent, Control control, int y, int height)
    {
        control.Bounds = new(0, y, parent.Width, height);
        control.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        parent.Controls.Add(control);
    }

    private static DataGridView Grid(params (string Label, int Weight)[] columns)
    {
        // Os módulos compartilham esse estilo. Cada linha guarda seu registro em Tag para abrir o formulário certo.
        var grid = new RoundedGrid
        {
            ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
            BackgroundColor = AuroraStyle.Paper, BorderStyle = BorderStyle.None, GridColor = AuroraStyle.Border,
            EnableHeadersVisualStyles = false, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None, ColumnHeadersHeight = 40,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            Font = AuroraStyle.Font(10), RowTemplate = { Height = 43 },
            DefaultCellStyle = new()
            {
                BackColor = AuroraStyle.Paper, ForeColor = AuroraStyle.Ink, SelectionBackColor = AuroraStyle.Tint,
                SelectionForeColor = AuroraStyle.Ink, Padding = new(9, 0, 2, 0), NullValue = "—"
            },
            ColumnHeadersDefaultCellStyle = new()
            {
                Font = AuroraStyle.Font(9, true), BackColor = AuroraStyle.Field, ForeColor = AuroraStyle.Ink,
                SelectionBackColor = AuroraStyle.Field, SelectionForeColor = AuroraStyle.Ink,
                Padding = new(9, 0, 2, 0)
            }
        };
        foreach (var (label, weight) in columns)
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = label, FillWeight = weight, SortMode = DataGridViewColumnSortMode.NotSortable });
        return grid;
    }

    private static void Row(DataGridView grid, object model, params object[] cells)
        => grid.Rows[grid.Rows.Add(cells)].Tag = model;

    private static void PlaceGrid(Panel parent, DataGridView grid, int y, int height)
    {
        var card = Card();
        PutWide(parent, card, y, height);
        grid.Bounds = new(10, 9, card.Width - 20, card.Height - 18);
        grid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        card.Controls.Add(grid);
    }

    private static ComboBox Choice(IEnumerable<string> values, string selected, string accessibleName = "")
    {
        var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = AuroraStyle.Font(10), AccessibleName = accessibleName, BackColor = AuroraStyle.Field, ForeColor = AuroraStyle.Ink, FlatStyle = FlatStyle.Flat };
        box.Items.AddRange(values.Cast<object>().ToArray());
        box.SelectedItem = selected;
        if (box.SelectedIndex < 0 && box.Items.Count > 0) box.SelectedIndex = 0;
        return box;
    }

    private static RhDateInput DateField(DateTime date, bool month = false) => new(date, month);

    private static TextBox TextField(string value = "", string placeholder = "") => new()
    {
        Text = value, PlaceholderText = placeholder, Font = AuroraStyle.Font(10), MaxLength = 120, BackColor = AuroraStyle.Field, ForeColor = AuroraStyle.Ink
    };

    private static string Chosen(ComboBox box) => (string?)box.SelectedItem ?? "";
    private static bool Matches(string item, string query) => CultureInfo.CurrentCulture.CompareInfo.IndexOf(item, query.Trim(), CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

    private Panel PeoplePage()
    {
        var panel = Module("Funcionários", "Consulte o cadastro por nome, cargo ou departamento.", EditAction("Novo funcionário", "plus", 169, true, () => EditEmployee(null)));
        var tools = Card(14);
        PutWide(panel, tools, 104, 66);
        AuroraStyle.Put(tools, AuroraStyle.Label("Buscar por nome ou cargo", 8.5f, false, AuroraStyle.Muted), 14, 6);
        AuroraStyle.Put(tools, AuroraStyle.Label("Departamento", 8.5f, false, AuroraStyle.Muted), 257, 6);
        AuroraStyle.Put(tools, AuroraStyle.Label("Situação", 8.5f, false, AuroraStyle.Muted), 441, 6);
        var search = TextField();
        search.AccessibleName = "Buscar por nome ou cargo";
        search.Width = 232;
        AuroraStyle.Put(tools, search, 14, 26);
        var team = Choice(RhOptions.Teams.Prepend("Todos os departamentos"), "Todos os departamentos", "Departamento");
        team.Width = 173;
        AuroraStyle.Put(tools, team, 257, 26);
        var state = Choice(RhOptions.EmployeeStatuses.Prepend("Todas as situações"), "Todas as situações", "Situação");
        state.Width = 144;
        AuroraStyle.Put(tools, state, 441, 26);
        var grid = Grid(("Funcionário", 125), ("Cargo", 113), ("Departamento", 87), ("Situação", 65), ("Admissão", 67));
        grid.AccessibleName = "Cadastro de funcionários";
        PlaceGrid(panel, grid, 183, 406);
        var count = AuroraStyle.Label("", 9, false, AuroraStyle.Muted);
        AuroraStyle.Put(panel, count, 5, 598);
        void Edit() { if (data.CanEdit && grid.CurrentRow?.Tag is Employee item) EditEmployee(item); }
        var edit = EditAction("Editar cadastro", "", 132, false, Edit);
        var remove = EditAction("Excluir", "", 73, false, () => { if (grid.CurrentRow?.Tag is Employee item) RemoveEmployee(item); });
        AuroraStyle.Put(panel, edit, 4, 625);
        AuroraStyle.Put(panel, remove, 146, 625);
        grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Edit(); };
        void Filter()
        {
            grid.Rows.Clear();
            foreach (var person in data.Employees.Where(item => Matches($"{item.Name} {item.Role}", search.Text)
                && (team.SelectedIndex == 0 || item.Team == Chosen(team)) && (state.SelectedIndex == 0 || item.Status == Chosen(state))))
                Row(grid, person, person.Name, person.Role, person.Team, person.Status, person.Started.ToString("dd/MM/yyyy"));
            count.Text = grid.RowCount == 0 ? "Nenhum funcionário corresponde aos filtros." : $"{grid.RowCount} {(grid.RowCount == 1 ? "funcionário" : "funcionários")}";
            edit.Enabled = remove.Enabled = data.CanEdit && grid.RowCount > 0;
        }
        search.TextChanged += (_, _) => Filter();
        team.SelectedIndexChanged += (_, _) => Filter();
        state.SelectedIndexChanged += (_, _) => Filter();
        Filter();
        return panel;
    }

    private void EditEmployee(Employee? person)
    {
        var name = TextField(person?.Name ?? "");
        var role = TextField(person?.Role ?? "");
        var team = Choice(RhOptions.Teams, person?.Team ?? "Pessoas");
        var state = Choice(RhOptions.EmployeeStatuses, person?.Status ?? "Ativo");
        var start = DateField(person?.Started ?? DemoRh.Today);
        using var form = new EntryForm(person is null ? "Novo funcionário" : "Editar funcionário",
            [("Nome", name), ("Cargo", role), ("Departamento", team), ("Situação", state), ("Admissão", start)],
            () => data.SaveEmployee(new(person?.Id ?? 0, name.Text, role.Text, Chosen(team), Chosen(state), start.Value)));
        if (form.ShowDialog(this) != DialogResult.OK) return;
        Navigate("people");
        Status("Cadastro salvo");
    }

    private void RemoveEmployee(Employee person)
    {
        if (MessageBox.Show(this, $"Excluir o cadastro de {person.Name}?", "Excluir funcionário", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
        try { data.DeleteEmployee(person.Id); }
        catch (RhRuleException issue) { MessageBox.Show(this, issue.Message, "Não foi possível excluir", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        Navigate("people");
        Status("Cadastro excluído");
    }

    // Clóvis: tela de Ponto e frequência. Em cima ficam o dia consultado e o resumo do dia
    // (entradas, atrasos e faltas). No meio, a tabela dos registros desse dia. Embaixo, os botões.
    // Cada linha da tabela guarda o registro de ponto no Tag, e é por ele que Editar registro sabe qual abrir.
    // O que eu acrescentei (Lançar falta, Lançar atraso e Consultar período) está em AuroraForm.Ponto.cs.
    private Panel TimePage()
    {
        // O filtro atual consulta um dia. AttendanceBetween também atende futuras consultas por período.
        var panel = Module("Ponto e frequência", "Acompanhe horários, faltas e atrasos por dia.", EditAction("Registrar ponto", "plus", 148, true, () => EditPunch()));
        var tools = Card(14);
        PutWide(panel, tools, 104, 66);
        var date = DateField(punchDay);
        date.AccessibleName = "Dia do registro";
        date.Width = 145;
        AuroraStyle.Put(tools, date, 14, 20);
        var summary = AuroraStyle.Label("", 10, true);
        AuroraStyle.Put(tools, summary, 180, 21);
        var grid = Grid(("Funcionário", 135), ("Data", 76), ("Entrada", 54), ("Saída", 54), ("Situação", 76), ("Motivo da ocorrência", 170));
        grid.AccessibleName = "Registros de ponto";
        PlaceGrid(panel, grid, 184, 366);

        void EditSelected()
        {
            if (data.CanEdit && grid.CurrentRow?.Tag is Attendance selected) EditPunch(selected);
        }

        var edit = EditAction("Editar registro", "", 130, false, EditSelected);
        AuroraStyle.Put(panel, edit, 3, 566);
        grid.SelectionChanged += (_, _) => edit.Enabled = data.CanEdit && grid.CurrentRow?.Tag is Attendance;
        grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditSelected(); };

        void Filter()
        {
            grid.Rows.Clear();
            punchDay = date.Value.Date;
            var list = data.AttendanceBetween(punchDay, punchDay);
            foreach (var item in list)
            {
                var reason = item.Reason.Length > 0 ? item.Reason.ReplaceLineEndings(" ") : Attendance.AcceptsReason(item.Status) ? "Não informado" : "—";
                Row(grid, item, data.Name(item.Person), item.Day.ToString("dd/MM/yyyy"), item.In.Length > 0 ? item.In : "—", item.Out.Length > 0 ? item.Out : "—", item.Status, reason);
                grid.Rows[grid.RowCount - 1].Cells[5].ToolTipText = item.Reason;
            }
            edit.Enabled = data.CanEdit && grid.CurrentRow?.Tag is Attendance;
            var entries = list.Count(item => item.In.Length > 0);
            var late = list.Count(item => item.Status == "Atraso");
            var absent = list.Count(item => item.Status == "Falta");
            summary.Text = list.Count == 0 ? "Nenhum registro neste dia" : $"{entries} {(entries == 1 ? "entrada" : "entradas")}  ·  {late} {(late == 1 ? "atraso" : "atrasos")}  ·  {absent} {(absent == 1 ? "falta" : "faltas")}";
        }
        date.ValueChanged += (_, _) => Filter();
        Filter();
        // Parte do Clóvis: botões Lançar falta, Lançar atraso e Consultar período (AuroraForm.Ponto.cs).
        FerramentasDoPonto(panel);
        return panel;
    }

    private void EditPunch(Attendance? original = null)
    {
        // Um registro antigo continua editável se a pessoa estiver afastada ou inativa hoje.
        var staff = data.Employees.Where(item => original is null ? item.Status == "Ativo" : item.Id == original.Person).ToArray();
        if (staff.Length == 0) { Status("Cadastre um funcionário ativo primeiro"); return; }
        var person = Choice(staff.Select(item => $"{item.Name} · {item.Id}"), "");
        var day = DateField(original?.Day ?? punchDay);
        person.Enabled = day.Enabled = original is null;
        var previousEntry = string.IsNullOrEmpty(original?.In) ? "08:00" : original.In;
        var previousExit = string.IsNullOrEmpty(original?.Out) ? "17:00" : original.Out;
        var entry = TextField(previousEntry);
        var exit = TextField(previousExit);
        var state = Choice(RhOptions.AttendanceStatuses, original?.Status ?? "Presença");
        var reason = TextField(original?.Reason ?? "", "Opcional para falta ou atraso");
        reason.Multiline = true;
        reason.Height = 60;
        reason.MaxLength = 240;
        reason.ScrollBars = ScrollBars.Vertical;
        reason.AccessibleDescription = "Campo opcional para faltas e atrasos, com até 240 caracteres.";

        void UpdateFields()
        {
            var status = Chosen(state);
            var showHours = status != "Falta";
            if (showHours != entry.Enabled)
            {
                // Guarde o que foi digitado se a pessoa mudar a situação e voltar atrás.
                if (!showHours) (previousEntry, previousExit) = (entry.Text, exit.Text);
                entry.Text = showHours ? previousEntry : "";
                exit.Text = showHours ? previousExit : "";
            }
            entry.Enabled = exit.Enabled = showHours;
            reason.Enabled = Attendance.AcceptsReason(status);
        }

        state.SelectedIndexChanged += (_, _) => UpdateFields();
        UpdateFields();
        using var form = new EntryForm(original is null ? "Registrar ponto" : "Editar registro de ponto",
            [("Funcionário", person), ("Data", day), ("Entrada (HH:mm)", entry), ("Saída (HH:mm)", exit), ("Situação", state), ("Motivo da ocorrência (opcional)", reason)],
            () => data.SaveAttendance(new(staff[person.SelectedIndex].Id, day.Value, entry.Text, exit.Text, Chosen(state), reason.Text), original is not null));
        if (form.ShowDialog(this) != DialogResult.OK) return;
        punchDay = day.Value.Date;
        Navigate("time");
        Status("Ponto registrado");
    }

    private Panel HirePage()
    {
        var panel = Module("Recrutamento", "Organize vagas e acompanhe cada candidatura.", EditAction("Novo candidato", "plus", 147, true, () => EditCandidate(null)));
        var jobs = Grid(("Vaga", 160), ("Departamento", 90), ("Situação", 70), ("Candidaturas", 70));
        jobs.AccessibleName = "Vagas de recrutamento";
        foreach (var item in data.Vacancies) Row(jobs, item, item.Title, item.Team, item.Status, data.Candidates.Count(c => c.Vacancy == item.Id));
        PlaceGrid(panel, jobs, 105, 189);
        AuroraStyle.Put(panel, EditAction("Nova vaga", "plus", 110, false, AddVacancy), 2, 305);
        AuroraStyle.Put(panel, EditAction("Encerrar ou reabrir vaga", "", 169, false, () =>
        {
            if (jobs.CurrentRow?.Tag is not Vacancy item) return;
            try { data.ToggleVacancy(item.Id); }
            catch (RhRuleException issue) { Status(issue.Message); return; }
            Navigate("hire");
            Status("Situação da vaga atualizada");
        }), 122, 305);
        AuroraStyle.Put(panel, AuroraStyle.Label("Candidaturas", 13, true), 3, 355);
        var candidates = Grid(("Candidato", 122), ("Vaga", 140), ("Etapa", 85));
        candidates.AccessibleName = "Lista de candidaturas";
        foreach (var item in data.Candidates) Row(candidates, item, item.Name, data.VacancyTitle(item.Vacancy), item.Stage);
        PlaceGrid(panel, candidates, 388, 231);
        void Edit() { if (data.CanEdit && candidates.CurrentRow?.Tag is Candidate item) EditCandidate(item); }
        AuroraStyle.Put(panel, EditAction("Alterar etapa", "", 115, false, Edit), 2, 629);
        candidates.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Edit(); };
        return panel;
    }

    private void AddVacancy()
    {
        var title = TextField();
        var team = Choice(RhOptions.Teams, "Pessoas");
        using var form = new EntryForm("Nova vaga", [("Título da vaga", title), ("Departamento", team)],
            () => data.AddVacancy(title.Text, Chosen(team)));
        if (form.ShowDialog(this) != DialogResult.OK) return;
        Navigate("hire");
        Status("Vaga cadastrada");
    }

    private void EditCandidate(Candidate? candidate)
    {
        var jobs = data.Vacancies.Where(item => item.Status == "Aberta" || item.Id == candidate?.Vacancy).ToArray();
        if (jobs.Length == 0) { Status("Abra uma vaga para cadastrar candidatos"); return; }
        var titles = jobs.Select(item => $"{item.Title} · {item.Id}").ToArray();
        var selected = Array.FindIndex(jobs, item => item.Id == candidate?.Vacancy);
        var name = TextField(candidate?.Name ?? "");
        var vacancy = Choice(titles, titles[Math.Max(0, selected)]);
        var stage = Choice(RhOptions.Stages, candidate?.Stage ?? "Triagem");
        using var form = new EntryForm(candidate is null ? "Novo candidato" : "Editar candidatura", [("Nome", name), ("Vaga", vacancy), ("Etapa", stage)],
            () => data.SaveCandidate(new(candidate?.Id ?? 0, name.Text, jobs[vacancy.SelectedIndex].Id, Chosen(stage))));
        if (form.ShowDialog(this) != DialogResult.OK) return;
        Navigate(page);
        Status("Candidatura salva");
    }

    private Panel CalendarPage()
    {
        var panel = Module("Calendário", "Entrevistas, admissões e compromissos da equipe.", EditAction("Novo compromisso", "plus", 180, true, AddAppointment));
        var card = Card();
        PutWide(panel, card, 105, 505);
        var label = AuroraStyle.Label("", 13, true);
        const int detailX = 310;
        AuroraStyle.Put(card, label, detailX, 17);
        var list = Grid(("Horário", 56), ("Compromisso", 165), ("Categoria", 82));
        list.AccessibleName = "Compromissos do dia";
        list.Bounds = new(detailX, 54, card.Width - detailX - 14, 434);
        list.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        card.Controls.Add(list);
        const int columns = 7;
        const int cellWidth = 35;
        const int columnStep = 39;
        const int calendarWidth = (columns - 1) * columnStep + cellWidth;
        const int arrowSize = 28;
        const int arrowGap = 4;
        var calendar = new Panel { Size = new(calendarWidth, 293), BackColor = Color.Transparent };
        AuroraStyle.Put(card, calendar, 17, 17);
        void Refresh()
        {
            // Só marcamos no calendário os compromissos que RhService liberou para a conta atual.
            var day = agendaDay;
            var month = new DateTime(day.Year, day.Month, 1);
            var first = month.AddDays(-((int)month.DayOfWeek + 6) % 7);
            var visible = data.Agenda;
            foreach (var control in calendar.Controls.Cast<Control>().ToArray()) control.Dispose();
            AuroraStyle.Put(calendar, AuroraStyle.Label(month.ToString("MMMM yyyy"), 11, true), 0, 6);
            void Move(int offset)
            {
                agendaDay = month.AddMonths(offset);
                Refresh();
                calendar.Controls.OfType<AuroraButton>().First(button => button.Glyph == (offset < 0 ? "prev" : "next")).Focus();
            }
            var back = Action("", "prev", arrowSize, false, () => Move(-1));
            var next = Action("", "next", arrowSize, false, () => Move(1));
            back.Height = next.Height = arrowSize;
            back.AccessibleName = "Mês anterior";
            next.AccessibleName = "Próximo mês";
            AuroraStyle.Put(calendar, back, calendarWidth - 2 * arrowSize - arrowGap, 0);
            AuroraStyle.Put(calendar, next, calendarWidth - arrowSize, 0);
            var weekdays = new[] { "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb", "Dom" };
            for (var i = 0; i < columns; i++)
            {
                var dayLabel = AuroraStyle.Label(weekdays[i], 8, true, AuroraStyle.Muted);
                dayLabel.AutoSize = false;
                dayLabel.TextAlign = ContentAlignment.MiddleCenter;
                dayLabel.Size = new(cellWidth, 22);
                AuroraStyle.Put(calendar, dayLabel, i * columnStep, 36);
            }
            for (var i = 0; i < 6 * columns; i++)
            {
                var date = first.AddDays(i);
                var hasEvent = visible.Any(item => item.Time.Date == date);
                var key = new AuroraButton(date.Day.ToString()) { Primary = date == day, CalendarDay = true, Marker = hasEvent, Size = new(cellWidth, 30), Tag = date };
                key.Click += (_, _) =>
                {
                    agendaDay = date;
                    Refresh();
                    calendar.Controls.OfType<AuroraButton>().First(button => Equals(button.Tag, date)).Focus();
                };
                key.Font = AuroraStyle.Font(9, hasEvent || date == day);
                key.AccessibleName = $"{date:dd/MM/yyyy}{(hasEvent ? ", com compromissos" : "")}";
                key.Enabled = date.Month == month.Month;
                AuroraStyle.Put(calendar, key, i % columns * columnStep, 59 + i / columns * 35);
            }
            list.Rows.Clear();
            foreach (var item in visible.Where(item => item.Time.Date == day).OrderBy(item => item.Time))
                Row(list, item, item.Time.ToString("HH:mm"), item.Title, item.Category);
            label.Text = $"{day:dd/MM/yyyy} · {(list.RowCount == 0 ? "Nenhum compromisso" : $"{list.RowCount} {(list.RowCount == 1 ? "compromisso" : "compromissos")}")}";
        }
        Refresh();
        return panel;
    }

    private void AddAppointment()
    {
        var title = TextField();
        var day = DateField(agendaDay);
        var time = TextField("09:00");
        var category = Choice(RhOptions.AgendaCategories, "Funcionários");
        using var form = new EntryForm("Novo compromisso", [("Título", title), ("Data", day), ("Horário (HH:mm)", time), ("Categoria", category)],
            () => data.AddAppointment(title.Text, day.Value, time.Text, Chosen(category)));
        if (form.ShowDialog(this) != DialogResult.OK) return;
        agendaDay = day.Value.Date;
        Navigate("calendar");
        Status("Compromisso salvo");
    }

    private Panel PayPage()
    {
        var panel = Module("Salários", "Consulte a folha por competência e lance valores simulados.", Action("Novo lançamento", "plus", 154, true, () => AddPay()));
        var card = Card(14);
        PutWide(panel, card, 104, 66);
        var month = DateField(payrollMonth, true);
        month.AccessibleName = "Competência da folha";
        month.Width = 124;
        AuroraStyle.Put(card, month, 14, 20);
        var total = AuroraStyle.Label("", 10, true);
        AuroraStyle.Put(card, total, 158, 21);
        var grid = Grid(("Funcionário", 132), ("Salário", 82), ("Bônus", 70), ("Descontos", 76), ("Total simulado", 92));
        grid.AccessibleName = "Lançamentos salariais";
        PlaceGrid(panel, grid, 184, 406);
        // Diaz: editar um lançamento existente (antes só dava para digitar tudo de novo em "Novo lançamento").
        void Edit() { if (data.CanEdit && grid.CurrentRow?.Tag is Payroll item) AddPay(item); }
        var edit = EditAction("Editar lançamento", "", 160, false, Edit);
        AuroraStyle.Put(panel, edit, 4, 625);
        grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Edit(); };
        void Filter()
        {
            payrollMonth = month.Value;
            grid.Rows.Clear();
            var entries = data.PayrollFor(month.Value);
            foreach (var item in entries) Row(grid, item, data.Name(item.Person), item.Base.ToString("C"), item.Bonus.ToString("C"), item.Discount.ToString("C"), item.Total.ToString("C"));
            total.Text = entries.Count == 0 ? "Nenhum lançamento nesta competência" : $"Total simulado: {entries.Sum(item => item.Total):C}";
            edit.Enabled = data.CanEdit && grid.RowCount > 0;
        }
        month.ValueChanged += (_, _) => Filter();
        Filter();
        return panel;
    }

    // entry nulo = novo lançamento. Com entry, o funcionário e a competência (a chave do lançamento) ficam travados.
    private void AddPay(Payroll? entry = null)
    {
        if (!CanViewPay) return;
        var staff = data.Employees.ToArray();
        if (staff.Length == 0) { Status("Cadastre um funcionário primeiro"); return; }
        var labels = staff.Select(item => $"{item.Name} · {item.Id}").ToArray();
        var index = entry is null ? -1 : Array.FindIndex(staff, item => item.Id == entry.Person);
        var person = Choice(labels, index >= 0 ? labels[index] : "");
        var month = DateField(entry?.Month ?? payrollMonth, true);
        if (entry is not null) person.Enabled = month.Enabled = false;
        static NumericUpDown Amount(decimal initial) => new() { DecimalPlaces = 2, Maximum = 1_000_000, ThousandsSeparator = true, Font = AuroraStyle.Font(10), Value = initial };
        var value = Amount(entry?.Base ?? 0);
        var bonus = Amount(entry?.Bonus ?? 0);
        var discount = Amount(entry?.Discount ?? 0);
        var title = entry is null ? "Novo lançamento" : "Editar lançamento";
        var note = entry is null
            ? "Um lançamento por funcionário e competência. Confirmar atualiza o valor existente."
            : "Funcionário e competência não mudam na edição. Altere os valores e confirme.";
        using var form = new EntryForm(title, [("Funcionário", person), ("Competência", month), ("Salário (R$)", value), ("Bônus (R$)", bonus), ("Descontos (R$)", discount)],
            () => data.SavePayroll(new(staff[person.SelectedIndex].Id, month.Value, value.Value, bonus.Value, discount.Value)),
            note);
        if (form.ShowDialog(this) != DialogResult.OK) return;
        payrollMonth = month.Value;
        Navigate("pay");
        Status("Lançamento salvo");
    }

}
