using Livros.Domain;

namespace Livros.Application.AdminExchanges {
    public sealed class AdminExchangesService {
        private const int PageSize = 10;
        private readonly IAdminExchangesDataProvider _dataProvider;

        public AdminExchangesService(IAdminExchangesDataProvider dataProvider) {
            _dataProvider = dataProvider;
        }

        public async Task<AdminExchangesResult> BuildAsync(AdminExchangesQuery query, CancellationToken cancellationToken = default) {
            var normalizedQuery = new AdminExchangesQuery {
                Busca = query.Busca,
                Status = query.Status,
                PaginaTrocas = Math.Max(query.PaginaTrocas, 1),
                PaginaCupons = Math.Max(query.PaginaCupons, 1)
            };

            var pageData = await _dataProvider.LoadPageAsync(normalizedQuery, PageSize, cancellationToken);

            return new AdminExchangesResult {
                Busca = normalizedQuery.Busca,
                StatusFiltro = normalizedQuery.Status,
                PaginaTrocasAtual = normalizedQuery.PaginaTrocas,
                TotalPaginasTrocas = Math.Max(1, (int)Math.Ceiling(pageData.TotalTrocas / (double)PageSize)),
                PaginaCuponsAtual = normalizedQuery.PaginaCupons,
                TotalPaginasCupons = Math.Max(1, (int)Math.Ceiling(pageData.TotalCupons / (double)PageSize)),
                Trocas = pageData.Trocas,
                CuponsRecentes = pageData.CuponsRecentes,
                Cupons = pageData.CuponsPagina,
                ClientesAtivos = pageData.ClientesAtivos,
                ValoresSugeridosCupomPorTroca = pageData.ValoresSugeridosCupomPorTroca
            };
        }

        public async Task<AdminExchangeActionResult> AnalyzeAsync(AdminExchangeAnalysisCommand command, CancellationToken cancellationToken = default) {
            var troca = await _dataProvider.GetTradeForUpdateAsync(command.TrocaId, cancellationToken);
            if (troca == null) {
                return Failure("Solicitacao de troca nao encontrada.");
            }

            if (!IsRequestedTrade(troca)) {
                return Failure("Esta solicitacao ja foi analisada.");
            }

            troca.ObservacaoAdmin = command.ObservacaoAdmin?.Trim();
            troca.DataAnalise = DateTime.Now;

            if (string.Equals(command.Decisao, "aprovar", StringComparison.OrdinalIgnoreCase)) {
                troca.Status = "TROCA AUTORIZADA";
                await _dataProvider.SaveChangesAsync(cancellationToken);
                return Success("Troca autorizada com sucesso. O cupom sera gerado somente apos o recebimento do item devolvido.");
            }

            troca.Status = "TROCA RECUSADA";
            await _dataProvider.SaveChangesAsync(cancellationToken);
            return Success("Solicitacao de troca recusada com sucesso.");
        }

        public async Task<AdminExchangeActionResult> ConfirmReceiptAsync(AdminExchangeReceiptCommand command, CancellationToken cancellationToken = default) {
            var troca = await _dataProvider.GetTradeForUpdateAsync(command.TrocaId, cancellationToken);
            if (troca == null) {
                return Failure("Solicitacao de troca nao encontrada.");
            }

            if (!IsAuthorizedTrade(troca)) {
                return Failure("Somente trocas autorizadas podem ter o recebimento confirmado.");
            }

            var valorSugerido = await _dataProvider.CalculateSuggestedCouponValueAsync(troca, cancellationToken);
            var valorCupom = command.ValorCupom > 0 ? command.ValorCupom : valorSugerido;

            if (valorCupom <= 0) {
                return Failure("Informe um valor valido para o cupom de troca.");
            }

            troca.ObservacaoAdmin = command.ObservacaoAdmin?.Trim();
            troca.DataRecebimento = DateTime.Now;
            troca.RetornarAoEstoque = command.RetornarAoEstoque;
            troca.Status = "TROCADO";

            if (command.RetornarAoEstoque) {
                await _dataProvider.ReintegrateTradeItemToStockAsync(troca.PedidoItem, cancellationToken);
            }

            if (!troca.CupomDescontoId.HasValue) {
                var cupom = await _dataProvider.CreateCouponAsync(new CupomDesconto {
                    Codigo = GenerateCouponCode("TROCA"),
                    Valor = valorCupom,
                    Tipo = "TROCA",
                    IsAtivo = true,
                    ClienteId = troca.ClienteId,
                    DataCriacao = DateTime.Now
                }, cancellationToken);

                troca.CupomDescontoId = cupom.Id;
                await _dataProvider.SaveChangesAsync(cancellationToken);

                var mensagemComCupom = command.RetornarAoEstoque
                    ? $"Recebimento confirmado, item devolvido reintegrado ao estoque e cupom {cupom.Codigo} gerado."
                    : $"Recebimento confirmado e cupom {cupom.Codigo} gerado com sucesso.";

                return Success(mensagemComCupom);
            }

            await _dataProvider.SaveChangesAsync(cancellationToken);

            return Success(command.RetornarAoEstoque
                ? "Recebimento confirmado e item devolvido reintegrado ao estoque."
                : "Recebimento confirmado com sucesso.");
        }

