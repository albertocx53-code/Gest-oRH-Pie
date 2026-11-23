// Camada de dados da versão web
//
// Usa o mesmo SQLite do sistema desktop, só que rodando no navegador (biblioteca sql.js).
// Na primeira vez, executa os mesmos scripts da pasta BancoDeDados
// (01_criar_tabelas.sql e 02_dados_ficticios.sql).
// O banco fica salvo no próprio navegador (localStorage), então cada computador
// tem a sua cópia dos dados.

const CHAVE_BANCO = "gestao-rh-banco-v1";
const CODIGO_RESTRICAO = /constraint/i; // erro do SQLite quando uma regra da tabela é violada

class ErroRegra extends Error {}

let SQL = null;
let db = null;

async function abrirBanco() {
  SQL = await initSqlJs({ locateFile: (arquivo) => "lib/" + arquivo });
  const salvo = lerBancoSalvo();
  if (salvo) {
    db = new SQL.Database(salvo);
  } else {
    await criarBancoNovo();
  }
  // No SQLite a chave estrangeira vem desligada, então liga sempre que abrir.
  db.exec("PRAGMA foreign_keys = ON;");
}

async function criarBancoNovo() {
  db = new SQL.Database();
  for (const script of ["sql/01_criar_tabelas.sql", "sql/02_dados_ficticios.sql"]) {
    const resposta = await fetch(script);
    if (!resposta.ok) throw new Error("Não foi possível carregar o script " + script);
    db.exec(await resposta.text());
  }
  salvarBanco();
}

async function restaurarDadosDeExemplo() {
  try { localStorage.removeItem(CHAVE_BANCO); } catch { /* sem acesso ao armazenamento */ }
  await criarBancoNovo();
  db.exec("PRAGMA foreign_keys = ON;");
}

function lerBancoSalvo() {
  try {
    const texto = localStorage.getItem(CHAVE_BANCO);
    if (!texto) return null;
    const binario = atob(texto);
    const bytes = new Uint8Array(binario.length);
    for (let i = 0; i < binario.length; i++) bytes[i] = binario.charCodeAt(i);
    return bytes;
  } catch {
    return null;
  }
}

function salvarBanco() {
  try {
    const bytes = db.export();
    let binario = "";
    for (let i = 0; i < bytes.length; i += 0x8000) {
      binario += String.fromCharCode.apply(null, bytes.subarray(i, i + 0x8000));
    }
    localStorage.setItem(CHAVE_BANCO, btoa(binario));
  } catch {
    // Se o navegador não deixar salvar, o sistema continua funcionando até fechar a aba.
  }
  // db.export() fecha as configurações da conexão, então liga as chaves de novo.
  db.exec("PRAGMA foreign_keys = ON;");
}

// SELECT: devolve uma lista de objetos, um por linha
function consultar(sql, parametros = {}) {
  const comando = db.prepare(sql);
  comando.bind(parametros);
  const linhas = [];
  while (comando.step()) linhas.push(comando.getAsObject());
  comando.free();
  return linhas;
}

// INSERT / UPDATE / DELETE: se o banco recusar, mostra uma mensagem que o usuário entende
function executar(sql, parametros, mensagemSeDerErro) {
  try {
    db.run(sql, parametros);
  } catch (erro) {
    if (CODIGO_RESTRICAO.test(erro.message)) throw new ErroRegra(mensagemSeDerErro);
    throw erro;
  }
  salvarBanco();
}

const vazioViraNull = (texto) => (texto && texto.trim() ? texto.trim() : null);

