-- Script 04 - Consultas do cadastro de funcionários
-- Autor: Jefferson (cadastro de funcionários)
--
-- Este script só faz SELECT, não altera nada no banco (o UPDATE do final está comentado).
-- São as informações que a tela de Funcionários e a "Ficha do funcionário" mostram,
-- escritas em SQL para conferir no DB Browser for SQLite, aba "Executar SQL".
-- Troque a matrícula 101 por outra para consultar outra pessoa.
-- A data de referência é 13/11/2026, a mesma da demonstração do sistema.
--
-- O sistema não precisa deste script para funcionar. O CriadorDoBanco só roda o 01 e o 02.


-- 1) Ficha de um funcionário pela matrícula
--    (JOIN com departamentos para mostrar o nome do setor, que é o que a tela mostra)
SELECT f.matricula,
       f.nome,
       f.cargo,
       d.nome AS departamento,
       f.situacao,
       f.data_admissao
FROM funcionarios f
JOIN departamentos d ON d.id = f.departamento_id
WHERE f.matricula = 101;


-- 2) Ponto do funcionário no mês da referência (novembro de 2026)
--    Mesma contagem da ficha: presenças, atrasos e faltas
SELECT SUM(CASE WHEN situacao = 'Presença' THEN 1 ELSE 0 END) AS presencas,
       SUM(CASE WHEN situacao = 'Atraso'   THEN 1 ELSE 0 END) AS atrasos,
       SUM(CASE WHEN situacao = 'Falta'    THEN 1 ELSE 0 END) AS faltas
FROM ponto
WHERE matricula = 101
  AND dia BETWEEN '2026-11-01' AND '2026-11-30';


-- 3) Último registro de ponto do funcionário
SELECT dia,
       entrada,
       saida,
       situacao,
       motivo
FROM ponto
WHERE matricula = 101
ORDER BY dia DESC
LIMIT 1;


-- 4) Último lançamento de salário, com o líquido calculado na hora
--    (o líquido não é salvo no banco: salario_base + bonus - desconto)
SELECT competencia,
       salario_base,
       bonus,
       desconto,
       (salario_base + bonus - desconto) AS liquido
FROM folha_pagamento
WHERE matricula = 101
ORDER BY competencia DESC
LIMIT 1;


-- 5) Cargos que já existem no cadastro, sem repetir
--    É a lista que o campo Cargo usa para sugerir enquanto a pessoa digita
SELECT DISTINCT cargo
FROM funcionarios
ORDER BY cargo;


-- 6) Quantos funcionários há em cada situação
SELECT situacao,
       COUNT(*) AS total
FROM funcionarios
GROUP BY situacao
ORDER BY total DESC;


-- 7) Quem pode ser excluído
--    Quem tem ponto ou folha não pode ser apagado (ON DELETE RESTRICT).
--    Para essas pessoas, o caminho é o botão Inativar.
SELECT f.matricula,
       f.nome,
       CASE WHEN EXISTS (SELECT 1 FROM ponto p WHERE p.matricula = f.matricula)
              OR EXISTS (SELECT 1 FROM folha_pagamento fp WHERE fp.matricula = f.matricula)
            THEN 'Não, tem histórico (use Inativar)'
            ELSE 'Sim'
       END AS pode_excluir
FROM funcionarios f
ORDER BY f.matricula;


-- 8) Tempo de casa na data de referência (13/11/2026), igual ao da ficha
--    Conta os meses entre a admissão e a referência. Se o dia da admissão
--    ainda não chegou no mês, tira 1 (mesma regra do C#).
SELECT nome,
       data_admissao,
       meses_de_casa / 12 AS anos,
       meses_de_casa % 12 AS meses
FROM (
    SELECT nome,
           data_admissao,
           (CAST(strftime('%Y', '2026-11-13') AS INTEGER) - CAST(strftime('%Y', data_admissao) AS INTEGER)) * 12
         + (CAST(strftime('%m', '2026-11-13') AS INTEGER) - CAST(strftime('%m', data_admissao) AS INTEGER))
         - (CASE WHEN CAST(strftime('%d', '2026-11-13') AS INTEGER) < CAST(strftime('%d', data_admissao) AS INTEGER)
                 THEN 1 ELSE 0 END) AS meses_de_casa
    FROM funcionarios
)
ORDER BY meses_de_casa DESC;


-- Ação do botão Inativar (deixe comentado: o sistema faz isso pelo botão)
-- Remova o "--" para testar. A situação muda e o histórico de ponto e folha continua lá.
-- UPDATE funcionarios SET situacao = 'Inativo' WHERE matricula = 102;
