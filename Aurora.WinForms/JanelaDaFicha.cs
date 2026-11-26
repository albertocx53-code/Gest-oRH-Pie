using System.Globalization;
using Aurora.Core;

namespace Aurora.WinForms;

// JanelaDaFicha - Jefferson (cadastro de funcionários)
//
// Janela "Ficha do funcionário", aberta pelo botão Consultar ficha.
// Ela só mostra dados. Quem decide o que entra na ficha é o serviço
// (CadastroDeFuncionarios.ConsultarFicha). Por isso o salário só aparece
// quando o perfil tem acesso à folha (Administrador e RH).
// O visual segue o JanelaDeCadastro: fundo escuro, título grande e rótulos em cinza.
internal sealed class JanelaDaFicha : Form
{
    // Posição vertical da próxima linha. Cada Secao e cada Linha empurra para baixo.
    private int y;

    public JanelaDaFicha(FichaDoFuncionario ficha)
    {
        var pessoa = ficha.Funcionario;
        Text = "Ficha do funcionário";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Estilo.CorDoFundo;

        // Cabeçalho: nome em destaque e, embaixo, cargo e departamento.
        // AutoEllipsis corta com "..." um nome muito comprido em vez de passar da janela.
        var nome = Estilo.CriarRotulo(pessoa.Nome, 18, true);
        nome.AutoSize = false;
        nome.AutoEllipsis = true;
        nome.Size = new(424, 34);
        nome.AccessibleName = "Nome";
        Estilo.Colocar(this, nome, 18, 13);
        var resumo = Estilo.CriarRotulo($"{pessoa.Cargo} · {pessoa.Departamento}", 10, false, Estilo.CorApagada);
        resumo.AutoSize = false;
        resumo.AutoEllipsis = true;
        resumo.Size = new(424, 22);
        Estilo.Colocar(this, resumo, 20, 49);
        y = 84;

        Secao("DADOS DO CADASTRO");
        Linha("Matrícula", pessoa.Id.ToString());
        Linha("Situação", pessoa.Situacao);
        Linha("Admissão", pessoa.Admissao.ToString("dd/MM/yyyy"));
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
            Linha("Competência", folha.Competencia.ToString("MM/yyyy"));
            Linha("Líquido simulado", folha.Total.ToString("C"));
        }
        else
        {
            Linha("Salário", "Nenhum lançamento nos últimos 12 meses");
        }

        // Fechar responde tanto ao Enter quanto ao Esc.
        var fechar = new Botao("Fechar") { Size = new(93, 32), Principal = true, DialogResult = DialogResult.OK };
        Estilo.Colocar(this, fechar, 349, y + 12);
        ClientSize = new(460, y + 58);
        AcceptButton = fechar;
        CancelButton = fechar;
        ActiveControl = fechar;
    }

    private void Secao(string titulo)
    {
        Estilo.Colocar(this, Estilo.CriarRotulo(titulo, 8, true, Estilo.CorDeDestaque), 20, y + 8);
        y += 32;
    }

    // Uma linha da ficha: rótulo à esquerda e valor à direita.
    // O valor recebe o mesmo nome do rótulo (AccessibleName), o que ajuda leitores de tela e os testes.
    private void Linha(string rotulo, string valor)
    {
        Estilo.Colocar(this, Estilo.CriarRotulo(rotulo, 9, true, Estilo.CorApagada), 20, y + 2);
        var texto = Estilo.CriarRotulo(valor, 10);
        texto.AutoSize = false;
        texto.AutoEllipsis = true;
        texto.Size = new(282, 22);
        texto.AccessibleName = rotulo;
        Estilo.Colocar(this, texto, 160, y);
        y += 27;
    }

    private static string UltimoRegistro(RegistroDePonto? ponto)
    {
        if (ponto is null) return "Nenhum registro de ponto";
        var dia = ponto.Dia.ToString("dd/MM/yyyy");
        // Na falta não há horário, então mostramos só o dia e a situação.
        return ponto.Entrada.Length > 0 ? $"{dia} · {ponto.Situacao} · {ponto.Entrada} às {ponto.Saida}" : $"{dia} · {ponto.Situacao}";
    }
}
