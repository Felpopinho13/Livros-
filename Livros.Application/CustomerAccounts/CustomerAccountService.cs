using Livros.Domain;

namespace Livros.Application.CustomerAccounts {
    public sealed class CustomerAccountService {
        private static readonly HashSet<string> EligibleRankingStatuses = new(StringComparer.OrdinalIgnoreCase) {
            "APROVADA",
            "PAGAMENTO APROVADO",
            "EM SEPARACAO",
            "EM TRANSPORTE",
            "ENVIADO",
            "ENTREGUE"
        };

        private readonly ICustomerAccountDataProvider _dataProvider;

        public CustomerAccountService(ICustomerAccountDataProvider dataProvider) {
            _dataProvider = dataProvider;
        }

        public CustomerDashboardResult GetDashboard(CustomerDashboardQuery query) {
            var customer = _dataProvider.LoadActiveCustomerByEmailWithAddressesAndCards(query.Email);
            if (customer == null) {
                return new CustomerDashboardResult {
                    CustomerFound = false
                };
            }

            var orders = _dataProvider.LoadOrdersByCustomerIdWithItemsAndBooks(customer.Id);
            var latestOrder = orders.FirstOrDefault();
            var availableCoupons = _dataProvider.LoadCouponsByCustomerId(customer.Id)
                .Where(c => c.IsAtivo && c.DataUtilizacao == null)
                .OrderByDescending(c => c.DataCriacao)
                .ToList();

            var ranking = BuildRanking(CalculateEligibleOrderValue(orders));
            var displayName = string.IsNullOrWhiteSpace(customer.Nome) ? customer.Email : customer.Nome;
            var firstName = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? displayName;

            return new CustomerDashboardResult {
                CustomerFound = true,
                NomeExibicao = displayName,
                PrimeiroNome = firstName,
                Email = customer.Email,
                TotalPedidos = orders.Count,
                ValorTotalCompras = orders.Sum(p => p.Total),
                QuantidadeEnderecos = customer.Enderecos?.Count(e => e.IsEntrega || e.IsCobranca) ?? 0,
                QuantidadeCartoes = customer.Cartoes?.Count ?? 0,
                QuantidadeCuponsDisponiveis = availableCoupons.Count,
                QuantidadeTrocasAbertas = _dataProvider.CountOpenExchangesByCustomerId(customer.Id),
                ItensNoCarrinho = query.CartItemCount,
                RankingNome = ranking.Name,
                RankingCssClass = ranking.CssClass,
                ValorElegivelRanking = ranking.EligibleValue,
                ProximoMarcoRanking = ranking.NextMilestone,
                ProximoRankingNome = ranking.NextName,
                UltimoPedido = latestOrder == null ? null : new CustomerDashboardOrderSummaryData {
                    Id = latestOrder.Id,
                    Data = latestOrder.Data,
                    Total = latestOrder.Total,
                    Status = latestOrder.Status,
                    QuantidadeItens = latestOrder.Itens?.Sum(i => i.Quantidade) ?? 0,
                    LivroPrincipal = latestOrder.Itens?.FirstOrDefault()?.Livro?.Titulo ?? "Pedido sem itens"
                },
                UltimoCupomDisponivel = availableCoupons
                    .Select(c => new CustomerDashboardCouponSummaryData {
                        Codigo = c.Codigo,
                        Valor = c.Valor,
                        Tipo = c.Tipo
                    })
                    .FirstOrDefault()
            };
        }

        public CustomerCouponsResult GetCoupons(CustomerCouponsQuery query) {
            var customer = _dataProvider.LoadActiveCustomerByEmail(query.Email);
            if (customer == null) {
                return new CustomerCouponsResult {
                    CustomerFound = false
                };
            }

            var coupons = _dataProvider.LoadCouponsByCustomerId(customer.Id)
                .OrderByDescending(c => c.DataCriacao)
                .ToList();

            return new CustomerCouponsResult {
                CustomerFound = true,
                NomeCliente = string.IsNullOrWhiteSpace(customer.Nome) ? customer.Email : customer.Nome,
                TotalCupons = coupons.Count,
                CuponsDisponiveis = coupons.Count(c => c.IsAtivo && c.DataUtilizacao == null),
                ValorDisponivel = coupons
                    .Where(c => c.IsAtivo && c.DataUtilizacao == null)
                    .Sum(c => c.Valor),
                Cupons = coupons.Select(c => new CustomerCouponData {
                    Codigo = c.Codigo,
                    Tipo = c.Tipo,
                    Valor = c.Valor,
                    DataCriacao = c.DataCriacao,
                    DataUtilizacao = c.DataUtilizacao,
                    PedidoId = c.PedidoId,
                    Status = c.DataUtilizacao != null
                        ? "Utilizado"
                        : c.IsAtivo
                            ? "Disponível"
                            : "Inativo",
                    Descricao = c.Tipo == "TROCA"
                        ? "Cupom de troca liberado a partir de uma solicitacao aprovada. Pode ser usado uma unica vez no checkout."
                        : "Cupom promocional para abater o valor dos produtos. Pode ser usado uma unica vez no checkout."
                }).ToList()
            };
        }

