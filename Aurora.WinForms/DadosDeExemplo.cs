using Aurora.Core;

namespace Aurora.WinForms;

// Dados de exemplo guardados na memória (sem banco).
// O sistema usava isso antes do banco. Hoje é usado nos testes.
internal sealed class DadosDeExemplo : IRepositorio
{
    public static readonly DateTime Hoje = new(2026, 11, 13);
    private int proximaMatricula = 109;
    private int proximaVaga = 4;
    private int proximoCandidato = 5;

    private List<Funcionario> Funcionarios { get; } =
    [
        new(101, "Marina Teixeira", "Analista de RH", "Pessoas", "Ativo", new(2022, 3, 14)),
        new(102, "Rafael Monteiro", "Desenvolvedor", "Tecnologia", "Ativo", new(2021, 8, 2)),
        new(103, "Helena Duarte", "Contadora", "Financeiro", "Ativo", new(2020, 5, 18)),
        new(104, "Caio Albuquerque", "Executivo de contas", "Comercial", "Férias", new(2023, 1, 9)),
        new(105, "Beatriz Lemos", "Coord. de operações", "Operações", "Ativo", new(2019, 11, 25)),
        new(106, "Tiago Ferreira", "Analista de suporte", "Tecnologia", "Afastado", new(2024, 2, 5)),
        new(107, "Larissa Campos", "Assistente financeiro", "Financeiro", "Ativo", new(2025, 6, 16)),
        new(108, "Otávio Prado", "Gerente comercial", "Comercial", "Ativo", new(2018, 9, 3))
    ];
    private List<RegistroDePonto> Pontos { get; } = [];
    private List<Vaga> Vagas { get; } =
    [new(1, "Desenvolvedor back-end", "Tecnologia", "Aberta"), new(2, "Analista financeiro", "Financeiro", "Aberta"), new(3, "Assistente de RH", "Pessoas", "Aberta")];
    private List<Candidato> Candidatos { get; } =
    [new(1, "Júlia Nogueira", 1, "Entrevista"), new(2, "Amanda Rocha", 1, "Teste técnico"), new(3, "Lucas Barreto", 2, "Triagem"), new(4, "Renata Siqueira", 2, "Proposta")];
    private List<LancamentoDaFolha> Folhas { get; } = [];
    private List<Compromisso> Agenda { get; } =
    [new("Entrevista · Júlia Nogueira", Hoje.AddHours(10.5), "Recrutamento"), new("Revisão das ocorrências de ponto", Hoje.AddHours(14), "Ponto"), new("Fechamento da folha de novembro", new(2026, 11, 27, 16, 0, 0), "Salários")];

    public DadosDeExemplo()
    {
        int[] quantidades = [5, 6, 4, 6, 5];
        var ativos = Funcionarios.Where(pessoa => pessoa.Situacao == "Ativo").ToArray();
        for (var dia = 0; dia < quantidades.Length; dia++)
        {
            var data = Hoje.AddDays(dia - 4);
            for (var i = 0; i < ativos.Length; i++)
            {
                var situacao = i >= quantidades[dia] ? "Falta" : i == 0 ? "Atraso" : "Presença";
                var motivo = situacao switch
                {
                    "Atraso" => "Atraso no transporte público.",
                    "Falta" => "Compromisso pessoal comunicado ao RH.",
                    _ => ""
                };
                Pontos.Add(new(ativos[i].Id, data, i < quantidades[dia] ? (i == 0 ? "08:14" : "08:00") : "", i < quantidades[dia] ? "17:00" : "", situacao, motivo));
            }
        }
        foreach (var pessoa in ativos)
            Folhas.Add(new(pessoa.Id, new(2026, 11, 1), 2900 + (pessoa.Id - 100) * 410, 180, 125));
    }

    // devolve uma cópia da lista para a tela não mexer na original
    IReadOnlyList<Funcionario> IRepositorio.Funcionarios => Funcionarios.ToArray();
    IReadOnlyList<RegistroDePonto> IRepositorio.Pontos => Pontos.ToArray();
    IReadOnlyList<Vaga> IRepositorio.Vagas => Vagas.ToArray();
    IReadOnlyList<Candidato> IRepositorio.Candidatos => Candidatos.ToArray();
    IReadOnlyList<LancamentoDaFolha> IRepositorio.Folhas => Folhas.ToArray();
    IReadOnlyList<Compromisso> IRepositorio.Agenda => Agenda.ToArray();

    void IRepositorio.SalvarFuncionario(Funcionario funcionario)
    {
        if (funcionario.Id == 0) Funcionarios.Add(funcionario with { Id = proximaMatricula++ });
        else Funcionarios[Funcionarios.FindIndex(item => item.Id == funcionario.Id)] = funcionario;
    }

    void IRepositorio.ExcluirFuncionario(int id) => Funcionarios.RemoveAll(item => item.Id == id);

    void IRepositorio.SalvarPonto(RegistroDePonto registro, bool editar)
    {
        if (editar) Pontos[Pontos.FindIndex(item => item.Matricula == registro.Matricula && item.Dia == registro.Dia)] = registro;
        else Pontos.Add(registro);
    }

    void IRepositorio.SalvarVaga(Vaga vaga)
    {
        if (vaga.Id == 0) Vagas.Add(vaga with { Id = proximaVaga++ });
        else Vagas[Vagas.FindIndex(item => item.Id == vaga.Id)] = vaga;
    }

    void IRepositorio.SalvarCandidato(Candidato candidato)
    {
        if (candidato.Id == 0) Candidatos.Add(candidato with { Id = proximoCandidato++ });
        else Candidatos[Candidatos.FindIndex(item => item.Id == candidato.Id)] = candidato;
    }

    void IRepositorio.SalvarFolha(LancamentoDaFolha lancamento)
    {
        var posicao = Folhas.FindIndex(item => item.Matricula == lancamento.Matricula && item.Competencia == lancamento.Competencia);
        if (posicao < 0) Folhas.Add(lancamento);
        else Folhas[posicao] = lancamento;
    }

    void IRepositorio.AdicionarCompromisso(Compromisso compromisso) => Agenda.Add(compromisso);
}
