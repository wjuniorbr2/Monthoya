namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private sealed class PessoasPageState : IShellPageState
    {
        public string SearchText { get; init; } = string.Empty;
        public string StatusFilter { get; init; } = "ativo";
        public Guid? SelectedPessoaId { get; init; }
        public bool IsEditing { get; init; } = true;
        public bool IsNew { get; init; } = true;
        public string DocumentoTipo { get; init; } = "cpf";
        public string DocumentoDono { get; init; } = string.Empty;
        public string DocumentoNome { get; init; } = string.Empty;
        public string DocumentoArquivo { get; init; } = string.Empty;
        public DateOnly? DocumentoValidade { get; init; }
        public string DocumentoObservacoes { get; init; } = string.Empty;



        public static PessoasPageState Default { get; } = new();
    }
}

