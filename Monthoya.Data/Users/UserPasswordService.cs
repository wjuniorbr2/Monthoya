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
        var errors = new List<string>();

        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Usuário não encontrado.");

        if (!user.IsActive)
        {
            throw new InvalidOperationException("Usuário inativo.");
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            errors.Add("Informe a senha atual.");
        }
        else
        {
            var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
            if (verification == PasswordVerificationResult.Failed)
            {
                errors.Add("A senha atual está incorreta.");
            }
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            errors.Add("Informe a nova senha.");
        }
        else if (request.NewPassword.Length < 6)
        {
            errors.Add("A nova senha deve ter pelo menos 6 caracteres.");
        }

        if (!string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal))
        {
            errors.Add("A confirmação da nova senha não confere.");
        }

        if (!string.IsNullOrWhiteSpace(request.CurrentPassword)
            && string.Equals(request.CurrentPassword, request.NewPassword, StringComparison.Ordinal))
        {
            errors.Add("A nova senha deve ser diferente da senha atual.");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
        }

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
