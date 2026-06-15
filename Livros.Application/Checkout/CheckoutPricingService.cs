using Livros.Domain;
namespace Livros.Application.Checkout {
    public sealed class CheckoutPricingService {
        private readonly ICheckoutPricingDataProvider _dataProvider;
        public CheckoutPricingService(ICheckoutPricingDataProvider dataProvider) {
            _dataProvider = dataProvider;
        }
        public CheckoutPricingResult Calculate(CheckoutPricingRequest request) {
            var shipping = CalculateShipping(new CheckoutShippingRequest {
                ClienteId = request.ClienteId,
                EnderecoId = request.EnderecoId,
                EstadoInformado = request.EstadoInformado,
                Quantidade = request.Quantidade
            });
            var coupons = ApplyCoupons(new CheckoutCouponApplicationRequest {
                ClienteId = request.ClienteId,
                CodigoCupom = request.CodigoCupom,
                CuponsTrocaSelecionados = request.CuponsTrocaSelecionados,
                Subtotal = request.Subtotal,
                Frete = shipping.Frete
            });
            return new CheckoutPricingResult {
                EstadoDestino = shipping.EstadoDestino,
                Frete = shipping.Frete,
                CupomPromocional = coupons.CupomPromocional,
                CodigoPromocionalAplicado = coupons.CodigoPromocionalAplicado,
                CuponsTrocaAplicados = coupons.CuponsTrocaAplicados,
                DescontoPromocional = coupons.DescontoPromocional,
                DescontoTroca = coupons.DescontoTroca,
                Mensagem = coupons.Mensagem
            };
        }
        public CheckoutShippingResult CalculateShipping(CheckoutShippingRequest request) {
            var estadoDestino = ResolveShippingState(request.ClienteId, request.EnderecoId, request.EstadoInformado);
            var frete = CalculateShippingValue(request.Quantidade, estadoDestino);
            return new CheckoutShippingResult {
                EstadoDestino = estadoDestino,
                Frete = frete
            };
        }
        public CheckoutPricingResult ApplyCoupons(CheckoutCouponApplicationRequest request) {
            var cupomDigitado = LoadTypedCoupon(request.ClienteId, request.CodigoCupom);
            var descontoPromocional = 0m;
            CupomDesconto? cupomPromocional = null;
            string? codigoPromocionalAplicado = null;
            if (cupomDigitado != null && !IsTradeCoupon(cupomDigitado)) {
                cupomPromocional = cupomDigitado;
                codigoPromocionalAplicado = cupomDigitado.Codigo;
                descontoPromocional = CalculateDiscount(cupomDigitado, null, request.Subtotal, request.Frete);
            } else if (string.Equals(request.CodigoCupom?.Trim(), "DESCONTO10", StringComparison.OrdinalIgnoreCase)) {
                codigoPromocionalAplicado = "DESCONTO10";
                descontoPromocional = CalculateDiscount(null, request.CodigoCupom, request.Subtotal, request.Frete);
            }
            var cuponsTrocaSelecionados = LoadTradeCoupons(request.ClienteId, request.CuponsTrocaSelecionados);
            if (cupomDigitado != null && IsTradeCoupon(cupomDigitado) && cuponsTrocaSelecionados.All(c => c.Id != cupomDigitado.Id)) {
                cuponsTrocaSelecionados.Add(cupomDigitado);
            }
            var subtotalRestante = Math.Max(request.Subtotal - descontoPromocional, 0);
            var baseTroca = subtotalRestante + request.Frete;
            if (!cuponsTrocaSelecionados.Any() || baseTroca <= 0) {
                return new CheckoutPricingResult {
                    Frete = request.Frete,
                    CupomPromocional = cupomPromocional,
                    CodigoPromocionalAplicado = codigoPromocionalAplicado,
                    DescontoPromocional = descontoPromocional
                };
            }
            var melhorCombinacao = ChooseBestTradeCouponCombination(cuponsTrocaSelecionados, baseTroca);
            var cuponsAplicados = melhorCombinacao.CuponsAplicados;
            var descontoTroca = Math.Min(baseTroca, melhorCombinacao.TotalSelecionado);
            string? mensagem = null;
            if (cuponsTrocaSelecionados.Count > cuponsAplicados.Count) {
                var quantidadeIgnorada = cuponsTrocaSelecionados.Count - cuponsAplicados.Count;
                mensagem = quantidadeIgnorada == 1
                    ? "Selecionamos automaticamente apenas os cupons necessarios para esta compra."
                    : "Selecionamos automaticamente a melhor combinacao de cupons para evitar sobra desnecessaria.";
            }
            return new CheckoutPricingResult {
                Frete = request.Frete,
                CupomPromocional = cupomPromocional,
                CodigoPromocionalAplicado = codigoPromocionalAplicado,
                CuponsTrocaAplicados = cuponsAplicados,
                DescontoPromocional = descontoPromocional,
                DescontoTroca = descontoTroca,
                Mensagem = mensagem
            };
        }
        public void MarkAppliedCouponsAsUsed(Pedido pedido, CheckoutPricingResult pricingResult) {
            if (pricingResult.CupomPromocional != null && pricingResult.DescontoPromocional > 0) {
                MarkPromotionalCouponAsUsed(pricingResult.CupomPromocional, pedido, pricingResult.DescontoPromocional);
            }
            if (pricingResult.CuponsTrocaAplicados.Any() && pricingResult.DescontoTroca > 0) {
                MarkTradeCouponsAsUsed(pricingResult.CuponsTrocaAplicados, pedido, pricingResult.DescontoTroca);
            }
        }
        private string ResolveShippingState(int clienteId, int? enderecoId, string? estadoInformado) {
            if (enderecoId.HasValue && enderecoId.Value > 0) {
                var estadoEndereco = _dataProvider.LoadStateForAddress(clienteId, enderecoId.Value);
                if (!string.IsNullOrWhiteSpace(estadoEndereco)) {
                    return estadoEndereco.Trim().ToUpperInvariant();
                }
            }
            if (!string.IsNullOrWhiteSpace(estadoInformado)) {
                return estadoInformado.Trim().ToUpperInvariant();
            }
            return "SP";
        }
        private static decimal CalculateShippingValue(int quantidade, string? estadoDestino) {
            if (quantidade <= 0) {
                quantidade = 1;
            }
            var uf = string.IsNullOrWhiteSpace(estadoDestino)
                ? "SP"
                : estadoDestino.Trim().ToUpperInvariant();
            decimal freteBase = uf switch {
                "SP" => 12m,
                "RJ" or "MG" or "ES" => 15m,
                "PR" or "SC" or "RS" => 18m,
                "DF" or "GO" or "MS" or "MT" => 20m,
                "BA" or "SE" or "AL" or "PE" or "PB" or "RN" or "CE" or "PI" or "MA" => 24m,
                "PA" or "AP" or "AM" or "RR" or "RO" or "AC" or "TO" => 29m,
                _ => 22m
            };
            var adicionalPorItem = uf == "SP" ? 1.50m : uf is "RJ" or "MG" or "ES" ? 2m : 2.50m;
            return freteBase + Math.Max(quantidade - 1, 0) * adicionalPorItem;
        }
        private CupomDesconto? LoadTypedCoupon(int clienteId, string? codigoCupom) {
            if (string.IsNullOrWhiteSpace(codigoCupom)) {
                return null;
            }
            return _dataProvider.LoadValidCoupon(clienteId, codigoCupom.Trim());
        }
        private List<CupomDesconto> LoadTradeCoupons(int clienteId, IReadOnlyCollection<int> ids) {
            var idsValidos = ids
                .Where(id => id > 0)
                .Distinct()
                .ToList();
            if (idsValidos.Count == 0) {
                return new List<CupomDesconto>();
            }
            return _dataProvider.LoadValidTradeCoupons(clienteId, idsValidos);
        }
        private static decimal CalculateDiscount(CupomDesconto? cupomAplicado, string? cupom, decimal subtotal, decimal frete = 0) {
            if (cupomAplicado != null) {
                var baseDesconto = IsTradeCoupon(cupomAplicado)
                    ? subtotal + frete
                    : subtotal;
                return Math.Min(baseDesconto, cupomAplicado.Valor);
            }
            if (string.IsNullOrWhiteSpace(cupom)) {
                return 0;
            }
            return string.Equals(cupom.Trim(), "DESCONTO10", StringComparison.OrdinalIgnoreCase)
                ? decimal.Round(subtotal * 0.10m, 2)
                : 0;
        }
        private static bool IsTradeCoupon(CupomDesconto cupom) {
            return string.Equals(cupom.Tipo, "TROCA", StringComparison.OrdinalIgnoreCase);
        }
        private static TradeCouponCombinationResult ChooseBestTradeCouponCombination(List<CupomDesconto> cupons, decimal valorAlvo) {
            var melhor = new TradeCouponCombinationResult {
                CuponsAplicados = cupons.ToList(),
                TotalSelecionado = cupons.Sum(c => c.Valor)
            };
            var quantidade = cupons.Count;
            if (quantidade == 0) {
                return melhor;
            }
            List<CupomDesconto>? melhorCobertura = null;
            decimal melhorTotalCobertura = decimal.MaxValue;
            var limite = 1 << quantidade;
            for (var mascara = 1; mascara < limite; mascara++) {
                var combinacao = new List<CupomDesconto>();
                decimal total = 0;
                for (var i = 0; i < quantidade; i++) {
                    if ((mascara & (1 << i)) == 0) {
                        continue;
                    }
                    combinacao.Add(cupons[i]);
                    total += cupons[i].Valor;
                }
                if (total < valorAlvo) {
                    continue;
                }
                if (melhorCobertura == null
                    || total < melhorTotalCobertura
                    || (total == melhorTotalCobertura && combinacao.Count < melhorCobertura.Count)) {
                    melhorCobertura = combinacao;
                    melhorTotalCobertura = total;
                }
            }
            if (melhorCobertura != null) {
                melhor.CuponsAplicados = melhorCobertura;
                melhor.TotalSelecionado = melhorTotalCobertura;
            }
            return melhor;
        }
        private void MarkPromotionalCouponAsUsed(CupomDesconto cupomAplicado, Pedido pedido, decimal descontoAplicado) {
            var valorOriginal = cupomAplicado.Valor;
            var valorUtilizado = Math.Min(valorOriginal, descontoAplicado);
            var saldoRestante = Math.Max(valorOriginal - valorUtilizado, 0);
            cupomAplicado.Valor = valorUtilizado;
            cupomAplicado.IsAtivo = false;
            cupomAplicado.DataUtilizacao = DateTime.Now;
            cupomAplicado.PedidoId = pedido.Id;
            if (IsTradeCoupon(cupomAplicado) && saldoRestante > 0) {
                _dataProvider.AddCoupon(new CupomDesconto {
                    Codigo = $"TROCA-{DateTime.Now:yyyyMMddHHmmss}",
                    Valor = decimal.Round(saldoRestante, 2),
                    Tipo = "TROCA",
                    IsAtivo = true,
                    ClienteId = cupomAplicado.ClienteId,
                    DataCriacao = DateTime.Now
                });
            }
        }
        private void MarkTradeCouponsAsUsed(List<CupomDesconto> cuponsAplicados, Pedido pedido, decimal descontoAplicado) {
            var restanteParaConsumir = descontoAplicado;
            var saldoRestanteTotal = 0m;
            foreach (var cupom in cuponsAplicados
                .OrderBy(c => c.Valor)
                .ThenBy(c => c.Id)) {
                var valorOriginal = cupom.Valor;
                var valorUtilizado = Math.Min(valorOriginal, Math.Max(restanteParaConsumir, 0));
                var saldoRestante = Math.Max(valorOriginal - valorUtilizado, 0);
                cupom.Valor = decimal.Round(valorUtilizado, 2);
                cupom.IsAtivo = false;
                cupom.DataUtilizacao = DateTime.Now;
                cupom.PedidoId = pedido.Id;
                restanteParaConsumir -= valorUtilizado;
                saldoRestanteTotal += saldoRestante;
            }
            if (saldoRestanteTotal > 0) {
                _dataProvider.AddCoupon(new CupomDesconto {
                    Codigo = $"TROCA-{DateTime.Now:yyyyMMddHHmmss}",
                    Valor = decimal.Round(saldoRestanteTotal, 2),
                    Tipo = "TROCA",
                    IsAtivo = true,
                    ClienteId = cuponsAplicados.First().ClienteId,
                    DataCriacao = DateTime.Now
                });
            }
        }
        private sealed class TradeCouponCombinationResult {
            public List<CupomDesconto> CuponsAplicados { get; set; } = new();
            public decimal TotalSelecionado { get; set; }
        }
    }
}
