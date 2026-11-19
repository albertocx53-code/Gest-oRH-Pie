using Microsoft.Data.Sqlite;

namespace Aurora.Dados;

// CriadorDoBanco - Alberto (parte do banco de dados)
//
// Cria o arquivo aurora.db na primeira vez que o sistema é aberto,
// executando os scripts da pasta BancoDeDados:
//   01_criar_tabelas.sql   -> cria as tabelas
//   02_dados_ficticios.sql -> coloca os dados de teste
//
// Se o arquivo já existir, nada é feito, para não apagar os dados cadastrados.
// Para voltar aos dados iniciais, basta apagar o aurora.db.
public static class CriadorDoBanco
{
    public static void CriarSeNaoExistir(string caminhoDoBanco, string pastaDosScripts)
    {
        if (File.Exists(caminhoDoBanco))
            return; // o banco já existe

        try
        {
            using var conexao = Conexao.Abrir(caminhoDoBanco);
            RodarScript(conexao, Path.Combine(pastaDosScripts, "01_criar_tabelas.sql"));
            RodarScript(conexao, Path.Combine(pastaDosScripts, "02_dados_ficticios.sql"));
        }
        catch
        {
            // Se der erro no meio, apaga o arquivo incompleto
            // para ele ser criado de novo na próxima vez.
            SqliteConnection.ClearAllPools();
            if (File.Exists(caminhoDoBanco))
                File.Delete(caminhoDoBanco);
            throw;
        }
    }

    private static void RodarScript(SqliteConnection conexao, string arquivo)
    {
        if (!File.Exists(arquivo))
            throw new FileNotFoundException("Não achei o script do banco: " + arquivo);

        // O SQLite aceita vários comandos separados por ";",
        // então o script inteiro é executado em um único comando.
        using var comando = conexao.CreateCommand();
        comando.CommandText = File.ReadAllText(arquivo);
        comando.ExecuteNonQuery();
    }
}
