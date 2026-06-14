using Livros.Domain;

namespace Livros.Application.AdminCustomers {
    public sealed class AdminCustomerTransactionsData {
        public Cliente? Cliente { get; init; }
        public List<Pedido> Pedidos { get; init; } = new();
        public List<Troca> Trocas { get; init; } = new();
        public List<CupomDesconto> Cupons { get; init; } = new();
    }

    public sealed class AdminCustomerTransactionsResult {
        public Cliente Cliente { get; init; } = new();
        public List<Pedido> Pedidos { get; init; } = new();
        public List<Troca> Trocas { get; init; } = new();
        public List<CupomDesconto> Cupons { get; init; } = new();
        public decimal ValorElegivelRanking { get; init; }
    }
}
