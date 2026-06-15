using Livros.Application.AdminCustomers;
using Livros.Application.AdminOrders;
using Livros.Domain;
using Livros.Web.Helpers;

namespace Livros.Web.Models.ViewModels {
    public static class AdminClienteTransacoesViewModelMapper {
        public static AdminClienteTransacoesViewModel Map(AdminCustomerTransactionsResult result) {
            var ranking = ClienteRankingHelper.ObterRanking(result.ValorElegivelRanking);
            var pagamentos = result.Pedidos
                .SelectMany(p => p.Pagamentos.Select(pg => new AdminClientePagamentoTransacaoViewModel {
                    PedidoId = p.Id,
                    Metodo = pg.Metodo,
                    Valor = pg.Valor,
                    Status = pg.Status
                }))
                .OrderByDescending(pg => pg.PedidoId)
                .ToList();

            return new AdminClienteTransacoesViewModel {
                ClienteId = result.Cliente.Id,
                ClienteNome = result.Cliente.Nome,
                ClienteEmail = result.Cliente.Email,
                TotalPedidos = result.Pedidos.Count,
                ValorTotalCompras = result.Pedidos.Sum(p => p.Total),
                TotalPagamentos = pagamentos.Count,
                TotalTrocas = result.Trocas.Count,
                TotalCupons = result.Cupons.Count,
                RankingNome = ranking.Nome,
                RankingCssClass = ranking.CssClass,
                ValorElegivelRanking = ranking.ValorElegivel,
                ProximoMarcoRanking = ranking.ProximoMarco,
                ProximoRankingNome = ranking.ProximoNome,
                Pedidos = result.Pedidos.Select(p => new AdminClientePedidoTransacaoViewModel {
                    PedidoId = p.Id,
                    Data = p.Data,
                    Status = OrderStatusHelper.NormalizeDisplayStatus(p.Status),
                    Total = p.Total,
                    ResumoItens = MontarResumoItensPedido(p)
                }).ToList(),
                Pagamentos = pagamentos,
                Trocas = result.Trocas.Select(t => new AdminClienteTrocaTransacaoViewModel {
                    Codigo = t.Codigo,
                    PedidoId = t.PedidoId,
                    LivroTitulo = t.PedidoItem?.Livro?.Titulo ?? "Livro",
                    Status = ObterStatusTrocaExibicao(t),
                    Data = t.DataSolicitacao
                }).ToList(),
                Cupons = result.Cupons.Select(c => new AdminClienteCupomTransacaoViewModel {
                    Codigo = c.Codigo,
                    Tipo = c.Tipo,
                    Valor = c.Valor,
                    Status = c.DataUtilizacao.HasValue ? "Utilizado" : c.IsAtivo ? "Ativo" : "Inativo",
                    DataCriacao = c.DataCriacao,
                    PedidoId = c.PedidoId
                }).ToList()
            };
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
}
