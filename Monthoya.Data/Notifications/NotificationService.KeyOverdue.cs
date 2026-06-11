using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.Notifications;

public sealed partial class NotificationService
{
    private async Task<IReadOnlyList<Guid>> GetKeyOverdueRecipientIdsAsync(CancellationToken cancellationToken)
    {
        var adminUsers = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.IsActive && (x.Role == UserRole.Administrador || x.Role == UserRole.Desenvolvedor))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (adminUsers.Count > 0)
        {
            return adminUsers;
        }

        return await dbContext.Users
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    private static string BuildKeyOverdueBody(ImovelChaveMovimento movimento, DateTimeOffset now)
    {
        var imovel = movimento.Imovel;
        var endereco = imovel is null
            ? "-"
            : $"{imovel.Rua}, {imovel.Numero} - {imovel.Bairro}, {imovel.Cidade}/{imovel.Estado}";
        var previsao = movimento.PrevisaoDevolucaoEm;
        var atraso = previsao.HasValue ? now - previsao.Value : TimeSpan.Zero;
        var atrasoTexto = atraso.TotalDays >= 1
            ? $"{Math.Floor(atraso.TotalDays)} dia(s)"
            : $"{Math.Max(1, Math.Floor(atraso.TotalHours))} hora(s)";

        var lines = new List<string>
        {
            $"Código da chave: {movimento.ChaveCodigo ?? imovel?.ChaveCodigo ?? "-"}",
            $"Imóvel: {endereco}",
            $"Proprietário: {imovel?.Proprietario?.NomeDisplay ?? "-"}",
            $"Retirado por: {movimento.RetiradoPorNome ?? "-"}",
            $"Telefone: {movimento.RetiradoPorTelefone ?? "-"}",
            $"Retirado em: {FormatDateTime(movimento.RetiradoEm)}",
            $"Previsão de devolução: {FormatDateTime(previsao)}",
            $"Tempo em atraso: {atrasoTexto}",
            $"Motivo: {movimento.Motivo ?? "-"}"
        };

        if (!string.IsNullOrWhiteSpace(movimento.Observacoes))
        {
            lines.Add($"Observações: {movimento.Observacoes}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}


