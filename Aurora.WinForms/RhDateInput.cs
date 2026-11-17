using System.Globalization;

namespace Aurora.WinForms;

// Enquanto a pessoa digita uma data incompleta, o filtro ainda usa o último valor válido.
internal sealed class RhDateInput : TextBox
{
    private readonly string format;
    private readonly bool month;
    private DateTime lastValid;

    internal DateTime Value => lastValid;
    internal bool IsValid => DateTime.TryParseExact(Text, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) && date.Year is >= 1900 and <= 2100;
    internal event EventHandler? ValueChanged;

    public RhDateInput(DateTime date, bool month)
    {
        this.month = month;
        format = month ? "MM/yyyy" : "dd/MM/yyyy";
        lastValid = month ? new DateTime(date.Year, date.Month, 1) : date.Date;
        BackColor = AuroraStyle.Field;
        ForeColor = AuroraStyle.Ink;
        Font = AuroraStyle.Font(10);
        AccessibleDescription = month ? "Formato mês e ano. Use as setas para avançar ou voltar." : "Formato dia, mês e ano. Use as setas para avançar ou voltar.";
        MaxLength = month ? 7 : 10;
        Text = lastValid.ToString(format, CultureInfo.InvariantCulture);
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        if (format is null) return;
        ForeColor = IsValid ? AuroraStyle.Ink : Color.LightPink;
        if (!IsValid) return;
        var parsed = DateTime.ParseExact(Text, format, CultureInfo.InvariantCulture);
        if (parsed == lastValid) return;
        lastValid = parsed;
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Modifiers == Keys.None && e.KeyCode is Keys.Up or Keys.Down)
        {
            var step = e.KeyCode == Keys.Up ? 1 : -1;
            var next = month ? lastValid.AddMonths(step) : lastValid.AddDays(step);
            if (next.Year is >= 1900 and <= 2100) Text = next.ToString(format, CultureInfo.InvariantCulture);
            e.SuppressKeyPress = true;
        }
        base.OnKeyDown(e);
    }
}
