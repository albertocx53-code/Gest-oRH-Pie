namespace Aurora.Core;

// CadastroDeFuncionarios - Jefferson (cadastro de funcionários)
//
// Acrescenta ao RhService as consultas e ações do cadastro de funcionários:
//   ConsultarFicha    -> junta numa ficha o cadastro, o ponto do mês e o último salário
//   CargosCadastrados -> lista os cargos que já existem, para sugerir no campo Cargo
//   MudarSituacao     -> troca só a situação (ex.: Inativar quem não pode ser excluído)
//   TempoDeCasa       -> calcula há quanto tempo a pessoa foi admitida
//
// Estes são "métodos de extensão" (repare no "this RhService rh"). Na tela a chamada
// fica rh.ConsultarFicha(...), como se o método fosse do próprio RhService, mas ele
// mora neste arquivo. Assim o RhService que o grupo já tinha não precisou mudar.
//
// Tudo aqui passa pelos métodos públicos do RhService (Employees, AttendanceBetween,
// PayrollFor e SaveEmployee). Por isso as regras de sessão, de perfil e de validação
// continuam valendo: o Gestor consulta, mas não grava e não vê salário.
public static class CadastroDeFuncionarios
{
    // Data inicial usada para procurar o último ponto em qualquer mês.
    private static readonly DateTime PrimeiroDia = new(1900, 1, 1);

    // Monta a ficha de uma matrícula. "referencia" é o dia usado como hoje
    // (na demonstração, 13/11/2026). O ponto contado é o do mês dessa data.
    public static FichaDoFuncionario ConsultarFicha(this RhService rh, int matricula, DateTime referencia)
    {
        var funcionario = Encontrar(rh, matricula);

        // Ponto do mês de referência, do dia 1 ao último dia do mês.
        var inicioDoMes = new DateTime(referencia.Year, referencia.Month, 1);
        var fimDoMes = inicioDoMes.AddMonths(1).AddDays(-1);
        var pontoDoMes = rh.AttendanceBetween(inicioDoMes, fimDoMes)
            .Where(item => item.Person == matricula)
            .ToArray();

        // Último registro de ponto até a data de referência, em qualquer mês.
        var ultimoPonto = rh.AttendanceBetween(PrimeiroDia, referencia.Date)
            .Where(item => item.Person == matricula)
            .OrderBy(item => item.Day)
            .LastOrDefault();

        // O salário só é procurado quando o perfil pode ver a folha (Administrador e RH).
        // Para o Gestor a ficha nem consulta a folha, então nenhum valor chega à tela.
        // A busca volta até 12 meses e para no primeiro lançamento encontrado.
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

    // Cargos que já existem no cadastro, sem repetição e em ordem alfabética.
    // A tela usa esta lista para sugerir o cargo enquanto a pessoa digita.
    // Isso evita o mesmo cargo escrito de jeitos diferentes ("Analista de RH" e "analista RH").
    public static IReadOnlyList<string> CargosCadastrados(this RhService rh) =>
        rh.Employees
            .Select(item => item.Role.Trim())
            .Where(cargo => cargo.Length > 0)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(cargo => cargo, StringComparer.CurrentCulture)
            .ToArray();

    // Troca só a situação do funcionário e mantém o resto do cadastro.
    // É o que o botão Inativar usa: quem tem ponto ou salário não pode ser excluído,
    // então o histórico fica guardado e a pessoa passa para "Inativo".
    public static void MudarSituacao(this RhService rh, int matricula, string novaSituacao)
    {
        var atual = Encontrar(rh, matricula);
        if (atual.Status == novaSituacao)
            throw new RhRuleException($"O cadastro já está com a situação {novaSituacao}.");

        // O SaveEmployee confere o perfil (Gestor não grava) e se a situação existe na lista.
        rh.SaveEmployee(atual with { Status = novaSituacao });
    }

    // Tempo entre a admissão e a data de referência, em anos e meses.
    // Ex.: admissão 14/03/2022 e referência 13/11/2026 -> "4 anos e 7 meses".
    // O mês só conta quando o dia da admissão já chegou (por isso o "meses--").
    public static string TempoDeCasa(DateTime admissao, DateTime referencia)
    {
        if (admissao.Date > referencia.Date) return "Admissão futura";

        var meses = (referencia.Year - admissao.Year) * 12 + referencia.Month - admissao.Month;
        if (referencia.Day < admissao.Day) meses--;

        var anos = meses / 12;
        meses %= 12;
        if (anos == 0 && meses == 0) return "Menos de 1 mês";

        var partes = new List<string>();
        if (anos > 0) partes.Add(anos == 1 ? "1 ano" : $"{anos} anos");
        if (meses > 0) partes.Add(meses == 1 ? "1 mês" : $"{meses} meses");
        return string.Join(" e ", partes);
    }

    private static Employee Encontrar(RhService rh, int matricula) =>
        rh.Employees.FirstOrDefault(item => item.Id == matricula)
        ?? throw new RhRuleException("Funcionário não encontrado.");
}

// FichaDoFuncionario - Jefferson
// O que a janela "Ficha do funcionário" mostra. Os números de ponto são do mês da referência.
// SalarioVisivel = false quer dizer que o perfil não tem acesso à folha (Gestor).
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
