using Livros.Application.AdminOrders;
using Livros.Domain;

public static class AdminPedidosViewModelMapper {
    public static AdminPedidosViewModel Map(AdminOrdersResult result) {
        return new AdminPedidosViewModel {
            Busca = result.Busca,
            StatusFiltro = result.StatusFiltro,
            PaginaAtual = result.PaginaAtual,
            TotalPaginas = result.TotalPaginas,
            Pedidos = result.Pedidos.Select(p => new AdminPedidoItemViewModel {
                PedidoId = p.Id,
                Data = p.Data,
                ClienteNome = p.Cliente?.Nome ?? string.Empty,
                ClienteEmail = p.Cliente?.Email ?? string.Empty,
                Total = p.Total,
                Status = OrderStatusHelper.NormalizeDisplayStatus(p.Status),
                StatusPagamento = ObterStatusPagamentoPedido(p),
                ResumoItens = MontarResumoItensPedido(p),
                QuantidadeItens = p.Itens.Count,
                QuantidadeLivros = p.Itens.Sum(i => i.Quantidade),
                Destino = MontarDestinoPedido(p),
                EstoqueBaixado = OrderStatusHelper.RequiresStockDecrease(p.Status),
                TemTroca = result.TrocasPorPedido.ContainsKey(p.Id),
                QuantidadeTrocas = result.TrocasPorPedido.TryGetValue(p.Id, out var quantidadeTrocas) ? quantidadeTrocas : 0,
                ProximosStatus = OrderStatusHelper.GetNextStatuses(p.Status).ToList()
            }).ToList()
        };
    }

    private static string ObterStatusPagamentoPedido(Pedido pedido) {
        if (pedido.Pagamentos == null || !pedido.Pagamentos.Any()) {
            return "Sem pagamento";
        }

        if (pedido.Pagamentos.All(p => string.Equals(p.Status, "Cancelado", StringComparison.OrdinalIgnoreCase))) {
            return "Cancelado";
        }

        if (pedido.Pagamentos.All(p => string.Equals(p.Status, "Recusado", StringComparison.OrdinalIgnoreCase))) {
            return "Recusado";
        }

        if (pedido.Pagamentos.All(p => string.Equals(p.Status, "Aprovado", StringComparison.OrdinalIgnoreCase))) {
            return "Aprovado";
        }

        return "Pendente";
    }

    private static string MontarResumoItensPedido(Pedido pedido) {
        var itemPrincipal = pedido.Itens.FirstOrDefault();
        if (itemPrincipal == null) {
            return "Pedido sem itens";
        }

        if (pedido.Itens.Count == 1) {
            return itemPrincipal.Livro?.Titulo ?? "Livro";
        }

        return $"{itemPrincipal.Livro?.Titulo ?? "Livro"} + {pedido.Itens.Count - 1} item(ns)";
    }

    private static string MontarDestinoPedido(Pedido pedido) {
        var cidade = pedido.Endereco?.Cidade?.Nome ?? string.Empty;
        var estado = pedido.Endereco?.Cidade?.Estado?.Sigla ?? string.Empty;

        if (string.IsNullOrWhiteSpace(cidade) && string.IsNullOrWhiteSpace(estado)) {
            return "Endereco nao informado";
        }

        return string.IsNullOrWhiteSpace(estado) ? cidade : $"{cidade}/{estado}";
    }
}
