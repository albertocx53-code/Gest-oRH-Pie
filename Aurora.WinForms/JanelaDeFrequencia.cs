using Aurora.Core;

namespace Aurora.WinForms;

// JanelaDeFrequencia - Clóvis (ponto e frequência)
//
// Janela "Frequência por período", aberta pelo botão Consultar período.
// Ela só mostra dados. Quem busca e soma é o serviço (FrequenciaDePonto.ConsultarPeriodo
// e ResumoDoPeriodo). Por isso o resumo e a tabela sempre usam os mesmos filtros.
// O visual segue o JanelaDeCadastro e a JanelaDaFicha: fundo escuro, título grande e rótulos em cinza.
//
// Em cima ficam as datas (De e Até) e os filtros de funcionário e ocorrência. Logo abaixo, uma
// linha de resumo. No meio, os registros do período. Embaixo, os totais de cada funcionário.
// Qualquer mudança nos filtros atualiza tudo na hora. Se alguma regra falhar (por exemplo, um
// período de mais de 366 dias), a mensagem aparece no lugar do resumo.
internal sealed class JanelaDeFrequencia : Form
{
    private readonly ServicoRh rh;
    private readonly Funcionario[] equipe;
    private readonly CampoDeData de;
    private readonly CampoDeData ate;
    private readonly ComboBox pessoa;
    private readonly ComboBox ocorrencia;
    private readonly Label resumo;
    private readonly DataGridView registros;
    private readonly DataGridView porFuncionario;

    public JanelaDeFrequencia(ServicoRh rh, DateTime hoje)
    {
        this.rh = rh;
        equipe = rh.Funcionarios.OrderBy(item => item.Nome, StringComparer.CurrentCulture).ToArray();
        Text = "Frequência por período";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Estilo.CorDoFundo;

        Estilo.Colocar(this, Estilo.CriarRotulo("Frequência por período", 18, true), 18, 13);
        Estilo.Colocar(this, Estilo.CriarRotulo($"Presenças, atrasos e faltas de um intervalo de até {FrequenciaDePonto.MaximoDeDias} dias.", 9, false, Estilo.CorApagada), 20, 49);

        // Filtros. As datas começam no primeiro dia do mês de "hoje" e vão até hoje.
        Estilo.Colocar(this, Estilo.CriarRotulo("De", 9, true), 20, 82);
        Estilo.Colocar(this, Estilo.CriarRotulo("Até", 9, true), 160, 82);
        Estilo.Colocar(this, Estilo.CriarRotulo("Funcionário", 9, true), 300, 82);
        Estilo.Colocar(this, Estilo.CriarRotulo("Ocorrência", 9, true), 560, 82);
        de = new CampoDeData(new DateTime(hoje.Year, hoje.Month, 1), false) { Width = 120, AccessibleName = "De" };
        ate = new CampoDeData(hoje, false) { Width = 120, AccessibleName = "Até" };
        pessoa = Lista(equipe.Select(item => $"{item.Nome} · {item.Id}").Prepend("Todos os funcionários"), 240, "Funcionário");
        ocorrencia = Lista(Opcoes.SituacoesDoPonto.Prepend("Todas as ocorrências"), 160, "Ocorrência");
        Estilo.Colocar(this, de, 20, 102);
        Estilo.Colocar(this, ate, 160, 102);
        Estilo.Colocar(this, pessoa, 300, 102);
        Estilo.Colocar(this, ocorrencia, 560, 102);

        resumo = Estilo.CriarRotulo("", 10, true);
        resumo.AutoSize = false;
        resumo.AutoEllipsis = true;
        resumo.Size = new(860, 22);
        resumo.AccessibleName = "Resumo do período";
        Estilo.Colocar(this, resumo, 20, 142);

        Estilo.Colocar(this, Estilo.CriarRotulo("REGISTROS DO PERÍODO", 8, true, Estilo.CorDeDestaque), 20, 176);
        registros = Tabela("Registros do período", 224, ("Funcionário", 140), ("Data", 72), ("Entrada", 52), ("Saída", 52), ("Horas", 62), ("Situação", 68), ("Motivo da ocorrência", 170));
        Estilo.Colocar(this, registros, 20, 198);

        Estilo.Colocar(this, Estilo.CriarRotulo("RESUMO POR FUNCIONÁRIO", 8, true, Estilo.CorDeDestaque), 20, 434);
        porFuncionario = Tabela("Resumo por funcionário", 160, ("Funcionário", 170), ("Matrícula", 60), ("Presenças", 62), ("Atrasos", 56), ("Faltas", 52), ("Horas trabalhadas", 90));
        Estilo.Colocar(this, porFuncionario, 20, 456);

        // Fechar responde tanto ao Enter quanto ao Esc.
        var fechar = new Botao("Fechar") { Size = new(93, 32), Principal = true, DialogResult = DialogResult.OK };
        Estilo.Colocar(this, fechar, 787, 630);
        ClientSize = new(900, 676);
        AcceptButton = fechar;
        CancelButton = fechar;

        // Os eventos só entram depois de tudo montado, para a primeira consulta rodar uma vez só.
        de.ValorMudou += (_, _) => Atualizar();
        ate.ValorMudou += (_, _) => Atualizar();
        pessoa.SelectedIndexChanged += (_, _) => Atualizar();
        ocorrencia.SelectedIndexChanged += (_, _) => Atualizar();
        Atualizar();
    }

