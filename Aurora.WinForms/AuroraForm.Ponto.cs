using Aurora.Core;

namespace Aurora.WinForms;

// AuroraForm.Ponto - Clóvis (ponto e frequência)
//
// O que eu acrescentei na tela de Ponto e frequência. O AuroraForm é uma "partial class":
// a mesma classe pode ser dividida em vários arquivos. Assim a minha parte fica
// separada e o código que o grupo já tinha continua igual.
//
// Ligação com a tela que já existia (AuroraForm.Modules.cs):
//   TimePage() chama FerramentasDoPonto no final -> botões Lançar falta, Lançar atraso e Consultar período
//
// As regras não ficam aqui. A tela pede ao serviço (FrequenciaDePonto / RhService)
// e só mostra o resultado ou a mensagem de erro.
internal sealed partial class AuroraForm
{
    private void FerramentasDoPonto(Panel panel)
    {
        // Lançar falta e Lançar atraso usam EditAction, então ficam desligados para o Gestor
        // (o serviço também bloqueia). Consultar período é só consulta, liberado para todos os perfis.
        // Os botões ficam na mesma linha do Editar registro, que já existia (x = 3, largura 130).
        AuroraStyle.Put(panel, EditAction("Lançar falta", "", 124, false, () => LancarOcorrencia("Falta")), 141, 566);
        AuroraStyle.Put(panel, EditAction("Lançar atraso", "", 132, false, () => LancarOcorrencia("Atraso")), 273, 566);
        AuroraStyle.Put(panel, Action("Consultar período", "", 160, false, AbrirConsultaDePeriodo), 413, 566);
    }

    // Formulário de lançamento. tipo = "Falta" ou "Atraso".
    // Falta não tem horário, então os campos Entrada e Saída só aparecem no atraso.
    // O funcionário já vem selecionado: é o primeiro ativo que ainda não tem ponto no dia da tela.
    // Ao confirmar, o EntryForm chama o serviço. Se alguma regra falhar, a mensagem aparece
    // no próprio formulário e o que foi digitado continua lá para corrigir.
    private void LancarOcorrencia(string tipo)
    {
        var equipe = data.Employees.Where(item => item.Status == "Ativo").ToArray();
        if (equipe.Length == 0) { Status("Cadastre um funcionário ativo primeiro"); return; }
        var inicial = data.SemRegistro(punchDay).FirstOrDefault() ?? equipe[0];
        var pessoa = Choice(equipe.Select(item => $"{item.Name} · {item.Id}"), $"{inicial.Name} · {inicial.Id}");
        var dia = DateField(punchDay);
        var entrada = TextField("08:15");
        var saida = TextField("17:00");
        var motivo = TextField("", "Opcional");
        motivo.Multiline = true;
        motivo.Height = 60;
        motivo.MaxLength = 240;
        motivo.ScrollBars = ScrollBars.Vertical;
        motivo.AccessibleDescription = "Campo opcional, com até 240 caracteres.";

        var falta = tipo == "Falta";
        var campos = falta
            ? new (string, Control)[] { ("Funcionário", pessoa), ("Data", dia), ("Motivo (opcional)", motivo) }
            : new (string, Control)[] { ("Funcionário", pessoa), ("Data", dia), ("Entrada (HH:mm)", entrada), ("Saída (HH:mm)", saida), ("Motivo (opcional)", motivo) };

        void Salvar()
        {
            var matricula = equipe[pessoa.SelectedIndex].Id;
            if (falta) data.LancarFalta(matricula, dia.Value, motivo.Text, DemoRh.Today);
            else data.LancarAtraso(matricula, dia.Value, entrada.Text, saida.Text, motivo.Text, DemoRh.Today);
        }

        using var form = new EntryForm(falta ? "Lançar falta" : "Lançar atraso", campos, Salvar,
            falta ? "A falta fica sem horário de entrada e de saída." : "No atraso, a entrada precisa ser depois das 08:00.");
        if (form.ShowDialog(this) != DialogResult.OK) return;
        punchDay = dia.Value.Date;
        Navigate("time");
        Status(falta ? "Falta lançada" : "Atraso lançado");
    }

    private void AbrirConsultaDePeriodo()
    {
        // DemoRh.Today (13/11/2026) é o "hoje" de toda a demonstração. A janela abre no mês dessa data.
        using var form = new FrequenciaPorPeriodoForm(data, DemoRh.Today);
        form.ShowDialog(this);
    }
}