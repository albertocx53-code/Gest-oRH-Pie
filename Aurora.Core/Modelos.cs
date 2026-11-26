namespace Aurora.Core;

// Os "modelos" do sistema: cada um é um tipo de informação que o sistema guarda.
// São records: guardam os dados e não mudam depois de criados.

// Jefferson: o cadastro do funcionário. Id é a matrícula.
// No banco, cada campo vira uma coluna da tabela funcionarios.
public record Funcionario(int Id, string Nome, string Cargo, string Departamento, string Situacao, DateTime Admissao);

// um registro de ponto de uma pessoa num dia
public record RegistroDePonto(int Matricula, DateTime Dia, string Entrada, string Saida, string Situacao, string Motivo = "")
{
    // só atraso e falta têm motivo
    public static bool AceitaMotivo(string situacao)
    {
        return situacao == "Atraso" || situacao == "Falta";
    }
}

public record Vaga(int Id, string Titulo, string Departamento, string Situacao);

public record Candidato(int Id, string Nome, int VagaId, string Etapa);

// um lançamento da folha de pagamento de uma pessoa num mês
public record LancamentoDaFolha(int Matricula, DateTime Competencia, decimal Salario, decimal Bonus, decimal Desconto)
{
    // líquido = salário + bônus - desconto
    public decimal Total
    {
        get { return Salario + Bonus - Desconto; }
    }
}

public record Compromisso(string Titulo, DateTime DataHora, string Categoria);

// As listas de opções do sistema
public static class Opcoes
{
    // Jefferson: essas listas aparecem nas telas e também são usadas pelo ServicoRh
    // para conferir os dados. Assim a tela e a regra usam a mesma lista.
    public static readonly IReadOnlyList<string> Departamentos = new List<string> { "Pessoas", "Tecnologia", "Financeiro", "Comercial", "Operações" };
    public static readonly IReadOnlyList<string> SituacoesDoFuncionario = new List<string> { "Ativo", "Férias", "Afastado", "Inativo" };
    public static readonly IReadOnlyList<string> SituacoesDoPonto = new List<string> { "Presença", "Atraso", "Falta" };
    public static readonly IReadOnlyList<string> Etapas = new List<string> { "Triagem", "Entrevista", "Teste técnico", "Proposta", "Contratado" };
    public static readonly IReadOnlyList<string> CategoriasDaAgenda = new List<string> { "Funcionários", "Ponto", "Recrutamento", "Salários" };
}
