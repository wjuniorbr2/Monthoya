using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
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
            .ThenBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);

        return users.Select(UserMapper.ToSummary).ToList();
    }

    public async Task<UserSummary> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        UserInputValidator.ValidateCreate(request);

        var normalizedEmail = UserInputValidator.NormalizeEmail(request.Email);
        var emailExists = await dbContext.Users.AnyAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
        if (emailExists)
        {
            throw new InvalidOperationException("Ja existe um usuario com este e-mail.");
        }

        var user = new AppUser
        {
            DisplayName = request.DisplayName.Trim(),
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            Role = request.Role,
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
            ?? throw new InvalidOperationException("Usuario nao encontrado.");

        var normalizedEmail = UserInputValidator.NormalizeEmail(request.Email);
        var emailExists = await dbContext.Users.AnyAsync(
            x => x.Id != request.Id && x.NormalizedEmail == normalizedEmail,
            cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException("Ja existe um usuario com este e-mail.");
        }

        user.DisplayName = request.DisplayName.Trim();
        user.Email = request.Email.Trim();
        user.NormalizedEmail = normalizedEmail;
        user.Role = request.Role;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return UserMapper.ToSummary(user);
    }

    public async Task SetUserActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("Usuario nao encontrado.");

        user.IsActive = isActive;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
