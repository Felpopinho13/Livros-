using System;
using System.Collections.Generic;

namespace Livros.Web.Models.ViewModels {
    public class AdminClienteTransacoesViewModel {
        public int ClienteId { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public string ClienteEmail { get; set; } = string.Empty;
        public int TotalPedidos { get; set; }
        public decimal ValorTotalCompras { get; set; }
        public int TotalPagamentos { get; set; }
        public int TotalTrocas { get; set; }
        public int TotalCupons { get; set; }
        public string RankingNome { get; set; } = string.Empty;
        public string RankingCssClass { get; set; } = string.Empty;
        public decimal ValorElegivelRanking { get; set; }
        public decimal? ProximoMarcoRanking { get; set; }
        public string? ProximoRankingNome { get; set; }
        public List<AdminClientePedidoTransacaoViewModel> Pedidos { get; set; } = new();
        public List<AdminClientePagamentoTransacaoViewModel> Pagamentos { get; set; } = new();
        public List<AdminClienteTrocaTransacaoViewModel> Trocas { get; set; } = new();
        public List<AdminClienteCupomTransacaoViewModel> Cupons { get; set; } = new();
    }

    public class AdminClientePedidoTransacaoViewModel {
        public int PedidoId { get; set; }
        public DateTime Data { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string ResumoItens { get; set; } = string.Empty;
    }

    public class AdminClientePagamentoTransacaoViewModel {
        public int PedidoId { get; set; }
        public string Metodo { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class AdminClienteTrocaTransacaoViewModel {
        public string Codigo { get; set; } = string.Empty;
        public int PedidoId { get; set; }
        public string LivroTitulo { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime Data { get; set; }
    }

    public class AdminClienteCupomTransacaoViewModel {
        public string Codigo { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public int? PedidoId { get; set; }
    }
}
