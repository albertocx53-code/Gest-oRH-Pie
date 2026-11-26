using Aurora.Core;

namespace Aurora.Dados;

// Busca as contas de login na tabela "usuarios" do banco.
// Antes as contas ficavam escritas no código (ContasDeExemplo).
// Essa classe só acha a conta. Quem confere a senha é a classe Autenticacao.
public class ContasDoBanco : IBuscaDeContas
{
    private string caminhoDoBanco;

    public ContasDoBanco(string caminhoDoBanco)
    {
        this.caminhoDoBanco = caminhoDoBanco;
    }

    public Conta? BuscarConta(string usuario)
    {
        using var conexao = Conexao.Abrir(caminhoDoBanco);
        using var comando = conexao.CreateCommand();

        // COLLATE NOCASE: não liga pra maiúscula e minúscula ("ADMIN" acha "admin")
        comando.CommandText = @"
            SELECT login, nome, perfil, senha_salt, senha_hash, iteracoes
            FROM usuarios
            WHERE login = $login COLLATE NOCASE";
        comando.Parameters.AddWithValue("$login", usuario);

        using var leitor = comando.ExecuteReader();
        if (!leitor.Read())
        {
            return null; // não tem ninguém com esse login
        }

        string login = leitor.GetString(0);
        string nome = leitor.GetString(1);
        PerfilDeAcesso perfil = ConverterPerfil(leitor.GetString(2));
        string salt = leitor.GetString(3);
        string hash = leitor.GetString(4);
        int iteracoes = leitor.GetInt32(5);

        return new Conta(login, nome, perfil, salt, hash, iteracoes);
    }

    // No banco o perfil fica escrito em português. Aqui eu transformo no tipo do sistema.
    private static PerfilDeAcesso ConverterPerfil(string perfil)
    {
        if (perfil == "Administrador") return PerfilDeAcesso.Administrador;
        if (perfil == "Recursos Humanos") return PerfilDeAcesso.RecursosHumanos;
        return PerfilDeAcesso.Gestor; // se vier outra coisa, fica com o perfil que pode menos
    }
}
