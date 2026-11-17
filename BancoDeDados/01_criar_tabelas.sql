-- Script 01 - Criação das tabelas do Gestão RH
-- Autor: Alberto (parte do banco de dados)
--
-- Banco utilizado: SQLite. O banco fica em um único arquivo (aurora.db),
-- sem precisar instalar servidor.
--
-- Ordem de execução: este script primeiro, depois o 02_dados_ficticios.sql


-- Liga a verificação de chave estrangeira (no SQLite ela vem desligada).
PRAGMA foreign_keys = ON;

-- Apaga as tabelas, caso já existam, para recriar do zero.
-- As tabelas dependentes são apagadas primeiro por causa das chaves estrangeiras.
DROP TABLE IF EXISTS agenda;
DROP TABLE IF EXISTS candidatos;
DROP TABLE IF EXISTS vagas;
DROP TABLE IF EXISTS folha_pagamento;
DROP TABLE IF EXISTS ponto;
DROP TABLE IF EXISTS funcionarios;
DROP TABLE IF EXISTS departamentos;
DROP TABLE IF EXISTS usuarios;


-- USUARIOS: contas de acesso ao sistema.
-- A senha não é salva em texto, apenas o salt e o hash usados no login.
CREATE TABLE usuarios (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    login       TEXT    NOT NULL UNIQUE,     -- ex.: admin, rh, gestor
    nome        TEXT    NOT NULL,
    perfil      TEXT    NOT NULL
                CHECK (perfil IN ('Administrador', 'Recursos Humanos', 'Gestor')),
    senha_salt  TEXT    NOT NULL,
    senha_hash  TEXT    NOT NULL,
    iteracoes   INTEGER NOT NULL DEFAULT 600000
);


-- DEPARTAMENTOS
-- Tabela separada para evitar repetir o nome do setor em funcionários e vagas.
CREATE TABLE departamentos (
    id    INTEGER PRIMARY KEY AUTOINCREMENT,
    nome  TEXT    NOT NULL UNIQUE
);


-- FUNCIONARIOS
-- A matrícula é a chave primária e liga o funcionário ao ponto e à folha.
CREATE TABLE funcionarios (
    matricula        INTEGER PRIMARY KEY AUTOINCREMENT,
    nome             TEXT    NOT NULL,
    cargo            TEXT    NOT NULL,
    departamento_id  INTEGER NOT NULL,
    situacao         TEXT    NOT NULL DEFAULT 'Ativo'
                     CHECK (situacao IN ('Ativo', 'Férias', 'Afastado', 'Inativo')),
    data_admissao    TEXT    NOT NULL,       -- formato AAAA-MM-DD

    FOREIGN KEY (departamento_id) REFERENCES departamentos (id)
);


-- PONTO
-- Um registro por funcionário por dia. A chave (matricula, dia)
-- impede dois registros no mesmo dia.
-- Em caso de falta, entrada e saída ficam NULL.
CREATE TABLE ponto (
    matricula  INTEGER NOT NULL,
    dia        TEXT    NOT NULL,             -- AAAA-MM-DD
    entrada    TEXT,                         -- HH:MM
    saida      TEXT,                         -- HH:MM
    situacao   TEXT    NOT NULL
               CHECK (situacao IN ('Presença', 'Atraso', 'Falta')),
    motivo     TEXT    CHECK (length(motivo) <= 240),   -- usado em falta ou atraso

    PRIMARY KEY (matricula, dia),

    -- RESTRICT: impede apagar funcionário que tem ponto registrado,
    -- para não perder o histórico. Nesse caso a situação vira "Inativo".
    FOREIGN KEY (matricula) REFERENCES funcionarios (matricula) ON DELETE RESTRICT
);


-- FOLHA_PAGAMENTO
-- Um lançamento por funcionário por mês.
-- A competência é sempre o dia 1 do mês (ex.: novembro = 2026-11-01).
-- O valor líquido não é salvo, é calculado: salario_base + bonus - desconto
CREATE TABLE folha_pagamento (
    matricula     INTEGER NOT NULL,
    competencia   TEXT    NOT NULL,
    salario_base  REAL    NOT NULL CHECK (salario_base >= 0),
    bonus         REAL    NOT NULL DEFAULT 0 CHECK (bonus >= 0),
    desconto      REAL    NOT NULL DEFAULT 0 CHECK (desconto >= 0),

    PRIMARY KEY (matricula, competencia),
    FOREIGN KEY (matricula) REFERENCES funcionarios (matricula) ON DELETE RESTRICT
);


-- VAGAS: vagas do recrutamento
CREATE TABLE vagas (
    id               INTEGER PRIMARY KEY AUTOINCREMENT,
    titulo           TEXT    NOT NULL,
    departamento_id  INTEGER NOT NULL,
    situacao         TEXT    NOT NULL DEFAULT 'Aberta'
                     CHECK (situacao IN ('Aberta', 'Encerrada')),

    FOREIGN KEY (departamento_id) REFERENCES departamentos (id)
);


-- CANDIDATOS
-- Cada candidato está ligado a uma vaga (vaga_id).
-- Uma vaga pode ter vários candidatos (relação 1:N).
CREATE TABLE candidatos (
    id       INTEGER PRIMARY KEY AUTOINCREMENT,
    nome     TEXT    NOT NULL,
    vaga_id  INTEGER NOT NULL,
    etapa    TEXT    NOT NULL DEFAULT 'Triagem'
             CHECK (etapa IN ('Triagem', 'Entrevista', 'Teste técnico', 'Proposta', 'Contratado')),

    FOREIGN KEY (vaga_id) REFERENCES vagas (id) ON DELETE RESTRICT
);


-- AGENDA: compromissos do RH. Não tem relação com outras tabelas.
CREATE TABLE agenda (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    titulo     TEXT    NOT NULL,
    data_hora  TEXT    NOT NULL,             -- AAAA-MM-DD HH:MM
    categoria  TEXT    NOT NULL
               CHECK (categoria IN ('Funcionários', 'Ponto', 'Recrutamento', 'Salários'))
);


-- Índices para deixar as buscas mais rápidas (ponto por dia, folha por mês, etc.).
CREATE INDEX idx_ponto_dia          ON ponto (dia);
CREATE INDEX idx_folha_competencia  ON folha_pagamento (competencia);
CREATE INDEX idx_candidatos_vaga    ON candidatos (vaga_id);
CREATE INDEX idx_agenda_data        ON agenda (data_hora);
