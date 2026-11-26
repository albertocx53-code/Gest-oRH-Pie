using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace Aurora.WinForms;
// Cores, fontes e funções de ajuda para montar as telas.
// Tudo que é visual fica aqui, para as telas ficarem com a mesma cara.
internal static class Estilo
{
    // cores usadas no sistema todo
    public static readonly Color CorDoTexto = Color.FromArgb(32, 32, 32);
    public static readonly Color CorApagada = Color.FromArgb(96, 96, 96);
    public static readonly Color CorDeDestaque = Color.FromArgb(0, 102, 204);
    public static readonly Color CorPrincipal = Color.FromArgb(0, 102, 204);
    public static readonly Color CorDoFundo = Color.White;
    public static readonly Color CorDaBorda = Color.FromArgb(210, 210, 210);
    public static readonly Color CorClara = Color.FromArgb(229, 241, 252);
    public static readonly Color CorDoCampo = Color.White;
    // vermelho das mensagens de erro (o rosa claro do tema escuro não aparecia no fundo branco)
    public static readonly Color CorDeErro = Color.FromArgb(192, 0, 0);
    // fonte padrão (Segoe UI), normal ou em negrito
    public static Font Fonte(float tamanho, bool negrito = false) => new("Segoe UI", tamanho, negrito ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point);
    // cria um texto (Label) já com a fonte e a cor certas
    public static Label CriarRotulo(string texto, float tamanho, bool negrito = false, Color? cor = null) => new()
    {
        Text = texto, Font = Fonte(tamanho, negrito), ForeColor = cor ?? CorDoTexto, BackColor = Color.Transparent,
        AutoSize = true, UseMnemonic = false, Margin = Padding.Empty
    };
    // coloca um controle dentro de outro na posição x, y
    public static void Colocar(Control pai, Control filho, int x, int y)
    {
        filho.Location = new(x, y);
        pai.Controls.Add(filho);
    }
    // desenha os iconezinhos dos botões (setas, mais, etc.)
    public static void DesenharIcone(Graphics g, string nome, float centroX, float centroY, Color cor, float tamanho = 20)
    {
        var estado = g.Save();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TranslateTransform(centroX - tamanho / 2, centroY - tamanho / 2);
        g.ScaleTransform(tamanho / 20, tamanho / 20);
        using var p = new Pen(cor, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        switch (nome)
        {
            case "home":
                g.DrawLines(p, [new(2, 9), new(10, 2), new(18, 9), new(16, 9), new(16, 17), new(4, 17), new(4, 9), new(2, 9)]);
                g.DrawRectangle(p, 8, 12, 4, 5);
                break;
            case "people":
                g.DrawEllipse(p, 4, 3, 6, 6); g.DrawArc(p, 1.5f, 11, 11, 12, 180, 180);
                g.DrawArc(p, 10.5f, 4, 6, 6, 260, 160); g.DrawArc(p, 9, 11, 9.5f, 12, 270, 90);
                break;
            case "time":
                g.DrawEllipse(p, 2, 2, 16, 16); g.DrawLines(p, [new(10, 5), new(10, 10), new(13, 12)]);
                break;
            case "hire":
                g.DrawRectangle(p, 2, 6, 16, 11); g.DrawLines(p, [new(7, 6), new(7, 3), new(13, 3), new(13, 6)]);
                g.DrawLine(p, 2, 10, 18, 10); g.DrawLine(p, 10, 9, 10, 12);
                break;
            case "calendar":
                g.DrawRectangle(p, 2, 4, 16, 14); g.DrawLine(p, 2, 8, 18, 8);
                g.DrawLine(p, 6, 2, 6, 6); g.DrawLine(p, 14, 2, 14, 6); g.DrawLine(p, 6, 12, 9, 12);
                break;
            case "pay":
                g.DrawRectangle(p, 2, 4, 16, 12); g.DrawEllipse(p, 7, 7, 6, 6);
                break;
            case "plus": g.DrawLine(p, 10, 4, 10, 16); g.DrawLine(p, 4, 10, 16, 10); break;
            case "prev": g.DrawLines(p, [new(13, 4), new(7, 10), new(13, 16)]); break;
            case "next": g.DrawLines(p, [new(7, 4), new(13, 10), new(7, 16)]); break;
        }
        g.Restore(estado);
    }
}
// o fundo cinza claro das janelas
internal sealed class FundoDaTela : Panel
{
    public FundoDaTela()
    {
        BackColor = Color.FromArgb(245, 245, 245);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var fundo = new SolidBrush(BackColor);
        e.Graphics.FillRectangle(fundo, ClientRectangle);
    }
}
// um painel branco com borda cinza (os "cartões" das telas)
internal sealed class Moldura : Panel
{
    private readonly Color borda;
    public Moldura(Color preenchimento, Color borda, int raio = 18)
    {
        this.borda = borda;
        BackColor = preenchimento;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        using var caneta = new Pen(borda);
        e.Graphics.DrawRectangle(caneta, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
        base.OnPaint(e);
    }
}
// tabela do sistema (é uma DataGridView normal)
internal sealed class TabelaDeDados : DataGridView
{
}

// Botão do sistema. Funciona igual ao Button normal, só muda o desenho.
// Principal = botão azul. DoMenu = botão do menu da esquerda.
internal sealed class Botao : Button
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal string Icone { get; init; } = "";
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool Principal { get; init; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool Selecionado { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool DoMenu { get; init; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool DiaDoCalendario { get; init; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool Marcado { get; init; }
    private bool mouseEmCima;
    private bool pressionado;

    public Botao(string texto)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Text = texto;
        BackColor = SystemColors.Control;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = Estilo.Fonte(10, true);
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseEnter(EventArgs e) { mouseEmCima = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { mouseEmCima = false; pressionado = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { pressionado = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { pressionado = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var corDoTexto = Principal ? Color.White : Estilo.CorDoTexto;
        if (!Enabled) corDoTexto = Estilo.CorApagada;
        var preenchimento = Principal
            ? pressionado ? ControlPaint.Dark(Estilo.CorPrincipal) : Estilo.CorPrincipal
            : DoMenu
                ? Selecionado ? Estilo.CorClara : mouseEmCima || pressionado ? Color.FromArgb(238, 238, 238) : Estilo.CorDoFundo
                : Selecionado ? Estilo.CorClara : mouseEmCima || pressionado ? Color.FromArgb(238, 238, 238) : SystemColors.Control;
        using (var pincel = new SolidBrush(preenchimento))
            e.Graphics.FillRectangle(pincel, ClientRectangle);
        if (Principal || !DoMenu)
        {
            using var caneta = new Pen(Principal ? Estilo.CorPrincipal : Estilo.CorDaBorda);
            e.Graphics.DrawRectangle(caneta, 0, 0, Width - 1, Height - 1);
        }
        if (Selecionado && DoMenu)
        {
            using var caneta = new Pen(Estilo.CorDeDestaque, 3);
            e.Graphics.DrawLine(caneta, 2, 3, 2, Height - 4);
        }
        const TextFormatFlags opcoesDoTexto = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
        if (DoMenu)
        {
            var esquerda = 12;
            if (Icone.Length > 0)
            {
                Estilo.DesenharIcone(e.Graphics, Icone, 23, Height / 2f, corDoTexto);
                esquerda = 40;
            }
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(esquerda, 0, Width - esquerda - 6, Height), corDoTexto, opcoesDoTexto);
        }
        else if (DiaDoCalendario)
        {
            DesenharDiaDoCalendario(e.Graphics, corDoTexto);
        }
        else if (Icone.Length == 0)
        {
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(4, 0, Width - 8, Height), corDoTexto, opcoesDoTexto | TextFormatFlags.HorizontalCenter);
        }
        else if (Text.Length == 0)
        {
            Estilo.DesenharIcone(e.Graphics, Icone, Width / 2f, Height / 2f, corDoTexto, 13);
        }
        else
        {
            const int tamanhoDoIcone = 16;
            const int espaco = 7;
            var medido = TextRenderer.MeasureText(e.Graphics, Text, Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;
            var larguraDoTexto = Math.Min(medido, Math.Max(1, Width - 16 - tamanhoDoIcone - espaco));
            var esquerda = (Width - tamanhoDoIcone - espaco - larguraDoTexto) / 2;
            Estilo.DesenharIcone(e.Graphics, Icone, esquerda + tamanhoDoIcone / 2f, Height / 2f, corDoTexto, tamanhoDoIcone);
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(esquerda + tamanhoDoIcone + espaco, 0, larguraDoTexto, Height), corDoTexto, opcoesDoTexto);
        }
        if (Focused && ShowFocusCues)
        {
            using var caneta = new Pen(Principal ? Color.White : Estilo.CorDeDestaque) { DashStyle = DashStyle.Dot };
            e.Graphics.DrawRectangle(caneta, 2, 2, Width - 5, Height - 5);
        }
    }

    private void DesenharDiaDoCalendario(Graphics desenho, Color cor)
    {
        var centro = Width / 2f;
        var escala = DeviceDpi / 96f;
        TextRenderer.DrawText(desenho, Text, Font, new Rectangle(0, 0, Width, Height - 5),
            cor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        if (Marcado)
        {
            using var bolinha = new SolidBrush(Principal ? Color.White : Estilo.CorDeDestaque);
            var diametro = 3f * escala;
            desenho.FillEllipse(bolinha, centro - diametro / 2, Height - 5 * escala, diametro, diametro);
        }
    }
}
