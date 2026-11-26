using Microsoft.Data.Sqlite;

namespace Aurora.Dados;

// Abre a conexão com o banco SQLite. Todas as outras classes do banco usam essa.
public static class Conexao
{
    public static SqliteConnection Abrir(string caminhoDoBanco)
    {
        SqliteConnectionStringBuilder configuracao = new SqliteConnectionStringBuilder();
        configuracao.DataSource = caminhoDoBanco;
        // No SQLite as chaves estrangeiras vêm desligadas, então eu ligo aqui.
        // Sem isso o banco deixaria apagar um funcionário que tem ponto.
        configuracao.ForeignKeys = true;

        SqliteConnection conexao = new SqliteConnection(configuracao.ToString());
        conexao.Open();
        return conexao;
    }
}
