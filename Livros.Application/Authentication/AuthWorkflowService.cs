using Livros.Application.CustomerAccounts;
using Livros.Domain;
using System.Text.Json;

namespace Livros.Application.Authentication {
    public sealed class AuthWorkflowService {
        private readonly IAuthWorkflowDataProvider _dataProvider;

        public AuthWorkflowService(IAuthWorkflowDataProvider dataProvider) {
            _dataProvider = dataProvider;
        }

        public CustomerRegistrationResult Register(CustomerRegistrationCommand command) {
            if (_dataProvider.ActiveEmailExists(command.Email)) {
                return Failure("Este email já está cadastrado.");
            }

            if (!CustomerPasswordPolicy.IsStrongPassword(command.Senha)) {
                return Failure(CustomerPasswordPolicy.RequirementMessage);
            }

            var customer = new Cliente {
                Nome = command.Nome,
                Email = command.Email,
                Senha = _dataProvider.HashPassword(command.Senha),
                CPF = command.CPF,
                Telefone = command.Telefone,
                Genero = command.Genero,
                DataNascimento = command.DataNascimento
            };

            var state = ResolveState(command.Estado);
            var city = ResolveCity(command.Cidade, state.Id);
            var neighborhood = ResolveNeighborhood(command.Bairro, city.Id);

            var address = new Endereco {
                NomeEndereco = command.NomeEndereco.Trim(),
                CEP = command.CEP.Trim(),
                TipoLogradouro = string.IsNullOrWhiteSpace(command.TipoLogradouro) ? "Rua" : command.TipoLogradouro.Trim(),
                Logradouro = command.Logradouro.Trim(),
                Numero = command.Numero.Trim(),
                Complemento = string.IsNullOrWhiteSpace(command.Complemento) ? null : command.Complemento.Trim(),
                TipoResidencia = string.IsNullOrWhiteSpace(command.TipoResidencia) ? "Casa" : command.TipoResidencia.Trim(),
                Pais = string.IsNullOrWhiteSpace(command.Pais) ? "Brasil" : command.Pais.Trim(),
                CidadeId = city.Id,
                BairroId = neighborhood.Id,
                Cliente = customer,
                IsEntrega = true,
                IsCobranca = true,
                IsPadrao = true
            };

            customer.Enderecos = new List<Endereco> { address };

            _dataProvider.AddCustomer(customer);
            _dataProvider.SaveChanges();

            return new CustomerRegistrationResult {
                Success = true,
                Customer = customer
            };
        }

        public CustomerLoginCartMergeResult MergeCartOnLogin(CustomerLoginCartMergeCommand command) {
            var currentSessionItems = DeserializeCart(command.CurrentSessionCartJson);
            var persistedItems = DeserializeCart(command.PersistedCartJson);
            var mergedItems = persistedItems
                .Concat(currentSessionItems)
                .GroupBy(item => item.LivroId)
                .Select(group => new LoginCartItem {
                    LivroId = group.Key,
                    Quantidade = group.Sum(x => x.Quantidade)
                })
                .Where(item => item.Quantidade > 0)
                .ToList();

            foreach (var reservation in _dataProvider.LoadAnonymousReservations(command.SessionKey)) {
                reservation.ClienteId = command.CustomerId;
                reservation.SessionKey = null;
            }

            _dataProvider.SaveChanges();

            return new CustomerLoginCartMergeResult {
                HasItems = mergedItems.Any(),
                MergedCartJson = mergedItems.Any() ? JsonSerializer.Serialize(mergedItems) : null
            };
        }

        private Estado ResolveState(string stateCode) {
            var normalizedStateCode = (stateCode ?? string.Empty).Trim().ToUpperInvariant();
            var state = _dataProvider.LoadStateByCode(normalizedStateCode);
            if (state != null) {
                return state;
            }

            state = new Estado {
                Nome = normalizedStateCode,
                Sigla = normalizedStateCode
            };

            _dataProvider.AddState(state);
            _dataProvider.SaveChanges();
            return state;
        }

        private Cidade ResolveCity(string cityName, int stateId) {
            var normalizedCityName = (cityName ?? string.Empty).Trim();
            var city = _dataProvider.LoadCityByNameAndState(normalizedCityName, stateId);
            if (city != null) {
                return city;
            }

            city = new Cidade {
                Nome = normalizedCityName,
                EstadoId = stateId
            };

            _dataProvider.AddCity(city);
            _dataProvider.SaveChanges();
            return city;
        }

        private Bairro ResolveNeighborhood(string neighborhoodName, int cityId) {
            var normalizedNeighborhoodName = (neighborhoodName ?? string.Empty).Trim();
            var neighborhood = _dataProvider.LoadNeighborhoodByNameAndCity(normalizedNeighborhoodName, cityId);
            if (neighborhood != null) {
                return neighborhood;
            }

            neighborhood = new Bairro {
                Nome = normalizedNeighborhoodName,
                CidadeId = cityId
            };

            _dataProvider.AddNeighborhood(neighborhood);
            _dataProvider.SaveChanges();
            return neighborhood;
        }

        private static List<LoginCartItem> DeserializeCart(string? cartJson) {
            if (string.IsNullOrWhiteSpace(cartJson)) {
                return new List<LoginCartItem>();
            }

            return JsonSerializer.Deserialize<List<LoginCartItem>>(cartJson) ?? new List<LoginCartItem>();
        }

        private static CustomerRegistrationResult Failure(string message) {
            return new CustomerRegistrationResult {
                Success = false,
                ErrorMessage = message
            };
        }

        private sealed class LoginCartItem {
            public int LivroId { get; set; }
            public int Quantidade { get; set; }
        }
    }
}