// Mesmas operações do RepositorioSqlite.cs
const Repositorio = {
  funcionarios() {
    return consultar(`
      SELECT f.matricula AS id, f.nome, f.cargo, d.nome AS departamento, f.situacao, f.data_admissao AS admissao
      FROM funcionarios f
      JOIN departamentos d ON d.id = f.departamento_id
      ORDER BY f.matricula`);
  },

  pontos() {
    return consultar(`
      SELECT matricula AS pessoa, dia, IFNULL(entrada, '') AS entrada, IFNULL(saida, '') AS saida,
             situacao, IFNULL(motivo, '') AS motivo
      FROM ponto ORDER BY dia, matricula`);
  },

  vagas() {
    return consultar(`
      SELECT v.id, v.titulo, d.nome AS departamento, v.situacao
      FROM vagas v JOIN departamentos d ON d.id = v.departamento_id
      ORDER BY v.id`);
  },

  candidatos() {
    return consultar("SELECT id, nome, vaga_id AS vaga, etapa FROM candidatos ORDER BY id");
  },

  folha() {
    return consultar(`
      SELECT matricula AS pessoa, competencia, salario_base AS base, bonus, desconto
      FROM folha_pagamento ORDER BY competencia, matricula`);
  },

  agenda() {
    return consultar("SELECT id, titulo, data_hora AS quando, categoria FROM agenda ORDER BY data_hora, id");
  },

  contaPorLogin(login) {
    return consultar(`
      SELECT login, nome, perfil, senha_salt AS salt, senha_hash AS hash, iteracoes
      FROM usuarios WHERE login = $login COLLATE NOCASE`, { $login: login })[0] || null;
  },

  salvarFuncionario(f) {
    const p = { $nome: f.nome, $cargo: f.cargo, $departamento: f.departamento, $situacao: f.situacao, $admissao: f.admissao };
    if (!f.id) {
      executar(`
        INSERT INTO funcionarios (nome, cargo, departamento_id, situacao, data_admissao)
        VALUES ($nome, $cargo, (SELECT id FROM departamentos WHERE nome = $departamento), $situacao, $admissao)`,
        p, "Não foi possível salvar o funcionário. Confira o departamento e a situação.");
    } else {
      executar(`
        UPDATE funcionarios
        SET nome = $nome, cargo = $cargo,
            departamento_id = (SELECT id FROM departamentos WHERE nome = $departamento),
            situacao = $situacao, data_admissao = $admissao
        WHERE matricula = $matricula`,
        { ...p, $matricula: f.id }, "Não foi possível salvar o funcionário. Confira o departamento e a situação.");
    }
  },

  apagarFuncionario(id) {
    executar("DELETE FROM funcionarios WHERE matricula = $matricula", { $matricula: id },
      "Este cadastro tem registros de ponto ou salário. Você pode mudar a situação para Inativo.");
  },

  salvarPonto(r, edicao) {
    const p = { $matricula: r.pessoa, $dia: r.dia, $entrada: vazioViraNull(r.entrada), $saida: vazioViraNull(r.saida),
                $situacao: r.situacao, $motivo: vazioViraNull(r.motivo) };
    const sql = edicao
      ? `UPDATE ponto SET entrada = $entrada, saida = $saida, situacao = $situacao, motivo = $motivo
         WHERE matricula = $matricula AND dia = $dia`
      : `INSERT INTO ponto (matricula, dia, entrada, saida, situacao, motivo)
         VALUES ($matricula, $dia, $entrada, $saida, $situacao, $motivo)`;
    executar(sql, p, "Já existe ponto nesse dia. Use Editar registro para alterá-lo.");
  },

  salvarVaga(v) {
    const p = { $titulo: v.titulo, $departamento: v.departamento, $situacao: v.situacao };
    if (!v.id) {
      executar(`INSERT INTO vagas (titulo, departamento_id, situacao)
                VALUES ($titulo, (SELECT id FROM departamentos WHERE nome = $departamento), $situacao)`,
        p, "Não foi possível salvar a vaga. Confira o departamento.");
    } else {
      executar(`UPDATE vagas SET titulo = $titulo,
                  departamento_id = (SELECT id FROM departamentos WHERE nome = $departamento), situacao = $situacao
                WHERE id = $id`, { ...p, $id: v.id }, "Não foi possível salvar a vaga. Confira o departamento.");
    }
  },

  salvarCandidato(c) {
    const p = { $nome: c.nome, $vaga: c.vaga, $etapa: c.etapa };
    if (!c.id) {
      executar("INSERT INTO candidatos (nome, vaga_id, etapa) VALUES ($nome, $vaga, $etapa)", p,
        "Não foi possível salvar a candidatura. Confira a vaga escolhida.");
    } else {
      executar("UPDATE candidatos SET nome = $nome, vaga_id = $vaga, etapa = $etapa WHERE id = $id", { ...p, $id: c.id },
        "Não foi possível salvar a candidatura. Confira a vaga escolhida.");
    }
  },

  salvarFolha(f) {
    // INSERT OR REPLACE: se já existir lançamento da pessoa no mês, substitui
    executar(`INSERT OR REPLACE INTO folha_pagamento (matricula, competencia, salario_base, bonus, desconto)
              VALUES ($matricula, $competencia, $base, $bonus, $desconto)`,
      { $matricula: f.pessoa, $competencia: f.competencia, $base: f.base, $bonus: f.bonus, $desconto: f.desconto },
      "Não foi possível salvar a folha. Confira o funcionário e os valores.");
  },

  adicionarCompromisso(a) {
    executar("INSERT INTO agenda (titulo, data_hora, categoria) VALUES ($titulo, $quando, $categoria)",
      { $titulo: a.titulo, $quando: a.quando, $categoria: a.categoria },
      "Não foi possível salvar o compromisso. Confira a categoria.");
  },
};
