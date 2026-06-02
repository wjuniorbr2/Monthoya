using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public static class RentalManagementMediaDeletionExtensions
{
    public static async Task DeleteImovelImagemRecordAsync(
        this IRentalManagementService rentalManagementService,
        Guid imagemId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rentalManagementService);

        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var dbContext = TryGetRentalManagementDbContext(rentalManagementService)
            ?? throw new InvalidOperationException("Não foi possível acessar o banco de dados para remover a mídia.");

        var imagem = await dbContext.ImovelImagens.SingleOrDefaultAsync(x => x.Id == imagemId, cancellationToken);
        if (imagem is null)
        {
            return;
        }

        dbContext.ImovelImagens.Remove(imagem);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static MonthoyaDbContext? TryGetRentalManagementDbContext(IRentalManagementService rentalManagementService)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        return rentalManagementService
            .GetType()
            .GetFields(flags)
            .Select(field => field.GetValue(rentalManagementService))
            .OfType<MonthoyaDbContext>()
            .FirstOrDefault();
    }
}
