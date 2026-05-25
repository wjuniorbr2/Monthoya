using Monthoya.Core.Services;

namespace Monthoya.Data.Users;

internal static class UserInputValidator
{
    public static void ValidateCreate(CreateUserRequest request)
    {
        ValidateUserFields(request.DisplayName, request.LoginName, request.Email);

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            throw new InvalidOperationException("A senha deve ter pelo menos 8 caracteres.");
        }
    }

    public static void ValidateUpdate(UpdateUserRequest request)
    {
        ValidateUserFields(request.DisplayName, request.LoginName, request.Email);
    }

    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    public static string NormalizeLoginName(string loginName) => loginName.Trim().ToUpperInvariant();

    private static void ValidateUserFields(string displayName, string loginName, string email)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new InvalidOperationException("Informe o nome do usuário.");
        }

        if (string.IsNullOrWhiteSpace(loginName) || loginName.Trim().Length < 3)
        {
            throw new InvalidOperationException("Informe um login com pelo menos 3 caracteres.");
        }

        if (loginName.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException("O login não pode conter espaços.");
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Informe um e-mail válido.");
        }
    }
}
