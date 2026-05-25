using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Security;
using Monthoya.Core.Services;
using Monthoya.Data;
using Monthoya.Data.RentalManagement;
using Monthoya.Data.Users;

namespace Monthoya.Tests;

public sealed class AuthAndUserTests
{
    [Fact]
    public async Task CreateFirstAdmin_HashesPassword_AndAllowsLogin()
    {
        await using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher<AppUser>();
        var userService = new UserService(dbContext, passwordHasher);
        var authService = new AuthService(dbContext, userService, passwordHasher);

        var setupResult = await authService.CreateFirstAdminAsync(
            new CreateUserRequest("Admin", "admin", "admin@monthoya.local", "strongpass123", UserRole.Usuario));

        Assert.True(setupResult.Succeeded);
        Assert.True(await userService.HasAnyUsersAsync());

        var user = await dbContext.Users.SingleAsync();
        Assert.NotEqual("strongpass123", user.PasswordHash);
        Assert.Equal(UserRole.Administrador, user.Role);
        Assert.Equal(RolePermissions.AdministratorAccess, user.Access);

        var loginResult = await authService.SignInAsync("admin", "strongpass123");

        Assert.True(loginResult.Succeeded);
        Assert.NotNull(loginResult.User);
        Assert.Equal(RolePermissions.AdministratorAccess, loginResult.User.Access);
    }

    [Fact]
    public async Task SignIn_ReturnsSafeError_ForWrongPassword()
    {
        await using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher<AppUser>();
        var userService = new UserService(dbContext, passwordHasher);
        var authService = new AuthService(dbContext, userService, passwordHasher);

        await authService.CreateFirstAdminAsync(
            new CreateUserRequest("Admin", "admin", "admin@monthoya.local", "strongpass123", UserRole.Administrador));

        var result = await authService.SignInAsync("admin", "wrong-password");

        Assert.False(result.Succeeded);
        Assert.Null(result.User);
        Assert.Equal("Login ou senha inválidos.", result.ErrorMessage);
    }

    [Fact]
    public void RolePermissions_MatchExpectedAccess()
    {
        Assert.False(RolePermissions.CanManageUsers(UserRole.Usuario));
        Assert.True(RolePermissions.CanManageUsers(UserRole.Usuario, UserAccess.UserManagement));
        Assert.True(RolePermissions.CanManageUsers(UserRole.Administrador));
        Assert.True(RolePermissions.CanManageUsers(UserRole.Desenvolvedor));

        Assert.False(RolePermissions.CanAccessDiagnostics(UserRole.Usuario));
        Assert.False(RolePermissions.CanAccessDiagnostics(UserRole.Administrador));
        Assert.True(RolePermissions.CanAccessDiagnostics(UserRole.Desenvolvedor));

        Assert.True(RolePermissions.CanAccess(UserRole.Usuario, UserAccess.None, UserAccess.Dashboard));
        Assert.False(RolePermissions.CanAccess(UserRole.Usuario, UserAccess.None, UserAccess.Documents));
        Assert.True(RolePermissions.CanAccess(UserRole.Usuario, UserAccess.UserManagement, UserAccess.UserManagement));
        Assert.True(RolePermissions.CanAccess(UserRole.Administrador, UserAccess.None, UserAccess.UserManagement));
        Assert.True(RolePermissions.CanAccess(UserRole.Desenvolvedor, UserAccess.None, UserAccess.Diagnostics));
    }

    [Fact]
    public async Task CreateUser_StoresOnlyCurrentNormalUserAccess()
    {
        await using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher<AppUser>();
        var userService = new UserService(dbContext, passwordHasher);

        var user = await userService.CreateUserAsync(
            new CreateUserRequest(
                "Atendente",
                "atendente",
                "atendente@monthoya.local",
                "strongpass123",
                UserRole.Usuario,
                UserAccess.Dashboard | UserAccess.Properties | UserAccess.UserManagement));

        Assert.Equal(UserAccess.UserManagement, user.Access);
        Assert.True(RolePermissions.CanAccess(user.Role, user.Access, UserAccess.Dashboard));
        Assert.True(RolePermissions.CanAccess(user.Role, user.Access, UserAccess.UserManagement));
        Assert.False(RolePermissions.CanAccess(user.Role, user.Access, UserAccess.Properties));
        Assert.False(RolePermissions.CanAccess(user.Role, user.Access, UserAccess.Financial));
    }

