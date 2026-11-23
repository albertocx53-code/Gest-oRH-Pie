using System.Diagnostics;

namespace Aurora.WinForms;

// Sobre o projeto
//
// Tela com o que antes só aparecia no site do projeto: a equipe, o banco de dados,
// os trechos de código de cada parte, onde entra a POO e como rodar o sistema.
// As abas de cima trocam o conteúdo do cartão. Clicar em uma imagem abre a
// janela ImagemForm, onde dá para ampliar.
// As imagens ficam na pasta Imagens e são copiadas junto com o programa (ver o .csproj).
internal sealed partial class AuroraForm
{
    // Aba e trecho de código escolhidos. Ficam guardados para a tela voltar como estava.
    private string abaSobre = "equipe";
    private int trechoAtual;

    private static readonly (string Iniciais, string Nome, string Parte, string Detalhe)[] Equipe =
    [
        ("AL", "Alberto", "Banco de dados", "Tabelas, scripts SQL, dados fictícios e o RepositorioSqlite."),
        ("RI", "Rickelmy", "Interface e back-end", "Telas do Aurora, login e as regras do RhService."),
        ("PV", "Paulo Victor", "Interface", "Telas do sistema."),
        ("JE", "Jefferson", "Cadastro de funcionários", "Consultar ficha, Inativar e sugestão de cargos."),
        ("CL", "Clóvis", "Ponto e frequência", "Lançar falta, lançar atraso e consulta por período."),
        ("KE", "Kerlon", "Recrutamento", "Funil de etapas, avançar etapa e mudar de vaga."),
        ("CD", "Cleberson Dias", "Salários", "Editar lançamento da folha e testes de salários.")
    ];

    private static readonly (string Imagem, string Titulo, string Quem, string[] Pontos)[] Trechos =
    [
        ("code_login.png", "Login com senha protegida", "Rickelmy · Authentication.cs",
            ["A senha vira um hash com PBKDF2 e SHA-256, nunca fica guardada aberta", "FixedTimeEquals compara sem dar pistas pelo tempo de resposta", "ZeroMemory apaga o hash calculado da memória"]),
        ("code_permissoes.png", "Regras e permissões", "Rickelmy · RhService.cs",
            ["Só Administrador e RH podem editar e ver salários", "Cada regra é conferida com Require antes de gravar", "Quem tem ponto ou folha não pode ser apagado"]),
        ("code_tabela_ponto.png", "A tabela de ponto", "Banco de dados · 01_criar_tabelas.sql",
            ["Chave dupla (matrícula + dia): um ponto por pessoa por dia", "CHECK só aceita Presença, Atraso ou Falta", "ON DELETE RESTRICT protege o histórico"]),
        ("code_repositorio.png", "Gravando no banco", "Banco de dados · RepositorioSqlite.cs",
            ["Valores vão por parâmetro, o que protege contra SQL Injection", "Entrada e saída vazias viram NULL na falta", "Se o banco recusar, aparece uma mensagem clara na tela"]),
        ("code_cadastro.png", "Ficha e Inativar", "Jefferson · CadastroDeFuncionarios.cs",
            ["O salário só é buscado se o perfil pode ver a folha", "Inativar troca só a situação e mantém o histórico", "As regras de perfil vêm do SaveEmployee do RhService"]),
        ("code_frequencia.png", "Lançar atraso", "Clóvis · FrequenciaDePonto.cs",
            ["Método de extensão: o RhService não precisou mudar", "Só aceita entrada depois das 08:00", "As outras regras vêm do SaveAttendance do RhService"]),
        ("code_recrutamento.png", "Avançar etapa", "Kerlon · Recrutamento.cs",
            ["A etapa seguinte vem da lista RhOptions.Stages", "Quem já foi contratado não tem etapa seguinte", "O mesmo nome não entra duas vezes na mesma vaga"]),
        ("code_salarios.png", "Editar lançamento", "Cleberson Dias · AuroraForm.Modules.cs",
            ["O mesmo formulário serve para novo lançamento e para edição", "Na edição, funcionário e competência ficam travados", "Duplo clique na linha também abre a edição"]),
        ("code_interface.png", "Interface IRhRepository", "Equipe · IRhRepository.cs",
            ["Define o que o repositório precisa fazer", "DemoRh guarda na memória, RepositorioSqlite guarda no banco", "As telas funcionam com qualquer um dos dois"])
    ];

