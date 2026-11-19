using System.Globalization;
using Aurora.Core;

namespace Aurora.WinForms;

// FichaDoFuncionarioForm - Jefferson (cadastro de funcionários)
//
// Janela "Ficha do funcionário", aberta pelo botão Consultar ficha.
// Ela só mostra dados. Quem decide o que entra na ficha é o serviço
// (CadastroDeFuncionarios.ConsultarFicha). Por isso o salário só aparece
// quando o perfil tem acesso à folha (Administrador e RH).
// O visual segue o EntryForm: fundo escuro, título grande e rótulos em cinza.
internal sealed class FichaDoFuncionarioForm : Form
{
    // Posição vertical da próxima linha. Cada Secao e cada Linha empurra para baixo.
    private int y;

    public FichaDoFuncionarioForm(FichaDoFuncionario ficha)
    {
        var pessoa = ficha.Funcionario;
        Text = "Ficha do funcionário";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = AuroraStyle.Paper;

        // Cabeçalho: nome em destaque e, embaixo, cargo e departamento.
        // AutoEllipsis corta com "..." um nome muito comprido em vez de passar da janela.
        var nome = AuroraStyle.Label(pessoa.Name, 18, true);
        nome.AutoSize = false;
        nome.AutoEllipsis = true;
        nome.Size = new(424, 34);
        nome.AccessibleName = "Nome";
        AuroraStyle.Put(this, nome, 18, 13);
        var resumo = AuroraStyle.Label($"{pessoa.Role} · {pessoa.Team}", 10, false, AuroraStyle.Muted);
        resumo.AutoSize = false;
        resumo.AutoEllipsis = true;
        resumo.Size = new(424, 22);
        AuroraStyle.Put(this, resumo, 20, 49);
        y = 84;

        Secao("DADOS DO CADASTRO");
        Linha("Matrícula", pessoa.Id.ToString());
        Linha("Situação", pessoa.Status);
        Linha("Admissão", pessoa.Started.ToString("dd/MM/yyyy"));
        Linha("Tempo de casa", ficha.TempoDeCasa);

        // Ex.: "PONTO EM NOVEMBRO DE 2026". O nome do mês sai em português por causa da cultura pt-BR.
        var mes = ficha.Referencia.ToString("MMMM 'de' yyyy", CultureInfo.CurrentCulture).ToUpper(CultureInfo.CurrentCulture);
        Secao($"PONTO EM {mes}");
        Linha("Presenças", ficha.Presencas.ToString());
        Linha("Atrasos", ficha.Atrasos.ToString());
        Linha("Faltas", ficha.Faltas.ToString());
        Linha("Último registro", UltimoRegistro(ficha.UltimoPonto));

        Secao("SALÁRIO");
        if (!ficha.SalarioVisivel)
        {
            Linha("Salário", "Sem acesso aos salários neste perfil");
        }
        else if (ficha.UltimaFolha is { } folha)
        {
            Linha("Competência", folha.Month.ToString("MM/yyyy"));
            Linha("Líquido simulado", folha.Total.ToString("C"));
        }
        else
        {
            Linha("Salário", "Nenhum lançamento nos últimos 12 meses");
        }

        // Fechar responde tanto ao Enter quanto ao Esc.
        var fechar = new AuroraButton("Fechar") { Size = new(93, 32), Primary = true, DialogResult = DialogResult.OK };
        AuroraStyle.Put(this, fechar, 349, y + 12);
        ClientSize = new(460, y + 58);
        AcceptButton = fechar;
        CancelButton = fechar;
        ActiveControl = fechar;
    }

    private void Secao(string titulo)
    {
        AuroraStyle.Put(this, AuroraStyle.Label(titulo, 8, true, AuroraStyle.Accent), 20, y + 8);
        y += 32;
    }

    // Uma linha da ficha: rótulo à esquerda e valor à direita.
    // O valor recebe o mesmo nome do rótulo (AccessibleName), o que ajuda leitores de tela e os testes.
    private void Linha(string rotulo, string valor)
    {
        AuroraStyle.Put(this, AuroraStyle.Label(rotulo, 9, true, AuroraStyle.Muted), 20, y + 2);
        var texto = AuroraStyle.Label(valor, 10);
        texto.AutoSize = false;
        texto.AutoEllipsis = true;
        texto.Size = new(282, 22);
        texto.AccessibleName = rotulo;
        AuroraStyle.Put(this, texto, 160, y);
        y += 27;
    }

    private static string UltimoRegistro(Attendance? ponto)
    {
        if (ponto is null) return "Nenhum registro de ponto";
        var dia = ponto.Day.ToString("dd/MM/yyyy");
        // Na falta não há horário, então mostramos só o dia e a situação.
        return ponto.In.Length > 0 ? $"{dia} · {ponto.Status} · {ponto.In} às {ponto.Out}" : $"{dia} · {ponto.Status}";
    }
}
