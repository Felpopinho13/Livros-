using Livros.Domain;
using System.Text.RegularExpressions;
namespace Livros.Application.Checkout {
    public sealed class CheckoutPaymentService {
        private readonly ICheckoutPaymentDataProvider _dataProvider;
        public CheckoutPaymentService(ICheckoutPaymentDataProvider dataProvider) {
            _dataProvider = dataProvider;
        }
        public List<string> Validate(CheckoutPaymentValidationRequest request) {
            var errors = new List<string>();
            var totalArredondado = decimal.Round(request.Total, 2);
            if (totalArredondado <= 0 && request.PermitirTotalZeroPorCupom) {
                return errors;
            }
            var pagamentos = request.Pagamentos.ToList();
            if (pagamentos.Count == 0 || string.IsNullOrWhiteSpace(pagamentos[0].Metodo)) {
                errors.Add("Selecione pelo menos uma forma de pagamento.");
                return errors;
            }
            var encontrouLacuna = false;
            foreach (var pagamento in pagamentos) {
                if (string.IsNullOrWhiteSpace(pagamento.Metodo)) {
                    encontrouLacuna = true;
                    continue;
                }
                if (encontrouLacuna) {
                    errors.Add("Adicione os meios de pagamento em sequencia, sem pular blocos intermediarios.");
                    break;
                }
                if (pagamento.Valor <= 0) {
                    errors.Add($"Informe um valor valido para o pagamento {pagamento.Indice}.");
                }
            }
            var pagamentosAtivos = pagamentos.Where(p => !string.IsNullOrWhiteSpace(p.Metodo)).ToList();
            var soma = decimal.Round(pagamentosAtivos.Sum(p => p.Valor), 2);
            if (soma != totalArredondado) {
                errors.Add("A soma dos pagamentos deve ser igual ao total do pedido.");
            }
            var quantidadePagamentosCartao = pagamentosAtivos.Count(p => string.Equals(p.Metodo, "cartao", StringComparison.OrdinalIgnoreCase));
            foreach (var pagamento in pagamentosAtivos) {
                errors.AddRange(ValidateCardPayment(request.ClienteId, pagamento, quantidadePagamentosCartao >= 2));
            }
            return errors;
        }
        public void AppendPaymentsToOrder(int clienteId, IEnumerable<CheckoutPaymentSlot> pagamentos, Pedido pedido) {
            foreach (var pagamento in pagamentos.Where(p => !string.IsNullOrWhiteSpace(p.Metodo))) {
                if (string.IsNullOrWhiteSpace(pagamento.Metodo) || pagamento.Valor <= 0) {
                    continue;
                }
                if (string.Equals(pagamento.Metodo, "cartao", StringComparison.OrdinalIgnoreCase)
                    && (!pagamento.CartaoId.HasValue || pagamento.CartaoId.Value == 0)
                    && pagamento.SalvarNovoCartao) {
                    var novoCartao = new Cartao {
                        ClienteId = clienteId,
                        NomeImpresso = (pagamento.NomeCartao ?? string.Empty).Trim(),
                        Numero = NormalizeDigits(pagamento.NumeroCartao),
                        Validade = (pagamento.Validade ?? string.Empty).Trim(),
                        CVV = NormalizeDigits(pagamento.CVV),
                        BandeiraCartaoId = pagamento.BandeiraCartaoId ?? 0
                    };
                    _dataProvider.AddCard(novoCartao);
                }
                pedido.Pagamentos.Add(new Pagamento {
                    Metodo = pagamento.Metodo.Trim().ToLowerInvariant(),
                    Valor = pagamento.Valor,
                    Status = "Pendente"
                });
            }
        }
        private List<string> ValidateCardPayment(int clienteId, CheckoutPaymentSlot pagamento, bool exigirValorMinimo) {
            var errors = new List<string>();
            if (!string.Equals(pagamento.Metodo, "cartao", StringComparison.OrdinalIgnoreCase)) {
                return errors;
            }
            if (exigirValorMinimo && pagamento.Valor < 10) {
                errors.Add($"O pagamento {pagamento.Indice} com cartao deve ter valor minimo de R$ 10,00.");
            }
            if (pagamento.CartaoId.HasValue && pagamento.CartaoId.Value > 0) {
                var cartaoExistente = _dataProvider.LoadCustomerCardWithBrand(clienteId, pagamento.CartaoId.Value);
                if (cartaoExistente == null) {
                    errors.Add($"Selecione um cartao valido no pagamento {pagamento.Indice}.");
                } else if (cartaoExistente.BandeiraCartao == null || !cartaoExistente.BandeiraCartao.IsAtiva) {
                    errors.Add($"O cartao selecionado no pagamento {pagamento.Indice} possui uma bandeira invalida.");
                }
                return errors;
            }
            if (string.IsNullOrWhiteSpace(pagamento.NomeCartao)
                || string.IsNullOrWhiteSpace(pagamento.NumeroCartao)
                || string.IsNullOrWhiteSpace(pagamento.Validade)
                || string.IsNullOrWhiteSpace(pagamento.CVV)) {
                errors.Add($"Preencha os dados completos do novo cartao no pagamento {pagamento.Indice}.");
                return errors;
            }
            if (!pagamento.BandeiraCartaoId.HasValue || !_dataProvider.IsCardBrandActive(pagamento.BandeiraCartaoId.Value)) {
                errors.Add($"Selecione uma bandeira valida no pagamento {pagamento.Indice}.");
            }
            var numeroNormalizado = NormalizeDigits(pagamento.NumeroCartao);
            if (numeroNormalizado.Length != 16) {
                errors.Add($"O cartao do pagamento {pagamento.Indice} deve ter exatamente 16 digitos.");
            }
            var cvvNormalizado = NormalizeDigits(pagamento.CVV);
            if (cvvNormalizado.Length != 3) {
                errors.Add($"O CVV do pagamento {pagamento.Indice} deve ter exatamente 3 digitos.");
            }
            if (!Regex.IsMatch(pagamento.Validade.Trim(), "^(0[1-9]|1[0-2])\\/\\d{2}$")) {
                errors.Add($"A validade do pagamento {pagamento.Indice} deve estar no formato MM/AA.");
            }
            return errors;
        }
        private static string NormalizeDigits(string? valor) {
            if (string.IsNullOrWhiteSpace(valor)) {
                return string.Empty;
            }
            return new string(valor.Where(char.IsDigit).ToArray());
        }
    }
}