    private Panel AboutPage()
    {
        var panel = Module("Sobre o projeto", "Gestão de RH · Programação Orientada a Objetos · Prof. Marcos Gomes da Silva Rocha · UniFacema");

        // Abas: cada botão guarda qual conteúdo mostrar e redesenha a tela.
        (string Id, string Texto, int Largura)[] abas =
            [("equipe", "Equipe", 96), ("banco", "Banco de dados", 138), ("codigo", "Códigos", 100), ("poo", "POO", 76), ("rodar", "Como rodar", 118)];
        var x = 2;
        foreach (var aba in abas)
        {
            var botao = new AuroraButton(aba.Texto) { Size = new(aba.Largura, 35), Selected = aba.Id == abaSobre };
            botao.Click += (_, _) => { abaSobre = aba.Id; Navigate("about"); };
            AuroraStyle.Put(panel, botao, x, 104);
            x += aba.Largura + 8;
        }

        var cartao = Card();
        PutWide(panel, cartao, 152, 508);
        switch (abaSobre)
        {
            case "banco": SobreBanco(cartao); break;
            case "codigo": SobreCodigo(cartao); break;
            case "poo": SobrePoo(cartao); break;
            case "rodar": SobreRodar(cartao); break;
            default: SobreEquipe(cartao); break;
        }
        return panel;
    }

    // Equipe: um quadrinho por pessoa (4 por linha) e o último com professor e matéria.
    private static void SobreEquipe(Panel cartao)
    {
        var grade = Grade(cartao, 4, 2);
        foreach (var pessoa in Equipe)
            grade.Controls.Add(Quadro(pessoa.Iniciais, pessoa.Nome, pessoa.Parte, pessoa.Detalhe));
        grade.Controls.Add(Quadro("PR", "Prof. Marcos", "Professor da matéria", "Programação Orientada a Objetos · UniFacema · 2º período."));
    }

    private static Surface Quadro(string iniciais, string nome, string parte, string detalhe)
    {
        var quadro = new Surface(AuroraStyle.Field, AuroraStyle.Border, 14) { Dock = DockStyle.Fill, Margin = new(6) };
        var selo = new Surface(AuroraStyle.Tint, AuroraStyle.Border, 10) { Size = new(42, 42) };
        var letras = AuroraStyle.Label(iniciais, 11, true, AuroraStyle.Accent);
        letras.AutoSize = false;
        letras.Dock = DockStyle.Fill;
        letras.TextAlign = ContentAlignment.MiddleCenter;
        selo.Controls.Add(letras);
        AuroraStyle.Put(quadro, selo, 16, 18);
        AuroraStyle.Put(quadro, AuroraStyle.Label(nome, 12.5f, true), 16, 72);
        AuroraStyle.Put(quadro, AuroraStyle.Label(parte, 9.5f, true, AuroraStyle.Accent), 17, 101);
        var texto = Texto(detalhe, 9, AuroraStyle.Muted);
        texto.Bounds = new(17, 128, 200, 60);
        texto.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        quadro.Controls.Add(texto);
        quadro.SizeChanged += (_, _) => texto.Width = Math.Max(40, quadro.Width - 30);
        return quadro;
    }

    // Banco de dados: o diagrama à esquerda (clique para ampliar) e os números à direita.
    private static void SobreBanco(Panel cartao)
    {
        var largura = cartao.Width * 62 / 100;
        var figura = Figura("diagrama_ER.png", "Diagrama do banco de dados");
        figura.Bounds = new(14, 14, largura - 14, cartao.Height - 54);
        figura.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
        cartao.Controls.Add(figura);
        AuroraStyle.Put(cartao, AuroraStyle.Label("Clique no diagrama para ver as colunas de perto", 9, false, AuroraStyle.Muted), 16, cartao.Height - 34);

        (string Titulo, string Texto)[] itens =
        [
            ("8 tabelas", "Funcionários, ponto, folha, vagas, candidatos, departamentos, usuários e agenda."),
            ("Relações 1:N", "Ponto e folha se ligam ao funcionário pela matrícula."),
            ("Regras no banco", "Um ponto por dia, valores válidos e histórico que não pode ser apagado."),
            ("Dados fictícios", "8 funcionários, 30 registros de ponto e 12 lançamentos de folha."),
            ("SQLite", "Tudo fica num arquivo só (aurora.db), criado na primeira vez que o sistema abre.")
        ];
        var y = 18;
        foreach (var item in itens)
        {
            AuroraStyle.Put(cartao, AuroraStyle.Label(item.Titulo, 11.5f, true, AuroraStyle.Accent), largura + 18, y);
            var texto = Texto(item.Texto, 9.5f, AuroraStyle.Ink);
            texto.Bounds = new(largura + 18, y + 26, cartao.Width - largura - 36, 44);
            texto.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cartao.Controls.Add(texto);
            y += 94;
        }
    }

