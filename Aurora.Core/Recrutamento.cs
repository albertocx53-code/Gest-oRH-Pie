namespace Aurora.Core;

// Recrutamento - Kerlon (vagas, candidatos e etapas do processo seletivo)
//
// Métodos que eu acrescentei na parte do recrutamento:
//   ConsultarVagas     -> vagas com filtro de situação (Aberta/Encerrada) e departamento
//   ResumoDasVagas     -> as vagas com o total de candidatos e de contratados
//   CandidatosDaVaga   -> candidatos de uma vaga (pode filtrar pela etapa)
//   FunilDeEtapas      -> quantos candidatos tem em cada etapa
//   CadastrarCandidato -> inscreve um candidato novo, sempre na Triagem
//   SalvarCandidatura  -> grava o formulário de candidatura sem deixar nome repetido
//   VincularAVaga      -> muda a candidatura de vaga (a etapa continua igual)
//   MoverEtapa         -> coloca o candidato numa etapa escolhida
//   AvancarEtapa       -> passa o candidato para a próxima etapa
//
// São métodos de extensão ("this ServicoRh rh"), igual o Jefferson e o Clóvis fizeram.
// Tudo passa pelo ServicoRh, então o Gestor consulta mas não grava nada.
public static class Recrutamento
{
    // situações que uma vaga pode ter
    public static readonly IReadOnlyList<string> SituacoesDeVaga = new List<string> { "Aberta", "Encerrada" };

    // Vagas com as abertas primeiro e depois em ordem de título.
    // situacao e departamento são filtros: null = todas.
    public static IReadOnlyList<Vaga> ConsultarVagas(this ServicoRh rh, string? situacao = null, string? departamento = null)
    {
        if (situacao != null && !SituacoesDeVaga.Contains(situacao))
        {
            throw new ErroDeRegra("Selecione uma situação de vaga válida.");
        }
        if (departamento != null && !Opcoes.Departamentos.Contains(departamento))
        {
            throw new ErroDeRegra("Selecione um departamento válido.");
        }

        List<Vaga> lista = new List<Vaga>();
        foreach (Vaga vaga in rh.Vagas)
        {
            if (situacao != null && vaga.Situacao != situacao) continue;
            if (departamento != null && vaga.Departamento != departamento) continue;
            lista.Add(vaga);
        }

        lista.Sort((a, b) =>
        {
            // aberta vem antes de encerrada
            int ordemA = a.Situacao == "Aberta" ? 0 : 1;
            int ordemB = b.Situacao == "Aberta" ? 0 : 1;
            if (ordemA != ordemB) return ordemA.CompareTo(ordemB);
            int comparacao = string.Compare(a.Titulo, b.Titulo, StringComparison.CurrentCulture);
            if (comparacao != 0) return comparacao;
            return a.Id.CompareTo(b.Id);
        });
        return lista;
    }

    // As mesmas vagas do ConsultarVagas, cada uma com o número de candidatos e de contratados
    public static IReadOnlyList<ResumoDaVaga> ResumoDasVagas(this ServicoRh rh, string? situacao = null, string? departamento = null)
    {
        List<ResumoDaVaga> lista = new List<ResumoDaVaga>();
        IReadOnlyList<Candidato> candidatos = rh.Candidatos;
        foreach (Vaga vaga in rh.ConsultarVagas(situacao, departamento))
        {
            int total = 0;
            int contratados = 0;
            foreach (Candidato candidato in candidatos)
            {
                if (candidato.VagaId != vaga.Id) continue;
                total++;
                if (candidato.Etapa == "Contratado") contratados++;
            }
            lista.Add(new ResumoDaVaga(vaga, total, contratados));
        }
        return lista;
    }

    // Candidatos de uma vaga, em ordem de etapa (Triagem primeiro) e depois de nome.
    // etapa é um filtro: null = todas.
    public static IReadOnlyList<Candidato> CandidatosDaVaga(this ServicoRh rh, int vagaId, string? etapa = null)
    {
        EncontrarVaga(rh, vagaId);
        if (etapa != null && !Opcoes.Etapas.Contains(etapa))
        {
            throw new ErroDeRegra("Selecione uma etapa válida.");
        }

        List<Candidato> lista = new List<Candidato>();
        foreach (Candidato candidato in rh.Candidatos)
        {
            if (candidato.VagaId != vagaId) continue;
            if (etapa != null && candidato.Etapa != etapa) continue;
            lista.Add(candidato);
        }

        lista.Sort((a, b) =>
        {
            int comparacao = IndiceDaEtapa(a.Etapa).CompareTo(IndiceDaEtapa(b.Etapa));
            if (comparacao != 0) return comparacao;
            comparacao = string.Compare(a.Nome, b.Nome, StringComparison.CurrentCulture);
            if (comparacao != 0) return comparacao;
            return a.Id.CompareTo(b.Id);
        });
        return lista;
    }

