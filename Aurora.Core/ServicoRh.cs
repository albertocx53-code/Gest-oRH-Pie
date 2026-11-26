using System.Globalization;

namespace Aurora.Core;

// Erro de regra: quando alguma regra do sistema não é cumprida.
// A tela pega esse erro e mostra a mensagem para o usuário.
public class ErroDeRegra : Exception
{
    public ErroDeRegra(string mensagem) : base(mensagem)
    {
    }
}

// Aqui ficam as regras do sistema. Toda tela passa por essa classe
// antes de ler ou gravar alguma coisa.
public class ServicoRh
{
    private IRepositorio repositorio;
    private Sessao sessao;

    public ServicoRh(IRepositorio repositorio, Sessao sessao)
    {
        this.repositorio = repositorio;
        this.sessao = sessao;
    }

    // nome de quem entrou no sistema
    public string Usuario
    {
        get { return sessao.NomeDeExibicao; }
    }

    // nome do perfil para mostrar na tela
    public string NomeDoPerfil
    {
        get
        {
            if (sessao.Perfil == PerfilDeAcesso.Administrador) return "Administrador";
            if (sessao.Perfil == PerfilDeAcesso.RecursosHumanos) return "Recursos Humanos";
            return "Gestor";
        }
    }

    // Só o Administrador e o RH podem editar. O Gestor só consulta.
    public bool PodeEditar
    {
        get
        {
            if (!sessao.Ativa) return false;
            return sessao.Perfil == PerfilDeAcesso.Administrador || sessao.Perfil == PerfilDeAcesso.RecursosHumanos;
        }
    }

    // quem pode editar também pode ver os salários
    public bool PodeVerSalarios
    {
        get { return PodeEditar; }
    }


    // ===== CONSULTAS =====

    public IReadOnlyList<Funcionario> Funcionarios
    {
        get
        {
            ConferirSessao();
            return repositorio.Funcionarios;
        }
    }

    public IReadOnlyList<Vaga> Vagas
    {
        get
        {
            ConferirSessao();
            return repositorio.Vagas;
        }
    }

    public IReadOnlyList<Candidato> Candidatos
    {
        get
        {
            ConferirSessao();
            return repositorio.Candidatos;
        }
    }

    // O Gestor não vê os compromissos da categoria Salários
    public IReadOnlyList<Compromisso> Agenda
    {
        get
        {
            ConferirSessao();
            List<Compromisso> lista = new List<Compromisso>();
            foreach (Compromisso item in repositorio.Agenda)
            {
                if (PodeVerSalarios || item.Categoria != "Salários")
                {
                    lista.Add(item);
                }
            }
            return lista;
        }
    }

    // registros de ponto entre duas datas
    public IReadOnlyList<RegistroDePonto> PontosEntre(DateTime inicio, DateTime fim)
    {
        ConferirSessao();
        Exigir(inicio.Date <= fim.Date, "A data final deve ser igual ou posterior à inicial.");

        List<RegistroDePonto> lista = new List<RegistroDePonto>();
        foreach (RegistroDePonto ponto in repositorio.Pontos)
        {
            if (ponto.Dia >= inicio.Date && ponto.Dia <= fim.Date)
            {
                lista.Add(ponto);
            }
        }
        return lista;
    }

    // lançamentos da folha de um mês
    public IReadOnlyList<LancamentoDaFolha> FolhaDoMes(DateTime mes)
    {
        ConferirSessao();
        Exigir(PodeVerSalarios, "Seu perfil não tem acesso aos salários.");

        List<LancamentoDaFolha> lista = new List<LancamentoDaFolha>();
        foreach (LancamentoDaFolha item in repositorio.Folhas)
        {
            if (item.Competencia == InicioDoMes(mes))
            {
                lista.Add(item);
            }
        }
        return lista;
    }

    public string NomeDoFuncionario(int id)
    {
        foreach (Funcionario funcionario in Funcionarios)
        {
            if (funcionario.Id == id) return funcionario.Nome;
        }
        return "Matrícula " + id;
    }

    public string TituloDaVaga(int id)
    {
        foreach (Vaga vaga in Vagas)
        {
            if (vaga.Id == id) return vaga.Titulo;
        }
        return "Vaga indisponível";
    }

    // quantas pessoas bateram ponto com entrada no dia
    public int PresentesNoDia(DateTime dia)
    {
        int quantidade = 0;
        foreach (RegistroDePonto ponto in PontosEntre(dia, dia))
        {
            if (ponto.Entrada.Length > 0) quantidade++;
        }
        return quantidade;
    }


    // ===== GRAVAÇÕES =====

