# Relatório de entrega · Kerlon

## Minha parte

Fiquei com o **recrutamento**: montar as telas para cadastrar vagas e candidatos, vincular candidaturas às vagas e atualizar as etapas do processo seletivo.

## Como encontrei o projeto

A tela de Recrutamento já tinha a tabela de vagas, a de candidaturas, **Nova vaga**, **Encerrar ou reabrir vaga**, **Novo candidato** e **Alterar etapa**. Para não mexer no que o grupo já tinha feito, coloquei a minha parte em arquivos novos. Nos arquivos que já existiam, só acrescentei linhas e troquei uma chamada (descrita abaixo).

## O que eu fiz

- **Funil de etapas.** Ao lado do título "Candidaturas", uma linha mostra quantos candidatos há em Triagem, Entrevista, Teste técnico, Proposta e Contratado. Com uma vaga selecionada na tabela de cima, conta só os candidatos dela; sem seleção, conta todos.
- **Candidatos da vaga.** O botão filtra a tabela de candidaturas pela vaga selecionada. Ele vira **Ver todos** para desligar o filtro, e trocar de vaga na tabela de cima troca os candidatos mostrados. Liberado para todos os perfis.
- **Avançar etapa.** Leva a candidatura selecionada para a etapa seguinte em um clique. Quem já está em Contratado não avança, e o sistema explica.
- **Mudar de vaga.** Abre um formulário com as outras vagas abertas. A etapa da candidatura continua a mesma.
- **Sem candidato repetido.** O mesmo nome (sem diferenciar maiúsculas) não entra duas vezes na mesma vaga, tanto no cadastro quanto na edição e na mudança de vaga.
- **Comentários** explicando o caminho da tela até o serviço. Todos começam com `Kerlon:` ou `Parte do Kerlon:`. No Visual Studio, Ctrl+Shift+F com "Kerlon" mostra todos.

## Como a minha parte conversa com o resto

```
Botão na tela (AuroraForm.Recrutamento.cs)
  -> Recrutamento (Aurora.Core)
    -> RhService (sessão, perfil e validações)
      -> IRhRepository -> RepositorioSqlite (Alberto) -> aurora.db
```

Usei **métodos de extensão** em `Recrutamento`, o mesmo caminho do Jefferson e do Clóvis. Na tela, a chamada fica `data.AvancarEtapa(...)`, como se o método fosse do próprio `RhService`, mas ele fica num arquivo meu. O `RhService` não precisou mudar, e as regras de sessão, perfil e validação continuam valendo. O Gestor consulta, mas não grava: os botões já aparecem desligados, e o `RhService` recusa a operação mesmo assim.

| Método (`Recrutamento.cs`) | O que faz |
| --- | --- |
| `ConsultarVagas` | Vagas com filtro por situação e departamento. Abertas primeiro. |
| `ResumoDasVagas` | As mesmas vagas com total de candidatos e de contratados. |
| `CandidatosDaVaga` | Candidatos de uma vaga, com filtro por etapa, em ordem do processo. |
| `FunilDeEtapas` | Quantos candidatos há em cada etapa, incluindo as vazias. |
| `CadastrarCandidato` | Inscreve em vaga aberta, sempre na Triagem, sem repetir nome na vaga. |
| `SalvarCandidatura` | Grava o formulário de candidatura (nova ou edição) conferindo duplicidade. |
| `VincularAVaga` | Muda a candidatura para outra vaga aberta, mantendo a etapa. |
| `MoverEtapa` | Coloca o candidato em uma etapa escolhida. |
| `AvancarEtapa` | Leva para a etapa seguinte. |

Regras que já vinham do `RhService` e que continuam valendo: vaga encerrada não recebe novas inscrições (mas quem já estava nela pode mudar de etapa), a etapa precisa estar em `RhOptions.Stages` e o nome tem até 120 caracteres.

## Arquivos que eu criei ou mexi

| Arquivo | O que é |
| --- | --- |
| `Aurora.Core/Recrutamento.cs` (novo) | Consultas e ações do recrutamento |
| `Aurora.WinForms/AuroraForm.Recrutamento.cs` (novo) | Funil, Candidatos da vaga, Avançar etapa e Mudar de vaga |
| `Aurora.WinForms/AuroraForm.Modules.cs` | Uma chamada ao meu código no fim de `HirePage`, comentários e **uma linha trocada** em `EditCandidate` (`SaveCandidate` virou `SalvarCandidatura`, que confere a duplicidade e depois chama o `SaveCandidate`) |
| `Aurora.WinForms/README.md` | Um parágrafo sobre a minha parte |

Como o `AuroraForm.Modules.cs` do zip é o arquivo completo, ele substitui o que está na pasta do projeto.

## Roteiro para a apresentação

1. Entrar como `rh` e abrir **Recrutamento**. O funil aparece ao lado de "Candidaturas".
2. Clicar em **Novo candidato**, cadastrar uma pessoa na vaga de Desenvolvedor back-end e confirmar. Ela entra na tabela e o funil sobe em Triagem.
3. Tentar cadastrar o mesmo nome na mesma vaga. O sistema recusa no próprio formulário.
4. Selecionar a candidatura nova e clicar em **Avançar etapa**. Ela vai para Entrevista.
5. Clicar em **Mudar de vaga** e escolher o Analista financeiro. A etapa continua a mesma.
6. Selecionar uma vaga e clicar em **Candidatos da vaga**. A tabela mostra só os candidatos dela. Clicar em **Ver todos**.
7. Encerrar uma vaga e tentar inscrever alguém nela. O sistema recusa.
8. Sair e entrar como `gestor`. Tudo é só consulta: **Avançar etapa** e **Mudar de vaga** ficam desligados.

## Limitações

- **Não compilei nem rodei nada.** O ambiente em que montei esta entrega não tem o SDK .NET, então o código foi escrito lendo os arquivos do grupo (`AuroraForm.Modules.cs`, `EntryForm.cs`, `RhService.cs`). Antes da apresentação, compile no Visual Studio (F5) e passe pelo roteiro acima.
- **Sem testes automáticos.** Não recebi o `TestProgram.cs`, então não escrevi o `TesteDoRecrutamento.cs`. Também não escrevi consultas SQL, porque não recebi o `01_criar_tabelas.sql`.
- A posição dos botões novos (y = 629) e do funil (y = 360) foi calculada pelas medidas da tela atual. Se algum botão ficar sobreposto ou cortado, é só ajustar essas coordenadas em `AuroraForm.Recrutamento.cs`.
- A repetição de candidato é conferida só pelo nome. Dois candidatos homônimos reais seriam bloqueados na mesma vaga.
- `MoverEtapa` existe no serviço (permite voltar etapas para corrigir erros), mas a tela só usa `AvancarEtapa`. Para voltar uma etapa, use o **Alterar etapa** que já existia. Não há histórico de quem mudou a etapa e quando.
- Para ver a minha parte funcionando, é só abrir a solução no Visual Studio e apertar F5.
