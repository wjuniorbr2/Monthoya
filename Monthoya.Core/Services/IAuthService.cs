namespace Monthoya.Core.Services;

public interface IAuthService
{
    Task<AuthResult> SignInAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<AuthResult> CreateFirstAdminAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
}
