using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;
using Monthoya.Data;
using Monthoya.Data.RentalManagement;

namespace Monthoya.Tests;

public class LocacaoServiceTests
{
    [Fact]
    public async Task CreateLocacao_CalculatesDefaultContractFee()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedRentalActorsAsync(dbContext);
        var service = new RentalManagementService(dbContext);

        var details = await service.CreateLocacaoAsync(CreateRequest(seed));

        Assert.Equal(12m, details.Dados.TaxaAdministracaoPercentual);
        Assert.Equal(50m, details.Dados.MetaComissaoPrimeiroAluguelPercentual);
        Assert.Equal(38m, details.Dados.TaxaContratoPercentual);
    }

    [Fact]
    public async Task CreateLocacao_CalculatesCustomContractFee()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedRentalActorsAsync(dbContext);
        var service = new RentalManagementService(dbContext);

        var details = await service.CreateLocacaoAsync(CreateRequest(seed, taxaAdministracao: 10m, metaComissao: 50m));

        Assert.Equal(40m, details.Dados.TaxaContratoPercentual);
    }

    [Fact]
    public async Task CreateLocacao_DoesNotCalculateNegativeContractFee()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedRentalActorsAsync(dbContext);
        var service = new RentalManagementService(dbContext);

        var details = await service.CreateLocacaoAsync(CreateRequest(seed, taxaAdministracao: 60m, metaComissao: 50m));

        Assert.Equal(0m, details.Dados.TaxaContratoPercentual);
    }

    [Fact]
    public async Task CreateLocacao_CannotActivateWithoutTenantOrOwner()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedRentalActorsAsync(dbContext);
        var service = new RentalManagementService(dbContext);

        var request = CreateRequest(seed, status: LocacaoStatus.Ativa) with { Partes = [] };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateLocacaoAsync(request));
        Assert.Contains("proprietário", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateLocacao_CannotActivateDuplicateRentalForSameProperty()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedRentalActorsAsync(dbContext);
        var service = new RentalManagementService(dbContext);
        var activeRequest = CreateRequest(seed, status: LocacaoStatus.Ativa);

        await service.CreateLocacaoAsync(activeRequest);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateLocacaoAsync(activeRequest));
        Assert.Contains("já possui uma locação ativa", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static MonthoyaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MonthoyaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MonthoyaDbContext(options);
    }

    private static async Task<RentalSeed> SeedRentalActorsAsync(MonthoyaDbContext dbContext)
    {
        var owner = new Pessoa
        {
            NomeDisplay = "Proprietário Teste",
            Roles = [new PessoaRole { Role = PessoaRoleTipo.Proprietario }]
        };
        var tenant = new Pessoa
        {
            NomeDisplay = "Locatário Teste",
            Roles = [new PessoaRole { Role = PessoaRoleTipo.Locatario }]
        };
        var imovel = new Imovel
        {
            ProprietarioId = owner.Id,
            Proprietario = owner,
            Rua = "Rua Teste",
            Numero = "123",
            Cidade = "Paranavaí",
            Estado = "PR"
        };

        dbContext.Pessoas.AddRange(owner, tenant);
        dbContext.Imoveis.Add(imovel);
        await dbContext.SaveChangesAsync();

        return new RentalSeed(imovel.Id, owner.Id, tenant.Id);
    }

    private static CreateLocacaoRequest CreateRequest(
        RentalSeed seed,
        LocacaoStatus? status = null,
        decimal? taxaAdministracao = null,
        decimal? metaComissao = null) =>
        new(
            seed.ImovelId,
            [
                new LocacaoParteRequest(seed.OwnerId, TipoParteLocacao.Proprietario, PercentualParticipacao: 100m, RecebeRepasse: true),
                new LocacaoParteRequest(seed.TenantId, TipoParteLocacao.Locatario, RecebeCobranca: true)
            ],
            Status: status,
            DataInicioLocacao: new DateOnly(2026, 6, 1),
            ValorAluguelInicial: 1500m,
            TaxaAdministracaoPercentual: taxaAdministracao,
            MetaComissaoPrimeiroAluguelPercentual: metaComissao);

    private sealed record RentalSeed(Guid ImovelId, Guid OwnerId, Guid TenantId);
}
