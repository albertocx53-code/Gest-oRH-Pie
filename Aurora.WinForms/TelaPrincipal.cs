using System.Drawing.Drawing2D;
using Aurora.Core;

namespace Aurora.WinForms;
// Janela principal do sistema: o menu da esquerda e a área onde as telas aparecem.
// As telas de cada área ficam em TelaPrincipal.Modulos.cs (e nos arquivos de cada colega).
internal sealed partial class TelaPrincipal : Form
{
    // itens do menu: o id da tela e o texto do botão
    private static readonly (string Id, string Texto)[] Paginas =
    [
        ("home", "Painel"), ("people", "Funcionários"), ("time", "Ponto e frequência"),
        ("hire", "Recrutamento"), ("calendar", "Calendário"), ("pay", "Salários")
    ];
    private readonly ServicoRh dados;
    private readonly FundoDaTela raiz = new() { Dock = DockStyle.Fill };
    private readonly Panel conteudo = new() { BackColor = Color.Transparent, AutoScroll = true };
    private readonly Dictionary<string, Botao> menu = [];
    private readonly Label rotuloDoPerfil;
    private readonly Label rodape;
    private string paginaAtual = "home";
    private DateTime diaDoPonto = DadosDeExemplo.Hoje;
    private DateTime diaDaAgenda = DadosDeExemplo.Hoje;
    private DateTime mesDaFolha = DadosDeExemplo.Hoje;
    private bool PodeVerSalarios => dados.PodeVerSalarios;
    internal bool VoltarParaLogin { get; private set; }
    // monta o cabeçalho, o menu e o rodapé
    public TelaPrincipal(ServicoRh dados)
    {
        this.dados = dados;
        Text = "Aurora — Gestão de RH";
        ClientSize = new(1280, 820);
        MinimumSize = new(1040, 720);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = Estilo.Fonte(10);
        Controls.Add(raiz);

        const int margemDoMenu = 24;
        const int topoDoCabecalho = 22;
        const int alturaDoCabecalho = 47;
        var cabecalho = CriarCaixa(20);
        cabecalho.Bounds = new(236, topoDoCabecalho, 1018, alturaDoCabecalho);
        cabecalho.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        raiz.Controls.Add(cabecalho);
        Estilo.Colocar(cabecalho, Estilo.CriarRotulo("Espaço de trabalho", 10, true), 15, 13);
        var trocarConta = CriarBotao("Sair da conta", "", 102, false, () => { VoltarParaLogin = true; Close(); });
        trocarConta.Location = new(cabecalho.Width - 111, 6);
        trocarConta.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        cabecalho.Controls.Add(trocarConta);

        var nomeDaMarca = Estilo.CriarRotulo("Aurora", 15, true);
        var subtituloDaMarca = Estilo.CriarRotulo("Gestão de RH", 9, false, Estilo.CorApagada);
        var textoDaMarca = new Panel { Size = new(129, alturaDoCabecalho), BackColor = Color.Transparent };
        var topoDoTexto = (alturaDoCabecalho - nomeDaMarca.PreferredHeight - subtituloDaMarca.PreferredHeight) / 2;
        Estilo.Colocar(textoDaMarca, nomeDaMarca, 0, topoDoTexto);
        Estilo.Colocar(textoDaMarca, subtituloDaMarca, 0, topoDoTexto + nomeDaMarca.PreferredHeight);
        Estilo.Colocar(raiz, textoDaMarca, margemDoMenu, topoDoCabecalho);

        var menuLateral = CriarCaixa(22);
        menuLateral.Bounds = new(margemDoMenu, 91, 191, 681);
        menuLateral.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
        raiz.Controls.Add(menuLateral);
        Estilo.Colocar(menuLateral, Estilo.CriarRotulo("ÁREAS DE RH", 8, true, Estilo.CorApagada), 16, 19);
        for (var i = 0; i < Paginas.Length; i++)
        {
            var item = Paginas[i];
            var botao = new Botao(item.Texto) { Size = new(169, 43), Font = Estilo.Fonte(9.5f, true), DoMenu = true, TabIndex = i };
            botao.Click += (_, _) => Navegar(item.Id);
            menu.Add(item.Id, botao);
            Estilo.Colocar(menuLateral, botao, 11, 46 + i * 48);
        }
        var pessoa = new Moldura(Estilo.CorDoFundo, Estilo.CorDaBorda, 0) { Size = new(167, 59) };
        pessoa.Location = new(12, menuLateral.Height - 72);
        pessoa.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        menuLateral.Controls.Add(pessoa);
        rotuloDoPerfil = Estilo.CriarRotulo("", 9, true);
        Estilo.Colocar(pessoa, rotuloDoPerfil, 10, 10);
        Estilo.Colocar(pessoa, Estilo.CriarRotulo("Sessão de demonstração", 7.5f, false, Estilo.CorApagada), 10, 31);

        conteudo.Bounds = new(236, 86, 1018, 686);
        conteudo.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        raiz.Controls.Add(conteudo);
        rodape = Estilo.CriarRotulo("", 8.5f, false, Estilo.CorApagada);
        rodape.BackColor = raiz.BackColor;
        rodape.Location = new(240, 788);
        rodape.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        raiz.Controls.Add(rodape);
        Navegar(paginaAtual);
    }
    // caixas brancas com borda
    private static Moldura CriarCaixa(int raio) => new(Estilo.CorDoFundo, Estilo.CorDaBorda, raio);
    private static Moldura CriarCartao(int raio = 18) => new(Estilo.CorDoFundo, Estilo.CorDaBorda, raio);
    // cria um botão normal
    private static Botao CriarBotao(string rotulo, string icone, int largura, bool principal, Action aoClicar)
    {
        var botao = new Botao(rotulo) { Icone = icone, Principal = principal, Size = new(largura, 35) };
        botao.Click += (_, _) => aoClicar();
        return botao;
    }
    // botão de editar: fica desligado para quem não pode editar (Gestor)
    private Botao CriarBotaoDeEdicao(string rotulo, string icone, int largura, bool principal, Action aoClicar)
    {
        // o botão fica desligado, mas o ServicoRh também confere o perfil
        var botao = CriarBotao(rotulo, icone, largura, principal, aoClicar);
        botao.Enabled = dados.PodeEditar;
        return botao;
    }
    // troca a tela que aparece quando clica no menu
    private void Navegar(string id)
    {
        if (id == "pay" && !PodeVerSalarios) id = "home";
        paginaAtual = id;
        foreach (var item in menu)
        {
            item.Value.Selecionado = item.Key == paginaAtual;
            item.Value.Visible = item.Key != "pay" || PodeVerSalarios;
            item.Value.AccessibleDescription = item.Value.Selecionado ? "Área atual" : "";
            item.Value.Invalidate();
        }
        rotuloDoPerfil.Text = dados.NomeDoPerfil;
        // apaga a tela antiga antes de montar a nova
        conteudo.SuspendLayout();
        foreach (var controle in conteudo.Controls.Cast<Control>().ToArray()) controle.Dispose();
        conteudo.AutoScrollPosition = Point.Empty;
        var visao = paginaAtual switch
        {
            "people" => TelaDeFuncionarios(), "time" => TelaDePonto(), "hire" => TelaDeRecrutamento(), "calendar" => TelaDaAgenda(), "pay" => TelaDeSalarios(), _ => TelaDoPainel()
        };
        visao.Dock = DockStyle.Top;
        conteudo.Controls.Add(visao);
        conteudo.ResumeLayout(true);
        conteudo.PerformLayout();
        visao.PerformLayout();
        MostrarMensagem();
    }
    // mostra uma mensagem no rodapé
    private void MostrarMensagem(string? mensagem = null) => rodape.Text = mensagem ?? "";
    // painel vazio onde cada tela é montada
    private static Panel NovaPagina() => new() { BackColor = Color.Transparent, Height = 668 };
    // título e subtítulo no topo de cada tela
    private static void CriarTitulo(Panel pai, string titulo, string subtitulo, Botao? acao = null)
    {
        var cabecalho = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
        cabecalho.ColumnStyles.Add(new(SizeType.Percent, 100));
        cabecalho.ColumnStyles.Add(new(SizeType.Absolute, (acao?.Width ?? 0) + 8));
        cabecalho.RowStyles.Add(new(SizeType.Percent, 100));
        ColocarLarguraToda(pai, cabecalho, 0, 91);
        var copia = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = Padding.Empty };
        cabecalho.Controls.Add(copia, 0, 0);
        var destaque = Estilo.CriarRotulo(titulo, 24, true);
        destaque.AutoSize = false;
        destaque.AutoEllipsis = true;
        destaque.Height = 53;
        Estilo.Colocar(copia, destaque, 3, 5);
        copia.SizeChanged += (_, _) => destaque.Width = Math.Max(0, copia.Width - 8);
        destaque.Width = Math.Max(0, copia.Width - 8);
        Estilo.Colocar(copia, Estilo.CriarRotulo(subtitulo, 11, false, Estilo.CorApagada), 5, 63);
        if (acao is null) return;
        acao.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        acao.Margin = new(0, 28, 2, 0);
        cabecalho.Controls.Add(acao, 1, 0);
    }
    // Tela inicial (Painel) com os números do dia, o gráfico e as últimas candidaturas
    private Panel TelaDoPainel()
    {
        // os números vêm dos mesmos dados das outras telas
        var painel = NovaPagina();
        painel.Width = conteudo.ClientSize.Width;
        var ativos = dados.Funcionarios.Count(p => p.Situacao == "Ativo");
        var hoje = dados.PresentesNoDia(DadosDeExemplo.Hoje);
        var vagas = dados.Vagas.Count(vagaDaLista => vagaDaLista.Situacao == "Aberta");
        var mes = PodeVerSalarios ? dados.FolhaDoMes(DadosDeExemplo.Hoje).Sum(p => p.Total) : 0m;
        CriarTitulo(painel, $"Olá, {dados.Usuario}", "Sexta-feira, 13 de novembro de 2026", CriarBotaoDeEdicao("Novo funcionário", "plus", 169, true, () => EditarFuncionario(null)));

        var indicadores = new TableLayoutPanel { ColumnCount = 4, RowCount = 1, BackColor = Color.Transparent };
        for (var i = 0; i < 4; i++) indicadores.ColumnStyles.Add(new(SizeType.Percent, 25));
        indicadores.RowStyles.Add(new(SizeType.Percent, 100));
        indicadores.Bounds = new(0, 107, painel.Width, 121);
        indicadores.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
        painel.Controls.Add(indicadores);
        indicadores.Controls.Add(Indicador("Funcionários ativos", ativos.ToString(), $"{dados.Funcionarios.Count} no cadastro", "people", 0), 0, 0);
        indicadores.Controls.Add(Indicador("Presentes hoje", hoje.ToString(), $"de {ativos} funcionários ativos", "time", 1), 1, 0);
        indicadores.Controls.Add(Indicador("Vagas abertas", vagas.ToString(), $"{dados.Candidatos.Count} candidaturas", "hire", 2), 2, 0);
        indicadores.Controls.Add(PodeVerSalarios
            ? Indicador("Folha de novembro", mes.ToString("C0"), "Líquido simulado", "pay", 3)
            : Indicador("Compromissos hoje", dados.Agenda.Count(a => a.DataHora.Date == DadosDeExemplo.Hoje).ToString(), "Agenda de RH", "calendar", 3), 3, 0);

        var linha = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
        linha.ColumnStyles.Add(new(SizeType.Percent, 56));
        linha.ColumnStyles.Add(new(SizeType.Percent, 44));
        linha.RowStyles.Add(new(SizeType.Percent, 100));
        linha.Bounds = new(0, 245, painel.Width, 281);
        linha.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
        painel.Controls.Add(linha);
        linha.Controls.Add(Grafico(ativos), 0, 0);
        linha.Controls.Add(CandidatosRecentes(), 1, 0);

        var agenda = CriarCartao();
        agenda.Bounds = new(0, 544, painel.Width, 107);
        agenda.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        painel.Controls.Add(agenda);
        Estilo.Colocar(agenda, Estilo.CriarRotulo("Agenda de hoje", 13, true), 17, 12);
        var compromissos = dados.Agenda.Where(a => a.DataHora.Date == DadosDeExemplo.Hoje).OrderBy(a => a.DataHora).Take(2).ToArray();
        for (var i = 0; i < compromissos.Length; i++)
        {
            var item = compromissos[i];
            Estilo.Colocar(agenda, Estilo.CriarRotulo(item.DataHora.ToString("HH:mm"), 9, true, Estilo.CorDeDestaque), 19, 47 + i * 24);
            var titulo = Estilo.CriarRotulo(item.Titulo, 10);
            titulo.AutoSize = false;
            titulo.AutoEllipsis = true;
            titulo.Size = new(agenda.Width - 234, 20);
            titulo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Estilo.Colocar(agenda, titulo, 75, 45 + i * 24);
        }
        var verAgenda = CriarBotao("Abrir calendário", "", 130, false, () => Navegar("calendar"));
        verAgenda.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        Estilo.Colocar(agenda, verAgenda, agenda.Width - 146, 37);
        return painel;
    }
    // um quadrinho de número do painel. Clicando nele, abre a tela da área.
    private Moldura Indicador(string nome, string valor, string detalhe, string alvo, int posicao)
    {
        var quadro = CriarCartao(15);
        quadro.Dock = DockStyle.Fill;
        quadro.Margin = new(posicao * 3, 0, (3 - posicao) * 3, 0);
        Estilo.Colocar(quadro, Estilo.CriarRotulo(nome, 9, true, Estilo.CorApagada), 13, 15);
        Estilo.Colocar(quadro, Estilo.CriarRotulo(valor, 22, true), 11, 38);
        Estilo.Colocar(quadro, Estilo.CriarRotulo(detalhe, 8.5f, false, Estilo.CorApagada), 14, 78);
        quadro.AccessibleName = $"Abrir {nome}";
        quadro.AccessibleRole = AccessibleRole.PushButton;
        quadro.TabStop = true;
        quadro.Cursor = Cursors.Hand;
        void AbrirTela() => Navegar(alvo);
        quadro.Click += (_, _) => AbrirTela();
        quadro.KeyDown += (_, e) =>
        {
            if (e.KeyCode is Keys.Enter or Keys.Space)
            {
                e.Handled = true;
                AbrirTela();
            }
        };
        foreach (Control filho in quadro.Controls)
        {
            filho.Click += (_, _) => AbrirTela();
            filho.Cursor = Cursors.Hand;
        }
        return quadro;
    }
    // gráfico de barras da presença da semana
    private Moldura Grafico(int ativos)
    {
        var caixa = CriarCartao();
        caixa.Dock = DockStyle.Fill;
        caixa.Margin = new(0, 0, 8, 0);
        Estilo.Colocar(caixa, Estilo.CriarRotulo("Frequência da semana", 15, true), 16, 13);
        Estilo.Colocar(caixa, Estilo.CriarRotulo("Pessoas com entrada registrada · 9 a 13 nov.", 9, false, Estilo.CorApagada), 18, 45);
        caixa.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var largura = (caixa.ClientSize.Width - 44) / 5f;
            using var linha = new Pen(Estilo.CorDaBorda);
            using var pequeno = Estilo.Fonte(9);
            using var fonteDoNumero = Estilo.Fonte(11, true);
            for (var y = 107; y <= 227; y += 40) g.DrawLine(linha, 22, y, caixa.Width - 22, y);
            var nomes = new[] { "Seg", "Ter", "Qua", "Qui", "Sex" };
            for (var i = 0; i < nomes.Length; i++)
            {
                var valor = dados.PresentesNoDia(DadosDeExemplo.Hoje.AddDays(i - 4));
                var h = Math.Max(2, 128f * valor / Math.Max(1, Math.Max(ativos, valor)));
                var meio = 22 + (i + .5f) * largura;
                var barra = new RectangleF(meio - largura * .29f, 227 - h, largura * .58f, h);
                using var preenchimento = new SolidBrush(Estilo.CorPrincipal);
                g.FillRectangle(preenchimento, barra);
                TextRenderer.DrawText(g, valor.ToString(), fonteDoNumero, new Rectangle((int)meio - 22, (int)barra.Top - 23, 44, 20), Estilo.CorDoTexto, TextFormatFlags.HorizontalCenter);
                TextRenderer.DrawText(g, nomes[i], pequeno, new Rectangle((int)meio - 24, 236, 48, 20), Estilo.CorApagada, TextFormatFlags.HorizontalCenter);
            }
        };
        caixa.AccessibleName = "Frequência de segunda a sexta";
        caixa.AccessibleDescription = string.Join(", ", Enumerable.Range(0, 5).Select(i => $"{DadosDeExemplo.Hoje.AddDays(i - 4):dddd}: {dados.PresentesNoDia(DadosDeExemplo.Hoje.AddDays(i - 4))}"));
        return caixa;
    }
    // lista das candidaturas mais novas
    private Moldura CandidatosRecentes()
    {
        var caixa = CriarCartao();
        caixa.Dock = DockStyle.Fill;
        caixa.Margin = new(8, 0, 0, 0);
        Estilo.Colocar(caixa, Estilo.CriarRotulo("Candidaturas recentes", 15, true), 16, 13);
        Estilo.Colocar(caixa, Estilo.CriarRotulo("Acompanhe as etapas da seleção", 9, false, Estilo.CorApagada), 18, 45);
        var lista = new TableLayoutPanel { ColumnCount = 2, RowCount = 4, BackColor = Color.Transparent };
        lista.ColumnStyles.Add(new(SizeType.Percent, 100));
        lista.ColumnStyles.Add(new(SizeType.Absolute, 101));
        lista.Bounds = new(16, 72, caixa.Width - 32, 194);
        caixa.SizeChanged += (_, _) => lista.Width = Math.Max(1, caixa.ClientSize.Width - 32);
        caixa.Controls.Add(lista);
        var candidatos = dados.Candidatos.TakeLast(4).Reverse().ToArray();
        for (var i = 0; i < candidatos.Length; i++)
        {
            lista.RowStyles.Add(new(SizeType.Absolute, 47));
            var candidato = candidatos[i];
            var nome = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = Padding.Empty };
            var rotulo = Estilo.CriarRotulo(candidato.Nome, 10, true);
            rotulo.AutoSize = false; rotulo.AutoEllipsis = true; rotulo.Size = new(145, 20);
            Estilo.Colocar(nome, rotulo, 0, 4);
            var vagaDaLista = Estilo.CriarRotulo(dados.TituloDaVaga(candidato.VagaId), 8.5f, false, Estilo.CorApagada);
            vagaDaLista.AutoSize = false; vagaDaLista.AutoEllipsis = true; vagaDaLista.Height = 18;
            Estilo.Colocar(nome, vagaDaLista, 0, 24);
            nome.SizeChanged += (_, _) => rotulo.Width = vagaDaLista.Width = Math.Max(1, nome.Width - 3);
            lista.Controls.Add(nome, 0, i);
            var etapa = CriarBotaoDeEdicao(candidato.Etapa, "", 100, false, () => EditarCandidato(candidato));
            etapa.Font = Estilo.Fonte(8, true);
            etapa.Height = 27;
            etapa.Margin = new(0, 9, 0, 0);
            etapa.AccessibleName = $"{candidato.Nome}, {candidato.Etapa}. Alterar etapa";
            lista.Controls.Add(etapa, 1, i);
        }
        return caixa;
    }
}
