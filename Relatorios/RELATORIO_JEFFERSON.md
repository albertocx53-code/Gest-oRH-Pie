# Relatório de entrega · Jefferson

## Minha parte

Fiquei com o **cadastro de funcionários**: montar a tela para cadastrar, consultar, editar e excluir funcionários e integrar os campos de cargo, departamento e situação aos serviços.

## Como encontrei o projeto

Quando peguei o projeto, a tela de Funcionários já tinha a base pronta na integração do Rickelmy: lista com busca por nome ou cargo, filtros de departamento e situação, formulário de cadastro e edição e exclusão com confirmação. O Alberto já tinha ligado tudo ao banco SQLite.

Para não mexer no que o grupo já tinha feito, coloquei a minha parte em arquivos novos. Nos arquivos que já existiam, só acrescentei linhas: duas chamadas para o meu código na tela, comentários explicando o cadastro, a chamada dos meus testes e um parágrafo no README. Nenhuma linha de código antiga foi apagada ou alterada. No README principal, só completei as linhas que falam da minha parte.

## O que eu fiz

- **Matrícula na tabela.** A tabela de funcionários ganhou a coluna Matrícula, com o número que o sistema gera no cadastro. Num banco novo, o próximo cadastro recebe a 109.
- **Consultar ficha.** O botão abre a ficha do funcionário selecionado: matrícula, cargo, departamento, situação, admissão, tempo de casa, ponto do mês (presenças, atrasos e faltas), último registro de ponto e último salário. O salário só aparece para Administrador e Recursos Humanos. Para o Gestor, a ficha mostra "Sem acesso aos salários neste perfil".
- **Inativar.** Quem tem ponto ou salário não pode ser excluído, para não perder o histórico. O botão Inativar muda a situação para Inativo em um passo e pede confirmação antes. Fica desligado para o Gestor.
- **Sugestão de cargos.** No formulário de cadastro, o campo Cargo sugere os cargos que já existem enquanto a pessoa digita. Isso evita o mesmo cargo escrito de jeitos diferentes. Um cargo novo pode ser digitado normalmente.
- **Duplo clique para o Gestor.** Para quem edita, o duplo clique continua abrindo a edição. Para o Gestor, que só consulta, ele abre a ficha.
- **Consultas em SQL** (`BancoDeDados/04_consultas_funcionarios.sql`): ficha, ponto do mês, último salário, cargos, funcionários por situação, quem pode ser excluído e tempo de casa. O script só tem SELECT e não é usado na criação do banco.
- **Comentários** explicando o caminho do cadastro, da tela até o banco. Todos começam com `Jefferson:` ou `Parte do Jefferson:`. No Visual Studio, Ctrl+Shift+F com "Jefferson" mostra todos.
- **Testes** da minha parte, com a lista em memória e com o banco.

## Como os campos se ligam aos serviços

Departamento e situação são listas fechadas. A mesma lista (`RhOptions`) preenche a tela e é usada pelo `RhService` para validar, então as duas nunca discordam. O banco confere de novo, como segunda proteção.

| Campo | Na tela | No serviço (`RhService.SaveEmployee`) | No banco (`funcionarios`) |
| --- | --- | --- | --- |
| Matrícula | Só leitura, na tabela e na ficha | Id 0 indica cadastro novo | `INTEGER PRIMARY KEY AUTOINCREMENT` |
| Nome | Texto | Obrigatório, até 120 caracteres | `NOT NULL` |
| Cargo | Texto com sugestões dos cargos já cadastrados | Obrigatório, até 120 caracteres | `NOT NULL` |
| Departamento | Lista fechada (`RhOptions.Teams`) | Precisa estar na lista | Chave estrangeira para `departamentos` |
| Situação | Lista fechada (`RhOptions.EmployeeStatuses`) | Precisa estar na lista | `CHECK` com Ativo, Férias, Afastado e Inativo |
| Admissão | Data no formato dd/mm/aaaa | Entre 1900 e 2100 | Texto `AAAA-MM-DD` |

O Gestor não grava nada: o botão já aparece desligado, e o `RhService` recusa a operação mesmo assim.

## Como a minha parte conversa com o resto

```
Botão na tela (AuroraForm.Funcionarios.cs)
  -> CadastroDeFuncionarios (Aurora.Core)
    -> RhService (sessão, perfil e validações)
      -> IRhRepository -> RepositorioSqlite (Alberto) -> aurora.db
```

