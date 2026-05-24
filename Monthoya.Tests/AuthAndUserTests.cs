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

        var loginResult = await authService.SignInAsync("admin", "strongpass123");

        Assert.True(loginResult.Succeeded);
        Assert.NotNull(loginResult.User);
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
    }

    private static MonthoyaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MonthoyaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MonthoyaDbContext(options);
    }
}
