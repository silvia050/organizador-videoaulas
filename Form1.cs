using System.Text.RegularExpressions;

namespace OrganizadorVideoaulas;

public partial class Form1 : Form
{
    // Extensões de arquivo que o programa reconhece como vídeo
    private static readonly string[] ExtensoesVideo =
        { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".webm" };

    // Encontra "aula" + número no começo do nome (ex.: "aula 1", "Aula2", "aula_03")
    private static readonly Regex RegexNumeroAula =
        new(@"^\s*aula[\s_\-]*(\d+)", RegexOptions.IgnoreCase);

    // Remove "aula" + número do começo do nome, para sobrar só o título
    private static readonly Regex RegexPrefixoAula =
        new(@"^\s*aula[\s_\-]*\d*", RegexOptions.IgnoreCase);

    // Componentes (controles) da janela
    private readonly Button btnEscolherPasta;
    private readonly Label lblPasta;
    private readonly Label lblPrefixo;
    private readonly TextBox txtPrefixo;
    private readonly Button btnPreVisualizar;
    private readonly Button btnRenomear;
    private readonly ListView lstVideos;
    private readonly Label lblResumo;

    // Estado do programa
    private string? pastaAtual;                                   // pasta escolhida
    private List<FileInfo> videosAtuais = new();                  // vídeos que estão na tabela
    private List<(FileInfo Arquivo, string NovoNome)> planoRenomear = new(); // o que vai ser renomeado

    public Form1()
    {
        InitializeComponent();

        // ===== Configuração da janela =====
        Text = "Organizador de Videoaulas";
        Size = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);

        // ===== Linha 1: escolher pasta =====
        btnEscolherPasta = new Button
        {
            Text = "Escolher pasta...",
            Location = new Point(15, 15),
            Size = new Size(160, 35)
        };
        btnEscolherPasta.Click += BtnEscolherPasta_Click;

        lblPasta = new Label
        {
            Text = "Nenhuma pasta selecionada",
            Location = new Point(190, 23),
            AutoSize = true,
            ForeColor = Color.DimGray
        };

        // ===== Linha 2: prefixo + botões de renomear =====
        lblPrefixo = new Label
        {
            Text = "Prefixo:",
            Location = new Point(15, 68),
            AutoSize = true
        };

        txtPrefixo = new TextBox
        {
            Text = "Aula",
            Location = new Point(80, 64),
            Size = new Size(120, 30)
        };
        // Se mudar o prefixo, precisa pré-visualizar de novo antes de renomear
        txtPrefixo.TextChanged += (sender, e) => btnRenomear!.Enabled = false;

        btnPreVisualizar = new Button
        {
            Text = "Pré-visualizar",
            Location = new Point(215, 60),
            Size = new Size(150, 35)
        };
        btnPreVisualizar.Click += BtnPreVisualizar_Click;

        btnRenomear = new Button
        {
            Text = "Renomear arquivos",
            Location = new Point(375, 60),
            Size = new Size(170, 35),
            Enabled = false // só libera depois da pré-visualização
        };
        btnRenomear.Click += BtnRenomear_Click;

        // ===== Tabela de vídeos =====
        lstVideos = new ListView
        {
            Location = new Point(15, 110),
            Size = new Size(855, 390),
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                     AnchorStyles.Left | AnchorStyles.Right
        };
        lstVideos.Columns.Add("Nome atual", 270);
        lstVideos.Columns.Add("Novo nome", 270);
        lstVideos.Columns.Add("Tamanho", 90, HorizontalAlignment.Right);
        lstVideos.Columns.Add("Modificado em", 140);
        lstVideos.Columns.Add("Tipo", 60);

        // ===== Resumo no rodapé =====
        lblResumo = new Label
        {
            Text = "",
            Location = new Point(15, 515),
            AutoSize = true,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };

