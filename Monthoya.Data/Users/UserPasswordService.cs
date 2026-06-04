using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.Users;

public sealed class UserPasswordService(
    MonthoyaDbContext dbContext,
    PasswordHasher<AppUser> passwordHasher) : IUserPasswordService
{
    public async Task ChangePasswordAsync(ChangeUserPasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            throw new InvalidOperationException("Informe a senha atual.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new InvalidOperationException("Informe a nova senha.");
        }

        if (request.NewPassword.Length < 6)
        {
            throw new InvalidOperationException("A nova senha deve ter pelo menos 6 caracteres.");
        }

        if (!string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A confirmação da nova senha não confere.");
        }

        if (string.Equals(request.CurrentPassword, request.NewPassword, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A nova senha deve ser diferente da senha atual.");
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Usuário não encontrado.");

        if (!user.IsActive)
        {
            throw new InvalidOperationException("Usuário inativo.");
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (verification == PasswordVerificationResult.Failed)
        {
            throw new InvalidOperationException("A senha atual está incorreta.");
        }

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
