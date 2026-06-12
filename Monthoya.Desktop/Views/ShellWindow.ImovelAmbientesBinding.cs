using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private void SetRealImovelAmbienteFields(CreateImovelRequest dados)
    {
        ImovelSalasBox.Text = dados.Salas?.ToString() ?? string.Empty;
        ImovelCozinhasBox.Text = dados.Cozinhas?.ToString() ?? string.Empty;
        ImovelCopasBox.Text = dados.Copas?.ToString() ?? string.Empty;
        ImovelDespensasBox.Text = dados.Despensas?.ToString() ?? string.Empty;
        ImovelLavanderiasBox.Text = dados.Lavanderias?.ToString() ?? string.Empty;
        ImovelAreasServicoBox.Text = dados.AreasServico?.ToString() ?? string.Empty;
        ImovelLavabosBox.Text = dados.Lavabos?.ToString() ?? string.Empty;
        ImovelSacadasBox.Text = dados.Sacadas?.ToString() ?? string.Empty;
        ImovelChurrasqueirasBox.Text = dados.Churrasqueiras?.ToString() ?? string.Empty;
        ImovelPiscinasBox.Text = dados.Piscinas?.ToString() ?? string.Empty;
        ImovelQuintaisBox.Text = dados.Quintais?.ToString() ?? string.Empty;
        ImovelHallsEntradaBox.Text = dados.HallsEntrada?.ToString() ?? string.Empty;
        ImovelEstendaisBox.Text = dados.Estendais?.ToString() ?? string.Empty;
    }

    private void ClearRealImovelAmbienteFields()
    {
        ImovelSalasBox.Clear();
        ImovelCozinhasBox.Clear();
        ImovelCopasBox.Clear();
        ImovelDespensasBox.Clear();
        ImovelLavanderiasBox.Clear();
        ImovelAreasServicoBox.Clear();
        ImovelLavabosBox.Clear();
        ImovelSacadasBox.Clear();
        ImovelChurrasqueirasBox.Clear();
        ImovelPiscinasBox.Clear();
        ImovelQuintaisBox.Clear();
        ImovelHallsEntradaBox.Clear();
        ImovelEstendaisBox.Clear();
    }
}
