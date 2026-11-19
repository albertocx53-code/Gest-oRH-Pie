# Gestão de RH

Sistema desktop de gestão de RH feito em C# (.NET 10, Windows Forms) com banco de dados SQLite.

Trabalho da disciplina de **Programação Orientada a Objetos** · UniFacema
Professor: Marcos Gomes da Silva Rocha

## Equipe

| Integrante | Parte |
| --- | --- |
| Alberto | Banco de dados (tabelas, scripts SQL e camada de acesso aos dados) |
| Rickelmy | Interface e back-end (telas, login, permissões e regras) |
| Clóvis | Ponto e frequência (lançar falta e atraso, consulta por período) |
| Jefferson | Cadastro de funcionários (ficha do funcionário, inativar e sugestão de cargos) |
| Paulo Victor | Interface |
| Kerlon | [parte] |
| Cleberson Dias | Salários (editar lançamento da folha) e testes da folha em memória e no banco |

## O que o sistema faz

- Login com três perfis: Administrador, Recursos Humanos e Gestor
- Cadastro de funcionários, com ficha do funcionário, inativação e sugestão de cargos
- Controle de ponto (presença, atraso e falta), com lançamento de falta e atraso e frequência por período
- Recrutamento (vagas e candidatos)
- Agenda do RH
- Folha de pagamento, com edição de lançamento
- Painel com o resumo do dia

## Estrutura

| Pasta | Conteúdo |
| --- | --- |
| `Aurora.Core` | Modelos, regras de negócio (`RhService`), login (`Authentication`), frequência do ponto (`FrequenciaDePonto`) e cadastro de funcionários (`CadastroDeFuncionarios`) |
| `Aurora.WinForms` | Telas do sistema |
| `Aurora.Dados` | Camada de acesso ao banco SQLite |
| `BancoDeDados` | Scripts SQL, diagrama das tabelas e banco de exemplo |
| `Aurora.Tests` | Testes |
| `Executar` | Versão compilada anterior (ainda sem o banco) |

## Versão web

A pasta `site/sistema` tem uma versão do sistema que roda no navegador, com as mesmas telas e regras. Ela usa o mesmo banco SQLite e os mesmos scripts SQL (biblioteca sql.js), e os dados ficam salvos no navegador.

Publicada em: https://gestao-rh-teal.vercel.app/sistema/

## Como rodar (Windows)

1. Abrir `Aurora.slnx` no Visual Studio (precisa do .NET 10 e de Windows).
2. Na primeira vez, o Visual Studio baixa o pacote `Microsoft.Data.Sqlite`.
3. Apertar F5. O banco `aurora.db` é criado sozinho ao lado do executável, com dados fictícios.

Login de teste: usuário `rh`, `admin` ou `gestor`, senha `Aurora.demo26`.

Mais detalhes do banco em [`BancoDeDados/LEIA-ME.md`](BancoDeDados/LEIA-ME.md).
