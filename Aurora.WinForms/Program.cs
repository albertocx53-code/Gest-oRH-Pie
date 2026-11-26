using System.Globalization;
using Aurora.Core;
using Aurora.Dados;

namespace Aurora.WinForms;

internal static class Program
{
    // É aqui que o programa começa
    [STAThread]
    private static void Main()
    {
        // datas e dinheiro no formato do Brasil
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
        CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
        ApplicationConfiguration.Initialize();
        Application.SetColorMode(SystemColorMode.Classic);

        // Os dados ficam no banco SQLite (arquivo aurora.db do lado do .exe).
        // Na primeira vez o banco é criado com os scripts da pasta BancoDeDados.
        string caminhoDoBanco = Path.Combine(AppContext.BaseDirectory, "aurora.db");
        string pastaDosScripts = Path.Combine(AppContext.BaseDirectory, "BancoDeDados");
        CriadorDoBanco.CriarSeNaoExistir(caminhoDoBanco, pastaDosScripts);

        // Antes aqui era "new DadosDeExemplo()" (memória). Agora é o banco.
        IRepositorio repositorio = new RepositorioSqlite(caminhoDoBanco);
        Autenticacao autenticacao = new Autenticacao(new ContasDoBanco(caminhoDoBanco));

        RodarSessoes(repositorio, autenticacao);
    }

    // Abre o login. Se entrar, abre a janela principal.
    // Quando a pessoa clica em "Sair da conta", volta para o login.
    internal static void RodarSessoes(IRepositorio repositorio, Autenticacao autenticacao)
    {
        while (true)
        {
            using var login = new TelaDeLogin(autenticacao);
            if (login.ShowDialog() != DialogResult.OK) return; // fechou o login
            Sessao? sessao = login.SessaoAberta;
            if (sessao == null) return;

            using var janela = new TelaPrincipal(new ServicoRh(repositorio, sessao));
            try
            {
                Application.Run(janela);
            }
            finally
            {
                sessao.Sair(); // encerra a sessão quando a janela fecha
            }

            if (!janela.VoltarParaLogin) return; // fechou o sistema
        }
    }
}
