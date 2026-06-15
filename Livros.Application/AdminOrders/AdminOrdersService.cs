using Livros.Application.Common.Logging;
using Livros.Domain;

namespace Livros.Application.AdminOrders {
    public sealed class AdminOrdersService {
        private const int PageSize = 10;
        private readonly IAdminOrdersDataProvider _dataProvider;
        private readonly IAppLogger<AdminOrdersService> _logger;

        public AdminOrdersService(IAdminOrdersDataProvider dataProvider, IAppLogger<AdminOrdersService> logger) {
            _dataProvider = dataProvider;
            _logger = logger;
        }

        public async Task<AdminOrdersResult> BuildAsync(AdminOrdersQuery query, CancellationToken cancellationToken = default) {
            var normalizedQuery = new AdminOrdersQuery {
                Busca = query.Busca,
                Status = query.Status,
                Pagina = Math.Max(query.Pagina, 1)
            };

            var pageData = await _dataProvider.LoadPageAsync(normalizedQuery, PageSize, cancellationToken);
            var pedidoIds = pageData.Pedidos.Select(p => p.Id).ToList();
            var trocasPorPedido = await _dataProvider.LoadTradeCountsAsync(pedidoIds, cancellationToken);

            return new AdminOrdersResult {
                Busca = normalizedQuery.Busca,
                StatusFiltro = normalizedQuery.Status,
                PaginaAtual = normalizedQuery.Pagina,
                TotalPaginas = Math.Max(1, (int)Math.Ceiling(pageData.TotalPedidos / (double)PageSize)),
                Pedidos = pageData.Pedidos,
                TrocasPorPedido = trocasPorPedido
            };
        }

        public async Task<AdminOrderStatusUpdateResult> UpdateStatusAsync(AdminOrderStatusUpdateCommand command, CancellationToken cancellationToken = default) {
            var pedido = await _dataProvider.LoadForStatusUpdateAsync(command.PedidoId, cancellationToken);
            if (pedido == null) {
                _logger.LogWarning("Pedido nao encontrado para atualizacao de status. PedidoId: {PedidoId}", command.PedidoId);
                return new AdminOrderStatusUpdateResult {
                    Succeeded = false,
                    Message = "Pedido nao encontrado."
                };
            }

            if (string.IsNullOrWhiteSpace(command.NovoStatus)) {
                _logger.LogWarning("Novo status vazio em atualizacao de pedido. PedidoId: {PedidoId}", command.PedidoId);
                return new AdminOrderStatusUpdateResult {
                    Succeeded = false,
                    Message = "Selecione um novo status para o pedido."
                };
            }

            var statusAtual = pedido.Status ?? string.Empty;
            var novoStatus = command.NovoStatus.Trim();
            var proximosStatus = OrderStatusHelper.GetNextStatuses(statusAtual).ToList();
            if (!proximosStatus.Contains(novoStatus)) {
                _logger.LogWarning("Transicao de status invalida. PedidoId: {PedidoId}, StatusAtual: {StatusAtual}, NovoStatus: {NovoStatus}", pedido.Id, statusAtual, novoStatus);
                return new AdminOrderStatusUpdateResult {
                    Succeeded = false,
                    Message = "A transicao de status informada nao e valida para este pedido."
                };
            }

            var livroIds = pedido.Itens.Select(i => i.LivroId).Distinct().ToList();
            var estoquesPorLivro = await _dataProvider.LoadStocksForBooksAsync(livroIds, cancellationToken);

            var estoqueEstaBaixado = OrderStatusHelper.RequiresStockDecrease(statusAtual);
            var estoqueDeveFicarBaixado = OrderStatusHelper.RequiresStockDecrease(novoStatus);

            if (!estoqueEstaBaixado && estoqueDeveFicarBaixado) {
                var erroBaixa = TryDecreaseStock(pedido, estoquesPorLivro);
                if (!string.IsNullOrWhiteSpace(erroBaixa)) {
                    _logger.LogWarning("Falha ao baixar estoque para o pedido {PedidoId}: {Mensagem}", pedido.Id, erroBaixa);
                    return new AdminOrderStatusUpdateResult {
                        Succeeded = false,
                        Message = erroBaixa
                    };
                }
            }
            else if (estoqueEstaBaixado && !estoqueDeveFicarBaixado) {
                ReplenishStock(pedido, estoquesPorLivro);
            }

            pedido.Status = novoStatus;
            UpdatePaymentsStatus(pedido, novoStatus);
            await _dataProvider.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Status do pedido atualizado. PedidoId: {PedidoId}, StatusAnterior: {StatusAnterior}, NovoStatus: {NovoStatus}", pedido.Id, statusAtual, novoStatus);

            return new AdminOrderStatusUpdateResult {
                Succeeded = true,
                Message = $"Pedido #{pedido.Id} atualizado para {novoStatus}."
            };
        }

        private static string? TryDecreaseStock(Pedido pedido, Dictionary<int, Estoque> estoquesPorLivro) {
            foreach (var item in pedido.Itens) {
                if (!estoquesPorLivro.TryGetValue(item.LivroId, out var estoque)) {
                    return $"Nao foi encontrado estoque para o livro \"{item.Livro?.Titulo ?? item.LivroId.ToString()}\".";
                }

                if (estoque.Quantidade < item.Quantidade) {
                    return $"Estoque insuficiente para o livro \"{item.Livro?.Titulo ?? item.LivroId.ToString()}\". Disponivel: {estoque.Quantidade}.";
                }
            }

            foreach (var item in pedido.Itens) {
                var estoque = estoquesPorLivro[item.LivroId];
                estoque.Quantidade -= item.Quantidade;
            }

            return null;
        }

        private void ReplenishStock(Pedido pedido, Dictionary<int, Estoque> estoquesPorLivro) {
            foreach (var item in pedido.Itens) {
                if (!estoquesPorLivro.TryGetValue(item.LivroId, out var estoque)) {
                    estoque = _dataProvider.CreateStock(item.LivroId);
                    estoquesPorLivro[item.LivroId] = estoque;
                }

                estoque.Quantidade += item.Quantidade;
            }
        }

        private static void UpdatePaymentsStatus(Pedido pedido, string novoStatus) {
            if (pedido.Pagamentos == null || !pedido.Pagamentos.Any()) {
                return;
            }

            var statusPagamento = OrderStatusHelper.NormalizeInternalStatus(novoStatus) switch {
                "APROVADA" => "Aprovado",
                "EM SEPARACAO" => "Aprovado",
                "EM TRANSPORTE" => "Aprovado",
                "ENTREGUE" => "Aprovado",
                "REPROVADA" => "Recusado",
                "CANCELADO" => "Cancelado",
                _ => "Pendente"
            };

            foreach (var pagamento in pedido.Pagamentos) {
                pagamento.Status = statusPagamento;
            }
        }
    }
}
