using Monthoya.Core.Services;

namespace Monthoya.Data.Users;

internal static class UserInputValidator
{
    public static void ValidateCreate(CreateUserRequest request)
    {
        ValidateNameAndEmail(request.DisplayName, request.Email);

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            throw new InvalidOperationException("A senha deve ter pelo menos 8 caracteres.");
        }
    }

    public static void ValidateUpdate(UpdateUserRequest request)
    {
        ValidateNameAndEmail(request.DisplayName, request.Email);
    }

    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    private static void ValidateNameAndEmail(string displayName, string email)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new InvalidOperationException("Informe o nome do usuario.");
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Informe um e-mail valido.");
        }
    }
}
