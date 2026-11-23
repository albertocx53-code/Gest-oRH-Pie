-- Script 02 - Dados fictícios para teste e apresentação
-- Parte: banco de dados
--
-- Todos os nomes e valores são inventados.
-- São os mesmos funcionários e vagas da demonstração do sistema.
-- A data de referência é 13/11/2026, por isso o ponto vai de 09/11 a 13/11.

PRAGMA foreign_keys = ON;


-- Usuários do login
-- Senha das três contas: Aurora.demo26 (informada no README do sistema).
-- Os valores abaixo são o salt e o hash da senha.
INSERT INTO usuarios (login, nome, perfil, senha_salt, senha_hash, iteracoes) VALUES
('admin',  'Administrador', 'Administrador',
           '7907557BF2ECD226C5A03C3A5F2B4FD2',
           '2A1AF8E248DD14D1A1FFD3B299D7461B1A61237461F402924B4BB6259F454202', 600000),
('rh',     'Helena',        'Recursos Humanos',
           'D96EE1CB7D75841B50F537815DB5352A',
           '3378310030710DD857016843E2C91781FFF7665942A8B81A4E35BA6238B64F3A', 600000),
('gestor', 'Gestor',        'Gestor',
           '4162217F056D5918171A220D05C764B3',
           '4611262F6DFCBB5CA51F7F898DB60563D2C60BC752165079DA0823DB6981C772', 600000);


-- Departamentos
INSERT INTO departamentos (id, nome) VALUES
(1, 'Pessoas'),
(2, 'Tecnologia'),
(3, 'Financeiro'),
(4, 'Comercial'),
(5, 'Operações');


-- Funcionários
-- As matrículas começam em 101. O próximo cadastro recebe a 109 (AUTOINCREMENT).
INSERT INTO funcionarios (matricula, nome, cargo, departamento_id, situacao, data_admissao) VALUES
(101, 'Marina Teixeira',  'Analista de RH',        1, 'Ativo',    '2022-03-14'),
(102, 'Rafael Monteiro',  'Desenvolvedor',         2, 'Ativo',    '2021-08-02'),
(103, 'Helena Duarte',    'Contadora',             3, 'Ativo',    '2020-05-18'),
(104, 'Caio Albuquerque', 'Executivo de contas',   4, 'Férias',   '2023-01-09'),
(105, 'Beatriz Lemos',    'Coord. de operações',   5, 'Ativo',    '2019-11-25'),
(106, 'Tiago Ferreira',   'Analista de suporte',   2, 'Afastado', '2024-02-05'),
(107, 'Larissa Campos',   'Assistente financeiro', 3, 'Ativo',    '2025-06-16'),
(108, 'Otávio Prado',     'Gerente comercial',     4, 'Ativo',    '2018-09-03');


-- Ponto da semana (09/11/2026 a 13/11/2026)
-- Só há registro para funcionários com situação "Ativo"
-- (Caio está de férias e Tiago está afastado).

-- Segunda 09/11
INSERT INTO ponto (matricula, dia, entrada, saida, situacao, motivo) VALUES
(101, '2026-11-09', '08:14', '17:00', 'Atraso',   'Atraso no transporte público.'),
(102, '2026-11-09', '08:00', '17:00', 'Presença', NULL),
(103, '2026-11-09', '08:00', '17:00', 'Presença', NULL),
(105, '2026-11-09', '08:00', '17:00', 'Presença', NULL),
(107, '2026-11-09', '08:00', '17:00', 'Presença', NULL),
(108, '2026-11-09', NULL,    NULL,    'Falta',    'Compromisso pessoal comunicado ao RH.');

-- Terça 10/11
INSERT INTO ponto (matricula, dia, entrada, saida, situacao, motivo) VALUES
(101, '2026-11-10', '08:14', '17:00', 'Atraso',   'Atraso no transporte público.'),
(102, '2026-11-10', '08:00', '17:00', 'Presença', NULL),
(103, '2026-11-10', '08:00', '17:00', 'Presença', NULL),
(105, '2026-11-10', '08:00', '17:00', 'Presença', NULL),
(107, '2026-11-10', '08:00', '17:00', 'Presença', NULL),
(108, '2026-11-10', '08:00', '17:00', 'Presença', NULL);

