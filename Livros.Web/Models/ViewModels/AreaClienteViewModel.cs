using System;

namespace Livros.Web.Models.ViewModels {
    public class AreaClienteViewModel {
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
        public AreaClientePedidoResumoViewModel? UltimoPedido { get; set; }
        public AreaClienteCupomResumoViewModel? UltimoCupomDisponivel { get; set; }
    }

    public class AreaClientePedidoResumoViewModel {
        public int Id { get; set; }
        public DateTime Data { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
        public int QuantidadeItens { get; set; }
        public string LivroPrincipal { get; set; } = string.Empty;
    }

    public class AreaClienteCupomResumoViewModel {
        public string Codigo { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Tipo { get; set; } = string.Empty;
    }
}
