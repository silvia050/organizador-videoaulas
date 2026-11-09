# 🎬 Organizador de Videoaulas

Aplicativo desktop em **C# / .NET 10 (WinForms)** para organizar arquivos de videoaulas: lista os vídeos de uma pasta e renomeia tudo em lote seguindo um padrão, com pré-visualização e confirmação antes de alterar qualquer arquivo.

Projeto criado para praticar desenvolvimento desktop com C# e automação de processos com arquivos de vídeo.

## ✨ Funcionalidades

- 📂 **Escolher pasta** e listar automaticamente os vídeos (`.mp4`, `.mkv`, `.avi`, `.mov`, `.wmv`, `.webm`)
- 📊 Tabela com **nome, tamanho, data de modificação e tipo**, além do total de vídeos e espaço ocupado
- ✏️ **Renomeação em lote** no padrão `Prefixo + Número + _ + Título` (ex.: `Aula01_Introducao.mp4`)
- 👀 **Pré-visualização** do novo nome antes de aplicar
- ✅ **Confirmação** antes de renomear e resumo do resultado
- 🛡️ Tratamento de erros: arquivos com nome já existente são ignorados, sem sobrescrever nada

## 🧠 Regras de negócio

| Antes | Depois | Regra |
|---|---|---|
| `aula 1 introducao.mp4` | `Aula01_Introducao.mp4` | Mantém o número da aula e usa 2 dígitos |
| `Aula2-variaveis.mp4` | `Aula02_Variaveis.mp4` | Remove símbolos e capitaliza as palavras |
| `aula_03 FUNCOES.mkv` | `Aula03_Funcoes.mkv` | Padroniza maiúsculas e minúsculas |
| `video final.avi` | `Aula04_VideoFinal.avi` | Sem número: recebe o próximo número livre |

- Números de aula repetidos não geram nomes duplicados
- Mudanças apenas de maiúscula/minúscula são feitas em duas etapas (o Windows não diferencia maiúsculas em nomes de arquivo)

## 🛠️ Tecnologias

- C# 14 / .NET 10
- Windows Forms (WinForms)
- LINQ, Regex, `System.IO` (`DirectoryInfo`, `FileInfo`)
- Git e GitHub

## ▶️ Como executar

Pré-requisito: [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0) no Windows.

```bash
git clone https://github.com/silvia050/organizador-videoaulas.git
cd organizador-videoaulas
dotnet run
```

## 🚀 Próximos passos

- [ ] Extrair o áudio dos vídeos com **FFmpeg**
- [ ] Mostrar a **duração** de cada vídeo
- [ ] Salvar o **histórico** de renomeações em um banco **SQLite** (com opção de desfazer)

## 👩‍💻 Autora

**Silvia Moura Barbosa**
[LinkedIn](https://www.linkedin.com/in/silvia-moura-barbosa) · [GitHub](https://github.com/silvia050)