    // Jefferson: grava o cadastro do funcionário (novo ou edição).
    // Regras: departamento e situação têm que estar na lista de Opcoes,
    // a data de admissão entre 1900 e 2100 e nome e cargo são obrigatórios.
    // Id 0 = funcionário novo.
    public void SalvarFuncionario(Funcionario item)
    {
        ConferirSePodeGravar();
        if (item.Id != 0)
        {
            Exigir(ExisteFuncionario(item.Id), "Funcionário não encontrado.");
        }
        Exigir(Opcoes.Departamentos.Contains(item.Departamento), "Selecione um departamento válido.");
        Exigir(Opcoes.SituacoesDoFuncionario.Contains(item.Situacao), "Selecione uma situação válida.");
        ConferirData(item.Admissao);

        string nome = LimparTexto(item.Nome, "nome");
        string cargo = LimparTexto(item.Cargo, "cargo");
        // "with" cria uma cópia do funcionário trocando só esses campos
        repositorio.SalvarFuncionario(item with { Nome = nome, Cargo = cargo, Admissao = item.Admissao.Date });
    }

    // Jefferson: só exclui quem não tem histórico.
    // Quem tem ponto ou salário fica guardado e pode ser inativado (botão Inativar).
    public void ExcluirFuncionario(int id)
    {
        ConferirSePodeGravar();
        Exigir(ExisteFuncionario(id), "Funcionário não encontrado.");

        bool temPonto = false;
        foreach (RegistroDePonto ponto in repositorio.Pontos)
        {
            if (ponto.Matricula == id) temPonto = true;
        }
        bool temFolha = false;
        foreach (LancamentoDaFolha item in repositorio.Folhas)
        {
            if (item.Matricula == id) temFolha = true;
        }
        Exigir(!temPonto && !temFolha, "Este cadastro tem registros de ponto ou salário. Você pode mudar a situação para Inativo.");

        repositorio.ExcluirFuncionario(id);
    }

    public void SalvarPonto(RegistroDePonto registro, bool editar)
    {
        ConferirSePodeGravar();
        ConferirData(registro.Dia);

        // para um ponto novo o funcionário precisa estar Ativo
        bool funcionarioOk = false;
        foreach (Funcionario funcionario in repositorio.Funcionarios)
        {
            if (funcionario.Id == registro.Matricula && (editar || funcionario.Situacao == "Ativo"))
            {
                funcionarioOk = true;
            }
        }
        Exigir(funcionarioOk, "Selecione um funcionário ativo para registrar o ponto.");
        Exigir(Opcoes.SituacoesDoPonto.Contains(registro.Situacao), "Selecione uma ocorrência válida.");

        DateTime dia = registro.Dia.Date;
        bool jaExiste = false;
        foreach (RegistroDePonto ponto in repositorio.Pontos)
        {
            if (ponto.Matricula == registro.Matricula && ponto.Dia == dia) jaExiste = true;
        }
        if (editar)
        {
            Exigir(jaExiste, "O registro de ponto não foi encontrado.");
        }
        else
        {
            Exigir(!jaExiste, "Já existe ponto nesse dia. Use Editar registro para alterá-lo.");
        }

        string entrada = registro.Entrada.Trim();
        string saida = registro.Saida.Trim();
        if (registro.Situacao == "Falta")
        {
            // falta não tem horário
            entrada = "";
            saida = "";
        }
        else
        {
            bool horariosOk = TentarLerHorario(entrada, out TimeOnly horaDeEntrada)
                && TentarLerHorario(saida, out TimeOnly horaDeSaida)
                && horaDeSaida > horaDeEntrada;
            Exigir(horariosOk, "Informe horários válidos. A saída deve ser posterior à entrada.");
        }

        // o motivo só vale para atraso e falta
        string motivo = "";
        if (RegistroDePonto.AceitaMotivo(registro.Situacao))
        {
            motivo = registro.Motivo.Trim();
        }
        Exigir(motivo.Length <= 240, "O motivo pode ter até 240 caracteres.");

        repositorio.SalvarPonto(registro with { Dia = dia, Entrada = entrada, Saida = saida, Motivo = motivo }, editar);
    }

    public void NovaVaga(string titulo, string departamento)
    {
        ConferirSePodeGravar();
        Exigir(Opcoes.Departamentos.Contains(departamento), "Selecione um departamento válido.");
        repositorio.SalvarVaga(new Vaga(0, LimparTexto(titulo, "título da vaga"), departamento, "Aberta"));
    }

    // se a vaga está aberta, fecha; se está fechada, abre
    public void AbrirOuFecharVaga(int id)
    {
        ConferirSePodeGravar();
        Vaga? vaga = null;
        foreach (Vaga item in repositorio.Vagas)
        {
            if (item.Id == id) vaga = item;
        }
        if (vaga == null)
        {
            throw new ErroDeRegra("Vaga não encontrada.");
        }

        string novaSituacao = "Aberta";
        if (vaga.Situacao == "Aberta") novaSituacao = "Encerrada";
        repositorio.SalvarVaga(vaga with { Situacao = novaSituacao });
    }

