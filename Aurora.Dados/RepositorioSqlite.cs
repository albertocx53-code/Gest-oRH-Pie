using System.Globalization;
using Aurora.Core;
using Microsoft.Data.Sqlite;

namespace Aurora.Dados;

// Classe que lê e grava os dados no banco (arquivo aurora.db).
// Antes o sistema guardava tudo numa lista na memória (DadosDeExemplo) e perdia tudo ao fechar.
// Ela segue a interface IRepositorio, então as telas continuam iguais.
public class RepositorioSqlite : IRepositorio
{
    // O SQLite não tem tipo de data, então a data é guardada como texto nesses formatos
    private const string FormatoDia = "yyyy-MM-dd";
    private const string FormatoDiaHora = "yyyy-MM-dd HH:mm";

    // Número do erro que o SQLite dá quando uma regra da tabela é quebrada
    // (chave repetida, chave estrangeira ou CHECK)
    private const int ErroDeRestricao = 19;

    private string caminhoDoBanco;

    public RepositorioSqlite(string caminhoDoBanco)
    {
        this.caminhoDoBanco = caminhoDoBanco;
    }


    // ===== LEITURAS (SELECT) =====
    // Todas usam ORDER BY para a lista vir sempre na mesma ordem.

    public IReadOnlyList<Funcionario> Funcionarios
    {
        get
        {
            List<Funcionario> lista = new List<Funcionario>();
            using var conexao = Conexao.Abrir(caminhoDoBanco);
            using var comando = conexao.CreateCommand();

            // JOIN com departamentos para pegar o nome do setor
            comando.CommandText = @"
                SELECT f.matricula, f.nome, f.cargo, d.nome, f.situacao, f.data_admissao
                FROM funcionarios f
                JOIN departamentos d ON d.id = f.departamento_id
                ORDER BY f.matricula";

            using var leitor = comando.ExecuteReader();
            while (leitor.Read())
            {
                int matricula = leitor.GetInt32(0);
                string nome = leitor.GetString(1);
                string cargo = leitor.GetString(2);
                string departamento = leitor.GetString(3);
                string situacao = leitor.GetString(4);
                DateTime admissao = LerDia(leitor.GetString(5));

                lista.Add(new Funcionario(matricula, nome, cargo, departamento, situacao, admissao));
            }
            return lista;
        }
    }

    public IReadOnlyList<RegistroDePonto> Pontos
    {
        get
        {
            List<RegistroDePonto> lista = new List<RegistroDePonto>();
            using var conexao = Conexao.Abrir(caminhoDoBanco);
            using var comando = conexao.CreateCommand();
            comando.CommandText = @"
                SELECT matricula, dia, entrada, saida, situacao, motivo
                FROM ponto
                ORDER BY dia, matricula";

            using var leitor = comando.ExecuteReader();
            while (leitor.Read())
            {
                int matricula = leitor.GetInt32(0);
                DateTime dia = LerDia(leitor.GetString(1));
                // na falta, entrada, saída e motivo ficam vazios (NULL) no banco
                string entrada = TextoOuVazio(leitor, 2);
                string saida = TextoOuVazio(leitor, 3);
                string situacao = leitor.GetString(4);
                string motivo = TextoOuVazio(leitor, 5);

                lista.Add(new RegistroDePonto(matricula, dia, entrada, saida, situacao, motivo));
            }
            return lista;
        }
    }

    public IReadOnlyList<Vaga> Vagas
    {
        get
        {
            List<Vaga> lista = new List<Vaga>();
            using var conexao = Conexao.Abrir(caminhoDoBanco);
            using var comando = conexao.CreateCommand();
            comando.CommandText = @"
                SELECT v.id, v.titulo, d.nome, v.situacao
                FROM vagas v
                JOIN departamentos d ON d.id = v.departamento_id
                ORDER BY v.id";

            using var leitor = comando.ExecuteReader();
            while (leitor.Read())
            {
                int id = leitor.GetInt32(0);
                string titulo = leitor.GetString(1);
                string departamento = leitor.GetString(2);
                string situacao = leitor.GetString(3);

                lista.Add(new Vaga(id, titulo, departamento, situacao));
            }
            return lista;
        }
    }

