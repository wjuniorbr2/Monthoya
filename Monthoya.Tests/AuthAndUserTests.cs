using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Integrations;
using Monthoya.Core.Security;
using Monthoya.Core.Services;
using Monthoya.Data;
using Monthoya.Data.Dashboard;
using Monthoya.Data.Diagnostics;
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
    public void SupabaseSchemaAudit_DefinesConservativeStorageAndCleanupChecks()
    {
        Assert.Equal(
            ["monthoya-documents", "monthoya-property-images"],
            SupabaseSchemaAudit.ExpectedPrivateBuckets);
        Assert.Contains(("pessoa_fisica", "Endereco"), SupabaseSchemaAudit.RemovedPessoaAddressColumns);
        Assert.Contains(("pessoa_juridica", "EnderecoEmpresa"), SupabaseSchemaAudit.RemovedPessoaAddressColumns);
        Assert.Contains(("pessoa_juridica", "ResponsavelEndereco"), SupabaseSchemaAudit.RemovedPessoaAddressColumns);
        Assert.Contains("imovel_imagens", SupabaseSchemaAudit.ExpectedRlsProtectedTables);
        Assert.Contains("pessoa_documentos", SupabaseSchemaAudit.ExpectedRlsProtectedTables);
        Assert.Contains("users", SupabaseSchemaAudit.ExpectedRlsProtectedTables);
        Assert.Equal("20260526085111_AddPessoaOcrAndAddressDetails", SupabaseSchemaAudit.HistoricalLiveOnlyMigrationId);
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
    public async Task Pessoa_CanBeCreatedWithoutManualRoles()
    {
        await using var dbContext = CreateDbContext();
        var rentalService = new RentalManagementService(dbContext);

        var pessoa = await rentalService.CreatePessoaAsync(
            new CreatePessoaRequest(
                TipoPessoa.Fisica,
                "Joao da Silva",
                "(44) 99999-0000",
                "joao@monthoya.local",
                "12345678901",
                null,
                null));

        var pessoas = await rentalService.GetPessoasAsync();

        Assert.Single(pessoas);
        Assert.Equal(pessoa.Id, pessoas[0].Id);
        Assert.Equal("-", pessoas[0].Roles);
        Assert.False(pessoas[0].IsProprietario);
        Assert.False(pessoas[0].IsLocatario);
        Assert.False(pessoas[0].IsFiador);
        Assert.Empty(dbContext.PessoaRoles);
    }

    [Fact]
    public async Task PessoaRoles_AreComputedFromActiveOperationalRecords()
    {
        await using var dbContext = CreateDbContext();
        var rentalService = new RentalManagementService(dbContext);

        var proprietario = await rentalService.CreatePessoaAsync(
            new CreatePessoaRequest(TipoPessoa.Fisica, "Maria Proprietaria", null, null, "111", null, null));
        var locatario = await rentalService.CreatePessoaAsync(
            new CreatePessoaRequest(TipoPessoa.Fisica, "Joao Locatario", null, null, "222", null, null));
        var fiador = await rentalService.CreatePessoaAsync(
            new CreatePessoaRequest(TipoPessoa.Fisica, "Bruno Fiador", null, null, "333", null, null));

        var imovel = await rentalService.CreateImovelAsync(
            new CreateImovelRequest(
                proprietario.Id,
                "Rua Operacional",
                "10",
                "Centro",
                "Paranavai",
                "PR",
                1500m,
                ImovelFinalidade.Locacao,
                null));

        var locacao = new Locacao
        {
            ImovelId = imovel.Id,
            ProprietarioId = proprietario.Id,
            LocatarioId = locatario.Id,
            DataInicio = new DateOnly(2026, 1, 1),
            ValorAluguel = 1500m,
            Status = LocacaoStatus.Ativa
        };
        locacao.Fiadores.Add(new LocacaoFiador { FiadorId = fiador.Id });
        dbContext.Locacoes.Add(locacao);
        await dbContext.SaveChangesAsync();

        var pessoas = await rentalService.GetPessoasAsync();

        var proprietarioSummary = pessoas.Single(x => x.Id == proprietario.Id);
        var locatarioSummary = pessoas.Single(x => x.Id == locatario.Id);
        var fiadorSummary = pessoas.Single(x => x.Id == fiador.Id);

        Assert.True(proprietarioSummary.IsProprietario);
        Assert.False(proprietarioSummary.IsLocatario);
        Assert.True(locatarioSummary.IsLocatario);
        Assert.True(fiadorSummary.IsFiador);
        Assert.Empty(dbContext.PessoaRoles);
    }

    [Fact]
    public async Task PessoaRoles_IgnoreEndedRentals()
    {
        await using var dbContext = CreateDbContext();
        var rentalService = new RentalManagementService(dbContext);

        var proprietario = await rentalService.CreatePessoaAsync(
            new CreatePessoaRequest(TipoPessoa.Fisica, "Proprietario", null, null, "111", null, null));
        var locatario = await rentalService.CreatePessoaAsync(
            new CreatePessoaRequest(TipoPessoa.Fisica, "Locatario Antigo", null, null, "222", null, null));
        var fiador = await rentalService.CreatePessoaAsync(
            new CreatePessoaRequest(TipoPessoa.Fisica, "Fiador Antigo", null, null, "333", null, null));
        var imovel = await rentalService.CreateImovelAsync(
            new CreateImovelRequest(proprietario.Id, "Rua Encerrada", "20", "Centro", "Paranavai", "PR", 1200m, ImovelFinalidade.Locacao, null));

        var locacao = new Locacao
        {
            ImovelId = imovel.Id,
            ProprietarioId = proprietario.Id,
            LocatarioId = locatario.Id,
            DataInicio = new DateOnly(2025, 1, 1),
            DataFim = new DateOnly(2025, 12, 31),
            ValorAluguel = 1200m,
            Status = LocacaoStatus.Encerrada
        };
        locacao.Fiadores.Add(new LocacaoFiador { FiadorId = fiador.Id });
        dbContext.Locacoes.Add(locacao);
        await dbContext.SaveChangesAsync();

        var pessoas = await rentalService.GetPessoasAsync();

        Assert.False(pessoas.Single(x => x.Id == locatario.Id).IsLocatario);
        Assert.False(pessoas.Single(x => x.Id == fiador.Id).IsFiador);
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
                Roles: null,
                Observacoes: "Cadastro completo.",
                Endereco: "Rua das Flores, 10",
                Rua: "Rua das Flores",
                Numero: "10",
                Complemento: "Casa",
                Bairro: "Centro",
                Cidade: "Paranavaí",
                Estado: "pr",
                Cep: "87702-000",
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

        Assert.Equal("Rua das Flores", pessoaFisica.Rua);
        Assert.Equal("10", pessoaFisica.Numero);
        Assert.Equal("Centro", pessoaFisica.Bairro);
        Assert.Equal("Paranavaí", pessoaFisica.Cidade);
        Assert.Equal("PR", pessoaFisica.Estado);
        Assert.Equal("87702000", pessoaFisica.Cep);
        Assert.Equal("Casada", pessoaFisica.EstadoCivil);
        Assert.Equal("Professora", pessoaFisica.Profissao);
        Assert.Single(documentos);
        Assert.Equal("Comprovante de residência", documentos[0].Tipo);
        Assert.Contains("ana-comprovante.pdf", documentos[0].StoragePath);
        Assert.Equal("NaoProcessado", documentos[0].OcrStatus);
    }

    [Fact]
    public async Task PessoaJuridica_StoresCompanyAndResponsibleAddressDetails()
    {
        await using var dbContext = CreateDbContext();
        var rentalService = new RentalManagementService(dbContext);

        await rentalService.CreatePessoaAsync(
            new CreatePessoaRequest(
                TipoPessoa: TipoPessoa.Juridica,
                NomeDisplay: "Empresa Completa Ltda",
                Telefone: "(44) 3222-0000",
                Email: "empresa@monthoya.local",
                Documento: "12345678000190",
                Roles: null,
                Observacoes: null,
                Endereco: "Avenida Brasil, 100",
                Rua: "Avenida Brasil",
                Numero: "100",
                Bairro: "Centro",
                Cidade: "Paranavaí",
                Estado: "pr",
                Cep: "87701-000",
                ResponsavelNome: "Carlos Responsável",
                ResponsavelRua: "Rua do Responsável",
                ResponsavelNumero: "200",
                ResponsavelBairro: "Jardim",
                ResponsavelCidade: "Paranavaí",
                ResponsavelEstado: "pr",
                ResponsavelCep: "87703-000",
                ResponsavelCpf: "12345678901"));

        var pessoaJuridica = await dbContext.PessoasJuridicas.SingleAsync();

        Assert.Equal("Avenida Brasil", pessoaJuridica.EmpresaRua);
        Assert.Equal("100", pessoaJuridica.EmpresaNumero);
        Assert.Equal("Centro", pessoaJuridica.EmpresaBairro);
        Assert.Equal("PR", pessoaJuridica.EmpresaEstado);
        Assert.Equal("Carlos Responsável", pessoaJuridica.ResponsavelNome);
        Assert.Equal("Rua do Responsável", pessoaJuridica.ResponsavelRua);
        Assert.Equal("200", pessoaJuridica.ResponsavelNumero);
        Assert.Equal("PR", pessoaJuridica.ResponsavelEstado);
    }

    [Fact]
    public async Task PessoaType_CreateOnlyPersistsSelectedTypeFields()
    {
        await using var dbContext = CreateDbContext();
        var rentalService = new RentalManagementService(dbContext);

        await rentalService.CreatePessoaAsync(
            new CreatePessoaRequest(
                TipoPessoa: TipoPessoa.Juridica,
                NomeDisplay: "Empresa Tipo Ltda",
                Telefone: null,
                Email: null,
                Documento: "12345678000190",
                Roles: null,
                Observacoes: null,
                Endereco: "Avenida Empresa",
                Rua: "Avenida Empresa",
                ResponsavelNome: "Responsavel Legal",
                ResponsavelCpf: "12345678901",
                Rg: "RG ignorado para juridica"));

        Assert.Empty(dbContext.PessoasFisicas);
        var pessoaJuridica = await dbContext.PessoasJuridicas.SingleAsync();
        Assert.Equal("Empresa Tipo Ltda", pessoaJuridica.NomeEmpresa);
        Assert.Equal("Responsavel Legal", pessoaJuridica.ResponsavelNome);
        Assert.Equal("12345678901", pessoaJuridica.ResponsavelCpf);
    }

    [Fact]
    public async Task PessoaDocumentos_AreFilteredByPersonAndRequireValidPerson()
    {
        await using var dbContext = CreateDbContext();
        var rentalService = new RentalManagementService(dbContext);

        var ana = await rentalService.CreatePessoaAsync(new CreatePessoaRequest(
            TipoPessoa.Fisica,
            "Ana",
            null,
            null,
            "111",
            null,
            null));
        var bruno = await rentalService.CreatePessoaAsync(new CreatePessoaRequest(
            TipoPessoa.Fisica,
            "Bruno",
            null,
            null,
            "222",
            null,
            null));

        await rentalService.CreatePessoaDocumentoAsync(new CreatePessoaDocumentoRequest(ana.Id, "cpf", "CPF Ana", "supabase/personas/ana-cpf.pdf", "application/pdf", null, null));
        await rentalService.CreatePessoaDocumentoAsync(new CreatePessoaDocumentoRequest(bruno.Id, "rg", "RG Bruno", "supabase/personas/bruno-rg.pdf", "application/pdf", null, null));

        var documentosAna = await rentalService.GetPessoaDocumentosAsync(ana.Id);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            rentalService.CreatePessoaDocumentoAsync(new CreatePessoaDocumentoRequest(Guid.Empty, "cpf", "Sem pessoa", "arquivo.pdf", "application/pdf", null, null)));

        Assert.Single(documentosAna);
        Assert.Equal(ana.Id, documentosAna[0].PessoaId);
        Assert.Equal("Selecione a pessoa do documento.", exception.Message);
    }

    [Fact]
    public async Task PessoaDocumento_StoresStoragePathAndOcrMetadataWithoutFileBytes()
    {
        await using var dbContext = CreateDbContext();
        var rentalService = new RentalManagementService(dbContext, new FakeDocumentOcrService("Texto extraído do documento."));
        var pessoa = await rentalService.CreatePessoaAsync(new CreatePessoaRequest(
            TipoPessoa.Fisica,
            "Cliente OCR",
            null,
            null,
            "333",
            null,
            null));

        await rentalService.CreatePessoaDocumentoAsync(new CreatePessoaDocumentoRequest(
            pessoa.Id,
            "comprovante_renda",
            "Comprovante de renda",
            "personas/cliente-ocr/renda.txt",
            "text/plain",
            null,
            null));

        var documento = await dbContext.PessoaDocumentos.SingleAsync();
        var contexto = await rentalService.GetPessoaContratoAutofillContextAsync(pessoa.Id);

        Assert.Equal("personas/cliente-ocr/renda.txt", documento.StoragePath);
        Assert.Equal(DocumentoOcrStatus.Processado, documento.OcrStatus);
        Assert.Equal("Texto extraído do documento.", documento.OcrTextoExtraido);
        Assert.NotNull(documento.OcrProcessadoEmUtc);
        Assert.Null(documento.OcrErroMensagem);
        Assert.Contains("Texto extraído do documento.", contexto!.TextoDocumentosOcr);
    }

    [Fact]
    public async Task PessoaDocumento_UploadsToStoragePathAndGeneratesSignedUrlSeparately()
    {
        await using var dbContext = CreateDbContext();
        var storage = new FakeFileStorageService();
        var rentalService = new RentalManagementService(dbContext, null, storage);
        var pessoa = await rentalService.CreatePessoaAsync(new CreatePessoaRequest(
            TipoPessoa.Fisica,
            "Cliente Storage",
            null,
            null,
            "444",
            null,
            null));
        var tempFile = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(tempFile, "conteudo do documento");

            var documento = await rentalService.CreatePessoaDocumentoAsync(new CreatePessoaDocumentoRequest(
                pessoa.Id,
                "cpf",
                "CPF Cliente",
                tempFile,
                "text/plain",
                null,
                null));
            var signedUrl = await storage.CreateSignedReadUrlAsync(documento.StoragePath);

            Assert.StartsWith("monthoya-documents/pessoas/", documento.StoragePath);
            Assert.Contains("/documentos/", documento.StoragePath);
            Assert.DoesNotContain(tempFile.Replace("\\", "/", StringComparison.Ordinal), documento.StoragePath);
            Assert.Equal("https://storage.test/signed/" + documento.StoragePath, signedUrl.Url);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task PessoaDocumento_OcrAutoFillsBlankFieldsOnly()
    {
        await using var dbContext = CreateDbContext();
        var ocrText = """
            Nome: Nome do Documento
            CPF: 123.456.789-01
            RG: 9876543
            Email: documento@monthoya.local
            Telefone: (44) 99999-2222
            CEP: 87702-000
            Endereco: Rua OCR, 123
            """;
        var rentalService = new RentalManagementService(dbContext, new FakeDocumentOcrService(ocrText));
        var pessoa = await rentalService.CreatePessoaAsync(new CreatePessoaRequest(
            TipoPessoa.Fisica,
            "Nome Existente",
            "(44) 98888-0000",
            null,
            null,
            null,
            null));

        var documento = await rentalService.CreatePessoaDocumentoAsync(new CreatePessoaDocumentoRequest(
            pessoa.Id,
            "residencia",
            "Documento OCR",
            "documentos/ocr.txt",
            "text/plain",
            null,
            null));

        var pessoaFisica = await dbContext.PessoasFisicas.SingleAsync();
        var pessoaEntity = await dbContext.Pessoas.SingleAsync();

        Assert.Equal("Nome Existente", pessoaEntity.NomeDisplay);
        Assert.Equal("44988880000", pessoaEntity.Telefone);
        Assert.Equal("documento@monthoya.local", pessoaEntity.Email);
        Assert.Equal("12345678901", pessoaFisica.Cpf);
        Assert.Equal("9876543", pessoaFisica.Rg);
        Assert.Equal("87702000", pessoaFisica.Cep);
        Assert.Equal("Rua OCR, 123", pessoaFisica.Rua);
        Assert.Contains("Email", documento.OcrCamposAplicados);
        Assert.DoesNotContain("Telefone", documento.OcrCamposAplicados);
    }

    [Fact]
    public async Task Imovel_RequiresOwnerAndComputesOwnerRole()
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
                null,
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
        Assert.True(pessoas.Single().IsProprietario);
        Assert.Empty(dbContext.PessoaRoles);
    }

    [Fact]
    public async Task ImovelImagem_StoresObjectPathOnly()
    {
        await using var dbContext = CreateDbContext();
        var storage = new FakeFileStorageService();
        var rentalService = new RentalManagementService(dbContext, null, storage);
        var proprietario = await rentalService.CreatePessoaAsync(
            new CreatePessoaRequest(TipoPessoa.Fisica, "Dono Foto", null, null, null, null, null));
        var imovel = await rentalService.CreateImovelAsync(
            new CreateImovelRequest(proprietario.Id, "Rua Foto", "1", "Centro", "Paranavai", "PR", 1000m, ImovelFinalidade.Locacao, null));
        var tempFile = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(tempFile, "imagem fake");

            var imagem = await rentalService.CreateImovelImagemAsync(new CreateImovelImagemRequest(
                imovel.Id,
                "fachada.jpg",
                tempFile,
                "image/jpeg"));

            Assert.StartsWith("monthoya-property-images/imoveis/", imagem.StoragePath);
            Assert.Contains("/fotos/", imagem.StoragePath);
            Assert.DoesNotContain(tempFile.Replace("\\", "/", StringComparison.Ordinal), imagem.StoragePath);
            Assert.Equal("fachada.jpg", imagem.FileName);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
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
                null,
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
    public async Task DataServices_SerializeConcurrentReadsOnSameContext()
    {
        await using var dbContext = CreateDbContext();
        var rentalService = new RentalManagementService(dbContext);
        var dashboardService = new DashboardService(dbContext);

        var proprietario = await rentalService.CreatePessoaAsync(
            new CreatePessoaRequest(TipoPessoa.Fisica, "Proprietário Concorrente", null, null, null));
        var locatario = await rentalService.CreatePessoaAsync(
            new CreatePessoaRequest(TipoPessoa.Fisica, "Locatário Concorrente", null, null, null));
        var imovel = await rentalService.CreateImovelAsync(
            new CreateImovelRequest(
                proprietario.Id,
                "Rua Paraná",
                "10",
                "Centro",
                "Paranavaí",
                "PR",
                1200m,
                ImovelFinalidade.Locacao,
                null));

        dbContext.Locacoes.Add(new Locacao
        {
            ImovelId = imovel.Id,
            ProprietarioId = proprietario.Id,
            LocatarioId = locatario.Id,
            ValorAluguel = 1200m,
            Status = LocacaoStatus.Ativa,
            DataInicio = new DateOnly(2026, 1, 1)
        });
        await dbContext.SaveChangesAsync();

        var tasks = Enumerable.Range(0, 30)
            .Select(async index => (index % 3) switch
            {
                0 => await ReadDashboardAsync(dashboardService),
                1 => await ReadPessoasAsync(rentalService),
                _ => await ReadLocacoesAsync(rentalService)
            })
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.Equal(30, results.Length);
        Assert.All(results, Assert.True);
    }

    private static async Task<bool> ReadDashboardAsync(DashboardService dashboardService)
    {
        await dashboardService.GetHomeSummaryAsync();
        return true;
    }

    private static async Task<bool> ReadPessoasAsync(RentalManagementService rentalService)
    {
        await rentalService.GetPessoasAsync();
        return true;
    }

    private static async Task<bool> ReadLocacoesAsync(RentalManagementService rentalService)
    {
        await rentalService.GetLocacoesAsync();
        return true;
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

    private sealed class FakeDocumentOcrService(string text) : IDocumentOcrService
    {
        public Task<DocumentOcrResult> ExtractTextAsync(
            string storagePath,
            string? contentType = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new DocumentOcrResult(true, text));
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public Task<string> SaveAsync(
            Stream content,
            string fileName,
            string? contentType = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult($"monthoya-documents/manual/{fileName}");

        public Task<StoredFile> SaveAsync(
            Stream content,
            FileStorageSaveRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new StoredFile(request.Bucket, request.ObjectPath, request.FileName, request.ContentType));

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream());

        public Task<FileStorageSignedUrl> CreateSignedReadUrlAsync(
            string storagePath,
            TimeSpan? expiresIn = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new FileStorageSignedUrl($"https://storage.test/signed/{storagePath}", DateTimeOffset.UtcNow.AddMinutes(15)));
    }
}




