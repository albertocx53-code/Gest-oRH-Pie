using Aurora.Core;

namespace Aurora.WinForms;

// FrequenciaPorPeriodoForm - Clóvis (ponto e frequência)
//
// Janela "Frequência por período", aberta pelo botão Consultar período.
// Ela só mostra dados. Quem busca e soma é o serviço (FrequenciaDePonto.ConsultarPeriodo
// e ResumoDoPeriodo). Por isso o resumo e a tabela sempre usam os mesmos filtros.
// O visual segue o EntryForm e a FichaDoFuncionarioForm: fundo escuro, título grande e rótulos em cinza.
//
// Em cima ficam as datas (De e Até) e os filtros de funcionário e ocorrência. Logo abaixo, uma
// linha de resumo. No meio, os registros do período. Embaixo, os totais de cada funcionário.
// Qualquer mudança nos filtros atualiza tudo na hora. Se alguma regra falhar (por exemplo, um
// período de mais de 366 dias), a mensagem aparece no lugar do resumo.
internal sealed class FrequenciaPorPeriodoForm : Form
{
    private readonly RhService rh;
    private readonly Employee[] equipe;
    private readonly RhDateInput de;
    private readonly RhDateInput ate;
    private readonly ComboBox pessoa;
    private readonly ComboBox ocorrencia;
    private readonly Label resumo;
    private readonly DataGridView registros;
    private readonly DataGridView porFuncionario;

    public FrequenciaPorPeriodoForm(RhService rh, DateTime hoje)
    {
        this.rh = rh;
        equipe = rh.Employees.OrderBy(item => item.Name, StringComparer.CurrentCulture).ToArray();
        Text = "Frequência por período";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = AuroraStyle.Paper;

        AuroraStyle.Put(this, AuroraStyle.Label("Frequência por período", 18, true), 18, 13);
        AuroraStyle.Put(this, AuroraStyle.Label($"Presenças, atrasos e faltas de um intervalo de até {FrequenciaDePonto.MaximoDeDias} dias.", 9, false, AuroraStyle.Muted), 20, 49);

        // Filtros. As datas começam no primeiro dia do mês de "hoje" e vão até hoje.
        AuroraStyle.Put(this, AuroraStyle.Label("De", 9, true), 20, 82);
        AuroraStyle.Put(this, AuroraStyle.Label("Até", 9, true), 160, 82);
        AuroraStyle.Put(this, AuroraStyle.Label("Funcionário", 9, true), 300, 82);
        AuroraStyle.Put(this, AuroraStyle.Label("Ocorrência", 9, true), 560, 82);
        de = new RhDateInput(new DateTime(hoje.Year, hoje.Month, 1), false) { Width = 120, AccessibleName = "De" };
        ate = new RhDateInput(hoje, false) { Width = 120, AccessibleName = "Até" };
        pessoa = Lista(equipe.Select(item => $"{item.Name} · {item.Id}").Prepend("Todos os funcionários"), 240, "Funcionário");
        ocorrencia = Lista(RhOptions.AttendanceStatuses.Prepend("Todas as ocorrências"), 160, "Ocorrência");
        AuroraStyle.Put(this, de, 20, 102);
        AuroraStyle.Put(this, ate, 160, 102);
        AuroraStyle.Put(this, pessoa, 300, 102);
        AuroraStyle.Put(this, ocorrencia, 560, 102);

        resumo = AuroraStyle.Label("", 10, true);
        resumo.AutoSize = false;
        resumo.AutoEllipsis = true;
        resumo.Size = new(860, 22);
        resumo.AccessibleName = "Resumo do período";
        AuroraStyle.Put(this, resumo, 20, 142);

        AuroraStyle.Put(this, AuroraStyle.Label("REGISTROS DO PERÍODO", 8, true, AuroraStyle.Accent), 20, 176);
        registros = Tabela("Registros do período", 224, ("Funcionário", 140), ("Data", 72), ("Entrada", 52), ("Saída", 52), ("Horas", 62), ("Situação", 68), ("Motivo da ocorrência", 170));
        AuroraStyle.Put(this, registros, 20, 198);

        AuroraStyle.Put(this, AuroraStyle.Label("RESUMO POR FUNCIONÁRIO", 8, true, AuroraStyle.Accent), 20, 434);
        porFuncionario = Tabela("Resumo por funcionário", 160, ("Funcionário", 170), ("Matrícula", 60), ("Presenças", 62), ("Atrasos", 56), ("Faltas", 52), ("Horas trabalhadas", 90));
        AuroraStyle.Put(this, porFuncionario, 20, 456);

        // Fechar responde tanto ao Enter quanto ao Esc.
        var fechar = new AuroraButton("Fechar") { Size = new(93, 32), Primary = true, DialogResult = DialogResult.OK };
        AuroraStyle.Put(this, fechar, 787, 630);
        ClientSize = new(900, 676);
        AcceptButton = fechar;
        CancelButton = fechar;

        // Os eventos só entram depois de tudo montado, para a primeira consulta rodar uma vez só.
        de.ValueChanged += (_, _) => Atualizar();
        ate.ValueChanged += (_, _) => Atualizar();
        pessoa.SelectedIndexChanged += (_, _) => Atualizar();
        ocorrencia.SelectedIndexChanged += (_, _) => Atualizar();
        Atualizar();
    }