    public IReadOnlyList<Candidato> Candidatos
    {
        get
        {
            List<Candidato> lista = new List<Candidato>();
            using var conexao = Conexao.Abrir(caminhoDoBanco);
            using var comando = conexao.CreateCommand();
            comando.CommandText = "SELECT id, nome, vaga_id, etapa FROM candidatos ORDER BY id";

            using var leitor = comando.ExecuteReader();
            while (leitor.Read())
            {
                int id = leitor.GetInt32(0);
                string nome = leitor.GetString(1);
                int vaga = leitor.GetInt32(2);
                string etapa = leitor.GetString(3);

                lista.Add(new Candidato(id, nome, vaga, etapa));
            }
            return lista;
        }
    }

    public IReadOnlyList<LancamentoDaFolha> Folhas
    {
        get
        {
            List<LancamentoDaFolha> lista = new List<LancamentoDaFolha>();
            using var conexao = Conexao.Abrir(caminhoDoBanco);
            using var comando = conexao.CreateCommand();
            comando.CommandText = @"
                SELECT matricula, competencia, salario_base, bonus, desconto
                FROM folha_pagamento
                ORDER BY competencia, matricula";

            using var leitor = comando.ExecuteReader();
            while (leitor.Read())
            {
                int matricula = leitor.GetInt32(0);
                DateTime competencia = LerDia(leitor.GetString(1));
                decimal salario = LerDinheiro(leitor, 2);
                decimal bonus = LerDinheiro(leitor, 3);
                decimal desconto = LerDinheiro(leitor, 4);

                lista.Add(new LancamentoDaFolha(matricula, competencia, salario, bonus, desconto));
            }
            return lista;
        }
    }

    public IReadOnlyList<Compromisso> Agenda
    {
        get
        {
            List<Compromisso> lista = new List<Compromisso>();
            using var conexao = Conexao.Abrir(caminhoDoBanco);
            using var comando = conexao.CreateCommand();
            comando.CommandText = "SELECT titulo, data_hora, categoria FROM agenda ORDER BY data_hora, id";

            using var leitor = comando.ExecuteReader();
            while (leitor.Read())
            {
                string titulo = leitor.GetString(0);
                DateTime quando = DateTime.ParseExact(leitor.GetString(1), FormatoDiaHora, CultureInfo.InvariantCulture);
                string categoria = leitor.GetString(2);

                lista.Add(new Compromisso(titulo, quando, categoria));
            }
            return lista;
        }
    }


    // ===== GRAVAÇÕES (INSERT, UPDATE e DELETE) =====
    // Os valores vão por parâmetro ($nome, $cargo...) e não colados no texto do SQL.
    // Assim não dá erro com nome que tem apóstrofo (ex.: D'Ávila) e evita SQL Injection.

    public void SalvarFuncionario(Funcionario funcionario)
    {
        using var conexao = Conexao.Abrir(caminhoDoBanco);
        using var comando = conexao.CreateCommand();

        if (funcionario.Id == 0)
        {
            // Id 0 = funcionário novo. A matrícula é o banco que gera (AUTOINCREMENT).
            comando.CommandText = @"
                INSERT INTO funcionarios (nome, cargo, departamento_id, situacao, data_admissao)
                VALUES ($nome, $cargo,
                        (SELECT id FROM departamentos WHERE nome = $departamento),
                        $situacao, $admissao)";
        }
        else
        {
            // funcionário que já existe: atualiza os dados
            comando.CommandText = @"
                UPDATE funcionarios
                SET nome = $nome,
                    cargo = $cargo,
                    departamento_id = (SELECT id FROM departamentos WHERE nome = $departamento),
                    situacao = $situacao,
                    data_admissao = $admissao
                WHERE matricula = $matricula";
            comando.Parameters.AddWithValue("$matricula", funcionario.Id);
        }

        comando.Parameters.AddWithValue("$nome", funcionario.Nome);
        comando.Parameters.AddWithValue("$cargo", funcionario.Cargo);
        comando.Parameters.AddWithValue("$departamento", funcionario.Departamento);
        comando.Parameters.AddWithValue("$situacao", funcionario.Situacao);
        comando.Parameters.AddWithValue("$admissao", EscreverDia(funcionario.Admissao));

        Executar(comando, "Não foi possível salvar o funcionário. Confira o departamento e a situação.");
    }

