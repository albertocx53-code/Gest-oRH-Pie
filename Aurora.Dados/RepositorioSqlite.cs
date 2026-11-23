using System.Globalization;
using Aurora.Core;
using Microsoft.Data.Sqlite;

namespace Aurora.Dados;

// RepositorioSqlite - camada de acesso aos dados
//
// Camada de acesso aos dados. Lê e grava as informações no arquivo aurora.db.
// Antes o sistema usava uma lista em memória (DemoRh) e perdia tudo ao fechar.
//
// Implementa a interface IRhRepository, então as telas e o RhService
// continuam chamando os mesmos métodos, sem precisar de mudança.
//
// Divisão de responsabilidades:
//   RhService         -> permissões e regras de negócio
//   RepositorioSqlite -> leitura e gravação no banco
public class RepositorioSqlite : IRhRepository
{
    // O SQLite não tem tipo próprio para data, então as datas são salvas como texto
    // nestes formatos.
    private const string FormatoDia = "yyyy-MM-dd";
    private const string FormatoDiaHora = "yyyy-MM-dd HH:mm";

    // Código de erro do SQLite quando uma restrição da tabela é violada
    // (chave primária repetida, chave estrangeira, CHECK).
    private const int ErroDeRestricao = 19;

    private readonly string caminhoDoBanco;

    public RepositorioSqlite(string caminhoDoBanco)
    {
        this.caminhoDoBanco = caminhoDoBanco;
    }


    // ---------- LEITURAS (SELECT) ----------
    // Todas as consultas usam ORDER BY para a lista vir sempre na mesma ordem.

    public IReadOnlyList<Employee> Employees
    {
        get
        {
            var lista = new List<Employee>();
            using var conexao = Conexao.Abrir(caminhoDoBanco);
            using var comando = conexao.CreateCommand();
            // JOIN com departamentos para trazer o nome do setor, que é o que a tela mostra
            comando.CommandText = @"
                SELECT f.matricula, f.nome, f.cargo, d.nome, f.situacao, f.data_admissao
                FROM funcionarios f
                JOIN departamentos d ON d.id = f.departamento_id
                ORDER BY f.matricula";

            using var leitor = comando.ExecuteReader();
            while (leitor.Read())
            {
                lista.Add(new Employee(
                    leitor.GetInt32(0),
                    leitor.GetString(1),
                    leitor.GetString(2),
                    leitor.GetString(3),
                    leitor.GetString(4),
                    LerDia(leitor.GetString(5))));
            }
            return lista;
        }
    }

    public IReadOnlyList<Attendance> Punches
    {
        get
        {
            var lista = new List<Attendance>();
            using var conexao = Conexao.Abrir(caminhoDoBanco);
            using var comando = conexao.CreateCommand();
            comando.CommandText = @"
                SELECT matricula, dia, entrada, saida, situacao, motivo
                FROM ponto
                ORDER BY dia, matricula";

            using var leitor = comando.ExecuteReader();
            while (leitor.Read())
            {
                // Na falta, entrada, saída e motivo ficam NULL no banco.
                // A tela espera texto vazio, por isso NULL vira "".
                lista.Add(new Attendance(
                    leitor.GetInt32(0),
                    LerDia(leitor.GetString(1)),
                    TextoOuVazio(leitor, 2),
                    TextoOuVazio(leitor, 3),
                    leitor.GetString(4),
                    TextoOuVazio(leitor, 5)));
            }
            return lista;
        }
    }

