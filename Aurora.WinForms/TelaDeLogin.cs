using Aurora.Core;

namespace Aurora.WinForms;
// Tela de login. Se o usuário e a senha estiverem certos, guarda a sessão e fecha.
internal sealed class TelaDeLogin : Form
{
    private readonly FundoDaTela raiz = new() { Dock = DockStyle.Fill };
    private readonly TextBox usuario = CriarCaixaDeTexto("Usuário", 64);
    private readonly TextBox senha = CriarCaixaDeTexto("Senha", 128);
    private readonly Label erro = Estilo.CriarRotulo("", 9, true, Estilo.CorDeErro);

    internal Sessao? SessaoAberta { get; private set; }

    public TelaDeLogin(Autenticacao autenticacao)
    {
        Text = "Aurora — Entrar";
        ClientSize = new(410, 410);
        MinimumSize = new(426, 448);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        MaximizeBox = false;
        Controls.Add(raiz);

        var cartao = new Moldura(Estilo.CorDoFundo, Estilo.CorDaBorda, 0) { Size = new(360, 370) };
        Estilo.Colocar(raiz, cartao, 25, 20);
        Estilo.Colocar(cartao, Estilo.CriarRotulo("Aurora — Gestão de RH", 18, true), 24, 18);
        Estilo.Colocar(cartao, Estilo.CriarRotulo("Entre com seu usuário e senha.", 9, false, Estilo.CorApagada), 24, 50);
        Estilo.Colocar(cartao, Estilo.CriarRotulo("Usuário", 10, true), 24, 86);
        Estilo.Colocar(cartao, usuario, 24, 108);
        Estilo.Colocar(cartao, Estilo.CriarRotulo("Senha", 10, true), 24, 148);
        senha.UseSystemPasswordChar = true;
        senha.TabIndex = 1;
        Estilo.Colocar(cartao, senha, 24, 170);
        var entrar = new Botao("Entrar") { Size = new(302, 38), Principal = true, TabIndex = 2 };
        Estilo.Colocar(cartao, entrar, 24, 214);
        Estilo.Colocar(cartao, erro, 24, 258);
        Estilo.Colocar(cartao, Estilo.CriarRotulo("Contas de teste: admin / rh / gestor", 9, false, Estilo.CorApagada), 24, 296);
        Estilo.Colocar(cartao, Estilo.CriarRotulo("Senha: Aurora.demo26", 9, false, Estilo.CorApagada), 24, 316);

        // o perfil vem da conta, a pessoa não escolhe
        entrar.Click += (_, _) =>
        {
            var sessao = autenticacao.Entrar(usuario.Text, senha.Text);
            senha.Clear();
            if (sessao is null)
            {
                erro.Text = "Usuário ou senha incorretos.";
                (usuario.Text.Trim().Length == 0 ? usuario : senha).Focus();
                return;
            }
            SessaoAberta = sessao;
            DialogResult = DialogResult.OK;
        };
        AcceptButton = entrar;
        Shown += (_, _) => usuario.Focus();
    }
    // caixa de texto do usuário e da senha
    private static TextBox CriarCaixaDeTexto(string nome, int limite) => new()
    {
        AccessibleName = nome, Width = 302, MaxLength = limite, Font = Estilo.Fonte(11),
        BackColor = Estilo.CorDoCampo, ForeColor = Estilo.CorDoTexto
    };
}
