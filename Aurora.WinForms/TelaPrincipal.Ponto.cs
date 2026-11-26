using Aurora.Core;

namespace Aurora.WinForms;

// TelaPrincipal.Ponto - Clóvis (ponto e frequência)
//
// O que eu acrescentei na tela de Ponto e frequência. O TelaPrincipal é uma "partial class":
// a mesma classe pode ser dividida em vários arquivos. Assim a minha parte fica
// separada e o código que o grupo já tinha continua igual.
//
// Ligação com a tela que já existia (TelaPrincipal.Modulos.cs):
//   TelaDePonto() chama FerramentasDoPonto no final -> botões Lançar falta, Lançar atraso e Consultar período
//
// As regras não ficam aqui. A tela pede ao serviço (FrequenciaDePonto / ServicoRh)
// e só mostra o resultado ou a mensagem de erro.
internal sealed partial class TelaPrincipal
{
    private void FerramentasDoPonto(Panel painel)
    {
        // Lançar falta e Lançar atraso usam CriarBotaoDeEdicao, então ficam desligados para o Gestor
        // (o serviço também bloqueia). Consultar período é só consulta, liberado para todos os perfis.
        // Os botões ficam na mesma linha do Editar registro, que já existia (x = 3, largura 130).
        Estilo.Colocar(painel, CriarBotaoDeEdicao("Lançar falta", "", 124, false, () => LancarOcorrencia("Falta")), 141, 566);
        Estilo.Colocar(painel, CriarBotaoDeEdicao("Lançar atraso", "", 132, false, () => LancarOcorrencia("Atraso")), 273, 566);
        Estilo.Colocar(painel, CriarBotao("Consultar período", "", 160, false, AbrirConsultaDePeriodo), 413, 566);
    }

    // Formulário de lançamento. tipo = "Falta" ou "Atraso".
    // Falta não tem horário, então os campos Entrada e Saída só aparecem no atraso.
    // O funcionário já vem selecionado: é o primeiro ativo que ainda não tem ponto no dia da tela.
    // Ao confirmar, o JanelaDeCadastro chama o serviço. Se alguma regra falhar, a mensagem aparece
    // no próprio formulário e o que foi digitado continua lá para corrigir.
    private void LancarOcorrencia(string tipo)
    {
        var equipe = dados.Funcionarios.Where(item => item.Situacao == "Ativo").ToArray();
        if (equipe.Length == 0) { MostrarMensagem("Cadastre um funcionário ativo primeiro"); return; }
        var inicial = dados.SemRegistro(diaDoPonto).FirstOrDefault() ?? equipe[0];
        var pessoa = CriarLista(equipe.Select(item => $"{item.Nome} · {item.Id}"), $"{inicial.Nome} · {inicial.Id}");
        var dia = CriarCampoDeData(diaDoPonto);
        var entrada = CriarCampoDeTexto("08:15");
        var saida = CriarCampoDeTexto("17:00");
        var motivo = CriarCampoDeTexto("", "Opcional");
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
            if (falta) dados.LancarFalta(matricula, dia.Valor, motivo.Text, DadosDeExemplo.Hoje);
            else dados.LancarAtraso(matricula, dia.Valor, entrada.Text, saida.Text, motivo.Text, DadosDeExemplo.Hoje);
        }

        using var janela = new JanelaDeCadastro(falta ? "Lançar falta" : "Lançar atraso", campos, Salvar,
            falta ? "A falta fica sem horário de entrada e de saída." : "No atraso, a entrada precisa ser depois das 08:00.");
        if (janela.ShowDialog(this) != DialogResult.OK) return;
        diaDoPonto = dia.Valor.Date;
        Navegar("time");
        MostrarMensagem(falta ? "Falta lançada" : "Atraso lançado");
    }

    private void AbrirConsultaDePeriodo()
    {
        // DadosDeExemplo.Today (13/11/2026) é o "hoje" de toda a demonstração. A janela abre no mês dessa data.
        using var janela = new JanelaDeFrequencia(dados, DadosDeExemplo.Hoje);
        janela.ShowDialog(this);
    }
}