    public void ExcluirFuncionario(int id)
    {
        using var conexao = Conexao.Abrir(caminhoDoBanco);
        using var comando = conexao.CreateCommand();
        comando.CommandText = "DELETE FROM funcionarios WHERE matricula = $matricula";
        comando.Parameters.AddWithValue("$matricula", id);

        // Se o funcionário tiver ponto ou folha, o banco não deixa apagar (ON DELETE RESTRICT)
        Executar(comando, "Este cadastro tem registros de ponto ou salário. Você pode mudar a situação para Inativo.");
    }

    public void SalvarPonto(RegistroDePonto registro, bool editar)
    {
        using var conexao = Conexao.Abrir(caminhoDoBanco);
        using var comando = conexao.CreateCommand();

        if (editar)
        {
            // na edição, a matrícula e o dia não mudam, só o resto
            comando.CommandText = @"
                UPDATE ponto
                SET entrada = $entrada, saida = $saida, situacao = $situacao, motivo = $motivo
                WHERE matricula = $matricula AND dia = $dia";
        }
        else
        {
            comando.CommandText = @"
                INSERT INTO ponto (matricula, dia, entrada, saida, situacao, motivo)
                VALUES ($matricula, $dia, $entrada, $saida, $situacao, $motivo)";
        }

        comando.Parameters.AddWithValue("$matricula", registro.Matricula);
        comando.Parameters.AddWithValue("$dia", EscreverDia(registro.Dia));
        comando.Parameters.AddWithValue("$entrada", VazioViraNull(registro.Entrada));
        comando.Parameters.AddWithValue("$saida", VazioViraNull(registro.Saida));
        comando.Parameters.AddWithValue("$situacao", registro.Situacao);
        comando.Parameters.AddWithValue("$motivo", VazioViraNull(registro.Motivo));

        // A chave (matricula, dia) não deixa ter dois pontos no mesmo dia
        Executar(comando, "Já existe ponto nesse dia. Use Editar registro para alterá-lo.");
    }

    public void SalvarVaga(Vaga vaga)
    {
        using var conexao = Conexao.Abrir(caminhoDoBanco);
        using var comando = conexao.CreateCommand();

        if (vaga.Id == 0)
        {
            comando.CommandText = @"
                INSERT INTO vagas (titulo, departamento_id, situacao)
                VALUES ($titulo, (SELECT id FROM departamentos WHERE nome = $departamento), $situacao)";
        }
        else
        {
            comando.CommandText = @"
                UPDATE vagas
                SET titulo = $titulo,
                    departamento_id = (SELECT id FROM departamentos WHERE nome = $departamento),
                    situacao = $situacao
                WHERE id = $id";
            comando.Parameters.AddWithValue("$id", vaga.Id);
        }

        comando.Parameters.AddWithValue("$titulo", vaga.Titulo);
        comando.Parameters.AddWithValue("$departamento", vaga.Departamento);
        comando.Parameters.AddWithValue("$situacao", vaga.Situacao);

        Executar(comando, "Não foi possível salvar a vaga. Confira o departamento.");
    }

    public void SalvarCandidato(Candidato candidato)
    {
        using var conexao = Conexao.Abrir(caminhoDoBanco);
        using var comando = conexao.CreateCommand();

        if (candidato.Id == 0)
        {
            comando.CommandText = "INSERT INTO candidatos (nome, vaga_id, etapa) VALUES ($nome, $vaga, $etapa)";
        }
        else
        {
            comando.CommandText = "UPDATE candidatos SET nome = $nome, vaga_id = $vaga, etapa = $etapa WHERE id = $id";
            comando.Parameters.AddWithValue("$id", candidato.Id);
        }

        comando.Parameters.AddWithValue("$nome", candidato.Nome);
        comando.Parameters.AddWithValue("$vaga", candidato.VagaId);
        comando.Parameters.AddWithValue("$etapa", candidato.Etapa);

        Executar(comando, "Não foi possível salvar a candidatura. Confira a vaga escolhida.");
    }