    public void SalvarCandidato(Candidato item)
    {
        ConferirSePodeGravar();
        Candidato? original = null;
        foreach (Candidato candidato in repositorio.Candidatos)
        {
            if (candidato.Id == item.Id) original = candidato;
        }
        Exigir(item.Id == 0 || original != null, "Candidatura não encontrada.");

        // Vaga fechada não aceita inscrição nova, mas quem já estava nela continua
        bool vagaOk = false;
        foreach (Vaga vaga in repositorio.Vagas)
        {
            if (vaga.Id == item.VagaId)
            {
                if (vaga.Situacao == "Aberta") vagaOk = true;
                if (original != null && original.VagaId == vaga.Id) vagaOk = true;
            }
        }
        Exigir(vagaOk, "Selecione uma vaga aberta.");
        Exigir(Opcoes.Etapas.Contains(item.Etapa), "Selecione uma etapa válida.");

        repositorio.SalvarCandidato(item with { Nome = LimparTexto(item.Nome, "nome do candidato") });
    }

    public void AdicionarCompromisso(string titulo, DateTime dia, string horario, string categoria)
    {
        ConferirSePodeGravar();
        ConferirData(dia);
        Exigir(Opcoes.CategoriasDaAgenda.Contains(categoria), "Selecione uma categoria válida.");
        Exigir(TentarLerHorario(horario.Trim(), out TimeOnly hora), "Informe um horário no formato HH:mm.");

        DateTime dataHora = dia.Date.Add(hora.ToTimeSpan());
        repositorio.AdicionarCompromisso(new Compromisso(LimparTexto(titulo, "título"), dataHora, categoria));
    }

    public void SalvarFolha(LancamentoDaFolha item)
    {
        ConferirSePodeGravar();
        Exigir(PodeVerSalarios, "Seu perfil não tem acesso aos salários.");
        Exigir(ExisteFuncionario(item.Matricula), "Funcionário não encontrado.");
        ConferirData(item.Competencia);
        Exigir(ValorValido(item.Salario) && ValorValido(item.Bonus) && ValorValido(item.Desconto),
            "Informe valores entre R$ 0,00 e R$ 1.000.000,00, com até duas casas decimais.");
        Exigir(item.Desconto <= item.Salario + item.Bonus, "O desconto não pode superar salário e bônus.");

        // um lançamento por pessoa por mês (sempre no dia 1)
        repositorio.SalvarFolha(item with { Competencia = InicioDoMes(item.Competencia) });
    }


    // ===== MÉTODOS DE AJUDA =====

    // se a pessoa já saiu da conta, não deixa fazer mais nada
    private void ConferirSessao()
    {
        Exigir(sessao.Ativa, "Entre novamente para continuar.");
    }

    // para gravar precisa estar logado e ter perfil que pode editar
    private void ConferirSePodeGravar()
    {
        Exigir(sessao.Ativa, "Entre novamente para continuar.");
        Exigir(PodeEditar, "Seu perfil pode consultar os dados, sem editar registros.");
    }

    private bool ExisteFuncionario(int id)
    {
        foreach (Funcionario funcionario in repositorio.Funcionarios)
        {
            if (funcionario.Id == id) return true;
        }
        return false;
    }

    // tira os espaços das pontas e confere o tamanho (1 a 120 letras)
    private static string LimparTexto(string texto, string campo)
    {
        texto = texto.Trim();
        Exigir(texto.Length > 0 && texto.Length <= 120, "Preencha " + campo + " com até 120 caracteres.");
        return texto;
    }

    // se a condição for falsa, dá erro com a mensagem
    private static void Exigir(bool condicao, string mensagem)
    {
        if (!condicao)
        {
            throw new ErroDeRegra(mensagem);
        }
    }

    private static void ConferirData(DateTime data)
    {
        Exigir(data.Year >= 1900 && data.Year <= 2100, "Informe uma data entre 1900 e 2100.");
    }

    private static DateTime InicioDoMes(DateTime data)
    {
        return new DateTime(data.Year, data.Month, 1);
    }

    // valor entre 0 e 1 milhão, com no máximo 2 casas depois da vírgula
    private static bool ValorValido(decimal valor)
    {
        if (valor < 0 || valor > 1000000) return false;
        return valor == decimal.Round(valor, 2);
    }

    // tenta ler um horário no formato 08:00
    private static bool TentarLerHorario(string texto, out TimeOnly horario)
    {
        return TimeOnly.TryParseExact(texto, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out horario);
    }
}
