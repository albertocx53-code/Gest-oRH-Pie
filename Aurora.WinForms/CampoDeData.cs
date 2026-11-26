using System.Globalization;

namespace Aurora.WinForms;

// Campo de data. Aceita dd/mm/aaaa (ou mm/aaaa para mês).
// As setas pra cima e pra baixo mudam o dia (ou o mês).
internal sealed class CampoDeData : TextBox
{
    private readonly string formato;
    private readonly bool mes;
    private DateTime ultimoValido;

    internal DateTime Valor => ultimoValido;
    internal bool Valido => DateTime.TryParseExact(Text, formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out var data) && data.Year is >= 1900 and <= 2100;
    internal event EventHandler? ValorMudou;

    public CampoDeData(DateTime data, bool mes)
    {
        this.mes = mes;
        formato = mes ? "MM/yyyy" : "dd/MM/yyyy";
        ultimoValido = mes ? new DateTime(data.Year, data.Month, 1) : data.Date;
        BackColor = Estilo.CorDoCampo;
        ForeColor = Estilo.CorDoTexto;
        Font = Estilo.Fonte(10);
        AccessibleDescription = mes ? "Formato mês e ano. Use as setas para avançar ou voltar." : "Formato dia, mês e ano. Use as setas para avançar ou voltar.";
        MaxLength = mes ? 7 : 10;
        Text = ultimoValido.ToString(formato, CultureInfo.InvariantCulture);
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        if (formato is null) return;
        ForeColor = Valido ? Estilo.CorDoTexto : Estilo.CorDeErro;
        if (!Valido) return;
        var convertido = DateTime.ParseExact(Text, formato, CultureInfo.InvariantCulture);
        if (convertido == ultimoValido) return;
        ultimoValido = convertido;
        ValorMudou?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Modifiers == Keys.None && e.KeyCode is Keys.Up or Keys.Down)
        {
            var passo = e.KeyCode == Keys.Up ? 1 : -1;
            var proximo = mes ? ultimoValido.AddMonths(passo) : ultimoValido.AddDays(passo);
            if (proximo.Year is >= 1900 and <= 2100) Text = proximo.ToString(formato, CultureInfo.InvariantCulture);
            e.SuppressKeyPress = true;
        }
        base.OnKeyDown(e);
    }
}
