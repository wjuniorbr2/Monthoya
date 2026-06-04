namespace Monthoya.Core.Services;

public sealed record ChangeUserPasswordRequest(
    Guid UserId,
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);

public interface IUserPasswordService
{
    Task ChangePasswordAsync(ChangeUserPasswordRequest request, CancellationToken cancellationToken = default);
}
