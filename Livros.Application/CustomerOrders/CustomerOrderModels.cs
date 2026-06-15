using Livros.Domain;

namespace Livros.Application.CustomerOrders {
    public sealed class CustomerOrderConfirmationQuery {
        public int CustomerId { get; set; }
        public int OrderId { get; set; }
    }

    public sealed class CustomerOrderConfirmationResult {
        public bool OrderFound { get; set; }
        public int PedidoId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string TipoEntrega { get; set; } = string.Empty;
        public DateTime? DataEntregaPrevista { get; set; }
        public decimal Total { get; set; }
        public string LivroTitulo { get; set; } = string.Empty;
        public int Quantidade { get; set; }
    }

    public sealed class CustomerOrdersQuery {
        public int CustomerId { get; set; }
    }

    public sealed class CustomerOrdersResult {
        public List<CustomerOrderSummaryData> Orders { get; set; } = new();
    }

    public sealed class CustomerOrderSummaryData {
        public int PedidoId { get; set; }
        public DateTime Data { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
        public string TipoEntrega { get; set; } = string.Empty;
        public DateTime? DataEntregaPrevista { get; set; }
        public string LivroTitulo { get; set; } = string.Empty;
        public string LivroAutor { get; set; } = string.Empty;
        public string LivroImagemUrl { get; set; } = string.Empty;
        public int QuantidadeItens { get; set; }
        public int QuantidadeLivros { get; set; }
        public int LivroIdPrincipal { get; set; }
    }

    public sealed class CustomerOrderDetailsQuery {
        public int CustomerId { get; set; }
        public int OrderId { get; set; }
    }

    public sealed class CustomerOrderDetailsResult {
        public bool OrderFound { get; set; }
        public int PedidoId { get; set; }
        public DateTime Data { get; set; }
        public string Status { get; set; } = string.Empty;
        public string TipoEntrega { get; set; } = string.Empty;
        public DateTime? DataEntregaPrevista { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public string EnderecoNome { get; set; } = string.Empty;
        public string Logradouro { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string Complemento { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string CEP { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal Frete { get; set; }
        public decimal Desconto { get; set; }
        public decimal Total { get; set; }
        public List<CustomerOrderDetailItemData> Itens { get; set; } = new();
        public List<CustomerOrderPaymentData> Pagamentos { get; set; } = new();
    }

    public sealed class CustomerOrderDetailItemData {
        public int PedidoItemId { get; set; }
        public int LivroId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Autor { get; set; } = string.Empty;
        public string ImagemUrl { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public int? TrocaId { get; set; }
        public string? TrocaStatus { get; set; }
        public string? CodigoCupomTroca { get; set; }
        public decimal? ValorCupomTroca { get; set; }
        public bool PedidoEntregue { get; set; }
    }

    public sealed class CustomerOrderPaymentData {
        public string Metodo { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public sealed class CustomerExchangeRequestCommand {
        public int CustomerId { get; set; }
        public int OrderId { get; set; }
        public int OrderItemId { get; set; }
        public int QuantityRequested { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? CustomerNote { get; set; }
    }

    public sealed class CustomerExchangeRequestResult {
        public bool OrderItemFound { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
    }
}
