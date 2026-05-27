using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Security;
using Monthoya.Core.Services;

namespace Monthoya.Data.Users;

public sealed class UserService(
    MonthoyaDbContext dbContext,
    PasswordHasher<AppUser> passwordHasher) : IUserService
{
    public Task<bool> HasAnyUsersAsync(CancellationToken cancellationToken = default) =>
        dbContext.Users.AnyAsync(cancellationToken);

    public async Task<IReadOnlyList<UserSummary>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await dbContext.Users
            .AsNoTracking()
            .OrderByDescending(x => x.Role)
            .ThenBy(x => x.LoginName)
            .ToListAsync(cancellationToken);

        return users.Select(UserMapper.ToSummary).ToList();
    }

    public async Task<UserSummary> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        UserInputValidator.ValidateCreate(request);

        var normalizedLoginName = UserInputValidator.NormalizeLoginName(request.LoginName);
        var loginExists = await dbContext.Users.AnyAsync(x => x.NormalizedLoginName == normalizedLoginName, cancellationToken);
        if (loginExists)
        {
            throw new InvalidOperationException("Já existe um usuário com este login.");
        }

        var normalizedEmail = UserInputValidator.NormalizeEmail(request.Email);
        var emailExists = await dbContext.Users.AnyAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
        if (emailExists)
        {
            throw new InvalidOperationException("Já existe um usuário com este e-mail.");
        }

        var user = new AppUser
        {
            DisplayName = request.DisplayName.Trim(),
            LoginName = request.LoginName.Trim(),
            NormalizedLoginName = normalizedLoginName,
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            Role = request.Role,
            Access = NormalizeAccessForRole(request.Role, request.Access),
            IsActive = true
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return UserMapper.ToSummary(user);
    }

    public async Task<UserSummary> UpdateUserAsync(UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        UserInputValidator.ValidateUpdate(request);

        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Usuário não encontrado.");

        var normalizedLoginName = UserInputValidator.NormalizeLoginName(request.LoginName);
        var loginExists = await dbContext.Users.AnyAsync(
            x => x.Id != request.Id && x.NormalizedLoginName == normalizedLoginName,
            cancellationToken);

        if (loginExists)
        {
            throw new InvalidOperationException("Já existe um usuário com este login.");
        }

        var normalizedEmail = UserInputValidator.NormalizeEmail(request.Email);
        var emailExists = await dbContext.Users.AnyAsync(
            x => x.Id != request.Id && x.NormalizedEmail == normalizedEmail,
            cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException("Já existe um usuário com este e-mail.");
        }

        user.DisplayName = request.DisplayName.Trim();
        user.LoginName = request.LoginName.Trim();
        user.NormalizedLoginName = normalizedLoginName;
        user.Email = request.Email.Trim();
        user.NormalizedEmail = normalizedEmail;
        user.Role = request.Role;
        user.Access = NormalizeAccessForRole(request.Role, request.Access);
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return UserMapper.ToSummary(user);
    }

    public async Task SetUserActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("Usuário não encontrado.");

        user.IsActive = isActive;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> VerifyPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return false;
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return verification != PasswordVerificationResult.Failed;
    }

    private static UserAccess NormalizeAccessForRole(UserRole role, UserAccess access) =>
        role switch
        {
            UserRole.Administrador => RolePermissions.AdministratorAccess,
            UserRole.Desenvolvedor => RolePermissions.DeveloperAccess,
            _ => access & UserAccess.UserManagement
        };
}