        public async Task<AdminExchangeActionResult> GeneratePromotionalCouponAsync(AdminPromotionalCouponCommand command, CancellationToken cancellationToken = default) {
            if (command.Valor <= 0) {
                return Failure("Informe um valor valido para gerar o cupom promocional.");
            }

            var codigoBase = GenerateCouponCode("PROMO");

            if (string.Equals(command.Destinatario, "todos", StringComparison.OrdinalIgnoreCase)) {
                var clientesAtivos = await _dataProvider.GetActiveCustomersAsync(cancellationToken);
                if (clientesAtivos.Count == 0) {
                    return Failure("Nao ha clientes ativos para receber este cupom.");
                }

                await _dataProvider.CreateCouponsAsync(
                    clientesAtivos.Select(cliente => new CupomDesconto {
                        Codigo = codigoBase,
                        Valor = command.Valor,
                        Tipo = "PROMOCIONAL",
                        IsAtivo = true,
                        ClienteId = cliente.Id,
                        DataCriacao = DateTime.Now
                    }),
                    cancellationToken);

                return Success($"Cupom promocional {codigoBase} gerado para {clientesAtivos.Count} cliente(s).");
            }

            if (string.Equals(command.Destinatario, "cliente", StringComparison.OrdinalIgnoreCase)) {
                if (!command.ClienteId.HasValue || command.ClienteId.Value <= 0) {
                    return Failure("Selecione um cliente valido para vincular o cupom.");
                }

                var cliente = await _dataProvider.GetActiveCustomerAsync(command.ClienteId.Value, cancellationToken);
                if (cliente == null) {
                    return Failure("Nao foi possivel localizar o cliente selecionado.");
                }

                var cupomCliente = await _dataProvider.CreateCouponAsync(new CupomDesconto {
                    Codigo = codigoBase,
                    Valor = command.Valor,
                    Tipo = "PROMOCIONAL",
                    IsAtivo = true,
                    ClienteId = cliente.Id,
                    DataCriacao = DateTime.Now
                }, cancellationToken);

                return Success($"Cupom promocional {cupomCliente.Codigo} gerado para {cliente.Nome}.");
            }

            var cupom = await _dataProvider.CreateCouponAsync(new CupomDesconto {
                Codigo = codigoBase,
                Valor = command.Valor,
                Tipo = "PROMOCIONAL",
                IsAtivo = true,
                DataCriacao = DateTime.Now
            }, cancellationToken);

            return Success($"Cupom promocional {cupom.Codigo} gerado com sucesso.");
        }

        public async Task<AdminExchangeActionResult> DeactivateCouponAsync(AdminCouponDeactivationCommand command, CancellationToken cancellationToken = default) {
            var cupom = await _dataProvider.GetCouponAsync(command.CupomId, cancellationToken);
            if (cupom == null) {
                return Failure("Cupom nao encontrado.");
            }

            if (!cupom.IsAtivo || cupom.DataUtilizacao.HasValue) {
                return Failure("Este cupom nao pode ser desativado manualmente.");
            }

            if (!string.Equals(cupom.Tipo, "PROMOCIONAL", StringComparison.OrdinalIgnoreCase)) {
                return Failure("Apenas cupons promocionais podem ser desativados manualmente.");
            }

            cupom.IsAtivo = false;
            await _dataProvider.SaveChangesAsync(cancellationToken);
            return Success($"Cupom {cupom.Codigo} desativado com sucesso.");
        }

        private static bool IsRequestedTrade(Troca troca) {
            return string.Equals(troca.Status, "EM TROCA", StringComparison.OrdinalIgnoreCase)
                || string.Equals(troca.Status, "Solicitado", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAuthorizedTrade(Troca troca) {
            return string.Equals(troca.Status, "TROCA AUTORIZADA", StringComparison.OrdinalIgnoreCase)
                || string.Equals(troca.Status, "Autorizada", StringComparison.OrdinalIgnoreCase)
                || (string.Equals(troca.Status, "Aprovado", StringComparison.OrdinalIgnoreCase) && !troca.CupomDescontoId.HasValue);
        }

        private static string GenerateCouponCode(string prefixo) {
            return $"{prefixo}-{DateTime.Now:yyyyMMddHHmmss}";
        }

        private static AdminExchangeActionResult Success(string message) {
            return new AdminExchangeActionResult {
                Succeeded = true,
                Message = message
            };
        }

        private static AdminExchangeActionResult Failure(string message) {
            return new AdminExchangeActionResult {
                Succeeded = false,
                Message = message
            };
        }
    }
}
