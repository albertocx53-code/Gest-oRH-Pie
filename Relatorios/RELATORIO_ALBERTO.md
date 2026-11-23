# Relatório de entrega · Alberto

## Minha parte

Fiquei com o **banco de dados**: modelar as tabelas e os relacionamentos, preparar os scripts de criação e os dados fictícios e implementar a camada de acesso aos dados.

## O que eu fiz

- Modelei 8 tabelas no SQLite: `usuarios`, `departamentos`, `funcionarios`, `ponto`, `folha_pagamento`, `vagas`, `candidatos` e `agenda`.
- Escrevi o script de criação (`BancoDeDados/01_criar_tabelas.sql`), com chave primária, chave estrangeira, CHECK e índices.
- Escrevi o script de dados fictícios (`BancoDeDados/02_dados_ficticios.sql`), com os mesmos funcionários e vagas da demonstração.
- Fiz um script com consultas de exemplo usando JOIN (`BancoDeDados/03_consultas_uteis.sql`).
- Criei o projeto `Aurora.Dados`, que implementa as interfaces que o Rickelmy deixou (`IRhRepository` e `IAccountDirectory`) usando o banco.
- Liguei o banco no `Program.cs`. Agora o que é cadastrado continua salvo quando o sistema é fechado.
- Fiz os testes do banco (`Aurora.Tests/TesteDoBanco.cs`).

Os detalhes de cada tabela e das decisões que tomei estão em `BancoDeDados/LEIA-ME.md`.

## Arquivos que eu criei ou mexi

| Arquivo | O que é |
| --- | --- |
| `BancoDeDados/` (pasta toda) | Scripts SQL, diagrama, banco de exemplo e documentação |
| `Aurora.Dados/` (projeto novo) | Camada de acesso aos dados |
| `Aurora.Tests/TesteDoBanco.cs` | Testes do banco |
| `Aurora.WinForms/Program.cs` | Troquei a `DemoRh` pelo `RepositorioSqlite` (as capturas de tela continuam com a demo) |
| `Aurora.WinForms/Aurora.WinForms.csproj` | Adicionei a referência ao `Aurora.Dados` |
| `Aurora.Tests/TestProgram.cs` | Uma linha a mais, pra chamar os testes do banco |
| `Aurora.slnx` | Coloquei o projeto `Aurora.Dados` na solução |

## Como testei

- Rodei os scripts 01, 02 e 03 num SQLite e conferi a quantidade de linhas de cada tabela.
- Tentei de propósito apagar funcionário com histórico, bater ponto duplicado, cadastrar candidato em vaga que não existe e gravar situação inválida. O banco recusou todos.
- Rodei os mesmos casos de teste que o Rickelmy fez com a lista em memória, só que em cima do banco: login, permissões, ponto, folha, vagas, candidatos, funcionários e agenda. Todos passaram.
- Fechei e abri o banco de novo pra ver se os dados continuavam lá. Continuavam.

## Observação pro grupo

Para gerar o executável, é só compilar no Visual Studio (F5) ou rodar `dotnet publish`. Na primeira vez ele baixa o pacote `Microsoft.Data.Sqlite` do NuGet.
