# Relatório de entrega · Rickelmy

## Divisão de responsabilidades

Nesta entrega, concluí a parte de interface atribuída ao Paulo no PDF de divisão da equipe e implementei minha parte de back-end em C#. O Aurora agora abre pela tela de login. Depois de entrar, a pessoa vê o painel e navega pelas áreas no menu lateral. Tabelas, botões e formulários seguem o visual aprovado pela equipe. Na área de ponto, faltas e atrasos têm um campo opcional de motivo, que aparece ao lado da situação e volta preenchido na edição.

## O que fiz no back-end

- Criei o login por usuário e senha. O perfil vem da conta autenticada. Sair da conta encerra a sessão e devolve o usuário à tela de login.
- Centralizei as permissões no serviço em C#. Administrador e Recursos Humanos podem consultar e alterar os registros. Gestor só consulta as áreas permitidas e fica sem acesso a salários, ao total da folha e a compromissos salariais.
- Levei as validações para o serviço. Ele impede exclusão de funcionários com histórico, verifica horários e duplicidades de ponto, controla a inscrição em vagas fechadas e valida os valores da folha. O formulário mantém o que foi digitado quando uma regra pede correção.
- Liguei funcionários, ponto, recrutamento, agenda, folha e indicadores ao mesmo conjunto de serviços. As telas montam os formulários e mostram os resultados. Os serviços decidem se cada operação é válida.
- Deixei comentários em português nas ligações entre interface, serviços, autenticação e dados, principalmente onde a regra afeta outra parte do sistema.

As contas são fictícias. A demonstração compara senhas por PBKDF2-HMAC-SHA256, com salt por conta. A fonte dos usuários pode ser substituída pela implementação do Alberto. [Método PBKDF2 no .NET](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.rfc2898derivebytes.pbkdf2?view=net-10.0) · [Armazenamento de senhas segundo a OWASP](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)

## Onde Alberto entra

| Arquivo | O que faz |
| --- | --- |
| `Aurora.WinForms/Program.cs` | Monta os serviços e injeta as fontes de dados. É por aqui que começa a troca do repositório em memória pelo banco. |
| `Aurora.Core/IRhRepository.cs` | Contrato para ler e gravar os dados de RH. Os formulários chamam `RhService`, que usa esse contrato. |
| `Aurora.Core/Authentication.cs` | Define `IAccountDirectory` para localizar a conta e verificar senha e perfil. |
| `Aurora.Core/Models.cs` | Define os dados entregues às telas e os valores usados nas validações. |
| `Aurora.Core/RhService.cs` | Concentra a autorização, a validação, a associação entre registros e a chamada ao repositório. |
| `Aurora.WinForms/DemoRh.cs` | Guarda os dados de exemplo em memória e mostra o comportamento esperado de cada operação. |
| `Aurora.WinForms/AuroraForm.Modules.cs` | Liga os filtros e botões das telas ao serviço correspondente. |

O repositório deve devolver listas estáveis para leitura e atribuir os IDs de novos funcionários, vagas e candidatos quando receber `Id = 0`. No banco, use a matrícula nas relações de ponto e salários e o ID da vaga nas candidaturas. Ponto tem uma chave por matrícula e dia. Salário tem uma chave por matrícula e competência, guardada como o primeiro dia do mês. A edição de ponto mantém a chave original. A confirmação de folha atualiza o lançamento daquela competência. A exclusão de funcionário com histórico deve ser impedida também pelas chaves do banco. As operações que validam e gravam dados relacionados precisam manter a integridade mesmo com mais de uma instância conectada.

## Verificação

O projeto compilou em Release com zero erros e zero avisos. Executei os testes de serviços e o fluxo real do Windows Forms, desde a tela de entrada até sair e entrar com outro perfil. O teste criou um funcionário, registrou e editou o motivo de uma falta, alterou uma candidatura, adicionou um compromisso e conferiu o líquido salarial. Também verificou senha incorreta, bloqueio de operações sem permissão, dados inválidos, duplicidade de ponto, preservação de histórico e sessão já encerrada.

Para repetir os testes em um Windows com o SDK .NET 10, execute `dotnet run --project Aurora.Tests -c Release` na pasta da solução.

## Limites desta entrega

O Aurora usa dados e contas de demonstração. Os registros ficam em memória enquanto o aplicativo está aberto e são reiniciados ao fechar. O banco e a fonte de contas reais ficam na integração com Alberto. O valor líquido é a simulação de salário mais bônus menos desconto. A tela de ponto consulta por dia, e o serviço já aceita um intervalo de datas para a próxima etapa do módulo.
