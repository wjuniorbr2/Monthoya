namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private void PrimeChavesBoardCodeCacheFromLoadedImoveis()
    {
        foreach (var imovel in _imoveis)
        {
            if (string.IsNullOrWhiteSpace(imovel.ChaveCodigo))
            {
                continue;
            }

            _chavesBoardCodeByImovelId[imovel.Id] = imovel.ChaveCodigo;
        }
    }
}
