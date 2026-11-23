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
| Kerlon | Recrutamento (funil de etapas, candidatos por vaga, avançar etapa e mudar de vaga) |
| Cleberson Dias | Salários (editar lançamento da folha) e testes da folha em memória e no banco |

## O que o sistema faz

- Login com três perfis: Administrador, Recursos Humanos e Gestor
- Cadastro de funcionários, com ficha do funcionário, inativação e sugestão de cargos
- Controle de ponto (presença, atraso e falta), com lançamento de falta e atraso e frequência por período
- Recrutamento (vagas e candidatos), com funil de etapas, filtro por vaga, avançar etapa e mudar de vaga
- Agenda do RH
- Folha de pagamento, com edição de lançamento
- Painel com o resumo do dia
- Sobre o projeto: equipe, diagrama do banco, trechos de código de cada parte, POO e como rodar (as imagens abrem ampliadas ao clicar)

## Estrutura

| Pasta | Conteúdo |
| --- | --- |
| `Aurora.Core` | Modelos, regras de negócio (`RhService`), login (`Authentication`), frequência do ponto (`FrequenciaDePonto`) cadastro de funcionários (`CadastroDeFuncionarios`) e recrutamento (`Recrutamento`) |
| `Aurora.WinForms` | Telas do sistema (a pasta `Imagens` tem as figuras da tela Sobre o projeto) |
| `Aurora.Dados` | Camada de acesso ao banco SQLite |
| `BancoDeDados` | Scripts SQL, diagrama das tabelas e banco de exemplo |
| `Aurora.Tests` | Testes de cada parte (banco, ponto, salários, funcionários e recrutamento) e das telas |
| `Relatorios` | Relatório geral da equipe (`RELATORIO_GERAL.md` e `.pdf`) e os relatórios de entrega de cada integrante |
| `site` | Página de apresentação e versão web do sistema (publicada no Vercel) |

## Versão web

A pasta `site/sistema` tem uma versão do sistema que roda no navegador, com as mesmas telas e regras. Ela usa o mesmo banco SQLite e os mesmos scripts SQL (biblioteca sql.js), e os dados ficam salvos no navegador.

Publicada em: https://gestao-rh-teal.vercel.app/sistema/

## Como rodar (Windows)

1. Abrir `Aurora.slnx` no Visual Studio (precisa do .NET 10 e de Windows).
2. Na primeira vez, o Visual Studio baixa o pacote `Microsoft.Data.Sqlite`.
3. Conferir se `Aurora.WinForms` está como projeto de inicialização (fica em negrito no Gerenciador de Soluções). Se não estiver, clicar com o botão direito nele e escolher "Definir como Projeto de Inicialização". `Aurora.Core` e `Aurora.Dados` são bibliotecas e não abrem sozinhas.
4. Apertar F5. O banco `aurora.db` é criado sozinho ao lado do executável, com dados fictícios.

Login de teste: usuário `rh`, `admin` ou `gestor`, senha `Aurora.demo26`.

## Testes

No Visual Studio, defina `Aurora.Tests` como projeto de inicialização e aperte F5 (ou rode `dotnet run --project Aurora.Tests -c Release`). Os testes conferem as regras de cada parte na lista em memória e no banco SQLite e depois abrem as telas e clicam nos botões. No fim aparece "Serviços e fluxos do Windows Forms aprovados."

Mais detalhes do banco em [`BancoDeDados/LEIA-ME.md`](BancoDeDados/LEIA-ME.md).
