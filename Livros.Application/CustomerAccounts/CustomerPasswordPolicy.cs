using System.Text.RegularExpressions;

namespace Livros.Application.CustomerAccounts {
    public static class CustomerPasswordPolicy {
        private static readonly Regex StrongPasswordRegex = new(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*[^A-Za-z0-9\s])(?=\S+$).{8,}$",
            RegexOptions.Compiled);

        public const string RequirementMessage = "A senha deve ter pelo menos 8 caracteres, conter letras maiusculas, minusculas e caractere especial, sem espacos.";

        public static bool IsStrongPassword(string? password) {
            if (string.IsNullOrWhiteSpace(password)) {
                return false;
            }

            return StrongPasswordRegex.IsMatch(password);
        }
    }
}
