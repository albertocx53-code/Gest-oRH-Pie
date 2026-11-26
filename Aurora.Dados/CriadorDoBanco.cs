using Microsoft.Data.Sqlite;

namespace Aurora.Dados;

// Cria o banco (aurora.db) na primeira vez que o sistema abre.
// Roda os scripts da pasta BancoDeDados:
//   01_criar_tabelas.sql   -> cria as tabelas
//   02_dados_ficticios.sql -> coloca os dados de exemplo
// Se o banco já existe, não faz nada (para não apagar o que foi cadastrado).
// Para voltar ao começo é só apagar o arquivo aurora.db.
public static class CriadorDoBanco
{
    public static void CriarSeNaoExistir(string caminhoDoBanco, string pastaDosScripts)
    {
        if (File.Exists(caminhoDoBanco))
        {
            return; // o banco já existe
        }

        try
        {
            using var conexao = Conexao.Abrir(caminhoDoBanco);
            RodarScript(conexao, Path.Combine(pastaDosScripts, "01_criar_tabelas.sql"));
            RodarScript(conexao, Path.Combine(pastaDosScripts, "02_dados_ficticios.sql"));
        }
        catch
        {
            // Se der erro no meio, apaga o banco pela metade
            // para ele ser criado certinho na próxima vez.
            SqliteConnection.ClearAllPools();
            if (File.Exists(caminhoDoBanco))
            {
                File.Delete(caminhoDoBanco);
            }
            throw;
        }
    }

    private static void RodarScript(SqliteConnection conexao, string arquivo)
    {
        if (!File.Exists(arquivo))
        {
            throw new FileNotFoundException("Não achei o script do banco: " + arquivo);
        }

        // O SQLite aceita vários comandos separados por ";",
        // então dá pra rodar o arquivo inteiro de uma vez.
        using var comando = conexao.CreateCommand();
        comando.CommandText = File.ReadAllText(arquivo);
        comando.ExecuteNonQuery();
    }
}
