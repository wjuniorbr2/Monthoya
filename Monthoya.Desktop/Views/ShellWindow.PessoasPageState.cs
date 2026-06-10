namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private sealed record PessoasPageState(
        string SearchText,
        string StatusFilter,
        Guid? SelectedPessoaId,
        bool IsEditing,
        bool IsNew,
        string DocumentoTipo,
        string DocumentoDono,
        string DocumentoNome,
        string DocumentoArquivo,
        DateOnly? DocumentoValidade,
        string DocumentoObservacoes) : IShellPageState
    {
        public static PessoasPageState Default { get; } = new(
            "",
            "ativo",
            null,
            true,
            true,
            "cpf",
            "",
            "",
            "",
            null,
            "");
    }
}