        // Coloca todos os controles dentro da janela
        Controls.Add(btnEscolherPasta);
        Controls.Add(lblPasta);
        Controls.Add(lblPrefixo);
        Controls.Add(txtPrefixo);
        Controls.Add(btnPreVisualizar);
        Controls.Add(btnRenomear);
        Controls.Add(lstVideos);
        Controls.Add(lblResumo);
    }

    // ================= EVENTOS DOS BOTÕES =================

    private void BtnEscolherPasta_Click(object? sender, EventArgs e)
    {
        using var dialogo = new FolderBrowserDialog
        {
            Description = "Escolha a pasta com as videoaulas",
            UseDescriptionForTitle = true
        };

        if (dialogo.ShowDialog() != DialogResult.OK)
            return;

        pastaAtual = dialogo.SelectedPath;
        lblPasta.Text = pastaAtual;
        CarregarVideos(pastaAtual);
    }

    private void BtnPreVisualizar_Click(object? sender, EventArgs e)
    {
        if (videosAtuais.Count == 0)
        {
            MessageBox.Show("Escolha uma pasta com vídeos primeiro.", "Aviso",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        GerarPlanoRenomear();
        btnRenomear.Enabled = planoRenomear.Count > 0;
    }

    private void BtnRenomear_Click(object? sender, EventArgs e)
    {
        if (planoRenomear.Count == 0 || pastaAtual == null)
            return;

        var resposta = MessageBox.Show(
            $"Renomear {planoRenomear.Count} arquivo(s)?\n\nConfira a coluna \"Novo nome\" antes de continuar.",
            "Confirmar",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (resposta != DialogResult.Yes)
            return;

        int renomeados = 0;
        var erros = new List<string>();

        foreach (var (arquivo, novoNome) in planoRenomear)
        {
            // Já está com o nome certo: não faz nada
            if (arquivo.Name == novoNome)
                continue;

            string destino = Path.Combine(pastaAtual, novoNome);
            bool soMudaMaiuscula = string.Equals(arquivo.Name, novoNome, StringComparison.OrdinalIgnoreCase);

            // Já existe OUTRO arquivo com esse nome: pula para não apagar nada
            if (File.Exists(destino) && !soMudaMaiuscula)
            {
                erros.Add($"{arquivo.Name} → já existe um arquivo chamado {novoNome}");
                continue;
            }

            try
            {
                if (soMudaMaiuscula)
                {
                    // O Windows não diferencia maiúsculas: renomeia em 2 etapas
                    string temporario = destino + ".tmp";
                    arquivo.MoveTo(temporario);
                    arquivo.MoveTo(destino);
                }
                else
                {
                    arquivo.MoveTo(destino);
                }
                renomeados++;
            }
            catch (Exception erro)
            {
                erros.Add($"{arquivo.Name} → {erro.Message}");
            }
        }

        string mensagem = $"{renomeados} arquivo(s) renomeado(s).";
        if (erros.Count > 0)
            mensagem += $"\n\nNão foi possível renomear:\n{string.Join("\n", erros)}";

        MessageBox.Show(mensagem, "Concluído", MessageBoxButtons.OK,
            erros.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

        // Recarrega a tabela para mostrar os nomes novos
        CarregarVideos(pastaAtual);
    }

    // ================= LÓGICA =================

    // Lê a pasta e preenche a tabela com os vídeos encontrados
    private void CarregarVideos(string caminho)
    {
        lstVideos.Items.Clear();
        planoRenomear.Clear();
        btnRenomear.Enabled = false;

        try
        {
            var pasta = new DirectoryInfo(caminho);

            videosAtuais = pasta.GetFiles()
                .Where(arquivo => ExtensoesVideo.Contains(arquivo.Extension.ToLowerInvariant()))
                .OrderBy(arquivo => arquivo.Name)
                .ToList();

            foreach (var video in videosAtuais)
            {
                var item = new ListViewItem(video.Name);
                item.SubItems.Add("");  // "Novo nome" fica vazio até pré-visualizar
                item.SubItems.Add(FormatarTamanho(video.Length));
                item.SubItems.Add(video.LastWriteTime.ToString("dd/MM/yyyy HH:mm"));
                item.SubItems.Add(video.Extension.TrimStart('.').ToUpper());
                lstVideos.Items.Add(item);
            }

            long totalBytes = videosAtuais.Sum(video => video.Length);
            lblResumo.Text = $"{videosAtuais.Count} vídeo(s) encontrado(s) · {FormatarTamanho(totalBytes)} no total";
        }
        catch (Exception erro)
        {
            videosAtuais = new List<FileInfo>();
            MessageBox.Show($"Não foi possível ler a pasta:\n{erro.Message}", "Erro",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // Calcula o novo nome de cada vídeo e mostra na coluna "Novo nome"
    private void GerarPlanoRenomear()
    {
        string prefixo = SomenteLetrasENumeros(txtPrefixo.Text);
        if (prefixo.Length == 0)
            prefixo = "Aula";

        // 1) Quem já tem número de aula no nome mantém o número
        var numeroDoArquivo = new Dictionary<FileInfo, int>();
        var numerosUsados = new HashSet<int>();

        foreach (var video in videosAtuais)
        {
            var resultado = RegexNumeroAula.Match(Path.GetFileNameWithoutExtension(video.Name));
            if (resultado.Success &&
                int.TryParse(resultado.Groups[1].Value, out int numero) &&
                numerosUsados.Add(numero)) // Add retorna false se o número já foi usado
            {
                numeroDoArquivo[video] = numero;
            }
        }

        // 2) Quem não tem número ganha o próximo livre
        int proximo = numerosUsados.Count > 0 ? numerosUsados.Max() + 1 : 1;
        foreach (var video in videosAtuais)
        {
            if (!numeroDoArquivo.ContainsKey(video))
                numeroDoArquivo[video] = proximo++;
        }

        // 3) Monta o nome final: Prefixo + número com 2 dígitos + _ + Título + extensão
        planoRenomear = videosAtuais
            .Select(video => (video,
                $"{prefixo}{numeroDoArquivo[video]:00}_{ExtrairTitulo(video)}{video.Extension.ToLowerInvariant()}"))
            .ToList();

        // 4) Mostra na tabela (a ordem da tabela é a mesma de videosAtuais)
        for (int i = 0; i < planoRenomear.Count; i++)
        {
            var item = lstVideos.Items[i];
            item.SubItems[1].Text = planoRenomear[i].NovoNome;
        }
    }

    // "aula 1 introducao" → "Introducao" | "video final" → "VideoFinal"
    private static string ExtrairTitulo(FileInfo arquivo)
    {
        string nome = Path.GetFileNameWithoutExtension(arquivo.Name);
        nome = RegexPrefixoAula.Replace(nome, "");

        // Quebra em palavras (tudo que não é letra/número separa) e capitaliza cada uma
        var palavras = Regex.Split(nome, @"[^\p{L}\p{N}]+")
            .Where(palavra => palavra.Length > 0)
            .Select(palavra => char.ToUpper(palavra[0]) + palavra.Substring(1).ToLower());

        string titulo = string.Concat(palavras);
        return titulo.Length > 0 ? titulo : "SemTitulo";
    }

    // Remove espaços e símbolos do prefixo digitado
    private static string SomenteLetrasENumeros(string texto) =>
        string.Concat(texto.Where(char.IsLetterOrDigit));

    // Transforma bytes em texto legível: 1536 → "1,5 KB"
    private static string FormatarTamanho(long bytes)
    {
        string[] unidades = { "B", "KB", "MB", "GB", "TB" };
        double tamanho = bytes;
        int indice = 0;

        while (tamanho >= 1024 && indice < unidades.Length - 1)
        {
            tamanho /= 1024;
            indice++;
        }

        return $"{tamanho:0.##} {unidades[indice]}";
    }
}