-- Quarta 11/11
INSERT INTO ponto (matricula, dia, entrada, saida, situacao, motivo) VALUES
(101, '2026-11-11', '08:14', '17:00', 'Atraso',   'Atraso no transporte público.'),
(102, '2026-11-11', '08:00', '17:00', 'Presença', NULL),
(103, '2026-11-11', '08:00', '17:00', 'Presença', NULL),
(105, '2026-11-11', '08:00', '17:00', 'Presença', NULL),
(107, '2026-11-11', NULL,    NULL,    'Falta',    'Compromisso pessoal comunicado ao RH.'),
(108, '2026-11-11', NULL,    NULL,    'Falta',    'Compromisso pessoal comunicado ao RH.');

-- Quinta 12/11
INSERT INTO ponto (matricula, dia, entrada, saida, situacao, motivo) VALUES
(101, '2026-11-12', '08:14', '17:00', 'Atraso',   'Atraso no transporte público.'),
(102, '2026-11-12', '08:00', '17:00', 'Presença', NULL),
(103, '2026-11-12', '08:00', '17:00', 'Presença', NULL),
(105, '2026-11-12', '08:00', '17:00', 'Presença', NULL),
(107, '2026-11-12', '08:00', '17:00', 'Presença', NULL),
(108, '2026-11-12', '08:00', '17:00', 'Presença', NULL);

-- Sexta 13/11
INSERT INTO ponto (matricula, dia, entrada, saida, situacao, motivo) VALUES
(101, '2026-11-13', '08:14', '17:00', 'Atraso',   'Atraso no transporte público.'),
(102, '2026-11-13', '08:00', '17:00', 'Presença', NULL),
(103, '2026-11-13', '08:00', '17:00', 'Presença', NULL),
(105, '2026-11-13', '08:00', '17:00', 'Presença', NULL),
(107, '2026-11-13', '08:00', '17:00', 'Presença', NULL),
(108, '2026-11-13', NULL,    NULL,    'Falta',    'Compromisso pessoal comunicado ao RH.');


-- Folha de pagamento
-- Outubro e novembro, para ter histórico de mais de um mês.
-- Bônus de R$ 180 e desconto de R$ 125 para todos.

-- Outubro/2026
INSERT INTO folha_pagamento (matricula, competencia, salario_base, bonus, desconto) VALUES
(101, '2026-10-01', 3310.00, 180.00, 125.00),
(102, '2026-10-01', 3720.00, 180.00, 125.00),
(103, '2026-10-01', 4130.00, 180.00, 125.00),
(105, '2026-10-01', 4950.00, 180.00, 125.00),
(107, '2026-10-01', 5770.00, 180.00, 125.00),
(108, '2026-10-01', 6180.00, 180.00, 125.00);

-- Novembro/2026
INSERT INTO folha_pagamento (matricula, competencia, salario_base, bonus, desconto) VALUES
(101, '2026-11-01', 3310.00, 180.00, 125.00),
(102, '2026-11-01', 3720.00, 180.00, 125.00),
(103, '2026-11-01', 4130.00, 180.00, 125.00),
(105, '2026-11-01', 4950.00, 180.00, 125.00),
(107, '2026-11-01', 5770.00, 180.00, 125.00),
(108, '2026-11-01', 6180.00, 180.00, 125.00);


-- Vagas abertas
INSERT INTO vagas (id, titulo, departamento_id, situacao) VALUES
(1, 'Desenvolvedor back-end', 2, 'Aberta'),
(2, 'Analista financeiro',    3, 'Aberta'),
(3, 'Assistente de RH',       1, 'Aberta');


-- Candidatos (cada um ligado a uma vaga pelo vaga_id)
INSERT INTO candidatos (id, nome, vaga_id, etapa) VALUES
(1, 'Júlia Nogueira',  1, 'Entrevista'),
(2, 'Amanda Rocha',    1, 'Teste técnico'),
(3, 'Lucas Barreto',   2, 'Triagem'),
(4, 'Renata Siqueira', 2, 'Proposta');


-- Agenda do RH
INSERT INTO agenda (titulo, data_hora, categoria) VALUES
('Entrevista · Júlia Nogueira',       '2026-11-13 10:30', 'Recrutamento'),
('Revisão das ocorrências de ponto',  '2026-11-13 14:00', 'Ponto'),
('Fechamento da folha de novembro',   '2026-11-27 16:00', 'Salários');
