namespace Livros.Web.Helpers;

public static class PasswordPolicyHelper {
    private static readonly System.Text.RegularExpressions.Regex StrongPasswordRegex = new(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*[^A-Za-z0-9\s])(?=\S+$).{8,}$",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    public const string MensagemRequisito = "A senha deve ter pelo menos 8 caracteres, conter letras maiusculas, minusculas e caractere especial, sem espacos.";

    public static bool IsStrongPassword(string? senha) {
        if (string.IsNullOrWhiteSpace(senha)) {
            return false;
        }

        return StrongPasswordRegex.IsMatch(senha);
    }
}
