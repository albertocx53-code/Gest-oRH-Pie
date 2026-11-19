using Aurora.Core;

namespace Aurora.WinForms;

// AuroraForm.Funcionarios - Jefferson (cadastro de funcionários)
//
// O que eu acrescentei na tela de Funcionários. O AuroraForm é uma "partial class":
// a mesma classe pode ser dividida em vários arquivos. Assim a minha parte fica
// separada e o código que o grupo já tinha continua igual.
//
// Ligação com a tela que já existia (AuroraForm.Modules.cs):
//   PeoplePage()   chama FerramentasDoCadastro no final -> coluna Matrícula, Consultar ficha e Inativar
//   EditEmployee() chama SugerirCargos                  -> o campo Cargo sugere os cargos já cadastrados
//
// As regras não ficam aqui. A tela pede ao serviço (CadastroDeFuncionarios / RhService)
// e só mostra o resultado ou a mensagem de erro.
internal sealed partial class AuroraForm
{
    private void FerramentasDoCadastro(Panel panel, DataGridView grid)
    {
        // Coluna Matrícula: mostra o número que o sistema gerou no cadastro (ex.: 109).
        // O filtro que já existia preenche as outras colunas. Esta é preenchida na hora de
        // desenhar (CellFormatting), lendo o funcionário guardado no Tag de cada linha.
        // DisplayIndex = 0 só muda a ordem na tela; a coluna "Funcionário" continua sendo a Cells[0].
        var matricula = new DataGridViewTextBoxColumn { HeaderText = "Matrícula", FillWeight = 52, SortMode = DataGridViewColumnSortMode.NotSortable };
        grid.Columns.Add(matricula);
        matricula.DisplayIndex = 0;
        grid.CellFormatting += (_, e) =>
        {
            if (e.ColumnIndex != matricula.Index || e.RowIndex < 0) return;
            if (grid.Rows[e.RowIndex].Tag is not Employee person) return;
            e.Value = person.Id.ToString();
            e.FormattingApplied = true;
        };

        // Consultar ficha: liberado para todos os perfis, porque é só consulta.
        // Inativar: usa EditAction, então fica desligado para o Gestor (o serviço também bloqueia).
        var ficha = Action("Consultar ficha", "", 132, false, () =>
        {
            if (grid.CurrentRow?.Tag is Employee person) AbrirFicha(person);
            else Status("Selecione um funcionário na tabela");
        });
        var inativar = EditAction("Inativar", "", 90, false, () =>
        {
            if (grid.CurrentRow?.Tag is Employee person) InativarFuncionario(person);
            else Status("Selecione um funcionário na tabela");
        });
        AuroraStyle.Put(panel, ficha, 229, 625);
        AuroraStyle.Put(panel, inativar, 371, 625);

        // Os botões acompanham a tabela: sem nenhuma linha (filtro vazio), ficam desligados.
        void Atualizar()
        {
            ficha.Enabled = grid.RowCount > 0;
            inativar.Enabled = data.CanEdit && grid.RowCount > 0;
        }
        grid.RowsAdded += (_, _) => Atualizar();
        grid.RowsRemoved += (_, _) => Atualizar();
        Atualizar();

        // Para quem pode editar, o duplo clique continua abrindo a edição (código do grupo).
        // Para o Gestor, que não edita, o duplo clique passa a abrir a ficha.
        grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && !data.CanEdit && grid.Rows[e.RowIndex].Tag is Employee person) AbrirFicha(person);
        };
    }

    private void AbrirFicha(Employee person)
    {
        // DemoRh.Today (13/11/2026) é o "hoje" de toda a demonstração. O ponto da ficha é desse mês.
        FichaDoFuncionario ficha;
        try { ficha = data.ConsultarFicha(person.Id, DemoRh.Today); }
        catch (RhRuleException issue) { Status(issue.Message); return; }
        using var form = new FichaDoFuncionarioForm(ficha);
        form.ShowDialog(this);
    }

    private void InativarFuncionario(Employee person)
    {
        if (person.Status == "Inativo")
        {
            MessageBox.Show(this, $"{person.Name} já está com a situação Inativo. Para reativar, use Editar cadastro.", "Inativar funcionário", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        // Mesmo cuidado da exclusão: pergunta antes e deixa "Não" como botão padrão.
        if (MessageBox.Show(this, $"Mudar a situação de {person.Name} para Inativo? O histórico de ponto e salário continua guardado.", "Inativar funcionário", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
        try { data.MudarSituacao(person.Id, "Inativo"); }
        catch (RhRuleException issue) { MessageBox.Show(this, issue.Message, "Não foi possível inativar", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        Navigate("people");
        Status("Cadastro inativado");
    }

    // Integra o campo Cargo ao serviço: as sugestões vêm de CargosCadastrados().
    // Enquanto a pessoa digita, o Windows mostra os cargos que começam com o mesmo texto.
    // O campo continua livre, então dá para digitar um cargo novo.
    private void SugerirCargos(TextBox role)
    {
        var sugestoes = new AutoCompleteStringCollection();
        sugestoes.AddRange(data.CargosCadastrados().ToArray());
        role.AutoCompleteCustomSource = sugestoes;
        role.AutoCompleteSource = AutoCompleteSource.CustomSource;
        role.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
    }
}