    public void SalvarFolha(LancamentoDaFolha lancamento)
    {
        using var conexao = Conexao.Abrir(caminhoDoBanco);
        using var comando = conexao.CreateCommand();

        // INSERT OR REPLACE: se a pessoa já tem lançamento nesse mês, ele é trocado.
        // Se não tem, cria um novo.
        comando.CommandText = @"
            INSERT OR REPLACE INTO folha_pagamento (matricula, competencia, salario_base, bonus, desconto)
            VALUES ($matricula, $competencia, $base, $bonus, $desconto)";

        // a competência é sempre guardada como o dia 1 do mês
        DateTime primeiroDiaDoMes = new DateTime(lancamento.Competencia.Year, lancamento.Competencia.Month, 1);

        comando.Parameters.AddWithValue("$matricula", lancamento.Matricula);
        comando.Parameters.AddWithValue("$competencia", EscreverDia(primeiroDiaDoMes));
        comando.Parameters.AddWithValue("$base", (double)lancamento.Salario);
        comando.Parameters.AddWithValue("$bonus", (double)lancamento.Bonus);
        comando.Parameters.AddWithValue("$desconto", (double)lancamento.Desconto);

        Executar(comando, "Não foi possível salvar a folha. Confira o funcionário e os valores.");
    }

    public void AdicionarCompromisso(Compromisso compromisso)
    {
        using var conexao = Conexao.Abrir(caminhoDoBanco);
        using var comando = conexao.CreateCommand();
        comando.CommandText = "INSERT INTO agenda (titulo, data_hora, categoria) VALUES ($titulo, $quando, $categoria)";
        comando.Parameters.AddWithValue("$titulo", compromisso.Titulo);
        comando.Parameters.AddWithValue("$quando", compromisso.DataHora.ToString(FormatoDiaHora, CultureInfo.InvariantCulture));
        comando.Parameters.AddWithValue("$categoria", compromisso.Categoria);

        Executar(comando, "Não foi possível salvar o compromisso. Confira a categoria.");
    }


    // ===== MÉTODOS DE AJUDA =====

    // Roda o comando no banco. Se o banco recusar por causa de alguma regra,
    // mostra uma mensagem em português no lugar do erro do SQLite.
    // (a tela já sabe mostrar a ErroDeRegra)
    private static void Executar(SqliteCommand comando, string mensagemDeErro)
    {
        try
        {
            comando.ExecuteNonQuery();
        }
        catch (SqliteException erro)
        {
            if (erro.SqliteErrorCode == ErroDeRestricao)
            {
                throw new ErroDeRegra(mensagemDeErro);
            }
            throw; // outro tipo de erro: deixa passar
        }
    }

    // transforma o texto "2026-11-13" em data
    private static DateTime LerDia(string texto)
    {
        return DateTime.ParseExact(texto, FormatoDia, CultureInfo.InvariantCulture);
    }

    // transforma a data em texto "2026-11-13" para salvar no banco
    private static string EscreverDia(DateTime dia)
    {
        return dia.ToString(FormatoDia, CultureInfo.InvariantCulture);
    }

    // se a coluna estiver vazia (NULL), devolve "" para a tela
    private static string TextoOuVazio(SqliteDataReader leitor, int coluna)
    {
        if (leitor.IsDBNull(coluna))
        {
            return "";
        }
        return leitor.GetString(coluna);
    }

    // se o texto estiver vazio, salva NULL no banco
    private static object VazioViraNull(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return DBNull.Value;
        }
        return texto;
    }

    // O dinheiro fica salvo como REAL (número com vírgula).
    // Arredonda para 2 casas para não aparecer coisa tipo 3310,0000001.
    private static decimal LerDinheiro(SqliteDataReader leitor, int coluna)
    {
        double valor = leitor.GetDouble(coluna);
        return Math.Round((decimal)valor, 2);
    }
}
