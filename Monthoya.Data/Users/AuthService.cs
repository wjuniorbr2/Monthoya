using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.Users;

public sealed class AuthService(
    MonthoyaDbContext dbContext,
    IUserService userService,
    PasswordHasher<AppUser> passwordHasher) : IAuthService
{
    private const string SafeLoginError = "Login ou senha invalidos.";

    public async Task<AuthResult> SignInAsync(string loginName, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(loginName) || string.IsNullOrWhiteSpace(password))
        {
            return new AuthResult(false, null, SafeLoginError);
        }

        var normalizedLoginName = UserInputValidator.NormalizeLoginName(loginName);
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.NormalizedLoginName == normalizedLoginName, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return new AuthResult(false, null, SafeLoginError);
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return new AuthResult(false, null, SafeLoginError);
        }

        user.LastLoginAtUtc = DateTimeOffset.UtcNow;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResult(true, UserMapper.ToAuthenticatedUser(user), null);
    }

    public async Task<AuthResult> CreateFirstAdminAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (await userService.HasAnyUsersAsync(cancellationToken))
        {
            return new AuthResult(false, null, "A configuracao inicial ja foi concluida.");
        }

        var adminRequest = request with { Role = UserRole.Administrador };
        var user = await userService.CreateUserAsync(adminRequest, cancellationToken);
        var authenticatedUser = new AuthenticatedUser(user.Id, user.DisplayName, user.LoginName, user.Email, user.Role, user.Access);

        return new AuthResult(true, authenticatedUser, null);
    }
}
