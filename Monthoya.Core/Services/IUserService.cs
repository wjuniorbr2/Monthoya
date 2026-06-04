namespace Monthoya.Core.Services;

public interface IUserService
{
    Task<bool> HasAnyUsersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserSummary>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<UserSummary> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task<UserSummary> UpdateUserAsync(UpdateUserRequest request, CancellationToken cancellationToken = default);

    Task SetUserActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);

    Task<bool> VerifyPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default);
}