        public CustomerProfileResult GetProfile(CustomerProfileQuery query) {
            var customer = _dataProvider.LoadActiveCustomerByEmail(query.Email);
            return new CustomerProfileResult {
                CustomerFound = customer != null,
                Customer = customer
            };
        }

        public CustomerAccountCommandResult UpdateProfile(CustomerProfileUpdateCommand command) {
            var customer = _dataProvider.LoadCustomerById(command.CustomerId);
            if (customer == null || !customer.IsAtivo) {
                return new CustomerAccountCommandResult {
                    CustomerFound = false,
                    Success = false
                };
            }

            var normalizedEmail = (command.Email ?? string.Empty).Trim();
            if (_dataProvider.EmailExistsForAnotherCustomer(normalizedEmail, customer.Id)) {
                return Failure("Este email já está em uso.");
            }

            customer.Nome = (command.Nome ?? string.Empty).Trim();
            customer.Email = normalizedEmail;
            customer.Telefone = string.IsNullOrWhiteSpace(command.Telefone) ? null : command.Telefone.Trim();
            customer.CPF = string.IsNullOrWhiteSpace(command.CPF) ? null : command.CPF.Trim();

            _dataProvider.SaveChanges();

            return new CustomerAccountCommandResult {
                CustomerFound = true,
                Success = true,
                UpdatedEmail = customer.Email
            };
        }

        public CustomerAccountCommandResult DeactivateAccount(string email) {
            var customer = _dataProvider.LoadActiveCustomerByEmail(email);
            if (customer == null) {
                return new CustomerAccountCommandResult {
                    CustomerFound = false,
                    Success = false
                };
            }

            customer.IsAtivo = false;
            _dataProvider.SaveChanges();

            return new CustomerAccountCommandResult {
                CustomerFound = true,
                Success = true
            };
        }

        public CustomerAccountCommandResult ChangePassword(CustomerPasswordChangeCommand command) {
            if (command.NewPassword != command.ConfirmPassword) {
                return Failure("As senhas não coincidem.");
            }

            if (!CustomerPasswordPolicy.IsStrongPassword(command.NewPassword)) {
                return Failure(CustomerPasswordPolicy.RequirementMessage);
            }

            var customer = _dataProvider.LoadCustomerById(command.CustomerId);
            if (customer == null || !customer.IsAtivo) {
                return new CustomerAccountCommandResult {
                    CustomerFound = false,
                    Success = false
                };
            }

            if (!_dataProvider.VerifyPassword(command.CurrentPassword, customer.Senha ?? string.Empty)) {
                return Failure("A senha atual informada está incorreta.");
            }

            customer.Senha = _dataProvider.HashPassword(command.NewPassword);
            _dataProvider.SaveChanges();

            return new CustomerAccountCommandResult {
                CustomerFound = true,
                Success = true
            };
        }

        private static decimal CalculateEligibleOrderValue(IEnumerable<Pedido> orders) {
            return decimal.Round(
                orders.Where(order => CountsForRanking(order.Status))
                    .Sum(order => order.Total),
                2);
        }

        private static bool CountsForRanking(string? status) {
            return !string.IsNullOrWhiteSpace(status)
                && EligibleRankingStatuses.Contains(status.Trim());
        }

        private static RankingData BuildRanking(decimal eligibleValue) {
            if (eligibleValue >= 1000m) {
                return new RankingData("Diamante", "diamante", eligibleValue, null, null);
            }

            if (eligibleValue >= 500m) {
                return new RankingData("Ouro", "ouro", eligibleValue, 1000m, "Diamante");
            }

            if (eligibleValue >= 200m) {
                return new RankingData("Prata", "prata", eligibleValue, 500m, "Ouro");
            }

            return new RankingData("Bronze", "bronze", eligibleValue, 200m, "Prata");
        }

        private static CustomerAccountCommandResult Failure(string message) {
            return new CustomerAccountCommandResult {
                CustomerFound = true,
                Success = false,
                ErrorMessage = message
            };
        }

        private sealed record RankingData(
            string Name,
            string CssClass,
            decimal EligibleValue,
            decimal? NextMilestone,
            string? NextName);
    }
}
