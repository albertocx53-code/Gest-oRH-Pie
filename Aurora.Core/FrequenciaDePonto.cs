using System.Globalization;

namespace Aurora.Core;

// FrequenciaDePonto - Clóvis (ponto e frequência)
//
// Métodos que eu acrescentei na parte do ponto:
//   ConsultarPeriodo -> pontos de um período, com filtro de funcionário e de ocorrência
//   ResumoDoPeriodo  -> soma presenças, atrasos, faltas e horas do período
//   LancarFalta      -> lança uma falta (sem horário)
//   LancarAtraso     -> lança um atraso (entrada depois das 08:00)
//   SemRegistro      -> funcionários ativos que ainda não bateram ponto no dia
//   HorasTrabalhadas -> horas entre a entrada e a saída
//
// São métodos de extensão (por causa do "this ServicoRh rh"). Na tela eu chamo
// dados.ConsultarPeriodo(...) como se fosse do ServicoRh, mas o código fica aqui.
// Assim não precisei mexer no ServicoRh, e as regras dele continuam valendo
// (o Gestor consulta, mas não lança nada).
public static class FrequenciaDePonto
{
    // Horário de entrada. Chegou depois disso é atraso.
    public static readonly TimeOnly HorarioPadrao = new TimeOnly(8, 0);

    // Período máximo da consulta (1 ano)
    public const int MaximoDeDias = 366;

    // Pontos entre duas datas, ordenados por dia e depois por nome.
    // matricula e ocorrencia são filtros: se vier null, mostra todos.
    public static IReadOnlyList<RegistroDePonto> ConsultarPeriodo(this ServicoRh rh, DateTime de, DateTime ate, int? matricula = null, string? ocorrencia = null)
    {
        // a ordem das datas o PontosEntre já confere, aqui só limito o tamanho
        if ((ate.Date - de.Date).TotalDays > MaximoDeDias - 1)
        {
            throw new ErroDeRegra("O período pode ter no máximo " + MaximoDeDias + " dias.");
        }
        if (matricula != null && !ExisteFuncionario(rh, matricula.Value))
        {
            throw new ErroDeRegra("Funcionário não encontrado.");
        }
        if (ocorrencia != null && !Opcoes.SituacoesDoPonto.Contains(ocorrencia))
        {
            throw new ErroDeRegra("Selecione uma ocorrência válida.");
        }

        List<RegistroDePonto> lista = new List<RegistroDePonto>();
        foreach (RegistroDePonto ponto in rh.PontosEntre(de, ate))
        {
            if (matricula != null && ponto.Matricula != matricula.Value) continue;
            if (ocorrencia != null && ponto.Situacao != ocorrencia) continue;
            lista.Add(ponto);
        }

        // ordena: primeiro pelo dia, depois pelo nome, depois pela matrícula
        Dictionary<int, string> nomes = Nomes(rh);
        lista.Sort((a, b) =>
        {
            int comparacao = a.Dia.CompareTo(b.Dia);
            if (comparacao != 0) return comparacao;
            comparacao = string.Compare(NomeDe(nomes, a.Matricula), NomeDe(nomes, b.Matricula), StringComparison.CurrentCulture);
            if (comparacao != 0) return comparacao;
            return a.Matricula.CompareTo(b.Matricula);
        });
        return lista;
    }

    // Soma os pontos que o ConsultarPeriodo devolve. Como usa a mesma consulta,
    // o resumo sempre bate com a tabela da tela.
    public static ResumoDeFrequencia ResumoDoPeriodo(this ServicoRh rh, DateTime de, DateTime ate, int? matricula = null, string? ocorrencia = null)
    {
        IReadOnlyList<RegistroDePonto> registros = rh.ConsultarPeriodo(de, ate, matricula, ocorrencia);
        Dictionary<int, string> nomes = Nomes(rh);

        // separa os registros de cada funcionário
        Dictionary<int, List<RegistroDePonto>> porMatricula = new Dictionary<int, List<RegistroDePonto>>();
        foreach (RegistroDePonto ponto in registros)
        {
            if (!porMatricula.ContainsKey(ponto.Matricula))
            {
                porMatricula[ponto.Matricula] = new List<RegistroDePonto>();
            }
            porMatricula[ponto.Matricula].Add(ponto);
        }

        // uma linha de totais para cada funcionário
        List<FrequenciaDoFuncionario> porFuncionario = new List<FrequenciaDoFuncionario>();
        foreach (int mat in porMatricula.Keys)
        {
            List<RegistroDePonto> dele = porMatricula[mat];
            string nome = "Matrícula " + mat;
            if (nomes.ContainsKey(mat)) nome = nomes[mat];

            porFuncionario.Add(new FrequenciaDoFuncionario(mat, nome,
                Contar(dele, "Presença"), Contar(dele, "Atraso"), Contar(dele, "Falta"), Somar(dele)));
        }
        porFuncionario.Sort((a, b) => string.Compare(a.Nome, b.Nome, StringComparison.CurrentCulture));

        return new ResumoDeFrequencia(
            de.Date,
            ate.Date,
            registros.Count,
            Contar(registros, "Presença"),
            Contar(registros, "Atraso"),
            Contar(registros, "Falta"),
            Somar(registros),
            porFuncionario);
    }

