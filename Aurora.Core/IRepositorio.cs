namespace Aurora.Core;

// Interface que diz o que um "repositório" (lugar onde os dados ficam) precisa ter.
// DadosDeExemplo guarda na memória e RepositorioSqlite guarda no banco.
// O Program.cs escolhe qual dos dois usar.
// Id 0 = cadastro novo. Quem gera o Id de verdade é o repositório.
public interface IRepositorio
{
    IReadOnlyList<Funcionario> Funcionarios { get; }
    IReadOnlyList<RegistroDePonto> Pontos { get; }
    IReadOnlyList<Vaga> Vagas { get; }
    IReadOnlyList<Candidato> Candidatos { get; }
    IReadOnlyList<LancamentoDaFolha> Folhas { get; }
    IReadOnlyList<Compromisso> Agenda { get; }

    void SalvarFuncionario(Funcionario funcionario);
    void ExcluirFuncionario(int id);
    void SalvarPonto(RegistroDePonto registro, bool editar);
    void SalvarVaga(Vaga vaga);
    void SalvarCandidato(Candidato candidato);
    void SalvarFolha(LancamentoDaFolha lancamento);
    void AdicionarCompromisso(Compromisso compromisso);
}
