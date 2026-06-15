using Livros.Domain;

namespace Livros.Application.AdminCustomers {
    public sealed class AdminCustomersPageData {
        public List<Cliente> Clientes { get; init; } = new();
        public int TotalClientes { get; init; }
    }

    public sealed class AdminCustomersResult {
        public List<Cliente> Clientes { get; init; } = new();
        public Dictionary<int, decimal> ValoresElegiveisPorCliente { get; init; } = new();
        public int PaginaAtual { get; init; }
        public int TotalPaginas { get; init; }
    }

    public sealed class AdminCustomerCreateCommand {
        public Cliente Cliente { get; init; } = new();
    }

    public sealed class AdminCustomerUpdateCommand {
        public Cliente Cliente { get; init; } = new();
    }

    public sealed class AdminCustomerStatusCommand {
        public int ClienteId { get; init; }
        public bool IsAtivo { get; init; }
    }

    public sealed class AdminCustomerDeletionCommand {
        public int ClienteId { get; init; }
    }

    public sealed class AdminCustomerOperationResult {
        public bool Succeeded { get; init; }
        public string Message { get; init; } = string.Empty;
    }
}