    // Códigos: lista dos trechos à esquerda; à direita, a imagem do código e o que ele faz.
    private void SobreCodigo(Panel cartao)
    {
        for (var i = 0; i < Trechos.Length; i++)
        {
            var indice = i;
            var botao = new AuroraButton(Trechos[i].Titulo) { Size = new(210, 46), Selected = i == trechoAtual, Font = AuroraStyle.Font(9, true) };
            botao.Click += (_, _) => { trechoAtual = indice; Navigate("about"); };
            AuroraStyle.Put(cartao, botao, 14, 14 + i * 53);
        }

        var trecho = Trechos[trechoAtual];
        var esquerda = 242;
        var largura = cartao.Width - esquerda - 16;
        AuroraStyle.Put(cartao, AuroraStyle.Label(trecho.Titulo, 15, true), esquerda, 14);
        AuroraStyle.Put(cartao, AuroraStyle.Label(trecho.Quem, 9.5f, false, AuroraStyle.Muted), esquerda + 2, 46);

        var figura = Figura(trecho.Imagem, $"{trecho.Titulo} · {trecho.Quem}");
        figura.Bounds = new(esquerda, 76, largura, 300);
        figura.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        cartao.Controls.Add(figura);

        var y = 390;
        foreach (var ponto in trecho.Pontos)
        {
            AuroraStyle.Put(cartao, AuroraStyle.Label("•  " + ponto, 10, false), esquerda + 2, y);
            y += 28;
        }
        AuroraStyle.Put(cartao, AuroraStyle.Label("Clique no código para ampliar", 9, false, AuroraStyle.Muted), esquerda + 2, y + 4);
    }

    // POO: os quatro conceitos da matéria e onde cada um aparece no código.
    private static void SobrePoo(Panel cartao)
    {
        (string Titulo, string Texto, string Codigo)[] conceitos =
        [
            ("Classes", "Cada parte do sistema é uma classe com uma função só.", "class RhService"),
            ("Herança", "As telas herdam do Windows Forms.", "LoginForm : Form"),
            ("Encapsulamento", "A tela só fala com o serviço, nunca direto com os dados.", "private List<Employee>"),
            ("Polimorfismo", "Memória ou banco, sem mudar as telas.", "RepositorioSqlite : IRhRepository")
        ];
        var grade = Grade(cartao, 2, 2);
        foreach (var conceito in conceitos)
        {
            var quadro = new Surface(AuroraStyle.Field, AuroraStyle.Border, 14) { Dock = DockStyle.Fill, Margin = new(6) };
            AuroraStyle.Put(quadro, AuroraStyle.Label(conceito.Titulo, 16, true, AuroraStyle.Accent), 20, 22);
            var texto = Texto(conceito.Texto, 11, AuroraStyle.Ink);
            texto.Bounds = new(22, 64, 400, 54);
            texto.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            quadro.Controls.Add(texto);
            var codigo = AuroraStyle.Label(conceito.Codigo, 11, false, AuroraStyle.Ink);
            codigo.Font = new Font("Consolas", 11);
            codigo.BackColor = AuroraStyle.Paper;
            codigo.Padding = new(8, 5, 8, 5);
            AuroraStyle.Put(quadro, codigo, 22, 136);
            quadro.SizeChanged += (_, _) => texto.Width = Math.Max(40, quadro.Width - 44);
            grade.Controls.Add(quadro);
        }
    }

