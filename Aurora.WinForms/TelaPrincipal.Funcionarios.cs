using Aurora.Core;

namespace Aurora.WinForms;

// TelaPrincipal.Funcionarios - Jefferson (cadastro de funcionários)
//
// O que eu acrescentei na tela de Funcionários. O TelaPrincipal é uma "partial class":
// a mesma classe pode ser dividida em vários arquivos. Assim a minha parte fica
// separada e o código que o grupo já tinha continua igual.
//
// Ligação com a tela que já existia (TelaPrincipal.Modulos.cs):
//   TelaDeFuncionarios()   chama FerramentasDoCadastro no final -> coluna Matrícula, Consultar ficha e Inativar
//   EditarFuncionario() chama SugerirCargos                  -> o campo Cargo sugere os cargos já cadastrados
//
// As regras não ficam aqui. A tela pede ao serviço (CadastroDeFuncionarios / ServicoRh)
// e só mostra o resultado ou a mensagem de erro.
internal sealed partial class TelaPrincipal
{
    private void FerramentasDoCadastro(Panel painel, DataGridView tabela)
    {
        // Coluna Matrícula: mostra o número que o sistema gerou no cadastro (ex.: 109).
        // O filtro que já existia preenche as outras colunas. Esta é preenchida na hora de
        // desenhar (CellFormatting), lendo o funcionário guardado no Tag de cada linha.
        // DisplayIndex = 0 só muda a ordem na tela; a coluna "Funcionário" continua sendo a Cells[0].
        var matricula = new DataGridViewTextBoxColumn { HeaderText = "Matrícula", FillWeight = 52, SortMode = DataGridViewColumnSortMode.NotSortable };
        tabela.Columns.Add(matricula);
        matricula.DisplayIndex = 0;
        tabela.CellFormatting += (_, e) =>
        {
            if (e.ColumnIndex != matricula.Index || e.RowIndex < 0) return;
            if (tabela.Rows[e.RowIndex].Tag is not Funcionario pessoa) return;
            e.Value = pessoa.Id.ToString();
            e.FormattingApplied = true;
        };

        // Consultar ficha: liberado para todos os perfis, porque é só consulta.
        // Inativar: usa CriarBotaoDeEdicao, então fica desligado para o Gestor (o serviço também bloqueia).
        var ficha = CriarBotao("Consultar ficha", "", 132, false, () =>
        {
            if (tabela.CurrentRow?.Tag is Funcionario pessoa) AbrirFicha(pessoa);
            else MostrarMensagem("Selecione um funcionário na tabela");
        });
        var inativar = CriarBotaoDeEdicao("Inativar", "", 90, false, () =>
        {
            if (tabela.CurrentRow?.Tag is Funcionario pessoa) InativarFuncionario(pessoa);
            else MostrarMensagem("Selecione um funcionário na tabela");
        });
        Estilo.Colocar(painel, ficha, 229, 625);
        Estilo.Colocar(painel, inativar, 371, 625);

        // Os botões acompanham a tabela: sem nenhuma linha (filtro vazio), ficam desligados.
        void Atualizar()
        {
            ficha.Enabled = tabela.RowCount > 0;
            inativar.Enabled = dados.PodeEditar && tabela.RowCount > 0;
        }
        tabela.RowsAdded += (_, _) => Atualizar();
        tabela.RowsRemoved += (_, _) => Atualizar();
        Atualizar();

        // Para quem pode editar, o duplo clique continua abrindo a edição (código do grupo).
        // Para o Gestor, que não edita, o duplo clique passa a abrir a ficha.
        tabela.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && !dados.PodeEditar && tabela.Rows[e.RowIndex].Tag is Funcionario pessoa) AbrirFicha(pessoa);
        };
    }

    private void AbrirFicha(Funcionario pessoa)
    {
        // DadosDeExemplo.Today (13/11/2026) é o "hoje" de toda a demonstração. O ponto da ficha é desse mês.
        FichaDoFuncionario ficha;
        try { ficha = dados.ConsultarFicha(pessoa.Id, DadosDeExemplo.Hoje); }
        catch (ErroDeRegra problema) { MostrarMensagem(problema.Message); return; }
        using var janela = new JanelaDaFicha(ficha);
        janela.ShowDialog(this);
    }

    private void InativarFuncionario(Funcionario pessoa)
    {
        if (pessoa.Situacao == "Inativo")
        {
            MessageBox.Show(this, $"{pessoa.Nome} já está com a situação Inativo. Para reativar, use Editar cadastro.", "Inativar funcionário", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        // Mesmo cuidado da exclusão: pergunta antes e deixa "Não" como botão padrão.
        if (MessageBox.Show(this, $"Mudar a situação de {pessoa.Nome} para Inativo? O histórico de ponto e salário continua guardado.", "Inativar funcionário", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
        try { dados.MudarSituacao(pessoa.Id, "Inativo"); }
        catch (ErroDeRegra problema) { MessageBox.Show(this, problema.Message, "Não foi possível inativar", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        Navegar("people");
        MostrarMensagem("Cadastro inativado");
    }

    // Integra o campo Cargo ao serviço: as sugestões vêm de CargosCadastrados().
    // Enquanto a pessoa digita, o Windows mostra os cargos que começam com o mesmo texto.
    // O campo continua livre, então dá para digitar um cargo novo.
    private void SugerirCargos(TextBox cargo)
    {
        var sugestoes = new AutoCompleteStringCollection();
        sugestoes.AddRange(dados.CargosCadastrados().ToArray());
        cargo.AutoCompleteCustomSource = sugestoes;
        cargo.AutoCompleteSource = AutoCompleteSource.CustomSource;
        cargo.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
    }
}
