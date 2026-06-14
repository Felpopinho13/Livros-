using Livros.Domain;

namespace Livros.Application.AdminExchanges {
    public sealed class AdminExchangesPageData {
        public List<Troca> Trocas { get; init; } = new();
        public int TotalTrocas { get; init; }
        public List<CupomDesconto> CuponsPagina { get; init; } = new();
        public int TotalCupons { get; init; }
        public List<CupomDesconto> CuponsRecentes { get; init; } = new();
        public List<Cliente> ClientesAtivos { get; init; } = new();
        public Dictionary<int, decimal> ValoresSugeridosCupomPorTroca { get; init; } = new();
    }

    public sealed class AdminExchangesResult {
        public string? Busca { get; init; }
        public string? StatusFiltro { get; init; }
        public int PaginaTrocasAtual { get; init; }
        public int TotalPaginasTrocas { get; init; }
        public int PaginaCuponsAtual { get; init; }
        public int TotalPaginasCupons { get; init; }
        public List<Troca> Trocas { get; init; } = new();
        public List<CupomDesconto> CuponsRecentes { get; init; } = new();
        public List<CupomDesconto> Cupons { get; init; } = new();
        public List<Cliente> ClientesAtivos { get; init; } = new();
        public Dictionary<int, decimal> ValoresSugeridosCupomPorTroca { get; init; } = new();
    }

    public sealed class AdminExchangeAnalysisCommand {
        public int TrocaId { get; init; }
        public string? Decisao { get; init; }
        public string? ObservacaoAdmin { get; init; }
    }

    public sealed class AdminExchangeReceiptCommand {
        public int TrocaId { get; init; }
        public bool RetornarAoEstoque { get; init; }
        public string? ObservacaoAdmin { get; init; }
        public decimal ValorCupom { get; init; }
    }

    public sealed class AdminPromotionalCouponCommand {
        public decimal Valor { get; init; }
        public string? Destinatario { get; init; }
        public int? ClienteId { get; init; }
    }

    public sealed class AdminCouponDeactivationCommand {
        public int CupomId { get; init; }
    }

    public sealed class AdminExchangeActionResult {
        public bool Succeeded { get; init; }
        public string Message { get; init; } = string.Empty;
    }
}
