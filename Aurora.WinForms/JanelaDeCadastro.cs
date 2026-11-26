using Aurora.Core;

namespace Aurora.WinForms;
// Janela de formulário usada em todos os cadastros (funcionário, ponto, vaga...).
// Recebe os campos e o que fazer ao salvar. Se der erro, mostra a mensagem e não fecha.
internal sealed class JanelaDeCadastro : Form
{
    public JanelaDeCadastro(string titulo, (string Texto, Control Campo)[] campos, Action salvarRegistro, string? observacao = null)
    {
        Text = titulo;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Estilo.CorDoFundo;
        var rotuloDoTitulo = Estilo.CriarRotulo(titulo, 18, true);
        Estilo.Colocar(this, rotuloDoTitulo, 18, 13);
        var y = 54;
        if (observacao is not null)
        {
            var explicacao = Estilo.CriarRotulo(observacao, 9, false, Estilo.CorApagada);
            explicacao.MaximumSize = new(404, 0);
            Estilo.Colocar(this, explicacao, 20, y);
            y += 43;
        }
        for (var i = 0; i < campos.Length; i++)
        {
            var (rotulo, campo) = campos[i];
            Estilo.Colocar(this, Estilo.CriarRotulo(rotulo, 9, true), 20, y);
            campo.Width = 398;
            campo.AccessibleName = rotulo;
            campo.TabIndex = i;
            Estilo.Colocar(this, campo, 20, y + 20);
            // campo de várias linhas precisa de mais espaço
            y += Math.Max(55, campo.Bottom - y + 12);
        }
        var erro = Estilo.CriarRotulo("", 8, true, Estilo.CorDeErro);
        erro.MaximumSize = new(401, 0);
        Estilo.Colocar(this, erro, 20, y - 5);
        var cancelar = new Botao("Cancelar") { Size = new(90, 32), DialogResult = DialogResult.Cancel, TabIndex = campos.Length + 1 };
        var salvar = new Botao("Confirmar") { Size = new(93, 32), Principal = true, TabIndex = campos.Length };
        salvar.Click += (_, _) =>
        {
            var dataInvalida = campos.FirstOrDefault(campo => campo.Campo is CampoDeData { Valido: false });
            if (dataInvalida.Campo is not null) { erro.Text = $"Confira o formato do campo {dataInvalida.Texto}."; return; }
            // mostra o erro e deixa o formulário aberto para corrigir
            try
            {
                salvarRegistro();
                DialogResult = DialogResult.OK;
            }
            catch (ErroDeRegra problema) { erro.Text = problema.Message; }
        };
        Estilo.Colocar(this, cancelar, 228, y + 13);
        Estilo.Colocar(this, salvar, 325, y + 13);
        ClientSize = new(440, y + 55);
        AcceptButton = salvar;
        CancelButton = cancelar;
        ActiveControl = campos[0].Campo;
    }
}