    // Limpa as duas tabelas e monta tudo de novo com os filtros atuais.
    private void Atualizar()
    {
        registros.Rows.Clear();
        porFuncionario.Rows.Clear();
        resumo.ForeColor = AuroraStyle.Ink;
        try
        {
            int? matricula = pessoa.SelectedIndex > 0 ? equipe[pessoa.SelectedIndex - 1].Id : null;
            string? situacao = ocorrencia.SelectedIndex > 0 ? (string)ocorrencia.SelectedItem! : null;
            var lista = rh.ConsultarPeriodo(de.Value, ate.Value, matricula, situacao);
            var totais = rh.ResumoDoPeriodo(de.Value, ate.Value, matricula, situacao);
            var nomes = equipe.ToDictionary(item => item.Id, item => item.Name);

            foreach (var item in lista)
            {
                var motivo = item.Reason.Length > 0 ? item.Reason.ReplaceLineEndings(" ") : Attendance.AcceptsReason(item.Status) ? "Não informado" : "—";
                var horas = FrequenciaDePonto.HorasTrabalhadas(item);
                var linha = registros.Rows.Add(
                    nomes.GetValueOrDefault(item.Person, $"Matrícula {item.Person}"),
                    item.Day.ToString("dd/MM/yyyy"),
                    item.In.Length > 0 ? item.In : "—",
                    item.Out.Length > 0 ? item.Out : "—",
                    horas > TimeSpan.Zero ? FrequenciaDePonto.FormatarHoras(horas) : "—",
                    item.Status,
                    motivo);
                registros.Rows[linha].Cells[6].ToolTipText = item.Reason;
            }
            foreach (var item in totais.PorFuncionario)
                porFuncionario.Rows.Add(item.Nome, item.Matricula.ToString(), item.Presencas, item.Atrasos, item.Faltas, FrequenciaDePonto.FormatarHoras(item.Horas));

            resumo.Text = totais.Registros == 0
                ? "Nenhum registro neste período"
                : $"{Plural(totais.Registros, "registro", "registros")}  ·  {Plural(totais.Presencas, "presença", "presenças")}  ·  {Plural(totais.Atrasos, "atraso", "atrasos")}  ·  {Plural(totais.Faltas, "falta", "faltas")}  ·  {FrequenciaDePonto.FormatarHoras(totais.Horas)}  ·  comparecimento de {totais.Comparecimento:P0}";
        }
        catch (RhRuleException issue)
        {
            resumo.ForeColor = Color.LightPink;
            resumo.Text = issue.Message;
        }
    }

    private static string Plural(int quantidade, string singular, string plural) => $"{quantidade} {(quantidade == 1 ? singular : plural)}";

    // Mesmo estilo das listas das outras telas (Choice, em AuroraForm.Modules.cs). Fica aqui
    // porque aquele método é privado do AuroraForm e eu não quis alterar o arquivo do grupo.
    private static ComboBox Lista(IEnumerable<string> itens, int largura, string nome)
    {
        var lista = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList, Font = AuroraStyle.Font(10), AccessibleName = nome,
            BackColor = AuroraStyle.Field, ForeColor = AuroraStyle.Ink, FlatStyle = FlatStyle.Flat, Width = largura
        };
        lista.Items.AddRange(itens.Cast<object>().ToArray());
        lista.SelectedIndex = 0;
        return lista;
    }

    // Mesmo estilo da Grid do AuroraForm, com linhas mais baixas para caber mais registros.
    private static DataGridView Tabela(string nome, int altura, params (string Titulo, int Peso)[] colunas)
    {
        var tabela = new RoundedGrid
        {
            AccessibleName = nome, Size = new(860, altura),
            ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
            BackgroundColor = AuroraStyle.Paper, BorderStyle = BorderStyle.None, GridColor = AuroraStyle.Border,
            EnableHeadersVisualStyles = false, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None, ColumnHeadersHeight = 34,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            Font = AuroraStyle.Font(9.5f), RowTemplate = { Height = 32 },
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
        foreach (var (titulo, peso) in colunas)
            tabela.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = titulo, FillWeight = peso, SortMode = DataGridViewColumnSortMode.NotSortable });
        return tabela;
    }
}