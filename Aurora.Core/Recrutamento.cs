namespace Aurora.Core;

// Recrutamento - Kerlon (vagas, candidatos e etapas do processo seletivo)
//
// Acrescenta ao RhService as consultas e ações da área de recrutamento:
//   ConsultarVagas       -> vagas com filtro por situação (Aberta/Encerrada) e departamento
//   ResumoDasVagas       -> as mesmas vagas com o total de candidatos e quantos já foram contratados
//   CandidatosDaVaga     -> candidatos de uma vaga, com filtro opcional por etapa
//   FunilDeEtapas        -> quantos candidatos há em cada etapa (Triagem ... Contratado)
//   CadastrarCandidato   -> inscreve um candidato novo em uma vaga aberta, sempre na Triagem
//   SalvarCandidatura    -> grava o formulário de candidatura (nova ou edição) conferindo duplicidade
//   VincularAVaga        -> muda a candidatura para outra vaga aberta (mantém a etapa)
//   MoverEtapa           -> coloca o candidato em uma etapa escolhida
//   AvancarEtapa         -> leva o candidato para a etapa seguinte do processo
//
// Estes são "métodos de extensão" (repare no "this RhService rh"), o mesmo caminho usado em
// CadastroDeFuncionarios e FrequenciaDePonto. Na tela a chamada fica data.ConsultarVagas(...),
// como se o método fosse do próprio RhService, mas ele mora neste arquivo.
//
// Tudo aqui passa pelos métodos públicos do RhService (Vacancies, Candidates, AddVacancy,
// ToggleVacancy e SaveCandidate). Por isso as regras de sessão, de perfil e de validação
// continuam valendo: o Gestor consulta o recrutamento, mas não grava nada.
public static class Recrutamento
{
    // Situações possíveis de uma vaga. O RhService cria como "Aberta" e ToggleVacancy alterna.
    public static readonly IReadOnlyList<string> SituacoesDeVaga = Array.AsReadOnly(new[] { "Aberta", "Encerrada" });

    // Vagas, da mais "viva" para a mais antiga: abertas primeiro e, dentro de cada situação, por título.
    // "situacao" e "departamento" são filtros opcionais: null quer dizer todas.
    public static IReadOnlyList<Vacancy> ConsultarVagas(this RhService rh, string? situacao = null, string? departamento = null)
    {
        if (situacao is not null && !SituacoesDeVaga.Contains(situacao))
            throw new RhRuleException("Selecione uma situação de vaga válida.");
        if (departamento is not null && !RhOptions.Teams.Contains(departamento))
            throw new RhRuleException("Selecione um departamento válido.");

        return rh.Vacancies
            .Where(item => situacao is null || item.Status == situacao)
            .Where(item => departamento is null || item.Team == departamento)
            .OrderBy(item => item.Status == "Aberta" ? 0 : 1)
            .ThenBy(item => item.Title, StringComparer.CurrentCulture)
            .ThenBy(item => item.Id)
            .ToArray();
    }

    // As mesmas vagas de ConsultarVagas (mesmos filtros), cada uma com os números dos candidatos.
    // Assim o resumo mostrado na tela sempre bate com a tabela de vagas.
    public static IReadOnlyList<ResumoDaVaga> ResumoDasVagas(this RhService rh, string? situacao = null, string? departamento = null)
    {
        var vagas = rh.ConsultarVagas(situacao, departamento);
        var candidatos = rh.Candidates.ToLookup(item => item.Vacancy);
        return vagas.Select(vaga =>
        {
            var daVaga = candidatos[vaga.Id].ToArray();
            return new ResumoDaVaga(vaga, daVaga.Length, daVaga.Count(item => item.Stage == "Contratado"));
        }).ToArray();
    }

    // Candidatos de uma vaga, em ordem de etapa (Triagem primeiro) e depois de nome.
    // "etapa" é um filtro opcional: null quer dizer todas as etapas.
    public static IReadOnlyList<Candidate> CandidatosDaVaga(this RhService rh, int vagaId, string? etapa = null)
    {
        EncontrarVaga(rh, vagaId);
        if (etapa is not null && !RhOptions.Stages.Contains(etapa))
            throw new RhRuleException("Selecione uma etapa válida.");

        return rh.Candidates
            .Where(item => item.Vacancy == vagaId)
            .Where(item => etapa is null || item.Stage == etapa)
            .OrderBy(item => IndiceDaEtapa(item.Stage))
            .ThenBy(item => item.Name, StringComparer.CurrentCulture)
            .ThenBy(item => item.Id)
            .ToArray();
    }

    // Quantos candidatos há em cada etapa, sempre na ordem do processo e incluindo etapas vazias
    // (zero). "vagaId" nulo conta todas as vagas; informado, conta só a vaga escolhida.
    public static IReadOnlyList<ContagemDeEtapa> FunilDeEtapas(this RhService rh, int? vagaId = null)
    {
        if (vagaId is { } id) EncontrarVaga(rh, id);
        var candidatos = rh.Candidates.Where(item => vagaId is null || item.Vacancy == vagaId).ToArray();
        return RhOptions.Stages
            .Select(etapa => new ContagemDeEtapa(etapa, candidatos.Count(item => item.Stage == etapa)))
            .ToArray();
    }

