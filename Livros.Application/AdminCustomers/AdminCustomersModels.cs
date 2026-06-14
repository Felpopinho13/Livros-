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
}