Usei **métodos de extensão** em `CadastroDeFuncionarios`. Na tela, a chamada fica `data.ConsultarFicha(...)`, como se o método fosse do próprio `RhService`, mas ele fica num arquivo meu. Assim o `RhService` não precisou mudar. Como esses métodos usam só o que o `RhService` já oferece, as regras de sessão, perfil e validação continuam valendo.

A ficha junta dados de outras áreas do sistema: o ponto e a folha. Na demonstração, depois de registrar o ponto e lançar o salário do funcionário novo, a ficha mostra os dois.

## Arquivos que eu criei ou mexi

| Arquivo | O que é |
| --- | --- |
| `Aurora.Core/CadastroDeFuncionarios.cs` (novo) | Ficha do funcionário, cargos cadastrados, mudar situação e tempo de casa |
| `Aurora.WinForms/AuroraForm.Funcionarios.cs` (novo) | Coluna Matrícula, botões Consultar ficha e Inativar e sugestão de cargos |
| `Aurora.WinForms/FichaDoFuncionarioForm.cs` (novo) | Janela da ficha |
| `Aurora.Tests/TesteDosFuncionarios.cs` (novo) | Testes da minha parte |
| `BancoDeDados/04_consultas_funcionarios.sql` (novo) | Consultas do cadastro em SQL |
| `Aurora.WinForms/AuroraForm.Modules.cs` | Duas linhas que chamam o meu código, mais comentários |
| `Aurora.Core/RhService.cs` e `Aurora.Core/Models.cs` | Só comentários |
| `Aurora.Tests/TestProgram.cs` | Uma linha para chamar os meus testes e verificações da ficha no teste das telas |
| `Aurora.WinForms/README.md` | Um parágrafo sobre a minha parte |
| `README.md` (principal) | Completei a minha linha na tabela da equipe, o item do cadastro e a descrição da pasta `Aurora.Core` |

## Como testar

No Windows, com o SDK .NET 10, na pasta da solução:

```
dotnet run --project Aurora.Tests -c Release
```

Os meus testes rodam duas vezes, com a lista em memória e com um banco SQLite temporário. Eles conferem:

- a ficha da Marina (5 atrasos na semana, tempo de casa de 4 anos e 7 meses e líquido de R$ 3.365,00);
- as faltas da Larissa e do Otávio e a ficha do Caio, que está de férias e não tem ponto nem folha;
- o Gestor vendo a ficha sem o salário;
- a lista de cargos sem repetição, mesmo com maiúsculas diferentes;
- o Rafael sendo inativado sem perder o histórico, e a situação continuando gravada depois de reabrir o banco;
- os bloqueios: matrícula que não existe, situação repetida ou inválida, Gestor tentando inativar e sessão encerrada.

No teste das telas, a ficha da funcionária cadastrada mostra a matrícula 109, o campo Cargo traz as sugestões, e para o Gestor o Inativar fica desligado e a ficha aparece sem salário.

### Roteiro para a apresentação

1. Entrar como `rh` e abrir **Funcionários**. A coluna Matrícula aparece na tabela.
2. Clicar em **Novo funcionário**. No Cargo, digitar "Ana" e mostrar a sugestão "Analista de RH". Confirmar.
3. Selecionar o funcionário novo e clicar em **Consultar ficha**.
4. Em **Ponto e frequência**, registrar o ponto dele. Voltar e abrir a ficha: aparece 1 presença.
5. Em **Salários**, lançar o salário de novembro. A ficha passa a mostrar o líquido.
6. Tentar **Excluir** a Marina. O sistema bloqueia, porque ela tem histórico. Clicar em **Inativar**.
7. Sair e entrar como `gestor`. O Inativar fica desligado, a ficha abre sem o salário e o duplo clique abre a ficha.

## Limitações

- A busca continua por nome ou cargo. A matrícula aparece na tabela e na ficha, mas não entrou na busca, porque isso exigiria mudar o filtro que já existia.
- O Inativar lê o cadastro e grava de novo só com a situação trocada. Com várias pessoas editando o mesmo funcionário ao mesmo tempo, o ideal seria um UPDATE só da situação, dentro de uma transação.
- A ficha usa 13/11/2026 como "hoje", igual ao resto da demonstração.
- Para ver a minha parte funcionando, é só abrir a solução no Visual Studio e apertar F5.
