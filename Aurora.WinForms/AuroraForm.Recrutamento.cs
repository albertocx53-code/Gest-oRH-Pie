using Aurora.Core;

namespace Aurora.WinForms;

// AuroraForm.Recrutamento - Kerlon (recrutamento)
//
// O que eu acrescentei na tela de Recrutamento. O AuroraForm é uma "partial class":
// a mesma classe pode ser dividida em vários arquivos. Assim a minha parte fica
// separada e o código que o grupo já tinha continua igual.
//
// Ligação com a tela que já existia (AuroraForm.Modules.cs):
//   HirePage()      chama FerramentasDoRecrutamento no final
//                   -> funil de etapas, botão Candidatos da vaga, Avançar etapa e Mudar de vaga
//   EditCandidate() chama SalvarCandidatura no lugar de SaveCandidate
//                   -> o mesmo candidato não entra duas vezes na mesma vaga
//
// As regras não ficam aqui. A tela pede ao serviço (Recrutamento / RhService)
// e só mostra o resultado ou a mensagem de erro.
internal sealed partial class AuroraForm
{
    private void FerramentasDoRecrutamento(Panel panel, DataGridView vagas, DataGridView candidatos)
    {
        // Funil: quantos candidatos há em cada etapa. Mostra todas as vagas ou, quando uma vaga
        // está selecionada na tabela de cima, só os candidatos dela. Fica ao lado do título "Candidaturas".
        var funil = AuroraStyle.Label("", 9, false, AuroraStyle.Muted);
        funil.AutoSize = false;
        funil.AutoEllipsis = true;
        funil.Size = new(Math.Max(300, panel.Width - 150), 22);
        funil.AccessibleName = "Funil do processo seletivo";
        AuroraStyle.Put(panel, funil, 140, 360);

        void AtualizarFunil()
        {
            var vaga = vagas.CurrentRow?.Tag as Vacancy;
            try
            {
                var etapas = data.FunilDeEtapas(vaga?.Id);
                funil.Text = $"{vaga?.Title ?? "Todas as vagas"}:  " + string.Join("  ·  ", etapas.Select(item => $"{item.Etapa} {item.Candidatos}"));
            }
            catch (RhRuleException issue) { funil.Text = issue.Message; }
        }

        // Candidatos da vaga: liga e desliga um filtro na tabela de candidaturas (a tabela do grupo
        // continua sendo montada pelo HirePage; aqui só trocamos as linhas). Liberado para todos os perfis.
        Vacancy? vagaFiltrada = null;
        AuroraButton? filtro = null;

        void Mostrar(IEnumerable<Candidate> lista)
        {
            candidatos.Rows.Clear();
            foreach (var item in lista) Row(candidatos, item, item.Name, data.VacancyTitle(item.Vacancy), item.Stage);
        }

        void AplicarFiltro()
        {
            try
            {
                if (vagaFiltrada is null)
                {
                    Mostrar(data.Candidates);
                    filtro!.Text = "Candidatos da vaga";
                    return;
                }
                Mostrar(data.CandidatosDaVaga(vagaFiltrada.Id));
                filtro!.Text = "Ver todos";
                Status($"Candidatos de {vagaFiltrada.Title}");
            }
            catch (RhRuleException issue) { Status(issue.Message); }
        }

        filtro = Action("Candidatos da vaga", "", 160, false, () =>
        {
            if (vagaFiltrada is not null) vagaFiltrada = null;
            else if (vagas.CurrentRow?.Tag is Vacancy vaga) vagaFiltrada = vaga;
            else { Status("Selecione uma vaga na tabela"); return; }
            AplicarFiltro();
        });
        AuroraStyle.Put(panel, filtro, 299, 305);

        // Com o filtro ligado, trocar de vaga na tabela de cima troca também os candidatos mostrados.
        vagas.SelectionChanged += (_, _) =>
        {
            AtualizarFunil();
            if (vagaFiltrada is not null && vagas.CurrentRow?.Tag is Vacancy vaga && vaga.Id != vagaFiltrada.Id)
            {
                vagaFiltrada = vaga;
                AplicarFiltro();
            }
        };
        AtualizarFunil();

        // Avançar etapa: leva a candidatura selecionada para a etapa seguinte. Usa EditAction,
        // então fica desligado para o Gestor (o serviço também bloqueia).
        var avancar = EditAction("Avançar etapa", "", 140, false, () =>
        {
            if (candidatos.CurrentRow?.Tag is not Candidate candidato) { Status("Selecione uma candidatura na tabela"); return; }
            try { data.AvancarEtapa(candidato.Id); }
            catch (RhRuleException issue) { MessageBox.Show(this, issue.Message, "Não foi possível avançar", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            Navigate("hire");
            Status($"{candidato.Name} avançou de etapa");
        });
        var mudar = EditAction("Mudar de vaga", "", 140, false, () =>
        {
            if (candidatos.CurrentRow?.Tag is Candidate candidato) MudarDeVaga(candidato);
            else Status("Selecione uma candidatura na tabela");
        });
        AuroraStyle.Put(panel, avancar, 125, 629);
        AuroraStyle.Put(panel, mudar, 273, 629);

        // Sem nenhuma candidatura na tabela, os botões ficam desligados.
        void Atualizar() => avancar.Enabled = mudar.Enabled = data.CanEdit && candidatos.RowCount > 0;
        candidatos.RowsAdded += (_, _) => Atualizar();
        candidatos.RowsRemoved += (_, _) => Atualizar();
        Atualizar();
    }

    // Formulário de mudança de vaga. Só aparecem as outras vagas abertas (o serviço confere de novo).
    // A etapa da candidatura continua a mesma. Se alguma regra falhar, a mensagem aparece no
    // próprio formulário e a escolha continua lá para corrigir.
    private void MudarDeVaga(Candidate candidato)
    {
        var abertas = data.ConsultarVagas("Aberta").Where(item => item.Id != candidato.Vacancy).ToArray();
        if (abertas.Length == 0) { Status("Não há outra vaga aberta"); return; }
        var titulos = abertas.Select(item => $"{item.Title} · {item.Id}").ToArray();
        var destino = Choice(titulos, titulos[0]);
        using var form = new EntryForm("Mudar de vaga", [("Nova vaga", destino)],
            () => data.VincularAVaga(candidato.Id, abertas[destino.SelectedIndex].Id),
            $"{candidato.Name} está em {data.VacancyTitle(candidato.Vacancy)}. A etapa ({candidato.Stage}) continua a mesma.");
        if (form.ShowDialog(this) != DialogResult.OK) return;
        Navigate("hire");
        Status("Candidatura mudou de vaga");
    }
}