    [Fact]
    public async Task CreateUser_RejectsShortLogin()
    {
        await using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher<AppUser>();
        var userService = new UserService(dbContext, passwordHasher);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            userService.CreateUserAsync(
                new CreateUserRequest(
                    "Teste",
                    "1",
                    "teste@monthoya.local",
                    "strongpass123",
                    UserRole.Usuario,
                    UserAccess.Dashboard)));

        Assert.Equal("Informe um login com pelo menos 3 caracteres.", exception.Message);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("abcdefgh")]
    [InlineData(" abc ")]
    public async Task CreateUser_AcceptsLoginWithAtLeastThreeTrimmedCharacters(string loginName)
    {
        await using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher<AppUser>();
        var userService = new UserService(dbContext, passwordHasher);

        var user = await userService.CreateUserAsync(
            new CreateUserRequest(
                "Teste",
                loginName,
                "teste@monthoya.local",
                "strongpass123",
                UserRole.Usuario,
                UserAccess.Dashboard));

        Assert.Equal(loginName.Trim(), user.LoginName);
    }

    [Fact]
    public async Task CreateUser_RejectsWeakPassword()
    {
        await using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher<AppUser>();
        var userService = new UserService(dbContext, passwordHasher);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            userService.CreateUserAsync(
                new CreateUserRequest(
                    "Teste",
                    "teste",
                    "teste@monthoya.local",
                    "1234567",
                    UserRole.Usuario,
                    UserAccess.Dashboard)));

        Assert.Equal("A senha deve ter pelo menos 8 caracteres.", exception.Message);
    }

    [Fact]
    public async Task CreateUser_RejectsLoginWithSpaces()
    {
        await using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher<AppUser>();
        var userService = new UserService(dbContext, passwordHasher);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            userService.CreateUserAsync(
                new CreateUserRequest(
                    "Teste",
                    "teste um",
                    "teste@monthoya.local",
                    "strongpass123",
                    UserRole.Usuario,
                    UserAccess.Dashboard)));

        Assert.Equal("O login não pode conter espaços.", exception.Message);
    }

    [Fact]
    public async Task CreateUser_RejectsInvalidEmail()
    {
        await using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher<AppUser>();
        var userService = new UserService(dbContext, passwordHasher);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            userService.CreateUserAsync(
                new CreateUserRequest(
                    "Teste",
                    "teste",
                    "email-sem-arroba",
                    "strongpass123",
                    UserRole.Usuario,
                    UserAccess.Dashboard)));

        Assert.Equal("Informe um e-mail válido.", exception.Message);
    }

    [Fact]
    public async Task CreateUser_RejectsDuplicateLoginAndEmail()
    {
        await using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher<AppUser>();
        var userService = new UserService(dbContext, passwordHasher);

        await userService.CreateUserAsync(
            new CreateUserRequest(
                "Primeiro",
                "junior",
                "junior@monthoya.local",
                "strongpass123",
                UserRole.Usuario,
                UserAccess.Dashboard));

        var duplicateLogin = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            userService.CreateUserAsync(
                new CreateUserRequest(
                    "Outro",
                    "JUNIOR",
                    "outro@monthoya.local",
                    "strongpass123",
                    UserRole.Usuario,
                    UserAccess.Dashboard)));

        var duplicateEmail = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            userService.CreateUserAsync(
                new CreateUserRequest(
                    "Outro",
                    "outro",
                    "JUNIOR@monthoya.local",
                    "strongpass123",
                    UserRole.Usuario,
                    UserAccess.Dashboard)));

        Assert.Equal("Já existe um usuário com este login.", duplicateLogin.Message);
        Assert.Equal("Já existe um usuário com este e-mail.", duplicateEmail.Message);
    }

    [Fact]
    public async Task SignIn_RejectsInactiveUserWithSafeError()
    {
        await using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher<AppUser>();
        var userService = new UserService(dbContext, passwordHasher);
        var authService = new AuthService(dbContext, userService, passwordHasher);

        var user = await userService.CreateUserAsync(
            new CreateUserRequest(
                "Atendente",
                "atendente",
                "atendente@monthoya.local",
                "strongpass123",
                UserRole.Usuario,
                UserAccess.Dashboard));

        await userService.SetUserActiveAsync(user.Id, false);

        var result = await authService.SignInAsync("atendente", "strongpass123");

        Assert.False(result.Succeeded);
        Assert.Null(result.User);
        Assert.Equal("Login ou senha inválidos.", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateFirstAdmin_RejectsSecondSetup()
    {
        await using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher<AppUser>();
        var userService = new UserService(dbContext, passwordHasher);
        var authService = new AuthService(dbContext, userService, passwordHasher);

        await authService.CreateFirstAdminAsync(
            new CreateUserRequest("Admin", "admin", "admin@monthoya.local", "strongpass123", UserRole.Administrador));

        var result = await authService.CreateFirstAdminAsync(
            new CreateUserRequest("Outro", "outro", "outro@monthoya.local", "strongpass123", UserRole.Administrador));

        Assert.False(result.Succeeded);
        Assert.Null(result.User);
        Assert.Equal("A configuração inicial já foi concluída.", result.ErrorMessage);
    }

    [Fact]
    public async Task Pessoa_CanHaveMultipleRolesWithoutDuplicateRecords()
    {
        await using var dbContext = CreateDbContext();
        var rentalService = new RentalManagementService(dbContext);

        var pessoa = await rentalService.CreatePessoaAsync(
            new CreatePessoaRequest(
                TipoPessoa.Fisica,
                "João da Silva",
                "(44) 99999-0000",
                "joao@monthoya.local",
                "12345678901",
                [PessoaRoleTipo.Proprietario, PessoaRoleTipo.Locatario, PessoaRoleTipo.Fiador],
                null));

        var pessoas = await rentalService.GetPessoasAsync();

        Assert.Single(pessoas);
        Assert.Equal(pessoa.Id, pessoas[0].Id);
        Assert.Contains("Proprietário", pessoas[0].Roles);
        Assert.Contains("Locatário", pessoas[0].Roles);
        Assert.Contains("Fiador", pessoas[0].Roles);
    }

    [Fact]
    public async Task PessoaJuridica_CanHaveMultipleRoles()
    {
        await using var dbContext = CreateDbContext();
        var rentalService = new RentalManagementService(dbContext);

        var pessoa = await rentalService.CreatePessoaAsync(
            new CreatePessoaRequest(
                TipoPessoa.Juridica,
                "Empresa Teste Ltda",
                "(44) 3222-0000",
                "empresa@monthoya.local",
                "12345678000190",
                [PessoaRoleTipo.Proprietario, PessoaRoleTipo.Fiador],
                "Cadastro preliminar para teste."));

        var pessoas = await rentalService.GetPessoasAsync();

        Assert.Single(pessoas);
        Assert.Equal(pessoa.Id, pessoas[0].Id);
        Assert.Equal("Jurídica", pessoas[0].Tipo);
        Assert.Contains("Proprietário", pessoas[0].Roles);
        Assert.Contains("Fiador", pessoas[0].Roles);
    }

    [Fact]
    public async Task Pessoa_StoresDetailedClientRegistryFieldsAndScannedDocumentMetadata()
    {
        await using var dbContext = CreateDbContext();
        var rentalService = new RentalManagementService(dbContext);

        var pessoa = await rentalService.CreatePessoaAsync(
            new CreatePessoaRequest(
                TipoPessoa: TipoPessoa.Fisica,
                NomeDisplay: "Ana Cliente",
                Telefone: "(44) 99999-1111",
                Email: "ana@monthoya.local",
                Documento: "12345678901",
                Roles: [PessoaRoleTipo.Locatario, PessoaRoleTipo.Fiador],
                Observacoes: "Cadastro completo.",
                Endereco: "Rua das Flores, 10",
                EstadoCivil: "Casada",
                Nacionalidade: "Brasileira",
                DataNascimento: new DateOnly(1990, 5, 20),
                Rg: "1234567",
                Profissao: "Professora",
                OndeTrabalha: "Escola Municipal",
                DadosBancarios: "Banco teste"));

        await rentalService.CreatePessoaDocumentoAsync(
            new CreatePessoaDocumentoRequest(
                pessoa.Id,
                "comprovante_residencia",
                "Comprovante de residência",
                @"C:\Monthoya\documentos\ana-comprovante.pdf",
                "application/pdf",
                null,
                "Arquivo digitalizado informado pelo atendimento."));

        var pessoaFisica = await dbContext.PessoasFisicas.SingleAsync();
        var documentos = await rentalService.GetPessoaDocumentosAsync(pessoa.Id);

        Assert.Equal("Rua das Flores, 10", pessoaFisica.Endereco);
        Assert.Equal("Casada", pessoaFisica.EstadoCivil);
        Assert.Equal("Professora", pessoaFisica.Profissao);
        Assert.Single(documentos);
        Assert.Equal("Comprovante de residência", documentos[0].Tipo);
        Assert.Contains("ana-comprovante.pdf", documentos[0].StoragePath);
    }

    [Fact]
    public async Task Imovel_RequiresOwnerAndAutoAddsOwnerRole()
    {
        await using var dbContext = CreateDbContext();
        var rentalService = new RentalManagementService(dbContext);

        var pessoa = await rentalService.CreatePessoaAsync(
            new CreatePessoaRequest(
                TipoPessoa.Fisica,
                "Maria Locatária",
                null,
                null,
                null,
                [PessoaRoleTipo.Locatario],
                null));

        var imovel = await rentalService.CreateImovelAsync(
            new CreateImovelRequest(
                pessoa.Id,
                "Rua Getúlio Vargas",
                "668",
                "Centro",
                "Paranavaí",
                "PR",
                1500m,
                ImovelFinalidade.Locacao,
                null));

        var pessoas = await rentalService.GetPessoasAsync();
        var imoveis = await rentalService.GetImoveisAsync();

        Assert.Equal(imovel.Id, imoveis.Single().Id);
        Assert.Contains("Proprietário", pessoas.Single().Roles);
    }

    [Fact]
    public async Task Imovel_StoresDetailedRegistryFields()
    {
        await using var dbContext = CreateDbContext();
        var rentalService = new RentalManagementService(dbContext);

        var proprietario = await rentalService.CreatePessoaAsync(
            new CreatePessoaRequest(
                TipoPessoa.Fisica,
                "Proprietário Teste",
                null,
                null,
                null,
                [PessoaRoleTipo.Proprietario],
                null));

        await rentalService.CreateImovelAsync(
            new CreateImovelRequest(
                ProprietarioId: proprietario.Id,
                Rua: "Rua Souza Naves",
                Numero: "100",
                Bairro: "Centro",
                Cidade: "Paranavaí",
                Estado: "PR",
                ValorAluguel: 2000m,
                Finalidade: ImovelFinalidade.Ambos,
                Observacoes: "Imóvel com dados completos.",
                Complemento: "Sala 2",
                Cep: "87702-000",
                SaneparMatricula: "SAN123",
                CopelMatricula: "COP456",
                IptuMatricula: "IPTU789",
                TipoImovel: "Casa",
                Descricao: "Casa residencial.",
                ValorVenda: 450000m,
                Latitude: -23.0816m,
                Longitude: -52.4617m));

        var imovel = await dbContext.Imoveis.SingleAsync();

        Assert.Equal("Sala 2", imovel.Complemento);
        Assert.Equal("SAN123", imovel.SaneparMatricula);
        Assert.Equal("Casa", imovel.TipoImovel);
        Assert.Equal(450000m, imovel.ValorVenda);
        Assert.Equal(-23.0816m, imovel.Latitude);
    }

    [Fact]
    public async Task Imovel_RejectsMissingOwner()
    {
        await using var dbContext = CreateDbContext();
        var rentalService = new RentalManagementService(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            rentalService.CreateImovelAsync(
                new CreateImovelRequest(
                    Guid.Empty,
                    "Rua Getúlio Vargas",
                    "668",
                    "Centro",
                    "Paranavaí",
                    "PR",
                    1500m,
                    ImovelFinalidade.Locacao,
                    null)));

        Assert.Equal("Selecione um proprietário.", exception.Message);
    }

    [Fact]
    public async Task PlaceholderProviders_ReportPendingConfiguration()
    {
        var boletoProvider = new LocalBoletoProvider();
        var nfseProvider = new ManualPortalNfseProvider();

        var boletoResult = await boletoProvider.GenerateBoletoAsync(new Boleto
        {
            Valor = 1500m,
            DataVencimento = new DateOnly(2026, 6, 10)
        });

        var nfseResult = await nfseProvider.EmitirNotaFiscalAsync(new NotaFiscal
        {
            ValorServico = 150m,
            Provider = "manual_portal"
        });

        Assert.False(boletoResult.Succeeded);
        Assert.Equal("Integração bancária ainda não configurada.", boletoResult.Message);
        Assert.False(nfseResult.Succeeded);
        Assert.Equal("Integração automática com NFS-e ainda não configurada. Use o fluxo manual/semi-manual.", nfseResult.Message);
    }

    private static MonthoyaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MonthoyaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MonthoyaDbContext(options);
    }
}