    // Como rodar: o passo a passo e os links do site e do GitHub.
    private static void SobreRodar(Panel cartao)
    {
        string[] passos =
        [
            "Instale o Visual Studio com o .NET 10.",
            "Baixe o projeto no GitHub (botão Code → Download ZIP) ou clone o repositório.",
            "Abra o arquivo Aurora.slnx no Visual Studio. Na primeira vez ele baixa o pacote do SQLite.",
            "Aperte F5. O banco aurora.db é criado sozinho com os dados de exemplo.",
            "Entre com o usuário rh, admin ou gestor e a senha Aurora.demo26."
        ];
        var y = 20;
        for (var i = 0; i < passos.Length; i++)
        {
            var numero = new Surface(AuroraStyle.Primary, AuroraStyle.Primary, 15) { Size = new(32, 32) };
            var digito = AuroraStyle.Label((i + 1).ToString(), 11, true, Color.White);
            digito.AutoSize = false;
            digito.Dock = DockStyle.Fill;
            digito.TextAlign = ContentAlignment.MiddleCenter;
            numero.Controls.Add(digito);
            AuroraStyle.Put(cartao, numero, 20, y);
            AuroraStyle.Put(cartao, AuroraStyle.Label(passos[i], 11, false), 66, y + 5);
            y += 52;
        }

        y += 14;
        AuroraStyle.Put(cartao, AuroraStyle.Label("PERFIS DE ACESSO", 8.5f, true, AuroraStyle.Muted), 22, y);
        AuroraStyle.Put(cartao, AuroraStyle.Label("admin e rh: alteram tudo e veem salários   ·   gestor: só consulta, sem salários", 10, false), 22, y + 24);

        y += 72;
        AuroraStyle.Put(cartao, AuroraStyle.Label("LINKS", 8.5f, true, AuroraStyle.Muted), 22, y);
        Link(cartao, "Versão web do sistema: gestao-rh-teal.vercel.app", "https://gestao-rh-teal.vercel.app/sistema/", 22, y + 24);
        Link(cartao, "Código no GitHub: github.com/albertocx53-code/Gest-oRH-Pie", "https://github.com/albertocx53-code/Gest-oRH-Pie", 22, y + 52);
    }

    // ---------- ajudantes da tela ----------

    // Grade de quadrinhos que ocupa o cartão inteiro, com colunas e linhas do mesmo tamanho.
    private static TableLayoutPanel Grade(Panel cartao, int colunas, int linhas)
    {
        var grade = new TableLayoutPanel { ColumnCount = colunas, RowCount = linhas, BackColor = Color.Transparent, Padding = new(8) };
        for (var i = 0; i < colunas; i++) grade.ColumnStyles.Add(new(SizeType.Percent, 100f / colunas));
        for (var i = 0; i < linhas; i++) grade.RowStyles.Add(new(SizeType.Percent, 100f / linhas));
        grade.Bounds = new(4, 4, cartao.Width - 8, cartao.Height - 8);
        grade.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        cartao.Controls.Add(grade);
        return grade;
    }

    // Texto que quebra a linha sozinho dentro da largura dada.
    private static Label Texto(string texto, float tamanho, Color cor)
    {
        var rotulo = AuroraStyle.Label(texto, tamanho, false, cor);
        rotulo.AutoSize = false;
        return rotulo;
    }

    private static void Link(Panel parent, string texto, string endereco, int x, int y)
    {
        var link = new LinkLabel
        {
            Text = texto, AutoSize = true, Font = AuroraStyle.Font(10), BackColor = Color.Transparent,
            LinkColor = AuroraStyle.Accent, ActiveLinkColor = Color.White, VisitedLinkColor = AuroraStyle.Accent
        };
        // UseShellExecute abre o endereço no navegador padrão do Windows.
        link.LinkClicked += (_, _) => Process.Start(new ProcessStartInfo(endereco) { UseShellExecute = true });
        AuroraStyle.Put(parent, link, x, y);
    }

    // Imagem da pasta Imagens que abre ampliada ao clicar.
    private static Control Figura(string arquivo, string titulo)
    {
        var imagem = CarregarImagem(arquivo);
        if (imagem is null)
            return AuroraStyle.Label($"Imagem {arquivo} não encontrada na pasta Imagens.", 10, false, AuroraStyle.Muted);

        var figura = new PictureBox
        {
            Image = imagem, SizeMode = PictureBoxSizeMode.Zoom, BackColor = AuroraStyle.Paper,
            Cursor = Cursors.Hand, AccessibleName = titulo
        };
        figura.Click += (_, _) =>
        {
            var copia = CarregarImagem(arquivo);
            if (copia is null) return;
            using var janela = new ImagemForm(titulo, copia);
            janela.ShowDialog(figura.FindForm());
        };
        // A imagem não é liberada junto com o PictureBox, então liberamos aqui.
        figura.Disposed += (_, _) => imagem.Dispose();
        return figura;
    }

    // Lê a imagem para a memória, assim o arquivo não fica preso enquanto o programa está aberto.
    internal static Image? CarregarImagem(string arquivo)
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Imagens", arquivo);
        if (!File.Exists(caminho)) return null;
        using var leitura = new MemoryStream(File.ReadAllBytes(caminho));
        using var original = Image.FromStream(leitura);
        return new Bitmap(original);
    }
}
