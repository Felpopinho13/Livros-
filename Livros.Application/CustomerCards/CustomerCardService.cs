using Livros.Domain;
using System.Text.RegularExpressions;

namespace Livros.Application.CustomerCards {
    public sealed class CustomerCardService {
        private readonly ICustomerCardDataProvider _dataProvider;

        public CustomerCardService(ICustomerCardDataProvider dataProvider) {
            _dataProvider = dataProvider;
        }

        public CustomerCardsResult List(CustomerCardsQuery query) {
            var customer = _dataProvider.LoadCustomerByEmailWithCards(query.Email);
            if (customer == null) {
                return new CustomerCardsResult {
                    CustomerFound = false
                };
            }

            return new CustomerCardsResult {
                CustomerFound = true,
                Cards = (customer.Cartoes ?? new List<Cartao>())
                    .OrderByDescending(c => c.IsPadrao)
                    .ThenBy(c => c.NomeImpresso)
                    .ToList(),
                Brands = _dataProvider.LoadActiveBrands()
            };
        }

        public CustomerCardCommandResult Create(CustomerCardCreateCommand command) {
            var customer = _dataProvider.LoadCustomerById(command.ClienteId);
            if (customer == null) {
                return new CustomerCardCommandResult {
                    CustomerFound = false,
                    Success = false
                };
            }

            var brand = _dataProvider.LoadActiveBrandById(command.BandeiraCartaoId);
            if (brand == null) {
                return Failure("Selecione uma bandeira de cartao valida.");
            }

            if (string.IsNullOrWhiteSpace(command.Nome) ||
                string.IsNullOrWhiteSpace(command.Numero) ||
                string.IsNullOrWhiteSpace(command.Validade) ||
                string.IsNullOrWhiteSpace(command.Cvv)) {
                return Failure("Preencha todos os dados obrigatorios do cartao.");
            }

            var normalizedNumber = NormalizeDigits(command.Numero);
            if (normalizedNumber.Length != 16) {
                return Failure("O numero do cartao deve ter exatamente 16 digitos.");
            }

            var normalizedCvv = NormalizeDigits(command.Cvv);
            if (normalizedCvv.Length != 3) {
                return Failure("O CVV deve ter exatamente 3 digitos.");
            }

            if (!Regex.IsMatch((command.Validade ?? string.Empty).Trim(), "^(0[1-9]|1[0-2])/\\d{2}$")) {
                return Failure("A validade deve estar no formato MM/AA.");
            }

            var card = new Cartao {
                NomeImpresso = command.Nome.Trim(),
                Numero = normalizedNumber,
                Validade = command.Validade!.Trim(),
                CVV = normalizedCvv,
                BandeiraCartaoId = brand.Id,
                ClienteId = customer.Id
            };

            _dataProvider.AddCard(card);
            _dataProvider.SaveChanges();

            return new CustomerCardCommandResult {
                CustomerFound = true,
                Success = true,
                CardId = card.Id
            };
        }

        public CustomerCardCommandResult SetDefault(CustomerCardSetDefaultCommand command) {
            var customer = _dataProvider.LoadCustomerByEmailWithCards(command.Email);
            if (customer?.Cartoes == null || !customer.Cartoes.Any()) {
                return new CustomerCardCommandResult {
                    CustomerFound = false,
                    Success = false,
                    ErrorMessage = "Cliente ou cartoes nao encontrados."
                };
            }

            var card = customer.Cartoes.FirstOrDefault(c => c.Id == command.CardId);
            if (card == null) {
                return new CustomerCardCommandResult {
                    CustomerFound = true,
                    Success = false,
                    CardFound = false,
                    ErrorMessage = "Cartao nao encontrado."
                };
            }

            foreach (var item in customer.Cartoes) {
                item.IsPadrao = false;
            }

            card.IsPadrao = true;
            _dataProvider.SaveChanges();

            return new CustomerCardCommandResult {
                CustomerFound = true,
                Success = true,
                CardId = card.Id
            };
        }

        public CustomerCardCommandResult Delete(CustomerCardDeleteCommand command) {
            var card = _dataProvider.LoadCardByIdForCustomer(command.Email, command.CardId);
            if (card == null) {
                return new CustomerCardCommandResult {
                    CustomerFound = true,
                    Success = false,
                    CardFound = false
                };
            }

            _dataProvider.RemoveCard(card);
            _dataProvider.SaveChanges();

            return new CustomerCardCommandResult {
                CustomerFound = true,
                Success = true
            };
        }

        private static string NormalizeDigits(string? value) {
            if (string.IsNullOrWhiteSpace(value)) {
                return string.Empty;
            }

            return new string(value.Where(char.IsDigit).ToArray());
        }

        private static CustomerCardCommandResult Failure(string message) {
            return new CustomerCardCommandResult {
                CustomerFound = true,
                Success = false,
                ErrorMessage = message
            };
        }
    }
}