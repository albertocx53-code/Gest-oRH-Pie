using Aurora.Core;

namespace Aurora.WinForms;

// TelaPrincipal.Recrutamento - Kerlon (recrutamento)
//
// O que eu acrescentei na tela de Recrutamento. O TelaPrincipal é uma "partial class":
// a mesma classe pode ser dividida em vários arquivos. Assim a minha parte fica
// separada e o código que o grupo já tinha continua igual.
//
// Ligação com a tela que já existia (TelaPrincipal.Modulos.cs):
//   TelaDeRecrutamento()      chama FerramentasDoRecrutamento no final
//                   -> funil de etapas, botão Candidatos da vaga, Avançar etapa e Mudar de vaga
//   EditarCandidato() chama SalvarCandidatura no lugar de SalvarCandidato
//                   -> o mesmo candidato não entra duas vezes na mesma vaga
//
// As regras não ficam aqui. A tela pede ao serviço (Recrutamento / ServicoRh)
// e só mostra o resultado ou a mensagem de erro.
internal sealed partial class TelaPrincipal
{
    private void FerramentasDoRecrutamento(Panel painel, DataGridView vagas, DataGridView candidatos)
    {
        // Funil: quantos candidatos há em cada etapa. Mostra todas as vagas ou, quando uma vaga
        // está selecionada na tabela de cima, só os candidatos dela. Fica ao lado do título "Candidaturas".
        var funil = Estilo.CriarRotulo("", 9, false, Estilo.CorApagada);
        funil.AutoSize = false;
        funil.AutoEllipsis = true;
        funil.Size = new(Math.Max(300, painel.Width - 150), 22);
        funil.AccessibleName = "Funil do processo seletivo";
        Estilo.Colocar(painel, funil, 140, 360);

        void AtualizarFunil()
        {
            var vaga = vagas.CurrentRow?.Tag as Vaga;
            try
            {
                var etapas = dados.FunilDeEtapas(vaga?.Id);
                funil.Text = $"{vaga?.Titulo ?? "Todas as vagas"}:  " + string.Join("  ·  ", etapas.Select(item => $"{item.Etapa} {item.Candidatos}"));
            }
            catch (ErroDeRegra problema) { funil.Text = problema.Message; }
        }

        // Candidatos da vaga: liga e desliga um filtro na tabela de candidaturas (a tabela do grupo
        // continua sendo montada pelo TelaDeRecrutamento; aqui só trocamos as linhas). Liberado para todos os perfis.
        Vaga? vagaFiltrada = null;
        Botao? filtro = null;

        void Mostrar(IEnumerable<Candidato> lista)
        {
            candidatos.Rows.Clear();
            foreach (var item in lista) AdicionarLinha(candidatos, item, item.Nome, dados.TituloDaVaga(item.VagaId), item.Etapa);
        }

        void AplicarFiltro()
        {
            try
            {
                if (vagaFiltrada is null)
                {
                    Mostrar(dados.Candidatos);
                    filtro!.Text = "Candidatos da vaga";
                    return;
                }
                Mostrar(dados.CandidatosDaVaga(vagaFiltrada.Id));
                filtro!.Text = "Ver todos";
                MostrarMensagem($"Candidatos de {vagaFiltrada.Titulo}");
            }
            catch (ErroDeRegra problema) { MostrarMensagem(problema.Message); }
        }

        filtro = CriarBotao("Candidatos da vaga", "", 160, false, () =>
        {
            if (vagaFiltrada is not null) vagaFiltrada = null;
            else if (vagas.CurrentRow?.Tag is Vaga vaga) vagaFiltrada = vaga;
            else { MostrarMensagem("Selecione uma vaga na tabela"); return; }
            AplicarFiltro();
        });
        Estilo.Colocar(painel, filtro, 299, 305);

        // Com o filtro ligado, trocar de vaga na tabela de cima troca também os candidatos mostrados.
        vagas.SelectionChanged += (_, _) =>
        {
            AtualizarFunil();
            if (vagaFiltrada is not null && vagas.CurrentRow?.Tag is Vaga vaga && vaga.Id != vagaFiltrada.Id)
            {
                vagaFiltrada = vaga;
                AplicarFiltro();
            }
        };
        AtualizarFunil();

        // Avançar etapa: leva a candidatura selecionada para a etapa seguinte. Usa CriarBotaoDeEdicao,
        // então fica desligado para o Gestor (o serviço também bloqueia).
        var avancar = CriarBotaoDeEdicao("Avançar etapa", "", 140, false, () =>
        {
            if (candidatos.CurrentRow?.Tag is not Candidato candidato) { MostrarMensagem("Selecione uma candidatura na tabela"); return; }
            try { dados.AvancarEtapa(candidato.Id); }
            catch (ErroDeRegra problema) { MessageBox.Show(this, problema.Message, "Não foi possível avançar", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            Navegar("hire");
            MostrarMensagem($"{candidato.Nome} avançou de etapa");
        });
        var mudar = CriarBotaoDeEdicao("Mudar de vaga", "", 140, false, () =>
        {
            if (candidatos.CurrentRow?.Tag is Candidato candidato) MudarDeVaga(candidato);
            else MostrarMensagem("Selecione uma candidatura na tabela");
        });
        Estilo.Colocar(painel, avancar, 125, 629);
        Estilo.Colocar(painel, mudar, 273, 629);

        // Sem nenhuma candidatura na tabela, os botões ficam desligados.
        void Atualizar() => avancar.Enabled = mudar.Enabled = dados.PodeEditar && candidatos.RowCount > 0;
        candidatos.RowsAdded += (_, _) => Atualizar();
        candidatos.RowsRemoved += (_, _) => Atualizar();
        Atualizar();
    }

    // Formulário de mudança de vaga. Só aparecem as outras vagas abertas (o serviço confere de novo).
    // A etapa da candidatura continua a mesma. Se alguma regra falhar, a mensagem aparece no
    // próprio formulário e a escolha continua lá para corrigir.
    private void MudarDeVaga(Candidato candidato)
    {
        var abertas = dados.ConsultarVagas("Aberta").Where(item => item.Id != candidato.VagaId).ToArray();
        if (abertas.Length == 0) { MostrarMensagem("Não há outra vaga aberta"); return; }
        var titulos = abertas.Select(item => $"{item.Titulo} · {item.Id}").ToArray();
        var destino = CriarLista(titulos, titulos[0]);
        using var janela = new JanelaDeCadastro("Mudar de vaga", [("Nova vaga", destino)],
            () => dados.VincularAVaga(candidato.Id, abertas[destino.SelectedIndex].Id),
            $"{candidato.Nome} está em {dados.TituloDaVaga(candidato.VagaId)}. A etapa ({candidato.Etapa}) continua a mesma.");
        if (janela.ShowDialog(this) != DialogResult.OK) return;
        Navegar("hire");
        MostrarMensagem("Candidatura mudou de vaga");
    }
}
