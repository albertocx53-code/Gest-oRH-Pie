using System.Globalization;
using Aurora.Core;

namespace Aurora.WinForms;

internal sealed partial class TelaPrincipal
{
    // começo de toda tela: página, título e botão de ação
    private Panel CriarModulo(string titulo, string descricao, Botao? acao = null)
    {
        var painel = NovaPagina();
        painel.Width = conteudo.ClientSize.Width;
        CriarTitulo(painel, titulo, descricao, acao);
        return painel;
    }
    // coloca o controle ocupando a largura toda da tela
    private static void ColocarLarguraToda(Panel pai, Control controle, int y, int altura)
    {
        controle.Bounds = new(0, y, pai.Width, altura);
        controle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        pai.Controls.Add(controle);
    }
    // cria uma tabela com as colunas pedidas (texto e peso da largura)
    private static DataGridView CriarTabela(params (string Texto, int Peso)[] colunas)
    {
        // todas as tabelas usam esse estilo
        var tabela = new TabelaDeDados
        {
            ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
            BackgroundColor = Estilo.CorDoFundo, BorderStyle = BorderStyle.None, GridColor = Estilo.CorDaBorda,
            EnableHeadersVisualStyles = false, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None, ColumnHeadersHeight = 40,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            Font = Estilo.Fonte(10), RowTemplate = { Height = 43 },
            DefaultCellStyle = new()
            {
                BackColor = Estilo.CorDoFundo, ForeColor = Estilo.CorDoTexto, SelectionBackColor = Estilo.CorClara,
                SelectionForeColor = Estilo.CorDoTexto, Padding = new(9, 0, 2, 0), NullValue = "—"
            },
            ColumnHeadersDefaultCellStyle = new()
            {
                Font = Estilo.Fonte(9, true), BackColor = Estilo.CorDoCampo, ForeColor = Estilo.CorDoTexto,
                SelectionBackColor = Estilo.CorDoCampo, SelectionForeColor = Estilo.CorDoTexto,
                Padding = new(9, 0, 2, 0)
            }
        };
        foreach (var (rotulo, peso) in colunas)
            tabela.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = rotulo, FillWeight = peso, SortMode = DataGridViewColumnSortMode.NotSortable });
        return tabela;
    }
    // adiciona uma linha na tabela e guarda o registro dela no Tag
    private static void AdicionarLinha(DataGridView tabela, object modelo, params object[] celulas)
        => tabela.Rows[tabela.Rows.Add(celulas)].Tag = modelo;
    // coloca a tabela na tela
    private static void PosicionarTabela(Panel pai, DataGridView tabela, int y, int altura)
    {
        var cartao = CriarCartao();
        ColocarLarguraToda(pai, cartao, y, altura);
        tabela.Bounds = new(10, 9, cartao.Width - 20, cartao.Height - 18);
        tabela.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        cartao.Controls.Add(tabela);
    }
    // caixa de escolha (ComboBox)
    private static ComboBox CriarLista(IEnumerable<string> valores, string selecionado, string nomeAcessivel = "")
    {
        var caixa = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = Estilo.Fonte(10), AccessibleName = nomeAcessivel, BackColor = Estilo.CorDoCampo, ForeColor = Estilo.CorDoTexto, FlatStyle = FlatStyle.Flat };
        caixa.Items.AddRange(valores.Cast<object>().ToArray());
        caixa.SelectedItem = selecionado;
        if (caixa.SelectedIndex < 0 && caixa.Items.Count > 0) caixa.SelectedIndex = 0;
        return caixa;
    }
    // campo de data (dd/mm/aaaa ou mm/aaaa)
    private static CampoDeData CriarCampoDeData(DateTime data, bool mes = false) => new(data, mes);
    // campo de texto simples
    private static TextBox CriarCampoDeTexto(string valor = "", string dica = "") => new()
    {
        Text = valor, PlaceholderText = dica, Font = Estilo.Fonte(10), MaxLength = 120, BackColor = Estilo.CorDoCampo, ForeColor = Estilo.CorDoTexto
    };
    // texto escolhido na ComboBox
    private static string ItemEscolhido(ComboBox caixa) => (string?)caixa.SelectedItem ?? "";
    // busca sem ligar pra maiúscula e acento ("operacoes" acha "Operações")
    private static bool Combina(string item, string busca) => CultureInfo.CurrentCulture.CompareInfo.IndexOf(item, busca.Trim(), CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

    // Jefferson: tela de Funcionários (cadastrar, consultar, editar e excluir).
    // Em cima ficam a busca por nome ou cargo e os filtros de departamento e situação.
    // No meio, a tabela. Embaixo, os botões. Cada linha da tabela guarda o funcionário no Tag,
    // e é por ele que os botões sabem qual cadastro abrir.
    // O que eu acrescentei (Matrícula, Consultar ficha e Inativar) está em TelaPrincipal.Funcionarios.cs.
    private Panel TelaDeFuncionarios()
    {
        var painel = CriarModulo("Funcionários", "Consulte o cadastro por nome, cargo ou departamento.", CriarBotaoDeEdicao("Novo funcionário", "plus", 169, true, () => EditarFuncionario(null)));
        var ferramentas = CriarCartao(14);
        ColocarLarguraToda(painel, ferramentas, 104, 66);
        Estilo.Colocar(ferramentas, Estilo.CriarRotulo("Buscar por nome ou cargo", 8.5f, false, Estilo.CorApagada), 14, 6);
        Estilo.Colocar(ferramentas, Estilo.CriarRotulo("Departamento", 8.5f, false, Estilo.CorApagada), 257, 6);
        Estilo.Colocar(ferramentas, Estilo.CriarRotulo("Situação", 8.5f, false, Estilo.CorApagada), 441, 6);
        var pesquisa = CriarCampoDeTexto();
        pesquisa.AccessibleName = "Buscar por nome ou cargo";
        pesquisa.Width = 232;
        Estilo.Colocar(ferramentas, pesquisa, 14, 26);
        var departamento = CriarLista(Opcoes.Departamentos.Prepend("Todos os departamentos"), "Todos os departamentos", "Departamento");
        departamento.Width = 173;
        Estilo.Colocar(ferramentas, departamento, 257, 26);
        var estado = CriarLista(Opcoes.SituacoesDoFuncionario.Prepend("Todas as situações"), "Todas as situações", "Situação");
        estado.Width = 144;
        Estilo.Colocar(ferramentas, estado, 441, 26);
        var tabela = CriarTabela(("Funcionário", 125), ("Cargo", 113), ("Departamento", 87), ("Situação", 65), ("Admissão", 67));
        tabela.AccessibleName = "Cadastro de funcionários";
        PosicionarTabela(painel, tabela, 183, 406);
        var quantidade = Estilo.CriarRotulo("", 9, false, Estilo.CorApagada);
        Estilo.Colocar(painel, quantidade, 5, 598);
        void Editar() { if (dados.PodeEditar && tabela.CurrentRow?.Tag is Funcionario item) EditarFuncionario(item); }
        var editar = CriarBotaoDeEdicao("Editar cadastro", "", 132, false, Editar);
        var remover = CriarBotaoDeEdicao("Excluir", "", 73, false, () => { if (tabela.CurrentRow?.Tag is Funcionario item) RemoverFuncionario(item); });
        Estilo.Colocar(painel, editar, 4, 625);
        Estilo.Colocar(painel, remover, 146, 625);
        tabela.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Editar(); };
        // Jefferson: Filter limpa e remonta a tabela a cada letra digitada ou filtro escolhido.
        // a busca não liga pra maiúscula nem acento
        // a opção 0 das listas é "Todos" (sem filtro)
        void Filtrar()
        {
            tabela.Rows.Clear();
            foreach (var pessoa in dados.Funcionarios.Where(item => Combina($"{item.Nome} {item.Cargo}", pesquisa.Text)
                && (departamento.SelectedIndex == 0 || item.Departamento == ItemEscolhido(departamento)) && (estado.SelectedIndex == 0 || item.Situacao == ItemEscolhido(estado))))
                AdicionarLinha(tabela, pessoa, pessoa.Nome, pessoa.Cargo, pessoa.Departamento, pessoa.Situacao, pessoa.Admissao.ToString("dd/MM/yyyy"));
            quantidade.Text = tabela.RowCount == 0 ? "Nenhum funcionário corresponde aos filtros." : $"{tabela.RowCount} {(tabela.RowCount == 1 ? "funcionário" : "funcionários")}";
            editar.Enabled = remover.Enabled = dados.PodeEditar && tabela.RowCount > 0;
        }
        pesquisa.TextChanged += (_, _) => Filtrar();
        departamento.SelectedIndexChanged += (_, _) => Filtrar();
        estado.SelectedIndexChanged += (_, _) => Filtrar();
        Filtrar();
        // Parte do Jefferson: coluna Matrícula e botões Consultar ficha e Inativar (TelaPrincipal.Funcionarios.cs).
        FerramentasDoCadastro(painel, tabela);
        return painel;
    }

    // formulário de funcionário: sem funcionário = novo, com funcionário = edição
    private void EditarFuncionario(Funcionario? pessoa)
    {
        var nome = CriarCampoDeTexto(pessoa?.Nome ?? "");
        var cargo = CriarCampoDeTexto(pessoa?.Cargo ?? "");
        // Parte do Jefferson: o campo Cargo sugere os cargos que já existem no cadastro.
        SugerirCargos(cargo);
        var departamento = CriarLista(Opcoes.Departamentos, pessoa?.Departamento ?? "Pessoas");
        var estado = CriarLista(Opcoes.SituacoesDoFuncionario, pessoa?.Situacao ?? "Ativo");
        var inicio = CriarCampoDeData(pessoa?.Admissao ?? DadosDeExemplo.Hoje);
        using var janela = new JanelaDeCadastro(pessoa is null ? "Novo funcionário" : "Editar funcionário",
            [("Nome", nome), ("Cargo", cargo), ("Departamento", departamento), ("Situação", estado), ("Admissão", inicio)],
            () => dados.SalvarFuncionario(new(pessoa?.Id ?? 0, nome.Text, cargo.Text, ItemEscolhido(departamento), ItemEscolhido(estado), inicio.Valor)));
        if (janela.ShowDialog(this) != DialogResult.OK) return;
        Navegar("people");
        MostrarMensagem("Cadastro salvo");
    }

    // excluir funcionário (pergunta antes)
    private void RemoverFuncionario(Funcionario pessoa)
    {
        if (MessageBox.Show(this, $"Excluir o cadastro de {pessoa.Nome}?", "Excluir funcionário", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
        try { dados.ExcluirFuncionario(pessoa.Id); }
        catch (ErroDeRegra problema) { MessageBox.Show(this, problema.Message, "Não foi possível excluir", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        Navegar("people");
        MostrarMensagem("Cadastro excluído");
    }

    // Clóvis: tela de Ponto e frequência. Em cima ficam o dia consultado e o resumo do dia
    // (entradas, atrasos e faltas). No meio, a tabela dos registros desse dia. Embaixo, os botões.
    // Cada linha da tabela guarda o registro de ponto no Tag, e é por ele que Editar registro sabe qual abrir.
    // O que eu acrescentei (Lançar falta, Lançar atraso e Consultar período) está em TelaPrincipal.Ponto.cs.
    private Panel TelaDePonto()
    {
        // mostra os pontos do dia escolhido
        var painel = CriarModulo("Ponto e frequência", "Acompanhe horários, faltas e atrasos por dia.", CriarBotaoDeEdicao("Registrar ponto", "plus", 148, true, () => EditarPonto()));
        var ferramentas = CriarCartao(14);
        ColocarLarguraToda(painel, ferramentas, 104, 66);
        var data = CriarCampoDeData(diaDoPonto);
        data.AccessibleName = "Dia do registro";
        data.Width = 145;
        Estilo.Colocar(ferramentas, data, 14, 20);
        var resumo = Estilo.CriarRotulo("", 10, true);
        Estilo.Colocar(ferramentas, resumo, 180, 21);
        var tabela = CriarTabela(("Funcionário", 135), ("Data", 76), ("Entrada", 54), ("Saída", 54), ("Situação", 76), ("Motivo da ocorrência", 170));
        tabela.AccessibleName = "Registros de ponto";
        PosicionarTabela(painel, tabela, 184, 366);

        void EditarSelecionado()
        {
            if (dados.PodeEditar && tabela.CurrentRow?.Tag is RegistroDePonto selecionado) EditarPonto(selecionado);
        }

        var editar = CriarBotaoDeEdicao("Editar registro", "", 130, false, EditarSelecionado);
        Estilo.Colocar(painel, editar, 3, 566);
        tabela.SelectionChanged += (_, _) => editar.Enabled = dados.PodeEditar && tabela.CurrentRow?.Tag is RegistroDePonto;
        tabela.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditarSelecionado(); };

        void Filtrar()
        {
            tabela.Rows.Clear();
            diaDoPonto = data.Valor.Date;
            var lista = dados.PontosEntre(diaDoPonto, diaDoPonto);
            foreach (var item in lista)
            {
                var motivo = item.Motivo.Length > 0 ? item.Motivo.ReplaceLineEndings(" ") : RegistroDePonto.AceitaMotivo(item.Situacao) ? "Não informado" : "—";
                AdicionarLinha(tabela, item, dados.NomeDoFuncionario(item.Matricula), item.Dia.ToString("dd/MM/yyyy"), item.Entrada.Length > 0 ? item.Entrada : "—", item.Saida.Length > 0 ? item.Saida : "—", item.Situacao, motivo);
                tabela.Rows[tabela.RowCount - 1].Cells[5].ToolTipText = item.Motivo;
            }
            editar.Enabled = dados.PodeEditar && tabela.CurrentRow?.Tag is RegistroDePonto;
            var lancamentos = lista.Count(item => item.Entrada.Length > 0);
            var atrasado = lista.Count(item => item.Situacao == "Atraso");
            var ausente = lista.Count(item => item.Situacao == "Falta");
            resumo.Text = lista.Count == 0 ? "Nenhum registro neste dia" : $"{lancamentos} {(lancamentos == 1 ? "entrada" : "entradas")}  ·  {atrasado} {(atrasado == 1 ? "atraso" : "atrasos")}  ·  {ausente} {(ausente == 1 ? "falta" : "faltas")}";
        }
        data.ValorMudou += (_, _) => Filtrar();
        Filtrar();
        // Parte do Clóvis: botões Lançar falta, Lançar atraso e Consultar período (TelaPrincipal.Ponto.cs).
        FerramentasDoPonto(painel);
        return painel;
    }
    // formulário do ponto: novo registro ou edição
    private void EditarPonto(RegistroDePonto? original = null)
    {
        // na edição dá pra escolher quem está afastado ou inativo hoje
        var listaDeFuncionarios = dados.Funcionarios.Where(item => original is null ? item.Situacao == "Ativo" : item.Id == original.Matricula).ToArray();
        if (listaDeFuncionarios.Length == 0) { MostrarMensagem("Cadastre um funcionário ativo primeiro"); return; }
        var pessoa = CriarLista(listaDeFuncionarios.Select(item => $"{item.Nome} · {item.Id}"), "");
        var dia = CriarCampoDeData(original?.Dia ?? diaDoPonto);
        pessoa.Enabled = dia.Enabled = original is null;
        var entradaAnterior = string.IsNullOrEmpty(original?.Entrada) ? "08:00" : original.Entrada;
        var saidaAnterior = string.IsNullOrEmpty(original?.Saida) ? "17:00" : original.Saida;
        var campoEntrada = CriarCampoDeTexto(entradaAnterior);
        var campoSaida = CriarCampoDeTexto(saidaAnterior);
        var estado = CriarLista(Opcoes.SituacoesDoPonto, original?.Situacao ?? "Presença");
        var motivo = CriarCampoDeTexto(original?.Motivo ?? "", "Opcional para falta ou atraso");
        motivo.Multiline = true;
        motivo.Height = 60;
        motivo.MaxLength = 240;
        motivo.ScrollBars = ScrollBars.Vertical;
        motivo.AccessibleDescription = "Campo opcional para faltas e atrasos, com até 240 caracteres.";

        void AtualizarCampos()
        {
            var situacao = ItemEscolhido(estado);
            var mostrarHoras = situacao != "Falta";
            if (mostrarHoras != campoEntrada.Enabled)
            {
                // guarda o que foi digitado se a pessoa trocar a situação e voltar
                if (!mostrarHoras) (entradaAnterior, saidaAnterior) = (campoEntrada.Text, campoSaida.Text);
                campoEntrada.Text = mostrarHoras ? entradaAnterior : "";
                campoSaida.Text = mostrarHoras ? saidaAnterior : "";
            }
            campoEntrada.Enabled = campoSaida.Enabled = mostrarHoras;
            motivo.Enabled = RegistroDePonto.AceitaMotivo(situacao);
        }

        estado.SelectedIndexChanged += (_, _) => AtualizarCampos();
        AtualizarCampos();
        using var janela = new JanelaDeCadastro(original is null ? "Registrar ponto" : "Editar registro de ponto",
            [("Funcionário", pessoa), ("Data", dia), ("Entrada (HH:mm)", campoEntrada), ("Saída (HH:mm)", campoSaida), ("Situação", estado), ("Motivo da ocorrência (opcional)", motivo)],
            () => dados.SalvarPonto(new(listaDeFuncionarios[pessoa.SelectedIndex].Id, dia.Valor, campoEntrada.Text, campoSaida.Text, ItemEscolhido(estado), motivo.Text), original is not null));
        if (janela.ShowDialog(this) != DialogResult.OK) return;
        diaDoPonto = dia.Valor.Date;
        Navegar("time");
        MostrarMensagem("Ponto registrado");
    }

    // Kerlon: tela de Recrutamento. Em cima, a tabela de vagas; embaixo, a das candidaturas.
    // Cada linha guarda a vaga ou a candidatura no Tag, e é por ele que os botões sabem qual abrir.
    // O que eu acrescentei (funil, Candidatos da vaga, Avançar etapa e Mudar de vaga) está em TelaPrincipal.Recrutamento.cs.
    private Panel TelaDeRecrutamento()
    {
        var painel = CriarModulo("Recrutamento", "Organize vagas e acompanhe cada candidatura.", CriarBotaoDeEdicao("Novo candidato", "plus", 147, true, () => EditarCandidato(null)));
        var vagas = CriarTabela(("Vaga", 160), ("Departamento", 90), ("Situação", 70), ("Candidaturas", 70));
        vagas.AccessibleName = "Vagas de recrutamento";
        foreach (var item in dados.Vagas) AdicionarLinha(vagas, item, item.Titulo, item.Departamento, item.Situacao, dados.Candidatos.Count(c => c.VagaId == item.Id));
        PosicionarTabela(painel, vagas, 105, 189);
        Estilo.Colocar(painel, CriarBotaoDeEdicao("Nova vaga", "plus", 110, false, NovaVaga), 2, 305);
        Estilo.Colocar(painel, CriarBotaoDeEdicao("Encerrar ou reabrir vaga", "", 169, false, () =>
        {
            if (vagas.CurrentRow?.Tag is not Vaga item) return;
            try { dados.AbrirOuFecharVaga(item.Id); }
            catch (ErroDeRegra problema) { MostrarMensagem(problema.Message); return; }
            Navegar("hire");
            MostrarMensagem("Situação da vaga atualizada");
        }), 122, 305);
        Estilo.Colocar(painel, Estilo.CriarRotulo("Candidaturas", 13, true), 3, 355);
        var candidatos = CriarTabela(("Candidato", 122), ("Vaga", 140), ("Etapa", 85));
        candidatos.AccessibleName = "Lista de candidaturas";
        foreach (var item in dados.Candidatos) AdicionarLinha(candidatos, item, item.Nome, dados.TituloDaVaga(item.VagaId), item.Etapa);
        PosicionarTabela(painel, candidatos, 388, 231);
        void Editar() { if (dados.PodeEditar && candidatos.CurrentRow?.Tag is Candidato item) EditarCandidato(item); }
        Estilo.Colocar(painel, CriarBotaoDeEdicao("Alterar etapa", "", 115, false, Editar), 2, 629);
        candidatos.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Editar(); };
        // Parte do Kerlon: funil de etapas, Candidatos da vaga, Avançar etapa e Mudar de vaga (TelaPrincipal.Recrutamento.cs).
        FerramentasDoRecrutamento(painel, vagas, candidatos);
        return painel;
    }
    // formulário de vaga nova
    private void NovaVaga()
    {
        var titulo = CriarCampoDeTexto();
        var departamento = CriarLista(Opcoes.Departamentos, "Pessoas");
        using var janela = new JanelaDeCadastro("Nova vaga", [("Título da vaga", titulo), ("Departamento", departamento)],
            () => dados.NovaVaga(titulo.Text, ItemEscolhido(departamento)));
        if (janela.ShowDialog(this) != DialogResult.OK) return;
        Navegar("hire");
        MostrarMensagem("Vaga cadastrada");
    }
    // formulário de candidato: novo ou edição
    private void EditarCandidato(Candidato? candidato)
    {
        var vagas = dados.Vagas.Where(item => item.Situacao == "Aberta" || item.Id == candidato?.VagaId).ToArray();
        if (vagas.Length == 0) { MostrarMensagem("Abra uma vaga para cadastrar candidatos"); return; }
        var titulos = vagas.Select(item => $"{item.Titulo} · {item.Id}").ToArray();
        var selecionado = Array.FindIndex(vagas, item => item.Id == candidato?.VagaId);
        var nome = CriarCampoDeTexto(candidato?.Nome ?? "");
        var vaga = CriarLista(titulos, titulos[Math.Max(0, selecionado)]);
        var etapa = CriarLista(Opcoes.Etapas, candidato?.Etapa ?? "Triagem");
        using var janela = new JanelaDeCadastro(candidato is null ? "Novo candidato" : "Editar candidatura", [("Nome", nome), ("Vaga", vaga), ("Etapa", etapa)],
            // Kerlon: SalvarCandidatura (Recrutamento.cs) confere se o mesmo nome já está na vaga e depois chama o SalvarCandidato.
            () => dados.SalvarCandidatura(new(candidato?.Id ?? 0, nome.Text, vagas[vaga.SelectedIndex].Id, ItemEscolhido(etapa))));
        if (janela.ShowDialog(this) != DialogResult.OK) return;
        Navegar(paginaAtual);
        MostrarMensagem("Candidatura salva");
    }
    // Tela da agenda: calendário do mês e os compromissos do dia escolhido
    private Panel TelaDaAgenda()
    {
        var painel = CriarModulo("Calendário", "Entrevistas, admissões e compromissos da equipe.", CriarBotaoDeEdicao("Novo compromisso", "plus", 180, true, AdicionarCompromisso));
        var cartao = CriarCartao();
        ColocarLarguraToda(painel, cartao, 105, 505);
        var rotulo = Estilo.CriarRotulo("", 13, true);
        const int detalheX = 310;
        Estilo.Colocar(cartao, rotulo, detalheX, 17);
        var lista = CriarTabela(("Horário", 56), ("Compromisso", 165), ("Categoria", 82));
        lista.AccessibleName = "Compromissos do dia";
        lista.Bounds = new(detalheX, 54, cartao.Width - detalheX - 14, 434);
        lista.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        cartao.Controls.Add(lista);
        const int colunas = 7;
        const int larguraDaCelula = 35;
        const int passoDaColuna = 39;
        const int larguraDoCalendario = (colunas - 1) * passoDaColuna + larguraDaCelula;
        const int tamanhoDaSeta = 28;
        const int espacoDaSeta = 4;
        var calendario = new Panel { Size = new(larguraDoCalendario, 293), BackColor = Color.Transparent };
        Estilo.Colocar(cartao, calendario, 17, 17);
        void Recarregar()
        {
            // marca no calendário os dias que têm compromisso
            var dia = diaDaAgenda;
            var mes = new DateTime(dia.Year, dia.Month, 1);
            var primeiro = mes.AddDays(-((int)mes.DayOfWeek + 6) % 7);
            var visivel = dados.Agenda;
            foreach (var controle in calendario.Controls.Cast<Control>().ToArray()) controle.Dispose();
            Estilo.Colocar(calendario, Estilo.CriarRotulo(mes.ToString("MMMM yyyy"), 11, true), 0, 6);
            void Mover(int deslocamento)
            {
                diaDaAgenda = mes.AddMonths(deslocamento);
                Recarregar();
                calendario.Controls.OfType<Botao>().First(botao => botao.Icone == (deslocamento < 0 ? "prev" : "next")).Focus();
            }
            var voltar = CriarBotao("", "prev", tamanhoDaSeta, false, () => Mover(-1));
            var proximo = CriarBotao("", "next", tamanhoDaSeta, false, () => Mover(1));
            voltar.Height = proximo.Height = tamanhoDaSeta;
            voltar.AccessibleName = "Mês anterior";
            proximo.AccessibleName = "Próximo mês";
            Estilo.Colocar(calendario, voltar, larguraDoCalendario - 2 * tamanhoDaSeta - espacoDaSeta, 0);
            Estilo.Colocar(calendario, proximo, larguraDoCalendario - tamanhoDaSeta, 0);
            var diasDaSemana = new[] { "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb", "Dom" };
            for (var i = 0; i < colunas; i++)
            {
                var rotuloDoDia = Estilo.CriarRotulo(diasDaSemana[i], 8, true, Estilo.CorApagada);
                rotuloDoDia.AutoSize = false;
                rotuloDoDia.TextAlign = ContentAlignment.MiddleCenter;
                rotuloDoDia.Size = new(larguraDaCelula, 22);
                Estilo.Colocar(calendario, rotuloDoDia, i * passoDaColuna, 36);
            }
            for (var i = 0; i < 6 * colunas; i++)
            {
                var data = primeiro.AddDays(i);
                var temEvento = visivel.Any(item => item.DataHora.Date == data);
                var chave = new Botao(data.Day.ToString()) { Principal = data == dia, DiaDoCalendario = true, Marcado = temEvento, Size = new(larguraDaCelula, 30), Tag = data };
                chave.Click += (_, _) =>
                {
                    diaDaAgenda = data;
                    Recarregar();
                    calendario.Controls.OfType<Botao>().First(botao => Equals(botao.Tag, data)).Focus();
                };
                chave.Font = Estilo.Fonte(9, temEvento || data == dia);
                chave.AccessibleName = $"{data:dd/MM/yyyy}{(temEvento ? ", com compromissos" : "")}";
                chave.Enabled = data.Month == mes.Month;
                Estilo.Colocar(calendario, chave, i % colunas * passoDaColuna, 59 + i / colunas * 35);
            }
            lista.Rows.Clear();
            foreach (var item in visivel.Where(item => item.DataHora.Date == dia).OrderBy(item => item.DataHora))
                AdicionarLinha(lista, item, item.DataHora.ToString("HH:mm"), item.Titulo, item.Categoria);
            rotulo.Text = $"{dia:dd/MM/yyyy} · {(lista.RowCount == 0 ? "Nenhum compromisso" : $"{lista.RowCount} {(lista.RowCount == 1 ? "compromisso" : "compromissos")}")}";
        }
        Recarregar();
        return painel;
    }
    // formulário de compromisso novo
    private void AdicionarCompromisso()
    {
        var titulo = CriarCampoDeTexto();
        var dia = CriarCampoDeData(diaDaAgenda);
        var horario = CriarCampoDeTexto("09:00");
        var categoria = CriarLista(Opcoes.CategoriasDaAgenda, "Funcionários");
        using var janela = new JanelaDeCadastro("Novo compromisso", [("Título", titulo), ("Data", dia), ("Horário (HH:mm)", horario), ("Categoria", categoria)],
            () => dados.AdicionarCompromisso(titulo.Text, dia.Valor, horario.Text, ItemEscolhido(categoria)));
        if (janela.ShowDialog(this) != DialogResult.OK) return;
        diaDaAgenda = dia.Valor.Date;
        Navegar("calendar");
        MostrarMensagem("Compromisso salvo");
    }
    // Tela de salários: lançamentos da folha do mês escolhido
    private Panel TelaDeSalarios()
    {
        var painel = CriarModulo("Salários", "Consulte a folha por competência e lance valores simulados.", CriarBotao("Novo lançamento", "plus", 154, true, () => LancarSalario()));
        var cartao = CriarCartao(14);
        ColocarLarguraToda(painel, cartao, 104, 66);
        var mes = CriarCampoDeData(mesDaFolha, true);
        mes.AccessibleName = "Competência da folha";
        mes.Width = 124;
        Estilo.Colocar(cartao, mes, 14, 20);
        var total = Estilo.CriarRotulo("", 10, true);
        Estilo.Colocar(cartao, total, 158, 21);
        var tabela = CriarTabela(("Funcionário", 132), ("Salário", 82), ("Bônus", 70), ("Descontos", 76), ("Total simulado", 92));
        tabela.AccessibleName = "Lançamentos salariais";
        PosicionarTabela(painel, tabela, 184, 406);
        // Diaz: editar um lançamento existente (antes só dava para digitar tudo de novo em "Novo lançamento").
        void Editar() { if (dados.PodeEditar && tabela.CurrentRow?.Tag is LancamentoDaFolha item) LancarSalario(item); }
        var editar = CriarBotaoDeEdicao("Editar lançamento", "", 160, false, Editar);
        Estilo.Colocar(painel, editar, 4, 625);
        tabela.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Editar(); };
        void Filtrar()
        {
            mesDaFolha = mes.Valor;
            tabela.Rows.Clear();
            var lancamentos = dados.FolhaDoMes(mes.Valor);
            foreach (var item in lancamentos) AdicionarLinha(tabela, item, dados.NomeDoFuncionario(item.Matricula), item.Salario.ToString("C"), item.Bonus.ToString("C"), item.Desconto.ToString("C"), item.Total.ToString("C"));
            total.Text = lancamentos.Count == 0 ? "Nenhum lançamento nesta competência" : $"Total simulado: {lancamentos.Sum(item => item.Total):C}";
            editar.Enabled = dados.PodeEditar && tabela.RowCount > 0;
        }
        mes.ValorMudou += (_, _) => Filtrar();
        Filtrar();
        return painel;
    }

    // Diaz: lancamento nulo = lançamento novo. Na edição, funcionário e competência ficam travados.
    private void LancarSalario(LancamentoDaFolha? lancamento = null)
    {
        if (!PodeVerSalarios) return;
        var listaDeFuncionarios = dados.Funcionarios.ToArray();
        if (listaDeFuncionarios.Length == 0) { MostrarMensagem("Cadastre um funcionário primeiro"); return; }
        var rotulos = listaDeFuncionarios.Select(item => $"{item.Nome} · {item.Id}").ToArray();
        var posicao = lancamento is null ? -1 : Array.FindIndex(listaDeFuncionarios, item => item.Id == lancamento.Matricula);
        var pessoa = CriarLista(rotulos, posicao >= 0 ? rotulos[posicao] : "");
        var mes = CriarCampoDeData(lancamento?.Competencia ?? mesDaFolha, true);
        if (lancamento is not null) pessoa.Enabled = mes.Enabled = false;
        static NumericUpDown CampoDeValor(decimal inicial) => new() { DecimalPlaces = 2, Maximum = 1_000_000, ThousandsSeparator = true, Font = Estilo.Fonte(10), Value = inicial };
        var valor = CampoDeValor(lancamento?.Salario ?? 0);
        var bonus = CampoDeValor(lancamento?.Bonus ?? 0);
        var desconto = CampoDeValor(lancamento?.Desconto ?? 0);
        var titulo = lancamento is null ? "Novo lançamento" : "Editar lançamento";
        var observacao = lancamento is null
            ? "Um lançamento por funcionário e competência. Confirmar atualiza o valor existente."
            : "Funcionário e competência não mudam na edição. Altere os valores e confirme.";
        using var janela = new JanelaDeCadastro(titulo, [("Funcionário", pessoa), ("Competência", mes), ("Salário (R$)", valor), ("Bônus (R$)", bonus), ("Descontos (R$)", desconto)],
            () => dados.SalvarFolha(new(listaDeFuncionarios[pessoa.SelectedIndex].Id, mes.Valor, valor.Value, bonus.Value, desconto.Value)),
            observacao);
        if (janela.ShowDialog(this) != DialogResult.OK) return;
        mesDaFolha = mes.Valor;
        Navegar("pay");
        MostrarMensagem("Lançamento salvo");
    }

}
