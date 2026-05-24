using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Security;
using Monthoya.Core.Services;
using Monthoya.Data;
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
        Assert.Equal("Login ou senha invalidos.", result.ErrorMessage);
    }

    [Fact]
    public void RolePermissions_MatchExpectedAccess()
    {
        Assert.False(RolePermissions.CanManageUsers(UserRole.Usuario));
        Assert.True(RolePermissions.CanManageUsers(UserRole.Administrador));
        Assert.True(RolePermissions.CanManageUsers(UserRole.Desenvolvedor));

        Assert.False(RolePermissions.CanAccessDiagnostics(UserRole.Usuario));
        Assert.False(RolePermissions.CanAccessDiagnostics(UserRole.Administrador));
        Assert.True(RolePermissions.CanAccessDiagnostics(UserRole.Desenvolvedor));

        Assert.True(RolePermissions.CanAccess(UserRole.Usuario, UserAccess.Dashboard, UserAccess.Dashboard));
        Assert.False(RolePermissions.CanAccess(UserRole.Usuario, UserAccess.Dashboard, UserAccess.Documents));
        Assert.True(RolePermissions.CanAccess(UserRole.Administrador, UserAccess.None, UserAccess.UserManagement));
        Assert.True(RolePermissions.CanAccess(UserRole.Desenvolvedor, UserAccess.None, UserAccess.Diagnostics));
    }

    [Fact]
    public async Task CreateUser_StoresRestrictedNormalUserAccess()
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
                UserAccess.Dashboard | UserAccess.Properties));

        Assert.Equal(UserAccess.Dashboard | UserAccess.Properties, user.Access);
        Assert.True(RolePermissions.CanAccess(user.Role, user.Access, UserAccess.Properties));
        Assert.False(RolePermissions.CanAccess(user.Role, user.Access, UserAccess.Financial));
    }

    private static MonthoyaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MonthoyaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MonthoyaDbContext(options);
    }
}
