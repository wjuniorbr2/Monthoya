using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private async Task CancelLocacaoWithPasswordAsync(LocacaoSummary locacao)
    {
        SetModuleNotice(string.Empty);
        var confirmed = await ConfirmDestructiveActionWithPasswordAsync(
            "Cancelar locação",
            "Cancelar esta locação altera o status para Cancelada. O histórico será preservado. Deseja continuar?",
            "Cancelar locação");
        if (!confirmed)
        {
            return;
        }

        try
        {
            var details = await _rentalManagementService.GetLocacaoAsync(locacao.Id);
            var updated = await _rentalManagementService.UpdateLocacaoAsync(new UpdateLocacaoRequest(
                locacao.Id,
                details.Dados with
                {
                    Status = LocacaoStatus.Cancelada,
                    DataEncerramento = DateOnly.FromDateTime(DateTime.Today),
                    MotivoEncerramento = "Locação cancelada pelo usuário."
                }));

            await LoadGenericModuleAsync(ShellPage.Locacoes);
            RestoreDataGridSelection(ModuleGrid, updated.Summary.Id);
            ShowLocacaoDetails(updated.Summary, "Locação cancelada com sucesso.");
        }
        catch (Exception ex)
        {
            SetModuleNotice($"Não foi possível cancelar a locação. {ex.Message}");
        }
    }
}