    // Inscreve um candidato novo. Toda candidatura começa na primeira etapa (Triagem).
    // O SaveCandidate confere o perfil, se a vaga está aberta e o tamanho do nome (até 120).
    // A checagem de duplicidade (mesmo nome na mesma vaga) é daqui, e só roda para quem pode
    // editar: para o Gestor a mensagem certa é a do perfil, que vem do SaveCandidate.
    public static void CadastrarCandidato(this RhService rh, string nome, int vagaId)
    {
        if (rh.CanEdit) ConferirDuplicidade(rh, nome, vagaId, ignorar: 0);
        rh.SaveCandidate(new Candidate(0, nome, vagaId, RhOptions.Stages[0]));
    }

    // Usado pelo formulário "Novo candidato" / "Editar candidatura" (AuroraForm.Modules.cs).
    // O formulário deixa escolher vaga e etapa, então aqui a etapa vem pronta. Faz a mesma conferência
    // de duplicidade de CadastrarCandidato (sem contar a própria candidatura, quando é edição)
    // e entrega o resto ao SaveCandidate.
    public static void SalvarCandidatura(this RhService rh, Candidate candidato)
    {
        if (rh.CanEdit) ConferirDuplicidade(rh, candidato.Name, candidato.Vacancy, ignorar: candidato.Id);
        rh.SaveCandidate(candidato);
    }

    // Muda a candidatura para outra vaga. A etapa continua a mesma.
    // O SaveCandidate só aceita vaga aberta; a vaga antiga pode estar encerrada sem problema.
    public static void VincularAVaga(this RhService rh, int candidatoId, int novaVagaId)
    {
        var atual = EncontrarCandidato(rh, candidatoId);
        if (atual.Vacancy == novaVagaId)
            throw new RhRuleException("O candidato já está nesta vaga.");
        if (rh.CanEdit) ConferirDuplicidade(rh, atual.Name, novaVagaId, ignorar: candidatoId);
        rh.SaveCandidate(atual with { Vacancy = novaVagaId });
    }

    // Coloca o candidato na etapa escolhida (para frente ou para trás, por exemplo ao corrigir um erro).
    // O SaveCandidate confere o perfil e se a etapa existe na lista RhOptions.Stages.
    public static void MoverEtapa(this RhService rh, int candidatoId, string novaEtapa)
    {
        var atual = EncontrarCandidato(rh, candidatoId);
        if (atual.Stage == novaEtapa)
            throw new RhRuleException($"A candidatura já está na etapa {novaEtapa}.");
        rh.SaveCandidate(atual with { Stage = novaEtapa });
    }

    // Leva o candidato para a etapa seguinte (Triagem -> Entrevista -> ... -> Contratado).
    // Quem já foi contratado não tem etapa seguinte.
    public static void AvancarEtapa(this RhService rh, int candidatoId)
    {
        var atual = EncontrarCandidato(rh, candidatoId);
        var indice = IndiceDaEtapa(atual.Stage);
        if (indice >= RhOptions.Stages.Count - 1)
            throw new RhRuleException("O candidato já está na última etapa do processo.");
        rh.SaveCandidate(atual with { Stage = RhOptions.Stages[indice + 1] });
    }

    // Posição da etapa no processo (0 = Triagem). Etapa desconhecida vai para o fim da lista.
    public static int IndiceDaEtapa(string etapa)
    {
        for (var i = 0; i < RhOptions.Stages.Count; i++)
            if (RhOptions.Stages[i] == etapa) return i;
        return int.MaxValue;
    }

    // O mesmo candidato (mesmo nome, sem diferenciar maiúsculas) não entra duas vezes na mesma vaga.
    private static void ConferirDuplicidade(RhService rh, string nome, int vagaId, int ignorar)
    {
        var limpo = nome.Trim();
        if (rh.Candidates.Any(item => item.Id != ignorar && item.Vacancy == vagaId
                                      && string.Equals(item.Name, limpo, StringComparison.CurrentCultureIgnoreCase)))
            throw new RhRuleException("Este candidato já está inscrito nesta vaga.");
    }

    private static Vacancy EncontrarVaga(RhService rh, int id) =>
        rh.Vacancies.FirstOrDefault(item => item.Id == id)
        ?? throw new RhRuleException("Vaga não encontrada.");

    private static Candidate EncontrarCandidato(RhService rh, int id) =>
        rh.Candidates.FirstOrDefault(item => item.Id == id)
        ?? throw new RhRuleException("Candidatura não encontrada.");
}

// ResumoDaVaga - Kerlon
// Uma linha da tabela de vagas: a vaga, quantos candidatos ela tem e quantos já foram contratados.
public sealed record ResumoDaVaga(Vacancy Vaga, int Candidatos, int Contratados);

// ContagemDeEtapa - Kerlon
// Uma barra do funil do processo seletivo: o nome da etapa e quantos candidatos estão nela.
public sealed record ContagemDeEtapa(string Etapa, int Candidatos);
