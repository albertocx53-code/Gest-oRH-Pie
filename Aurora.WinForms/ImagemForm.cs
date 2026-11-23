namespace Aurora.WinForms;

// ImagemForm - Alberto
//
// Janela para ver uma imagem ampliada (diagrama do banco ou trecho de código).
// Abre com a imagem inteira na janela. Dá para ampliar e diminuir pelos botões,
// pelas teclas + e -, ou clicando na imagem (botão esquerdo amplia no ponto
// clicado, botão direito diminui). A tecla 0 volta a caber na janela e Esc fecha.
internal sealed class ImagemForm : Form
{
    private const float Passo = 1.4f;
    private readonly Image imagem;
    private readonly Panel area = new() { AutoScroll = true, BackColor = Color.FromArgb(18, 22, 34), Dock = DockStyle.Fill };
    private readonly PictureBox figura = new() { SizeMode = PictureBoxSizeMode.StretchImage, Cursor = Cursors.Hand };
    private readonly Label porcentagem = AuroraStyle.Label("100%", 10, true, AuroraStyle.Muted);
    private float zoom = 1;

    public ImagemForm(string titulo, Image imagem)
    {
        this.imagem = imagem;
        Text = titulo;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new(1120, 720);
        MinimumSize = new(640, 420);
        ShowInTaskbar = false;
        KeyPreview = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = AuroraStyle.Paper;

        var barra = new Panel { Width = ClientSize.Width, Height = 56, Dock = DockStyle.Top, BackColor = AuroraStyle.Paper };
        var nome = AuroraStyle.Label(titulo, 12, true);
        nome.AutoSize = false;
        nome.AutoEllipsis = true;
        nome.Bounds = new(16, 16, 420, 26);
        nome.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        barra.Controls.Add(nome);

        // Botões da direita para a esquerda, presos no canto direito da barra.
        var direita = 12;
        foreach (var (texto, largura, acao) in new (string, int, Action)[]
        {
            ("Fechar", 84, () => Close()), ("Tamanho real", 116, () => Mudar(1)), ("Caber na janela", 130, Caber),
            ("+", 40, () => Mudar(zoom * Passo)), ("−", 40, () => Mudar(zoom / Passo))
        })
        {
            var botao = new AuroraButton(texto) { Size = new(largura, 35), Primary = texto == "Fechar", Anchor = AnchorStyles.Top | AnchorStyles.Right };
            botao.Click += (_, _) => acao();
            direita += largura;
            botao.Location = new(barra.Width - direita, 10);
            barra.Controls.Add(botao);
            direita += 8;
        }
        porcentagem.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        porcentagem.Location = new(barra.Width - direita - 56, 18);
        barra.Controls.Add(porcentagem);
        nome.Width = Math.Max(80, barra.Width - direita - 90);

        figura.Image = imagem;
        figura.MouseClick += Clicou;
        area.Controls.Add(figura);
        Controls.Add(barra);
        Controls.Add(area);
        // O painel que ocupa o resto da janela precisa ficar na frente para o Dock funcionar certo.
        area.BringToFront();

        Shown += (_, _) => Caber();
        KeyDown += Tecla;
        FormClosed += (_, _) => imagem.Dispose();
    }

    private void Tecla(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Escape: Close(); break;
            case Keys.Add or Keys.Oemplus: Mudar(zoom * Passo); break;
            case Keys.Subtract or Keys.OemMinus: Mudar(zoom / Passo); break;
            case Keys.D0 or Keys.NumPad0: Caber(); break;
            default: return;
        }
        e.Handled = true;
    }

    // Amplia mantendo embaixo do mouse o ponto que foi clicado.
    private void Clicou(object? sender, MouseEventArgs e)
    {
        var fator = e.Button == MouseButtons.Right ? 1 / Passo : Passo;
        var antes = zoom;
        Mudar(zoom * fator);
        var real = zoom / antes;
        var alvoX = (int)(e.X * real - area.ClientSize.Width / 2f);
        var alvoY = (int)(e.Y * real - area.ClientSize.Height / 2f);
        area.AutoScrollPosition = new(Math.Max(0, alvoX), Math.Max(0, alvoY));
    }

    private void Caber()
    {
        var largura = (area.ClientSize.Width - 24f) / imagem.Width;
        var altura = (area.ClientSize.Height - 24f) / imagem.Height;
        Mudar(Math.Min(largura, altura));
    }

    private void Mudar(float novo)
    {
        zoom = Math.Clamp(novo, 0.1f, 5f);
        area.AutoScrollPosition = Point.Empty;
        figura.Size = new((int)(imagem.Width * zoom), (int)(imagem.Height * zoom));
        // Se a imagem for menor que a janela, ela fica no meio.
        figura.Location = new(Math.Max(0, (area.ClientSize.Width - figura.Width) / 2), Math.Max(0, (area.ClientSize.Height - figura.Height) / 2));
        porcentagem.Text = $"{zoom * 100:0}%";
    }
}
