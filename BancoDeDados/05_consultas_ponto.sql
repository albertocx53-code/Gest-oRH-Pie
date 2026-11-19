-- Script 05 - Consultas do ponto e frequência
-- Autor: Clóvis (ponto e frequência)
--
-- Este script só faz SELECT, não altera nada no banco (os INSERTs do final estão comentados).
-- São as informações que a janela "Frequência por período" e os botões Lançar falta e
-- Lançar atraso usam, escritas em SQL para conferir no DB Browser for SQLite, aba "Executar SQL".
-- Troque as datas e a matrícula 108 pelas que você quiser consultar.
-- A data de referência é 13/11/2026, a mesma da demonstração do sistema.
--
-- O sistema não precisa deste script para funcionar. O CriadorDoBanco só roda o 01 e o 02.


-- 1) Registros de ponto de um período, com o nome do funcionário
--    É a tabela do meio da janela "Frequência por período" (do dia mais antigo para o mais novo
--    e, no mesmo dia, em ordem de nome). Para filtrar, acrescente no WHERE:
--      AND p.matricula = 108          (um funcionário)
--      AND p.situacao = 'Falta'       (uma ocorrência)
SELECT f.nome,
       p.dia,
       p.entrada,
       p.saida,
       p.situacao,
       p.motivo
FROM ponto p
JOIN funcionarios f ON f.matricula = p.matricula
WHERE p.dia BETWEEN '2026-11-01' AND '2026-11-13'
ORDER BY p.dia, f.nome;


-- 2) Totais do período: registros, presenças, atrasos, faltas e comparecimento
--    Comparecimento = (presenças + atrasos) / registros. Quem chegou atrasado compareceu.
SELECT COUNT(*)                                                AS registros,
       SUM(CASE WHEN situacao = 'Presença' THEN 1 ELSE 0 END)  AS presencas,
       SUM(CASE WHEN situacao = 'Atraso'   THEN 1 ELSE 0 END)  AS atrasos,
       SUM(CASE WHEN situacao = 'Falta'    THEN 1 ELSE 0 END)  AS faltas,
       ROUND(100.0 * SUM(CASE WHEN situacao <> 'Falta' THEN 1 ELSE 0 END) / COUNT(*), 1) AS comparecimento_pct
FROM ponto
WHERE dia BETWEEN '2026-11-01' AND '2026-11-13';


-- 3) Horas trabalhadas no período
--    Saída menos entrada, em minutos, sem descontar intervalo (mesma conta do C#).
--    Falta não tem horário (entrada e saída NULL), então não soma nada.
SELECT SUM(minutos)         AS minutos,
       SUM(minutos) / 60    AS horas,
       SUM(minutos) % 60    AS minutos_restantes
FROM (
    SELECT (strftime('%s', '2000-01-01 ' || saida) - strftime('%s', '2000-01-01 ' || entrada)) / 60 AS minutos
    FROM ponto
    WHERE dia BETWEEN '2026-11-01' AND '2026-11-13'
      AND entrada IS NOT NULL
      AND saida IS NOT NULL
      AND saida > entrada
);


-- 4) Resumo por funcionário no período
--    É a tabela de baixo da janela: presenças, atrasos, faltas e horas de cada pessoa.
SELECT f.matricula,
       f.nome,
       SUM(CASE WHEN p.situacao = 'Presença' THEN 1 ELSE 0 END) AS presencas,
       SUM(CASE WHEN p.situacao = 'Atraso'   THEN 1 ELSE 0 END) AS atrasos,
       SUM(CASE WHEN p.situacao = 'Falta'    THEN 1 ELSE 0 END) AS faltas,
       COALESCE(SUM(CASE WHEN p.entrada IS NOT NULL AND p.saida IS NOT NULL AND p.saida > p.entrada
                         THEN (strftime('%s', '2000-01-01 ' || p.saida) - strftime('%s', '2000-01-01 ' || p.entrada)) / 60
                    END), 0) AS minutos_trabalhados
FROM ponto p
JOIN funcionarios f ON f.matricula = p.matricula
WHERE p.dia BETWEEN '2026-11-01' AND '2026-11-13'
GROUP BY f.matricula, f.nome
ORDER BY f.nome;


-- 5) Funcionários ativos que ainda não têm ponto em um dia
--    É a lista que o botão Lançar falta usa para já deixar alguém selecionado.
--    Em 13/11/2026 ninguém está pendente. Troque a data por 2026-11-06 para ver os 6 ativos.
SELECT f.matricula,
       f.nome
FROM funcionarios f
WHERE f.situacao = 'Ativo'
  AND NOT EXISTS (SELECT 1
                  FROM ponto p
                  WHERE p.matricula = f.matricula
                    AND p.dia = '2026-11-13')
ORDER BY f.nome;


-- 6) Atrasos que não respeitam a regra do sistema
--    O sistema só lança atraso com entrada depois das 08:00. Esta consulta procura registros
--    de Atraso com entrada às 08:00 ou antes. Nos dados fictícios o resultado é vazio.
SELECT f.nome,
       p.dia,
       p.entrada
FROM ponto p
JOIN funcionarios f ON f.matricula = p.matricula
WHERE p.situacao = 'Atraso'
  AND p.entrada <= '08:00'
ORDER BY p.dia;


-- 7) Frequência dia a dia no período
--    Os mesmos números do resumo do dia da tela (entradas, atrasos e faltas).
SELECT dia,
       COUNT(entrada)                                           AS entradas,
       SUM(CASE WHEN situacao = 'Atraso' THEN 1 ELSE 0 END)     AS atrasos,
       SUM(CASE WHEN situacao = 'Falta'  THEN 1 ELSE 0 END)    AS faltas
FROM ponto
WHERE dia BETWEEN '2026-11-01' AND '2026-11-13'
GROUP BY dia
ORDER BY dia;


-- Ações dos botões Lançar falta e Lançar atraso (deixe comentado: o sistema faz isso pelos botões)
-- Remova o "--" de uma linha por vez para testar. Os dois comandos dão certo no dia 06/11/2026,
-- que não tem ponto. Se repetir o mesmo comando, o banco recusa (chave primária matricula + dia).

-- Falta: sem horário, com motivo (o motivo é opcional)
-- INSERT INTO ponto (matricula, dia, entrada, saida, situacao, motivo) VALUES (101, '2026-11-06', NULL, NULL, 'Falta', 'Consulta médica comunicada.');

-- Atraso: com horário, e a entrada depois das 08:00
-- INSERT INTO ponto (matricula, dia, entrada, saida, situacao, motivo) VALUES (102, '2026-11-06', '08:20', '17:00', 'Atraso', 'Trânsito.');