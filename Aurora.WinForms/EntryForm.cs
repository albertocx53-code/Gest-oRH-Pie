using Aurora.Core;

namespace Aurora.WinForms;

internal sealed class EntryForm : Form
{
    public EntryForm(string title, (string Label, Control Input)[] fields, Action saveRecord, string? note = null)
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = AuroraStyle.Paper;
        var heading = AuroraStyle.Label(title, 18, true);
        AuroraStyle.Put(this, heading, 18, 13);
        var y = 54;
        if (note is not null)
        {
            var explanation = AuroraStyle.Label(note, 9, false, AuroraStyle.Muted);
            explanation.MaximumSize = new(404, 0);
            AuroraStyle.Put(this, explanation, 20, y);
            y += 43;
        }
        for (var i = 0; i < fields.Length; i++)
        {
            var (label, input) = fields[i];
            AuroraStyle.Put(this, AuroraStyle.Label(label, 9, true), 20, y);
            input.Width = 398;
            input.AccessibleName = label;
            input.TabIndex = i;
            AuroraStyle.Put(this, input, 20, y + 20);
            // Um campo de várias linhas precisa de mais espaço para os controles abaixo dele.
            y += Math.Max(55, input.Bottom - y + 12);
        }
        var error = AuroraStyle.Label("", 8, true, Color.LightPink);
        error.MaximumSize = new(401, 0);
        AuroraStyle.Put(this, error, 20, y - 5);
        var cancel = new AuroraButton("Cancelar") { Size = new(90, 32), DialogResult = DialogResult.Cancel, TabIndex = fields.Length + 1 };
        var save = new AuroraButton("Confirmar") { Size = new(93, 32), Primary = true, TabIndex = fields.Length };
        save.Click += (_, _) =>
        {
            var invalidDate = fields.FirstOrDefault(field => field.Input is RhDateInput { IsValid: false });
            if (invalidDate.Input is not null) { error.Text = $"Confira o formato do campo {invalidDate.Label}."; return; }
            // RhService explica o que precisa ser corrigido. Mantemos o formulário aberto com os dados digitados.
            try
            {
                saveRecord();
                DialogResult = DialogResult.OK;
            }
            catch (RhRuleException issue) { error.Text = issue.Message; }
        };
        AuroraStyle.Put(this, cancel, 228, y + 13);
        AuroraStyle.Put(this, save, 325, y + 13);
        ClientSize = new(440, y + 55);
        AcceptButton = save;
        CancelButton = cancel;
        ActiveControl = fields[0].Input;
    }
}
