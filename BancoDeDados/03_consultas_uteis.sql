-- Script 03 - Consultas de exemplo
-- Autor: Alberto
--
-- Este script só faz SELECT, não altera nada no banco.
-- Mostra os relacionamentos funcionando (JOINs) e ajuda a conferir os dados.
-- Pode ser executado no DB Browser for SQLite, aba "Executar SQL".


-- 1) Lista de funcionários com o nome do departamento
--    (JOIN entre funcionarios e departamentos)
SELECT f.matricula,
       f.nome,
       f.cargo,
       d.nome AS departamento,
       f.situacao
FROM funcionarios f
JOIN departamentos d ON d.id = f.departamento_id
ORDER BY f.matricula;


-- 2) Quantidade de funcionários por departamento
SELECT d.nome AS departamento,
       COUNT(f.matricula) AS total
FROM departamentos d
LEFT JOIN funcionarios f ON f.departamento_id = d.id
GROUP BY d.nome
ORDER BY total DESC;


-- 3) Ponto de um dia (13/11/2026) com o nome de cada funcionário
SELECT f.nome,
       p.entrada,
       p.saida,
       p.situacao,
       p.motivo
FROM ponto p
JOIN funcionarios f ON f.matricula = p.matricula
WHERE p.dia = '2026-11-13'
ORDER BY f.nome;


-- 4) Quantidade de presentes em cada dia
--    (falta não tem horário de entrada, por isso não é contada)
SELECT dia,
       COUNT(entrada) AS presentes
FROM ponto
GROUP BY dia
ORDER BY dia;


-- 5) Ranking de faltas e atrasos no mês
SELECT f.nome,
       SUM(CASE WHEN p.situacao = 'Falta'  THEN 1 ELSE 0 END) AS faltas,
       SUM(CASE WHEN p.situacao = 'Atraso' THEN 1 ELSE 0 END) AS atrasos
FROM ponto p
JOIN funcionarios f ON f.matricula = p.matricula
WHERE p.dia BETWEEN '2026-11-01' AND '2026-11-30'
GROUP BY f.nome
HAVING faltas > 0 OR atrasos > 0
ORDER BY faltas DESC, atrasos DESC;


-- 6) Folha de novembro com o valor líquido calculado
SELECT f.nome,
       fp.salario_base,
       fp.bonus,
       fp.desconto,
       (fp.salario_base + fp.bonus - fp.desconto) AS liquido
FROM folha_pagamento fp
JOIN funcionarios f ON f.matricula = fp.matricula
WHERE fp.competencia = '2026-11-01'
ORDER BY liquido DESC;


-- 7) Total da folha por mês
SELECT competencia,
       SUM(salario_base + bonus - desconto) AS total_liquido
FROM folha_pagamento
GROUP BY competencia
ORDER BY competencia;


-- 8) Candidatos com a vaga e o departamento da vaga
--    (dois JOINs: candidatos -> vagas -> departamentos)
SELECT c.nome      AS candidato,
       v.titulo    AS vaga,
       d.nome      AS departamento,
       c.etapa
FROM candidatos c
JOIN vagas v         ON v.id = c.vaga_id
JOIN departamentos d ON d.id = v.departamento_id
ORDER BY v.titulo, c.nome;


-- 9) Vagas e quantos candidatos cada uma tem
--    (LEFT JOIN para mostrar também vagas sem candidatos)
SELECT v.titulo,
       v.situacao,
       COUNT(c.id) AS candidatos
FROM vagas v
LEFT JOIN candidatos c ON c.vaga_id = v.id
GROUP BY v.id
ORDER BY v.id;


-- 10) Compromissos da agenda de um dia
SELECT strftime('%H:%M', data_hora) AS hora,
       titulo,
       categoria
FROM agenda
WHERE date(data_hora) = '2026-11-13'
ORDER BY data_hora;


-- Teste das restrições (estes comandos devem dar erro)
-- Remova o "--" de uma linha por vez e execute para ver o banco bloqueando.

-- Tentar apagar a Marina, que tem ponto e folha -> dá erro de FOREIGN KEY
-- DELETE FROM funcionarios WHERE matricula = 101;

-- Tentar bater ponto duas vezes no mesmo dia -> dá erro de UNIQUE/PRIMARY KEY
-- INSERT INTO ponto (matricula, dia, entrada, saida, situacao) VALUES (102, '2026-11-13', '09:00', '18:00', 'Presença');

-- Candidato numa vaga que não existe -> dá erro de FOREIGN KEY
-- INSERT INTO candidatos (nome, vaga_id, etapa) VALUES ('Fulano', 99, 'Triagem');

-- Situação que não existe -> dá erro de CHECK
-- UPDATE funcionarios SET situacao = 'Demitido' WHERE matricula = 102;
