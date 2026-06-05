namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private void PrimeChavesBoardCodeCacheFromLoadedImoveis()
    {
        foreach (var imovel in _imoveis)
        {
            if (_chavesBoardCodeByImovelId.ContainsKey(imovel.Id))
            {
                continue;
            }

            // Chaves already loads ImovelSummary for the grid. Use that data instead of
            // doing one extra GetImovelAsync call per property just to obtain the key code.
            _chavesBoardCodeByImovelId[imovel.Id] = string.IsNullOrWhiteSpace(imovel.ChaveCodigo)
                ? "-"
                : imovel.ChaveCodigo;
        }
    }
}