    // Quantos candidatos tem em cada etapa, na ordem do processo (etapa vazia aparece com zero).
    // vagaId null = todas as vagas.
    public static IReadOnlyList<ContagemDeEtapa> FunilDeEtapas(this ServicoRh rh, int? vagaId = null)
    {
        if (vagaId != null) EncontrarVaga(rh, vagaId.Value);

        List<ContagemDeEtapa> funil = new List<ContagemDeEtapa>();
        foreach (string etapa in Opcoes.Etapas)
        {
            int quantidade = 0;
            foreach (Candidato candidato in rh.Candidatos)
            {
                if (vagaId != null && candidato.VagaId != vagaId.Value) continue;
                if (candidato.Etapa == etapa) quantidade++;
            }
            funil.Add(new ContagemDeEtapa(etapa, quantidade));
        }
        return funil;
    }

    // Inscreve um candidato novo, sempre na primeira etapa (Triagem).
    // O nome repetido eu só confiro para quem pode editar.
    // Para o Gestor, quem dá a mensagem certa (de perfil) é o SalvarCandidato.
    public static void CadastrarCandidato(this ServicoRh rh, string nome, int vagaId)
    {
        if (rh.PodeEditar) ConferirDuplicidade(rh, nome, vagaId, 0);
        rh.SalvarCandidato(new Candidato(0, nome, vagaId, Opcoes.Etapas[0]));
    }

    // Usado no formulário "Novo candidato" / "Editar candidatura".
    // Confere o nome repetido (sem contar a própria candidatura) e grava.
    public static void SalvarCandidatura(this ServicoRh rh, Candidato candidato)
    {
        if (rh.PodeEditar) ConferirDuplicidade(rh, candidato.Nome, candidato.VagaId, candidato.Id);
        rh.SalvarCandidato(candidato);
    }

    // Muda a candidatura de vaga. A etapa continua a mesma.
    // O SalvarCandidato só aceita vaga aberta.
    public static void VincularAVaga(this ServicoRh rh, int candidatoId, int novaVagaId)
    {
        Candidato atual = EncontrarCandidato(rh, candidatoId);
        if (atual.VagaId == novaVagaId)
        {
            throw new ErroDeRegra("O candidato já está nesta vaga.");
        }
        if (rh.PodeEditar) ConferirDuplicidade(rh, atual.Nome, novaVagaId, candidatoId);
        rh.SalvarCandidato(atual with { VagaId = novaVagaId });
    }

    // Coloca o candidato numa etapa escolhida (pode voltar, para corrigir um erro)
    public static void MoverEtapa(this ServicoRh rh, int candidatoId, string novaEtapa)
    {
        Candidato atual = EncontrarCandidato(rh, candidatoId);
        if (atual.Etapa == novaEtapa)
        {
            throw new ErroDeRegra("A candidatura já está na etapa " + novaEtapa + ".");
        }
        rh.SalvarCandidato(atual with { Etapa = novaEtapa });
    }

    // Passa para a próxima etapa (Triagem -> Entrevista -> ... -> Contratado).
    // Quem já foi contratado não tem próxima etapa.
    public static void AvancarEtapa(this ServicoRh rh, int candidatoId)
    {
        Candidato atual = EncontrarCandidato(rh, candidatoId);
        int indice = IndiceDaEtapa(atual.Etapa);
        if (indice >= Opcoes.Etapas.Count - 1)
        {
            throw new ErroDeRegra("O candidato já está na última etapa do processo.");
        }
        rh.SalvarCandidato(atual with { Etapa = Opcoes.Etapas[indice + 1] });
    }

    // Posição da etapa (0 = Triagem). Se não achar, vai pro fim da lista.
    public static int IndiceDaEtapa(string etapa)
    {
        for (int i = 0; i < Opcoes.Etapas.Count; i++)
        {
            if (Opcoes.Etapas[i] == etapa) return i;
        }
        return int.MaxValue;
    }

    // O mesmo nome (sem ligar pra maiúscula) não pode entrar duas vezes na mesma vaga
    private static void ConferirDuplicidade(ServicoRh rh, string nome, int vagaId, int ignorar)
    {
        string limpo = nome.Trim();
        foreach (Candidato candidato in rh.Candidatos)
        {
            if (candidato.Id == ignorar) continue;
            if (candidato.VagaId == vagaId && string.Equals(candidato.Nome, limpo, StringComparison.CurrentCultureIgnoreCase))
            {
                throw new ErroDeRegra("Este candidato já está inscrito nesta vaga.");
            }
        }
    }

    private static Vaga EncontrarVaga(ServicoRh rh, int id)
    {
        foreach (Vaga vaga in rh.Vagas)
        {
            if (vaga.Id == id) return vaga;
        }
        throw new ErroDeRegra("Vaga não encontrada.");
    }

    private static Candidato EncontrarCandidato(ServicoRh rh, int id)
    {
        foreach (Candidato candidato in rh.Candidatos)
        {
            if (candidato.Id == id) return candidato;
        }
        throw new ErroDeRegra("Candidatura não encontrada.");
    }
}

// ResumoDaVaga - Kerlon
// Uma linha da tabela de vagas: a vaga, quantos candidatos tem e quantos foram contratados.
public record ResumoDaVaga(Vaga Vaga, int Candidatos, int Contratados);

// ContagemDeEtapa - Kerlon
// Uma parte do funil: o nome da etapa e quantos candidatos estão nela.
public record ContagemDeEtapa(string Etapa, int Candidatos);
