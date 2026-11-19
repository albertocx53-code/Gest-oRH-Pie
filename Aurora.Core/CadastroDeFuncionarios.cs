namespace Aurora.Core;

// Cadastro de funcionários - parte do Jefferson
// Fiz como métodos de extensão do RhService (por isso o "this RhService rh"),
// assim não precisei mexer no código que o grupo já tinha.
// Tudo passa pelo RhService, então o Gestor só consulta: não salva e não vê salário.
public static class CadastroDeFuncionarios
{
    // Data antiga só pra achar o último ponto em qualquer mês
    private static readonly DateTime PrimeiroDia = new(1900, 1, 1);

    // Monta a ficha pela matrícula. O ponto mostrado é do mês da data de referência.
    public static FichaDoFuncionario ConsultarFicha(this RhService rh, int matricula, DateTime referencia)
    {
        var funcionario = Encontrar(rh, matricula);

        // Ponto do mês inteiro
        var inicioDoMes = new DateTime(referencia.Year, referencia.Month, 1);
        var fimDoMes = inicioDoMes.AddMonths(1).AddDays(-1);
        var pontoDoMes = rh.AttendanceBetween(inicioDoMes, fimDoMes)
            .Where(item => item.Person == matricula)
            .ToArray();

        // Último ponto até a referência, de qualquer mês
        var ultimoPonto = rh.AttendanceBetween(PrimeiroDia, referencia.Date)
            .Where(item => item.Person == matricula)
            .OrderBy(item => item.Day)
            .LastOrDefault();

        // Salário só pra Administrador e RH. Procura até 12 meses atrás.
        Payroll? ultimaFolha = null;
        if (rh.CanViewPay)
        {
            for (var meses = 0; meses < 12 && ultimaFolha is null; meses++)
                ultimaFolha = rh.PayrollFor(inicioDoMes.AddMonths(-meses)).FirstOrDefault(item => item.Person == matricula);
        }

        return new FichaDoFuncionario(
            funcionario,
            referencia.Date,
            TempoDeCasa(funcionario.Started, referencia),
            pontoDoMes.Count(item => item.Status == "Presença"),
            pontoDoMes.Count(item => item.Status == "Atraso"),
            pontoDoMes.Count(item => item.Status == "Falta"),
            ultimoPonto,
            rh.CanViewPay,
            ultimaFolha);
    }

    // Cargos já cadastrados, sem repetir, pra sugerir no campo Cargo
    public static IReadOnlyList<string> CargosCadastrados(this RhService rh) =>
        rh.Employees
            .Select(item => item.Role.Trim())
            .Where(cargo => cargo.Length > 0)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(cargo => cargo, StringComparer.CurrentCulture)
            .ToArray();

    // Usado no botão Inativar, já que quem tem ponto ou salário não pode ser excluído
    public static void MudarSituacao(this RhService rh, int matricula, string novaSituacao)
    {
        var atual = Encontrar(rh, matricula);
        if (atual.Status == novaSituacao)
            throw new RhRuleException($"O cadastro já está com a situação {novaSituacao}.");

        // O SaveEmployee já barra o Gestor e valida a situação
        rh.SaveEmployee(atual with { Status = novaSituacao });
    }

    // Tempo de casa em anos e meses (ex.: 14/03/2022 a 13/11/2026 = 4 anos e 7 meses)
    public static string TempoDeCasa(DateTime admissao, DateTime referencia)
    {
        if (admissao.Date > referencia.Date) return "Admissão futura";

        var meses = (referencia.Year - admissao.Year) * 12 + referencia.Month - admissao.Month;
        if (referencia.Day < admissao.Day) meses--; // o mês só conta quando chega o dia da admissão

        var anos = meses / 12;
        meses %= 12;
        if (anos == 0 && meses == 0) return "Menos de 1 mês";

        var partes = new List<string>();
        if (anos > 0) partes.Add(anos == 1 ? "1 ano" : $"{anos} anos");
        if (meses > 0) partes.Add(meses == 1 ? "1 mês" : $"{meses} meses");
        return string.Join(" e ", partes);
    }

    // Busca pela matrícula ou dá erro
    private static Employee Encontrar(RhService rh, int matricula) =>
        rh.Employees.FirstOrDefault(item => item.Id == matricula)
        ?? throw new RhRuleException("Funcionário não encontrado.");
}

// Ficha do funcionário - Jefferson
// Dados da janela "Ficha do funcionário". SalarioVisivel fica false quando é Gestor.
public sealed record FichaDoFuncionario(
    Employee Funcionario,
    DateTime Referencia,
    string TempoDeCasa,
    int Presencas,
    int Atrasos,
    int Faltas,
    Attendance? UltimoPonto,
    bool SalarioVisivel,
    Payroll? UltimaFolha);
