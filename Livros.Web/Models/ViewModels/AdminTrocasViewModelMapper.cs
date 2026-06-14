using Livros.Application.AdminExchanges;
using Livros.Domain;

public static class AdminTrocasViewModelMapper {
    public static AdminTrocasViewModel Map(AdminExchangesResult result) {
        return new AdminTrocasViewModel {
            Busca = result.Busca,
            StatusFiltro = result.StatusFiltro,
            PaginaTrocasAtual = result.PaginaTrocasAtual,
            TotalPaginasTrocas = result.TotalPaginasTrocas,
            PaginaCuponsAtual = result.PaginaCuponsAtual,
            TotalPaginasCupons = result.TotalPaginasCupons,
            Trocas = result.Trocas.Select(t => new AdminTrocaItemViewModel {
                Id = t.Id,
                Codigo = t.Codigo,
                PedidoId = t.PedidoId,
                ClienteNome = t.Cliente?.Nome ?? string.Empty,
                LivroTitulo = t.PedidoItem?.Livro?.Titulo ?? "Livro",
                Motivo = t.Motivo,
                ObservacaoCliente = t.ObservacaoCliente,
                ObservacaoAdmin = t.ObservacaoAdmin,
                Status = ObterStatusTrocaExibicao(t),
                DataSolicitacao = t.DataSolicitacao,
                DataRecebimento = t.DataRecebimento,
                RetornarAoEstoque = t.RetornarAoEstoque,
                ValorSugeridoCupom = result.ValoresSugeridosCupomPorTroca.TryGetValue(t.Id, out var valor) ? valor : 0m,
                ValorCupomGerado = t.CupomDesconto?.Valor,
                CodigoCupom = t.CupomDesconto?.Codigo,
                PodeAnalisar = TrocaEstaSolicitada(t),
                PodeConfirmarRecebimento = TrocaEstaAutorizada(t)
            }).ToList(),
            CuponsRecentes = result.CuponsRecentes,
            Cupons = result.Cupons.Select(c => new AdminCupomItemViewModel {
                Id = c.Id,
                Codigo = c.Codigo,
                Valor = c.Valor,
                Tipo = c.Tipo,
                Status = !c.IsAtivo ? "Inativo" : c.DataUtilizacao.HasValue ? "Utilizado" : "Ativo",
                Publico = c.ClienteId.HasValue ? "Cliente específico" : "Código manual",
                DataCriacao = c.DataCriacao,
                DataUtilizacao = c.DataUtilizacao,
                ClienteNome = c.Cliente != null ? c.Cliente.Nome : null,
                PedidoId = c.PedidoId,
                PodeDesativar = c.IsAtivo && !c.DataUtilizacao.HasValue && c.Tipo == "PROMOCIONAL"
            }).ToList(),
            ClientesAtivos = result.ClientesAtivos.Select(c => new AdminCupomClienteOptionViewModel {
                Id = c.Id,
                Nome = c.Nome,
                Email = c.Email
            }).ToList()
        };
    }

    private static bool TrocaEstaSolicitada(Troca troca) {
        return string.Equals(troca.Status, "EM TROCA", StringComparison.OrdinalIgnoreCase)
            || string.Equals(troca.Status, "Solicitado", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TrocaEstaAutorizada(Troca troca) {
        return string.Equals(troca.Status, "TROCA AUTORIZADA", StringComparison.OrdinalIgnoreCase)
            || string.Equals(troca.Status, "Autorizada", StringComparison.OrdinalIgnoreCase)
            || (string.Equals(troca.Status, "Aprovado", StringComparison.OrdinalIgnoreCase) && !troca.CupomDescontoId.HasValue);
    }

    private static bool TrocaEstaRecebida(Troca troca) {
        return string.Equals(troca.Status, "TROCADO", StringComparison.OrdinalIgnoreCase)
            || string.Equals(troca.Status, "Recebida", StringComparison.OrdinalIgnoreCase)
            || (string.Equals(troca.Status, "Aprovado", StringComparison.OrdinalIgnoreCase) && troca.CupomDescontoId.HasValue);
    }

    private static string ObterStatusTrocaExibicao(Troca troca) {
        if (TrocaEstaRecebida(troca)) {
            return "TROCADO";
        }

        if (TrocaEstaAutorizada(troca)) {
            return "TROCA AUTORIZADA";
        }

        if (string.Equals(troca.Status, "TROCA RECUSADA", StringComparison.OrdinalIgnoreCase)
            || string.Equals(troca.Status, "Recusado", StringComparison.OrdinalIgnoreCase)) {
            return "TROCA RECUSADA";
        }

        if (string.Equals(troca.Status, "EM TROCA", StringComparison.OrdinalIgnoreCase)
            || string.Equals(troca.Status, "Solicitado", StringComparison.OrdinalIgnoreCase)) {
            return "EM TROCA";
        }

        return troca.Status;
    }
}
