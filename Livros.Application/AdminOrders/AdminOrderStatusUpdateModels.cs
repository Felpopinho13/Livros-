namespace Livros.Application.AdminOrders {
    public sealed class AdminOrderStatusUpdateCommand {
        public int PedidoId { get; init; }
        public string NovoStatus { get; init; } = string.Empty;
    }

    public sealed class AdminOrderStatusUpdateResult {
        public bool Succeeded { get; init; }
        public string Message { get; init; } = string.Empty;
    }
}