    public IReadOnlyList<Vacancy> Vacancies
    {
        get
        {
            var lista = new List<Vacancy>();
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
                lista.Add(new Vacancy(leitor.GetInt32(0), leitor.GetString(1), leitor.GetString(2), leitor.GetString(3)));
            }
            return lista;
        }
    }

    public IReadOnlyList<Candidate> Candidates
    {
        get
        {
            var lista = new List<Candidate>();
            using var conexao = Conexao.Abrir(caminhoDoBanco);
            using var comando = conexao.CreateCommand();
            comando.CommandText = "SELECT id, nome, vaga_id, etapa FROM candidatos ORDER BY id";

            using var leitor = comando.ExecuteReader();
            while (leitor.Read())
            {
                lista.Add(new Candidate(leitor.GetInt32(0), leitor.GetString(1), leitor.GetInt32(2), leitor.GetString(3)));
            }
            return lista;
        }
    }

    public IReadOnlyList<Payroll> Pay
    {
        get
        {
            var lista = new List<Payroll>();
            using var conexao = Conexao.Abrir(caminhoDoBanco);
            using var comando = conexao.CreateCommand();
            comando.CommandText = @"
                SELECT matricula, competencia, salario_base, bonus, desconto
                FROM folha_pagamento
                ORDER BY competencia, matricula";

            using var leitor = comando.ExecuteReader();
            while (leitor.Read())
            {
                lista.Add(new Payroll(
                    leitor.GetInt32(0),
                    LerDia(leitor.GetString(1)),
                    LerDinheiro(leitor, 2),
                    LerDinheiro(leitor, 3),
                    LerDinheiro(leitor, 4)));
            }
            return lista;
        }
    }

    public IReadOnlyList<Appointment> Agenda
    {
        get
        {
            var lista = new List<Appointment>();
            using var conexao = Conexao.Abrir(caminhoDoBanco);
            using var comando = conexao.CreateCommand();
            comando.CommandText = "SELECT titulo, data_hora, categoria FROM agenda ORDER BY data_hora, id";

            using var leitor = comando.ExecuteReader();
            while (leitor.Read())
            {
                var quando = DateTime.ParseExact(leitor.GetString(1), FormatoDiaHora, CultureInfo.InvariantCulture);
                lista.Add(new Appointment(leitor.GetString(0), quando, leitor.GetString(2)));
            }
            return lista;
        }
    }


    // ---------- GRAVAÇÕES (INSERT / UPDATE / DELETE) ----------
    // Os valores são passados por parâmetros ($nome, $cargo...) e não juntando texto
    // no SQL. Isso evita erro com nomes que têm apóstrofo (ex.: D'Ávila)
    // e protege contra SQL Injection.

    public void SaveEmployee(Employee employee)
    {
        using var conexao = Conexao.Abrir(caminhoDoBanco);
        using var comando = conexao.CreateCommand();

        if (employee.Id == 0)
        {
            // Id 0 = funcionário novo. A matrícula é gerada pelo banco (AUTOINCREMENT).
            comando.CommandText = @"
                INSERT INTO funcionarios (nome, cargo, departamento_id, situacao, data_admissao)
                VALUES ($nome, $cargo,
                        (SELECT id FROM departamentos WHERE nome = $departamento),
                        $situacao, $admissao)";
        }
        else
        {
            comando.CommandText = @"
                UPDATE funcionarios
                SET nome = $nome,
                    cargo = $cargo,
                    departamento_id = (SELECT id FROM departamentos WHERE nome = $departamento),
                    situacao = $situacao,
                    data_admissao = $admissao
                WHERE matricula = $matricula";
            comando.Parameters.AddWithValue("$matricula", employee.Id);
        }

        comando.Parameters.AddWithValue("$nome", employee.Name);
        comando.Parameters.AddWithValue("$cargo", employee.Role);
        comando.Parameters.AddWithValue("$departamento", employee.Team);
        comando.Parameters.AddWithValue("$situacao", employee.Status);
        comando.Parameters.AddWithValue("$admissao", EscreverDia(employee.Started));

        Executar(comando, "Não foi possível salvar o funcionário. Confira o departamento e a situação.");
    }

    public void DeleteEmployee(int id)
    {
        using var conexao = Conexao.Abrir(caminhoDoBanco);
        using var comando = conexao.CreateCommand();
        comando.CommandText = "DELETE FROM funcionarios WHERE matricula = $matricula";
        comando.Parameters.AddWithValue("$matricula", id);

        // O RhService já verifica isso antes, mas o banco também bloqueia
        // por causa do ON DELETE RESTRICT.
        Executar(comando, "Este cadastro tem registros de ponto ou salário. Você pode mudar a situação para Inativo.");
    }

    public void SaveAttendance(Attendance record, bool edit)
    {
        using var conexao = Conexao.Abrir(caminhoDoBanco);
        using var comando = conexao.CreateCommand();

        if (edit)
        {
            // Na edição, matrícula e dia (a chave) não mudam, só os outros campos.
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

        comando.Parameters.AddWithValue("$matricula", record.Person);
        comando.Parameters.AddWithValue("$dia", EscreverDia(record.Day));
        comando.Parameters.AddWithValue("$entrada", VazioViraNull(record.In));
        comando.Parameters.AddWithValue("$saida", VazioViraNull(record.Out));
        comando.Parameters.AddWithValue("$situacao", record.Status);
        comando.Parameters.AddWithValue("$motivo", VazioViraNull(record.Reason));

        // A chave (matricula, dia) impede dois registros de ponto no mesmo dia.
        Executar(comando, "Já existe ponto nesse dia. Use Editar registro para alterá-lo.");
    }

    public void SaveVacancy(Vacancy vacancy)
    {
        using var conexao = Conexao.Abrir(caminhoDoBanco);
        using var comando = conexao.CreateCommand();

        if (vacancy.Id == 0)
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
            comando.Parameters.AddWithValue("$id", vacancy.Id);
        }

        comando.Parameters.AddWithValue("$titulo", vacancy.Title);
        comando.Parameters.AddWithValue("$departamento", vacancy.Team);
        comando.Parameters.AddWithValue("$situacao", vacancy.Status);

        Executar(comando, "Não foi possível salvar a vaga. Confira o departamento.");
    }

    public void SaveCandidate(Candidate candidate)
    {
        using var conexao = Conexao.Abrir(caminhoDoBanco);
        using var comando = conexao.CreateCommand();

        if (candidate.Id == 0)
        {
            comando.CommandText = "INSERT INTO candidatos (nome, vaga_id, etapa) VALUES ($nome, $vaga, $etapa)";
        }
        else
        {
            comando.CommandText = "UPDATE candidatos SET nome = $nome, vaga_id = $vaga, etapa = $etapa WHERE id = $id";
            comando.Parameters.AddWithValue("$id", candidate.Id);
        }

        comando.Parameters.AddWithValue("$nome", candidate.Name);
        comando.Parameters.AddWithValue("$vaga", candidate.Vacancy);
        comando.Parameters.AddWithValue("$etapa", candidate.Stage);

        Executar(comando, "Não foi possível salvar a candidatura. Confira a vaga escolhida.");
    }

    public void SavePayroll(Payroll payroll)
    {
        using var conexao = Conexao.Abrir(caminhoDoBanco);
        using var comando = conexao.CreateCommand();

        // INSERT OR REPLACE: se já existir lançamento da pessoa nesse mês, substitui;
        // se não existir, cria um novo.
        comando.CommandText = @"
            INSERT OR REPLACE INTO folha_pagamento (matricula, competencia, salario_base, bonus, desconto)
            VALUES ($matricula, $competencia, $base, $bonus, $desconto)";

        // A competência é sempre o dia 1 do mês.
        var primeiroDiaDoMes = new DateTime(payroll.Month.Year, payroll.Month.Month, 1);

        comando.Parameters.AddWithValue("$matricula", payroll.Person);
        comando.Parameters.AddWithValue("$competencia", EscreverDia(primeiroDiaDoMes));
        comando.Parameters.AddWithValue("$base", (double)payroll.Base);
        comando.Parameters.AddWithValue("$bonus", (double)payroll.Bonus);
        comando.Parameters.AddWithValue("$desconto", (double)payroll.Discount);

        Executar(comando, "Não foi possível salvar a folha. Confira o funcionário e os valores.");
    }

    public void AddAppointment(Appointment appointment)
    {
        using var conexao = Conexao.Abrir(caminhoDoBanco);
        using var comando = conexao.CreateCommand();
        comando.CommandText = "INSERT INTO agenda (titulo, data_hora, categoria) VALUES ($titulo, $quando, $categoria)";
        comando.Parameters.AddWithValue("$titulo", appointment.Title);
        comando.Parameters.AddWithValue("$quando", appointment.Time.ToString(FormatoDiaHora, CultureInfo.InvariantCulture));
        comando.Parameters.AddWithValue("$categoria", appointment.Category);

        Executar(comando, "Não foi possível salvar o compromisso. Confira a categoria.");
    }


    // ---------- MÉTODOS AUXILIARES ----------

    // Executa o comando. Se o banco recusar por alguma restrição, troca o erro
    // do SQLite por uma mensagem que o usuário entende.
    // (RhRuleException é a exceção que as telas já sabem mostrar)
    private static void Executar(SqliteCommand comando, string mensagemSeDerErro)
    {
        try
        {
            comando.ExecuteNonQuery();
        }
        catch (SqliteException erro) when (erro.SqliteErrorCode == ErroDeRestricao)
        {
            throw new RhRuleException(mensagemSeDerErro);
        }
    }

    private static DateTime LerDia(string texto)
    {
        return DateTime.ParseExact(texto, FormatoDia, CultureInfo.InvariantCulture);
    }

    private static string EscreverDia(DateTime dia)
    {
        return dia.ToString(FormatoDia, CultureInfo.InvariantCulture);
    }

    private static string TextoOuVazio(SqliteDataReader leitor, int coluna)
    {
        return leitor.IsDBNull(coluna) ? "" : leitor.GetString(coluna);
    }

    private static object VazioViraNull(string texto)
    {
        // Texto vazio é salvo como NULL no banco.
        if (string.IsNullOrWhiteSpace(texto))
            return DBNull.Value;
        return texto;
    }

    private static decimal LerDinheiro(SqliteDataReader leitor, int coluna)
    {
        // O dinheiro é salvo como REAL. O valor é arredondado para 2 casas
        // para evitar números como 3310,0000001.
        return Math.Round((decimal)leitor.GetDouble(coluna), 2);
    }
}