    // Lança uma falta (sem entrada e sem saída).
    // "hoje" é a data de hoje (na demonstração é 13/11/2026). Não dá pra lançar no futuro.
    // Se já tem ponto nesse dia, o ServicoRh não deixa.
    public static void LancarFalta(this ServicoRh rh, int matricula, DateTime dia, string motivo, DateTime hoje)
    {
        ConferirDia(rh, dia, hoje);
        rh.SalvarPonto(new RegistroDePonto(matricula, dia, "", "", "Falta", motivo), false);
    }

    // Lança um atraso. A entrada tem que ser depois das 08:00.
    // O resto (funcionário ativo, horários, dia sem ponto) o SalvarPonto confere.
    public static void LancarAtraso(this ServicoRh rh, int matricula, DateTime dia, string entrada, string saida, string motivo, DateTime hoje)
    {
        ConferirDia(rh, dia, hoje);
        // Só confiro o horário se o perfil pode editar.
        // Para o Gestor, a mensagem certa é a de perfil, que vem do SalvarPonto.
        if (rh.PodeEditar && TentarLerHora(entrada.Trim(), out TimeOnly hora) && hora <= HorarioPadrao)
        {
            throw new ErroDeRegra("Para lançar atraso, a entrada deve ser depois das " + HorarioPadrao.ToString("HH:mm", CultureInfo.InvariantCulture) + ".");
        }
        rh.SalvarPonto(new RegistroDePonto(matricula, dia, entrada, saida, "Atraso", motivo), false);
    }

    // Funcionários ativos que ainda não têm ponto no dia.
    // A tela usa isso para já deixar selecionado quem precisa de falta ou atraso.
    public static IReadOnlyList<Funcionario> SemRegistro(this ServicoRh rh, DateTime dia)
    {
        List<int> comRegistro = new List<int>();
        foreach (RegistroDePonto ponto in rh.PontosEntre(dia, dia))
        {
            comRegistro.Add(ponto.Matricula);
        }

        List<Funcionario> lista = new List<Funcionario>();
        foreach (Funcionario funcionario in rh.Funcionarios)
        {
            if (funcionario.Situacao == "Ativo" && !comRegistro.Contains(funcionario.Id))
            {
                lista.Add(funcionario);
            }
        }
        return lista;
    }

    // Horas entre a entrada e a saída. Falta ou horário errado = zero.
    public static TimeSpan HorasTrabalhadas(RegistroDePonto registro)
    {
        if (TentarLerHora(registro.Entrada, out TimeOnly entrada) && TentarLerHora(registro.Saida, out TimeOnly saida) && saida > entrada)
        {
            return saida - entrada;
        }
        return TimeSpan.Zero;
    }

    // Ex.: 232 horas e 50 minutos vira "232 h 50 min"
    public static string FormatarHoras(TimeSpan horas)
    {
        int totalDeHoras = (int)horas.TotalHours;
        return totalDeHoras + " h " + horas.Minutes.ToString("00") + " min";
    }

    private static void ConferirDia(ServicoRh rh, DateTime dia, DateTime hoje)
    {
        if (rh.PodeEditar && dia.Date > hoje.Date)
        {
            throw new ErroDeRegra("Não é possível lançar falta ou atraso em uma data futura.");
        }
    }

    // conta quantos registros têm a situação pedida
    private static int Contar(IEnumerable<RegistroDePonto> registros, string situacao)
    {
        int quantidade = 0;
        foreach (RegistroDePonto ponto in registros)
        {
            if (ponto.Situacao == situacao) quantidade++;
        }
        return quantidade;
    }

    // soma as horas trabalhadas de todos os registros
    private static TimeSpan Somar(IEnumerable<RegistroDePonto> registros)
    {
        TimeSpan soma = TimeSpan.Zero;
        foreach (RegistroDePonto ponto in registros)
        {
            soma = soma + HorasTrabalhadas(ponto);
        }
        return soma;
    }

    // matrícula -> nome de cada funcionário
    private static Dictionary<int, string> Nomes(ServicoRh rh)
    {
        Dictionary<int, string> nomes = new Dictionary<int, string>();
        foreach (Funcionario funcionario in rh.Funcionarios)
        {
            nomes[funcionario.Id] = funcionario.Nome;
        }
        return nomes;
    }

    private static string NomeDe(Dictionary<int, string> nomes, int matricula)
    {
        if (nomes.ContainsKey(matricula)) return nomes[matricula];
        return "";
    }

    private static bool ExisteFuncionario(ServicoRh rh, int matricula)
    {
        foreach (Funcionario funcionario in rh.Funcionarios)
        {
            if (funcionario.Id == matricula) return true;
        }
        return false;
    }

    private static bool TentarLerHora(string texto, out TimeOnly hora)
    {
        return TimeOnly.TryParseExact(texto, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out hora);
    }
}

// FrequenciaDoFuncionario - Clóvis
// Uma linha do resumo: presenças, atrasos, faltas e horas de um funcionário.
public record FrequenciaDoFuncionario(int Matricula, string Nome, int Presencas, int Atrasos, int Faltas, TimeSpan Horas);

// ResumoDeFrequencia - Clóvis
// O que aparece em cima da tabela na janela "Frequência por período".
// Comparecimento = (presenças + atrasos) / registros (quem chegou atrasado também veio).
public record ResumoDeFrequencia(
    DateTime De,
    DateTime Ate,
    int Registros,
    int Presencas,
    int Atrasos,
    int Faltas,
    TimeSpan Horas,
    IReadOnlyList<FrequenciaDoFuncionario> PorFuncionario)
{
    public double Comparecimento
    {
        get
        {
            if (Registros == 0) return 0;
            return (double)(Presencas + Atrasos) / Registros;
        }
    }
}
