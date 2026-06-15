using Livros.Domain;

namespace Livros.Application.CustomerCart {
    public sealed class CustomerCartItemEntry {
        public int LivroId { get; set; }
        public int Quantidade { get; set; }
    }

    public sealed class CustomerCartAddCommand {
        public List<CustomerCartItemEntry> Items { get; set; } = new();
        public int LivroId { get; set; }
        public int Quantidade { get; set; }
        public int? CustomerId { get; set; }
        public string SessionKey { get; set; } = string.Empty;
    }

    public sealed class CustomerCartUpdateCommand {
        public List<CustomerCartItemEntry> Items { get; set; } = new();
        public int LivroId { get; set; }
        public int Quantidade { get; set; }
        public int? CustomerId { get; set; }
        public string SessionKey { get; set; } = string.Empty;
    }

    public sealed class CustomerCartRemoveCommand {
        public List<CustomerCartItemEntry> Items { get; set; } = new();
        public int LivroId { get; set; }
        public int? CustomerId { get; set; }
        public string SessionKey { get; set; } = string.Empty;
    }

    public sealed class CustomerCartClearCommand {
        public int? CustomerId { get; set; }
        public string SessionKey { get; set; } = string.Empty;
    }

    public sealed class CustomerCartSyncCommand {
        public List<CustomerCartItemEntry> Items { get; set; } = new();
        public int? CustomerId { get; set; }
        public string SessionKey { get; set; } = string.Empty;
        public bool RenewReservations { get; set; }
    }

    public sealed class CustomerCartActionResult {
        public bool Success { get; set; }
        public bool ItemFound { get; set; } = true;
        public List<CustomerCartItemEntry> Items { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public string? WarningMessage { get; set; }
    }

    public sealed class CustomerCartSyncResult {
        public List<CustomerCartNormalizedItem> Itens { get; set; } = new();
        public List<CustomerCartItemEntry> UpdatedItems { get; set; } = new();
        public List<string> Avisos { get; set; } = new();
        public bool CarrinhoMudou { get; set; }
        public bool RequerRevisao { get; set; }
    }

    public sealed class CustomerCartNormalizedItem {
        public Livro Livro { get; set; } = null!;
        public int Quantidade { get; set; }
        public int EstoqueDisponivel { get; set; }
        public DateTime? ReservaExpiraEm { get; set; }
        public bool ReservaExpirando { get; set; }
        public string? AvisoReserva { get; set; }
    }
}
