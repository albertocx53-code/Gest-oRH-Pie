using Microsoft.Data.Sqlite;

namespace Aurora.Dados;

// Conexao - camada de acesso aos dados
// Abre a conexão com o banco SQLite. Usada por todas as classes de dados.
public static class Conexao
{
    public static SqliteConnection Abrir(string caminhoDoBanco)
    {
        var config = new SqliteConnectionStringBuilder
        {
            DataSource = caminhoDoBanco,
            // Liga as chaves estrangeiras (no SQLite elas vêm desligadas).
            // Assim o banco não deixa, por exemplo, apagar funcionário que tem ponto.
            ForeignKeys = true
        };

        var conexao = new SqliteConnection(config.ToString());
        conexao.Open();
        return conexao;
    }
}
