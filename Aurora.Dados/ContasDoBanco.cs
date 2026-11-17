using Aurora.Core;

namespace Aurora.Dados;

// ContasDoBanco - Alberto (parte do banco de dados)
//
// Busca as contas de login na tabela "usuarios".
// Substitui as contas que antes ficavam fixas no código (DemoAccounts).
//
// Esta classe só encontra a conta. A conferência da senha é feita
// pela classe Authentication.
public class ContasDoBanco : IAccountDirectory
{
    private readonly string caminhoDoBanco;

    public ContasDoBanco(string caminhoDoBanco)
    {
        this.caminhoDoBanco = caminhoDoBanco;
    }

    public RhAccount? Find(string username)
    {
        using var conexao = Conexao.Abrir(caminhoDoBanco);
        using var comando = conexao.CreateCommand();

        // COLLATE NOCASE: ignora maiúsculas e minúsculas ("ADMIN" encontra "admin")
        comando.CommandText = @"
            SELECT login, nome, perfil, senha_salt, senha_hash, iteracoes
            FROM usuarios
            WHERE login = $login COLLATE NOCASE";
        comando.Parameters.AddWithValue("$login", username);

        using var leitor = comando.ExecuteReader();
        if (!leitor.Read())
            return null; // nenhum usuário com esse login

        return new RhAccount(
            leitor.GetString(0),
            leitor.GetString(1),
            ConverterPerfil(leitor.GetString(2)),
            leitor.GetString(3),
            leitor.GetString(4),
            leitor.GetInt32(5));
    }

    // Converte o perfil salvo no banco (texto em português) para o enum RhRole
    // usado pelo sistema.
    private static RhRole ConverterPerfil(string perfil)
    {
        switch (perfil)
        {
            case "Administrador":
                return RhRole.Administrator;
            case "Recursos Humanos":
                return RhRole.HumanResources;
            default:
                return RhRole.Manager; // valor padrão: perfil com menos permissão
        }
    }
}
