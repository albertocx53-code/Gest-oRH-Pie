using System.Drawing.Imaging;
using Aurora.Core;

namespace Aurora.WinForms;

internal sealed class LoginForm : Form
{
    private readonly AuroraBackdrop root = new() { Dock = DockStyle.Fill };
    private readonly TextBox username = Field("Usuário", 64);
    private readonly TextBox password = Field("Senha", 128);
    private readonly Label error = AuroraStyle.Label("", 9, true, Color.LightPink);

    internal RhSession? Session { get; private set; }

    public LoginForm(Authentication authentication)
    {
        Text = "Aurora — Entrar";
        ClientSize = new(742, 450);
        MinimumSize = new(758, 488);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        MaximizeBox = false;
        Controls.Add(root);

        var mark = new Surface(Color.FromArgb(48, 39, 100), Color.FromArgb(139, 126, 211), 18) { Size = new(62, 62) };
        mark.Paint += (_, e) => AuroraStyle.DrawBrand(e.Graphics, mark.ClientRectangle);
        AuroraStyle.Put(root, mark, 45, 108);
        AuroraStyle.Put(root, AuroraStyle.Label("Aurora", 35, true), 41, 179);
        AuroraStyle.Put(root, AuroraStyle.Label("Gestão de Recursos Humanos", 12, false, AuroraStyle.Muted), 46, 248);
        AuroraStyle.Put(root, AuroraStyle.Label("Ambiente de demonstração", 9, true, AuroraStyle.Accent), 46, 321);

        var card = new Surface(AuroraStyle.Paper, AuroraStyle.Border) { Size = new(350, 360), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        AuroraStyle.Put(root, card, 353, 44);
        AuroraStyle.Put(card, AuroraStyle.Label("Entrar no Aurora", 21, true), 21, 20);
        AuroraStyle.Put(card, AuroraStyle.Label("Use a conta atribuída ao seu perfil.", 9, false, AuroraStyle.Muted), 24, 60);
        AuroraStyle.Put(card, AuroraStyle.Label("Usuário", 10, true), 24, 99);
        AuroraStyle.Put(card, username, 24, 122);
        AuroraStyle.Put(card, AuroraStyle.Label("Senha", 10, true), 24, 162);
        password.UseSystemPasswordChar = true;
        password.TabIndex = 1;
        AuroraStyle.Put(card, password, 24, 185);
        var enter = new AuroraButton("Entrar") { Size = new(302, 38), Primary = true, TabIndex = 2 };
        AuroraStyle.Put(card, enter, 24, 229);
        AuroraStyle.Put(card, error, 24, 275);
        AuroraStyle.Put(card, AuroraStyle.Label("Contas de teste · admin / rh / gestor", 9, false, AuroraStyle.Muted), 24, 309);
        AuroraStyle.Put(card, AuroraStyle.Label("Senha de teste · Aurora.demo26", 9, false, AuroraStyle.Muted), 24, 328);

        // O perfil vem da conta autenticada. A tela de login nunca pede para a pessoa escolher o próprio acesso.
        enter.Click += (_, _) =>
        {
            var session = authentication.SignIn(username.Text, password.Text);
            password.Clear();
            if (session is null)
            {
                error.Text = "Usuário ou senha incorretos.";
                (username.Text.Trim().Length == 0 ? username : password).Focus();
                return;
            }
            Session = session;
            DialogResult = DialogResult.OK;
        };
        AcceptButton = enter;
        Shown += (_, _) => username.Focus();
    }

    internal void ExportPreview(string directory)
    {
        Directory.CreateDirectory(directory);
        ShowInTaskbar = false;
        Opacity = 0;
        Show();
        Application.DoEvents();
        using var image = new Bitmap(root.Width, root.Height);
        root.DrawToBitmap(image, root.ClientRectangle);
        image.Save(Path.Combine(directory, "Aurora-login.png"), ImageFormat.Png);
    }

    private static TextBox Field(string name, int limit) => new()
    {
        AccessibleName = name, Width = 302, MaxLength = limit, Font = AuroraStyle.Font(11),
        BackColor = AuroraStyle.Field, ForeColor = AuroraStyle.Ink
    };
}
