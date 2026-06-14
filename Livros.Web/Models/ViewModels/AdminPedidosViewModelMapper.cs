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
                Status = NormalizarStatusPedidoExibicao(p.Status),
                StatusPagamento = ObterStatusPagamentoPedido(p),
                ResumoItens = MontarResumoItensPedido(p),
                QuantidadeItens = p.Itens.Count,
                QuantidadeLivros = p.Itens.Sum(i => i.Quantidade),
                Destino = MontarDestinoPedido(p),
                EstoqueBaixado = StatusExigeBaixaEstoque(p.Status),
                TemTroca = result.TrocasPorPedido.ContainsKey(p.Id),
                QuantidadeTrocas = result.TrocasPorPedido.TryGetValue(p.Id, out var quantidadeTrocas) ? quantidadeTrocas : 0,
                ProximosStatus = ObterProximosStatusPedido(p.Status).ToList()
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

    private static bool StatusExigeBaixaEstoque(string? status) {
        if (string.IsNullOrWhiteSpace(status)) {
            return false;
        }

        return status.Equals("APROVADA", StringComparison.OrdinalIgnoreCase)
            || status.Equals("PAGAMENTO APROVADO", StringComparison.OrdinalIgnoreCase)
            || status.Equals("EM SEPARACAO", StringComparison.OrdinalIgnoreCase)
            || status.Equals("EM TRANSPORTE", StringComparison.OrdinalIgnoreCase)
            || status.Equals("ENVIADO", StringComparison.OrdinalIgnoreCase)
            || status.Equals("ENTREGUE", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> ObterProximosStatusPedido(string? statusAtual) {
        var status = NormalizarStatusPedidoInterno(statusAtual);

        return status switch {
            "APROVADA" => new[] { "EM SEPARACAO", "CANCELADO" },
            "EM SEPARACAO" => new[] { "EM TRANSPORTE", "CANCELADO" },
            "EM TRANSPORTE" => new[] { "ENTREGUE" },
            _ => Array.Empty<string>()
        };
    }

    private static string NormalizarStatusPedidoInterno(string? statusAtual) {
        return (statusAtual ?? string.Empty).Trim().ToUpperInvariant() switch {
            "EM PROCESSAMENTO" => "APROVADA",
            "PAGAMENTO APROVADO" => "APROVADA",
            "PAGAMENTO RECUSADO" => "REPROVADA",
            "ENVIADO" => "EM TRANSPORTE",
            var status => status
        };
    }

    private static string NormalizarStatusPedidoExibicao(string? statusAtual) {
        return NormalizarStatusPedidoInterno(statusAtual) switch {
            "APROVADA" => "APROVADA",
            "REPROVADA" => "REPROVADA",
            "EM SEPARACAO" => "EM SEPARACAO",
            "EM TRANSPORTE" => "EM TRANSPORTE",
            "ENTREGUE" => "ENTREGUE",
            "CANCELADO" => "CANCELADO",
            _ => statusAtual ?? "NAO INFORMADO"
        };
    }
}