    // Limpa as duas tabelas e monta tudo de novo com os filtros atuais.
    private void Atualizar()
    {
        registros.Rows.Clear();
        porFuncionario.Rows.Clear();
        resumo.ForeColor = Estilo.CorDoTexto;
        try
        {
            int? matricula = pessoa.SelectedIndex > 0 ? equipe[pessoa.SelectedIndex - 1].Id : null;
            string? situacao = ocorrencia.SelectedIndex > 0 ? (string)ocorrencia.SelectedItem! : null;
            var lista = rh.ConsultarPeriodo(de.Valor, ate.Valor, matricula, situacao);
            var totais = rh.ResumoDoPeriodo(de.Valor, ate.Valor, matricula, situacao);
            var nomes = equipe.ToDictionary(item => item.Id, item => item.Nome);

            foreach (var item in lista)
            {
                var motivo = item.Motivo.Length > 0 ? item.Motivo.ReplaceLineEndings(" ") : RegistroDePonto.AceitaMotivo(item.Situacao) ? "Não informado" : "—";
                var horas = FrequenciaDePonto.HorasTrabalhadas(item);
                var linha = registros.Rows.Add(
                    nomes.GetValueOrDefault(item.Matricula, $"Matrícula {item.Matricula}"),
                    item.Dia.ToString("dd/MM/yyyy"),
                    item.Entrada.Length > 0 ? item.Entrada : "—",
                    item.Saida.Length > 0 ? item.Saida : "—",
                    horas > TimeSpan.Zero ? FrequenciaDePonto.FormatarHoras(horas) : "—",
                    item.Situacao,
                    motivo);
                registros.Rows[linha].Cells[6].ToolTipText = item.Motivo;
            }
            foreach (var item in totais.PorFuncionario)
                porFuncionario.Rows.Add(item.Nome, item.Matricula.ToString(), item.Presencas, item.Atrasos, item.Faltas, FrequenciaDePonto.FormatarHoras(item.Horas));

            resumo.Text = totais.Registros == 0
                ? "Nenhum registro neste período"
                : $"{Plural(totais.Registros, "registro", "registros")}  ·  {Plural(totais.Presencas, "presença", "presenças")}  ·  {Plural(totais.Atrasos, "atraso", "atrasos")}  ·  {Plural(totais.Faltas, "falta", "faltas")}  ·  {FrequenciaDePonto.FormatarHoras(totais.Horas)}  ·  comparecimento de {totais.Comparecimento:P0}";
        }
        catch (ErroDeRegra problema)
        {
            resumo.ForeColor = Estilo.CorDeErro;
            resumo.Text = problema.Message;
        }
    }

    private static string Plural(int quantidade, string singular, string plural) => $"{quantidade} {(quantidade == 1 ? singular : plural)}";

    // Mesmo estilo das listas das outras telas (Choice, em TelaPrincipal.Modulos.cs). Fica aqui
    // porque aquele método é privado do TelaPrincipal e eu não quis alterar o arquivo do grupo.
    private static ComboBox Lista(IEnumerable<string> itens, int largura, string nome)
    {
        var lista = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList, Font = Estilo.Fonte(10), AccessibleName = nome,
            BackColor = Estilo.CorDoCampo, ForeColor = Estilo.CorDoTexto, FlatStyle = FlatStyle.Flat, Width = largura
        };
        lista.Items.AddRange(itens.Cast<object>().ToArray());
        lista.SelectedIndex = 0;
        return lista;
    }

    // Mesmo estilo da Grid do TelaPrincipal, com linhas mais baixas para caber mais registros.
    private static DataGridView Tabela(string nome, int altura, params (string Titulo, int Peso)[] colunas)
    {
        var tabela = new TabelaDeDados
        {
            AccessibleName = nome, Size = new(860, altura),
            ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
            BackgroundColor = Estilo.CorDoFundo, BorderStyle = BorderStyle.None, GridColor = Estilo.CorDaBorda,
            EnableHeadersVisualStyles = false, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None, ColumnHeadersHeight = 34,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            Font = Estilo.Fonte(9.5f), RowTemplate = { Height = 32 },
            DefaultCellStyle = new()
            {
                BackColor = Estilo.CorDoFundo, ForeColor = Estilo.CorDoTexto, SelectionBackColor = Estilo.CorClara,
                SelectionForeColor = Estilo.CorDoTexto, Padding = new(9, 0, 2, 0), NullValue = "—"
            },
            ColumnHeadersDefaultCellStyle = new()
            {
                Font = Estilo.Fonte(9, true), BackColor = Estilo.CorDoCampo, ForeColor = Estilo.CorDoTexto,
                SelectionBackColor = Estilo.CorDoCampo, SelectionForeColor = Estilo.CorDoTexto,
                Padding = new(9, 0, 2, 0)
            }
        };
        foreach (var (titulo, peso) in colunas)
            tabela.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = titulo, FillWeight = peso, SortMode = DataGridViewColumnSortMode.NotSortable });
        return tabela;
    }
}