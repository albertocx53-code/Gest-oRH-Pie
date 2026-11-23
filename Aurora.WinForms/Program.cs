using System.Globalization;
using Aurora.Core;
using Aurora.Dados;

namespace Aurora.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
        CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
        ApplicationConfiguration.Initialize();
        Application.SetColorMode(SystemColorMode.Dark);
        if (args is ["--capturas", var directory])
        {
            // As capturas de tela continuam usando os dados em memória (DemoRh),
            // pra sair sempre igual, não importa o que tenha sido salvo no banco.
            IRhRepository demo = new DemoRh();
            var demoAuthentication = new Authentication(new DemoAccounts());
            using (var login = new LoginForm(demoAuthentication)) login.ExportPreview(directory);
            var previewSession = demoAuthentication.SignIn("rh", "Aurora.demo26")!;
            using var form = new AuroraForm(new RhService(demo, previewSession));
            form.ExportPreviews(directory);
            previewSession.SignOut();
            return;
        }

        // Daqui para baixo os dados vêm do banco SQLite.
        // O arquivo aurora.db fica do lado do .exe. Na primeira vez ele é criado
        // com os scripts da pasta BancoDeDados (tabelas + dados fictícios).
        var caminhoDoBanco = Path.Combine(AppContext.BaseDirectory, "aurora.db");
        CriadorDoBanco.CriarSeNaoExistir(caminhoDoBanco, Path.Combine(AppContext.BaseDirectory, "BancoDeDados"));

        IRhRepository repository = new RepositorioSqlite(caminhoDoBanco);
        var authentication = new Authentication(new ContasDoBanco(caminhoDoBanco));

        RunSessionLoop(repository, authentication);
    }

    internal static void RunSessionLoop(IRhRepository repository, Authentication authentication)
    {
        // Trocar de conta mantém os dados da execução e encerra a sessão usada pela janela anterior.
        while (true)
        {
            using var login = new LoginForm(authentication);
            if (login.ShowDialog() != DialogResult.OK || login.Session is not { } session) return;
            using var form = new AuroraForm(new RhService(repository, session));
            try { Application.Run(form); }
            finally { session.SignOut(); }
            if (!form.ReturnToLogin) return;
        }
    }
}
