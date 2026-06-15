using Livros.Domain;

namespace Livros.Application.CustomerAccounts {
    public sealed class CustomerDashboardQuery {
        public string Email { get; set; } = string.Empty;
        public int CartItemCount { get; set; }
    }

    public sealed class CustomerDashboardResult {
        public bool CustomerFound { get; set; }
        public string NomeExibicao { get; set; } = string.Empty;
        public string PrimeiroNome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalPedidos { get; set; }
        public decimal ValorTotalCompras { get; set; }
        public int QuantidadeEnderecos { get; set; }
        public int QuantidadeCartoes { get; set; }
        public int QuantidadeCuponsDisponiveis { get; set; }
        public int QuantidadeTrocasAbertas { get; set; }
        public int ItensNoCarrinho { get; set; }
        public string RankingNome { get; set; } = string.Empty;
        public string RankingCssClass { get; set; } = string.Empty;
        public decimal ValorElegivelRanking { get; set; }
        public decimal? ProximoMarcoRanking { get; set; }
        public string? ProximoRankingNome { get; set; }
        public CustomerDashboardOrderSummaryData? UltimoPedido { get; set; }
        public CustomerDashboardCouponSummaryData? UltimoCupomDisponivel { get; set; }
    }

    public sealed class CustomerDashboardOrderSummaryData {
        public int Id { get; set; }
        public DateTime Data { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
        public int QuantidadeItens { get; set; }
        public string LivroPrincipal { get; set; } = string.Empty;
    }

    public sealed class CustomerDashboardCouponSummaryData {
        public string Codigo { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Tipo { get; set; } = string.Empty;
    }

    public sealed class CustomerCouponsQuery {
        public string Email { get; set; } = string.Empty;
    }

    public sealed class CustomerCouponsResult {
        public bool CustomerFound { get; set; }
        public string NomeCliente { get; set; } = string.Empty;
        public int TotalCupons { get; set; }
        public int CuponsDisponiveis { get; set; }
        public decimal ValorDisponivel { get; set; }
        public List<CustomerCouponData> Cupons { get; set; } = new();
    }

    public sealed class CustomerCouponData {
        public string Codigo { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public DateTime? DataUtilizacao { get; set; }
        public int? PedidoId { get; set; }
    }

    public sealed class CustomerProfileQuery {
        public string Email { get; set; } = string.Empty;
    }

    public sealed class CustomerProfileResult {
        public bool CustomerFound { get; set; }
        public Cliente? Customer { get; set; }
    }

    public sealed class CustomerProfileUpdateCommand {
        public int CustomerId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public string? CPF { get; set; }
    }

    public sealed class CustomerPasswordChangeCommand {
        public int CustomerId { get; set; }
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public sealed class CustomerAccountCommandResult {
        public bool CustomerFound { get; set; } = true;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? UpdatedEmail { get; set; }
    }
}
