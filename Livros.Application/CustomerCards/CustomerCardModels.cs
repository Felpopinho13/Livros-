using Livros.Domain;

namespace Livros.Application.CustomerCards {
    public sealed class CustomerCardsQuery {
        public string Email { get; init; } = string.Empty;
    }

    public sealed class CustomerCardsResult {
        public bool CustomerFound { get; init; }
        public List<Cartao> Cards { get; init; } = new();
        public List<BandeiraCartao> Brands { get; init; } = new();
    }

    public sealed class CustomerCardCreateCommand {
        public int ClienteId { get; init; }
        public string Nome { get; init; } = string.Empty;
        public string Numero { get; init; } = string.Empty;
        public int BandeiraCartaoId { get; init; }
        public string Validade { get; init; } = string.Empty;
        public string Cvv { get; init; } = string.Empty;
    }

    public sealed class CustomerCardSetDefaultCommand {
        public string Email { get; init; } = string.Empty;
        public int CardId { get; init; }
    }

    public sealed class CustomerCardDeleteCommand {
        public string Email { get; init; } = string.Empty;
        public int CardId { get; init; }
    }

    public sealed class CustomerCardCommandResult {
        public bool Success { get; init; }
        public bool CustomerFound { get; init; }
        public bool CardFound { get; init; } = true;
        public string? ErrorMessage { get; init; }
        public int? CardId { get; init; }
    }
}