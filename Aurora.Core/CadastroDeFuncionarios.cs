namespace Aurora.Core;

// Cadastro de funcionários - parte do Jefferson
// Fiz como métodos de extensão do ServicoRh (por isso o "this ServicoRh rh"),
// assim não precisei mexer no código que o grupo já tinha.
// Tudo passa pelo ServicoRh, então o Gestor só consulta: não salva e não vê salário.
public static class CadastroDeFuncionarios
{
    // Data antiga só pra achar o último ponto em qualquer mês
    private static readonly DateTime PrimeiroDia = new DateTime(1900, 1, 1);

    // Monta a ficha pela matrícula. O ponto mostrado é do mês da data de referência.
    public static FichaDoFuncionario ConsultarFicha(this ServicoRh rh, int matricula, DateTime referencia)
    {
        var funcionario = Encontrar(rh, matricula);

        // Ponto do mês inteiro
        var inicioDoMes = new DateTime(referencia.Year, referencia.Month, 1);
        var fimDoMes = inicioDoMes.AddMonths(1).AddDays(-1);
        var pontoDoMes = rh.PontosEntre(inicioDoMes, fimDoMes)
            .Where(item => item.Matricula == matricula)
            .ToArray();

        // Último ponto até a referência, de qualquer mês
        var ultimoPonto = rh.PontosEntre(PrimeiroDia, referencia.Date)
            .Where(item => item.Matricula == matricula)
            .OrderBy(item => item.Dia)
            .LastOrDefault();

        // Salário só pra Administrador e RH. Procura até 12 meses atrás.
        LancamentoDaFolha? ultimaFolha = null;
        if (rh.PodeVerSalarios)
        {
            for (int meses = 0; meses < 12 && ultimaFolha == null; meses++)
            {
                foreach (LancamentoDaFolha item in rh.FolhaDoMes(inicioDoMes.AddMonths(-meses)))
                {
                    if (item.Matricula == matricula) ultimaFolha = item;
                }
            }
        }

        return new FichaDoFuncionario(
            funcionario,
            referencia.Date,
            TempoDeCasa(funcionario.Admissao, referencia),
            pontoDoMes.Count(item => item.Situacao == "Presença"),
            pontoDoMes.Count(item => item.Situacao == "Atraso"),
            pontoDoMes.Count(item => item.Situacao == "Falta"),
            ultimoPonto,
            rh.PodeVerSalarios,
            ultimaFolha);
    }

    // Cargos já cadastrados, sem repetir, pra sugerir no campo Cargo
    public static IReadOnlyList<string> CargosCadastrados(this ServicoRh rh)
    {
        List<string> cargos = new List<string>();
        foreach (Funcionario item in rh.Funcionarios)
        {
            string cargo = item.Cargo.Trim();
            if (cargo.Length == 0) continue;

            // confere se já está na lista (sem ligar pra maiúscula/minúscula)
            bool jaTem = false;
            foreach (string c in cargos)
            {
                if (string.Equals(c, cargo, StringComparison.CurrentCultureIgnoreCase)) jaTem = true;
            }
            if (!jaTem) cargos.Add(cargo);
        }
        cargos.Sort(StringComparer.CurrentCulture); // ordem alfabética
        return cargos;
    }

    // Usado no botão Inativar, já que quem tem ponto ou salário não pode ser excluído
    public static void MudarSituacao(this ServicoRh rh, int matricula, string novaSituacao)
    {
        var atual = Encontrar(rh, matricula);
        if (atual.Situacao == novaSituacao)
        {
            throw new ErroDeRegra("O cadastro já está com a situação " + novaSituacao + ".");
        }

        // O SalvarFuncionario já barra o Gestor e valida a situação
        rh.SalvarFuncionario(atual with { Situacao = novaSituacao });
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
    private static Funcionario Encontrar(ServicoRh rh, int matricula)
    {
        foreach (Funcionario item in rh.Funcionarios)
        {
            if (item.Id == matricula) return item;
        }
        throw new ErroDeRegra("Funcionário não encontrado.");
    }
}

// Ficha do funcionário - Jefferson
// Dados da janela "Ficha do funcionário". SalarioVisivel fica false quando é Gestor.
public record FichaDoFuncionario(
    Funcionario Funcionario,
    DateTime Referencia,
    string TempoDeCasa,
    int Presencas,
    int Atrasos,
    int Faltas,
    RegistroDePonto? UltimoPonto,
    bool SalarioVisivel,
    LancamentoDaFolha? UltimaFolha);
