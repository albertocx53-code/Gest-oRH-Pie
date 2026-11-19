# Aurora · Gestão de RH

No Windows, abra `Aurora.slnx` no Visual Studio com o SDK .NET 10 e a carga de trabalho **Desenvolvimento para desktop com .NET**. Selecione `Aurora.WinForms` como projeto de inicialização e pressione **F5**. Pelo terminal, na pasta que contém a solução, rode `dotnet run --project Aurora.WinForms`.

Se você recebeu o ZIP da entrega, pode abrir `Executar/Aurora.WinForms.exe` no Windows de 64 bits com o **.NET Desktop Runtime 10** instalado. Extraia a pasta inteira, pois o executável usa os arquivos que estão ao lado dele.

| Usuário de demonstração | Perfil |
| --- | --- |
| `admin` | Administrador |
| `rh` | Recursos Humanos |
| `gestor` | Gestor |

A senha fictícia das três contas é `Aurora.demo26`. O perfil Gestor consulta as áreas de RH e fica sem acesso à folha salarial e aos compromissos dessa categoria. Administrador e Recursos Humanos podem editar os registros da demonstração. **Sair da conta** retorna à tela de login e mantém os dados até o aplicativo ser fechado.

Para executar os testes no Windows, rode `dotnet run --project Aurora.Tests -c Release`. Consulte `RELATORIO_RICKELMY.md` para localizar as regras e os contratos que Alberto usará na integração com o banco.

**Banco de dados (Alberto):** ao rodar pelo Visual Studio, os dados ficam salvos no arquivo `aurora.db`, criado ao lado do executável na primeira execução. Para voltar aos dados iniciais, feche o sistema e apague esse arquivo. Os scripts SQL e a explicação das tabelas ficam na pasta `BancoDeDados` e em `RELATORIO_ALBERTO.md`.

**Cadastro de funcionários (Jefferson):** na área Funcionários, a tabela mostra a matrícula de cada pessoa. **Consultar ficha** abre o cadastro, o tempo de casa, o ponto do mês e o último salário. O salário só aparece para Administrador e Recursos Humanos. **Inativar** muda a situação para Inativo, o caminho para quem tem histórico e não pode ser excluído. No cadastro, o campo Cargo sugere os cargos que já existem. Os detalhes estão em `RELATORIO_JEFFERSON.md`.

**Ponto e frequência (Clóvis):** na área Ponto e frequência, **Lançar falta** e **Lançar atraso** registram a ocorrência de um funcionário ativo em um dia, e **Consultar período** abre a frequência de qualquer intervalo de até 366 dias, com filtro por funcionário e ocorrência, os totais do período e os totais de cada funcionário. Lançar é só para Administrador e Recursos Humanos; o Gestor consulta. Os detalhes estão em `RELATORIO_CLOVIS.md`.