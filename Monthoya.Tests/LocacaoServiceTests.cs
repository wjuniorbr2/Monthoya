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

    [Fact]
    public async Task CreateLocacao_CannotUseSamePersonAsOwnerAndTenant()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedRentalActorsAsync(dbContext);
        var service = new RentalManagementService(dbContext);

        var request = CreateRequest(seed) with
        {
            Partes =
            [
                new LocacaoParteRequest(seed.OwnerId, TipoParteLocacao.Proprietario, PercentualParticipacao: 100m, RecebeRepasse: true),
                new LocacaoParteRequest(seed.OwnerId, TipoParteLocacao.Locatario, RecebeCobranca: true)
            ]
        };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateLocacaoAsync(request));
        Assert.Contains("mesma pessoa", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateLocacao_AddsMissingPessoaRolesForTenantAndFiadorOnly()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedRentalActorsWithoutRolesAsync(dbContext);
        var fiador = await AddPessoaAsync(dbContext, "Fiador Teste");
        var service = new RentalManagementService(dbContext);

        await service.CreateLocacaoAsync(CreateRequest(seed) with
        {
            Partes =
            [
                new LocacaoParteRequest(seed.OwnerId, TipoParteLocacao.Proprietario, PercentualParticipacao: 100m, RecebeRepasse: true),
                new LocacaoParteRequest(seed.TenantId, TipoParteLocacao.Locatario, RecebeCobranca: true),
                new LocacaoParteRequest(fiador.Id, TipoParteLocacao.Fiador, IsPrincipal: true, RecebeNotificacao: true)
            ]
        });

        Assert.False(await HasRoleAsync(dbContext, seed.OwnerId, PessoaRoleTipo.Proprietario));
        Assert.True(await HasRoleAsync(dbContext, seed.TenantId, PessoaRoleTipo.Locatario));
        Assert.True(await HasRoleAsync(dbContext, fiador.Id, PessoaRoleTipo.Fiador));
    }

    [Fact]
    public async Task CreateImovel_AddsMissingPessoaRoleForOwner()
    {
        await using var dbContext = CreateDbContext();
        var owner = await AddPessoaAsync(dbContext, "Novo Proprietário");
        var service = new RentalManagementService(dbContext);

        await service.CreateImovelAsync(new CreateImovelRequest(
            owner.Id,
            "Rua do Imóvel",
            "100",
            "Centro",
            "Paranavaí",
            "PR",
            1500m,
            ImovelFinalidade.Locacao,
            null));

        Assert.True(await HasRoleAsync(dbContext, owner.Id, PessoaRoleTipo.Proprietario));
    }
    [Fact]
    public async Task CreateLocacao_DoesNotDuplicateExistingPessoaRoles()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedRentalActorsAsync(dbContext);
        var service = new RentalManagementService(dbContext);

        await service.CreateLocacaoAsync(CreateRequest(seed));

        Assert.Equal(1, await CountRoleAsync(dbContext, seed.OwnerId, PessoaRoleTipo.Proprietario));
        Assert.Equal(1, await CountRoleAsync(dbContext, seed.TenantId, PessoaRoleTipo.Locatario));
    }

    [Fact]
    public async Task CreateLocacao_AcceptsMultipleTenantsAndFiadoresWithPropertyOwner()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedRentalActorsWithoutRolesAsync(dbContext);
        var secondTenant = await AddPessoaAsync(dbContext, "Locatário Dois");
        var fiador = await AddPessoaAsync(dbContext, "Fiador Teste");
        var secondFiador = await AddPessoaAsync(dbContext, "Fiador Dois");
        var service = new RentalManagementService(dbContext);

        var details = await service.CreateLocacaoAsync(CreateRequest(seed) with
        {
            Partes =
            [
                new LocacaoParteRequest(seed.OwnerId, TipoParteLocacao.Proprietario, IsPrincipal: true, PercentualParticipacao: 100m, RecebeRepasse: true),
                new LocacaoParteRequest(seed.TenantId, TipoParteLocacao.Locatario, IsPrincipal: true, RecebeCobranca: true),
                new LocacaoParteRequest(secondTenant.Id, TipoParteLocacao.Locatario, RecebeCobranca: true),
                new LocacaoParteRequest(fiador.Id, TipoParteLocacao.Fiador, IsPrincipal: true, RecebeNotificacao: true),
                new LocacaoParteRequest(secondFiador.Id, TipoParteLocacao.Fiador, RecebeNotificacao: true)
            ]
        });

        Assert.Equal(5, details.Partes.Count);
        Assert.Single(details.Partes, x => x.TipoParte == TipoParteLocacao.Proprietario);
        Assert.Equal(2, details.Partes.Count(x => x.TipoParte == TipoParteLocacao.Locatario));
        Assert.Equal(2, details.Partes.Count(x => x.TipoParte == TipoParteLocacao.Fiador));
    }
    [Fact]
    public async Task CreateLocacao_InvalidOwnerPercentagesStillFail()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedRentalActorsAsync(dbContext);
        var secondOwner = await AddPessoaAsync(dbContext, "Proprietário Dois");
        var service = new RentalManagementService(dbContext);

        var request = CreateRequest(seed) with
        {
            Partes =
            [
                new LocacaoParteRequest(seed.OwnerId, TipoParteLocacao.Proprietario, PercentualParticipacao: 60m, RecebeRepasse: true),
                new LocacaoParteRequest(secondOwner.Id, TipoParteLocacao.Proprietario, PercentualParticipacao: 60m, RecebeRepasse: true),
                new LocacaoParteRequest(seed.TenantId, TipoParteLocacao.Locatario, RecebeCobranca: true)
            ]
        };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateLocacaoAsync(request));
        Assert.Contains("100", error.Message, StringComparison.OrdinalIgnoreCase);
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

    private static async Task<RentalSeed> SeedRentalActorsWithoutRolesAsync(MonthoyaDbContext dbContext)
    {
        var owner = new Pessoa { NomeDisplay = "Proprietário Teste" };
        var tenant = new Pessoa { NomeDisplay = "Locatário Teste" };
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

    private static async Task<Pessoa> AddPessoaAsync(MonthoyaDbContext dbContext, string nome)
    {
        var pessoa = new Pessoa { NomeDisplay = nome };
        dbContext.Pessoas.Add(pessoa);
        await dbContext.SaveChangesAsync();
        return pessoa;
    }

    private static Task<bool> HasRoleAsync(MonthoyaDbContext dbContext, Guid pessoaId, PessoaRoleTipo role) =>
        dbContext.PessoaRoles.AnyAsync(x => x.PessoaId == pessoaId && x.Role == role);

    private static Task<int> CountRoleAsync(MonthoyaDbContext dbContext, Guid pessoaId, PessoaRoleTipo role) =>
        dbContext.PessoaRoles.CountAsync(x => x.PessoaId == pessoaId && x.Role == role);

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
