using Livros.Domain;

namespace Livros.Application.CustomerAddresses {
    public sealed class CustomerAddressService {
        private readonly ICustomerAddressDataProvider _dataProvider;

        public CustomerAddressService(ICustomerAddressDataProvider dataProvider) {
            _dataProvider = dataProvider;
        }

        public CustomerAddressListResult List(CustomerAddressListQuery query) {
            var customer = _dataProvider.LoadCustomerByEmailWithAddresses(query.Email);
            if (customer == null) {
                return new CustomerAddressListResult {
                    CustomerFound = false
                };
            }

            var addresses = SortSavedAddresses(customer.Enderecos ?? new List<Endereco>());

            return new CustomerAddressListResult {
                CustomerFound = true,
                Addresses = addresses
            };
        }

        public CustomerAddressDetailsResult GetForEdit(CustomerAddressEditQuery query) {
            var address = _dataProvider.LoadSavedAddressByIdWithRelationsForCustomer(query.Email, query.AddressId);
            if (address == null) {
                return new CustomerAddressDetailsResult {
                    Found = false
                };
            }

            return new CustomerAddressDetailsResult {
                Found = true,
                Address = new CustomerAddressEditData {
                    Id = address.Id,
                    NomeEndereco = address.NomeEndereco,
                    CEP = address.CEP,
                    TipoLogradouro = address.TipoLogradouro,
                    Logradouro = address.Logradouro,
                    Numero = address.Numero,
                    Complemento = address.Complemento,
                    TipoResidencia = address.TipoResidencia,
                    Pais = address.Pais,
                    IsEntrega = address.IsEntrega,
                    IsCobranca = address.IsCobranca,
                    Bairro = address.Bairro.Nome,
                    Cidade = address.Cidade.Nome,
                    Estado = address.Cidade.Estado.Sigla
                }
            };
        }

        public CustomerAddressCommandResult Create(CustomerAddressCreateCommand command) {
            var customer = _dataProvider.LoadCustomerById(command.ClienteId);
            if (customer == null) {
                return new CustomerAddressCommandResult {
                    CustomerFound = false,
                    Success = false
                };
            }

            if (!HasAddressPurpose(command.IsEntrega, command.IsCobranca)) {
                return Failure("Selecione pelo menos uma finalidade para o endereco: entrega, cobranca ou ambas.");
            }

            var existingAddresses = _dataProvider.LoadAddressesByCustomerId(command.ClienteId);
            if (!CustomerKeepsRequiredAddress(existingAddresses, null, command.IsEntrega, command.IsCobranca, out var requiredAddressError)) {
                return Failure(requiredAddressError!);
            }

            var location = ResolveLocation(command.Estado, command.Cidade, command.Bairro);
            var address = new Endereco {
                NomeEndereco = command.NomeEndereco,
                CEP = command.CEP,
                TipoLogradouro = command.TipoLogradouro,
                Logradouro = command.Logradouro,
                Numero = command.Numero,
                Complemento = command.Complemento,
                TipoResidencia = command.TipoResidencia,
                Pais = command.Pais,
                IsEntrega = command.IsEntrega,
                IsCobranca = command.IsCobranca,
                IsPadrao = command.IsEntrega && !existingAddresses.Any(e => e.IsEntrega),
                CidadeId = location.City.Id,
                BairroId = location.Neighborhood.Id,
                ClienteId = command.ClienteId
            };

            NormalizeAddress(address);
            _dataProvider.AddAddress(address);
            _dataProvider.SaveChanges();

            return new CustomerAddressCommandResult {
                CustomerFound = true,
                Success = true,
                EnderecoId = address.Id
            };
        }

        public CustomerAddressCommandResult Update(CustomerAddressUpdateCommand command) {
            var address = _dataProvider.LoadSavedAddressByIdWithRelationsForCustomer(command.Email, command.AddressId);
            if (address == null) {
                return new CustomerAddressCommandResult {
                    Success = false,
                    CustomerFound = true,
                    AddressFound = false
                };
            }

            if (!HasAddressPurpose(command.IsEntrega, command.IsCobranca)) {
                return Failure("Selecione pelo menos uma finalidade para o endereco: entrega, cobranca ou ambas.");
            }

            var existingAddresses = _dataProvider.LoadAddressesByCustomerId(address.ClienteId);
            if (!CustomerKeepsRequiredAddress(existingAddresses, address.Id, command.IsEntrega, command.IsCobranca, out var requiredAddressError)) {
                return Failure(requiredAddressError!);
            }

            var location = ResolveLocation(command.Estado, command.Cidade, command.Bairro);

            address.NomeEndereco = command.NomeEndereco;
            address.CEP = command.CEP;
            address.TipoLogradouro = command.TipoLogradouro;
            address.Logradouro = command.Logradouro;
            address.Numero = command.Numero;
            address.Complemento = command.Complemento;
            address.TipoResidencia = command.TipoResidencia;
            address.Pais = command.Pais;
            address.IsEntrega = command.IsEntrega;
            address.IsCobranca = command.IsCobranca;
            address.CidadeId = location.City.Id;
            address.BairroId = location.Neighborhood.Id;
            address.IsPadrao = address.IsEntrega && address.IsPadrao;

            if (!address.IsEntrega) {
                address.IsPadrao = false;
            }

            NormalizeAddress(address);
            _dataProvider.SaveChanges();

            EnsureDefaultDelivery(_dataProvider.LoadAddressesByCustomerId(address.ClienteId), address.IsEntrega ? address.Id : null);
            _dataProvider.SaveChanges();

            return new CustomerAddressCommandResult {
                CustomerFound = true,
                Success = true,
                EnderecoId = address.Id
            };
        }

        public CustomerAddressCommandResult SetDefault(CustomerAddressSetDefaultCommand command) {
            var customer = _dataProvider.LoadCustomerByEmailWithAddresses(command.Email);
            if (customer == null) {
                return new CustomerAddressCommandResult {
                    CustomerFound = false,
                    Success = false
                };
            }

            var address = (customer.Enderecos ?? new List<Endereco>())
                .FirstOrDefault(e => e.Id == command.AddressId && (e.IsEntrega || e.IsCobranca));

            if (address == null || !address.IsEntrega) {
                return Failure("Somente enderecos de entrega podem ser definidos como padrao.");
            }

            foreach (var item in customer.Enderecos ?? new List<Endereco>()) {
                item.IsPadrao = false;
            }

            address.IsPadrao = true;
            _dataProvider.SaveChanges();

            return new CustomerAddressCommandResult {
                CustomerFound = true,
                Success = true,
                EnderecoId = address.Id
            };
        }

        public CustomerAddressCommandResult Delete(CustomerAddressDeleteCommand command) {
            var address = _dataProvider.LoadSavedAddressByIdWithRelationsForCustomer(command.Email, command.AddressId);
            if (address == null) {
                return new CustomerAddressCommandResult {
                    Success = false,
                    CustomerFound = true,
                    AddressFound = false
                };
            }

            if (_dataProvider.HasOrdersUsingAddress(address.Id)) {
                return Failure("Este endereço já foi usado em um pedido e não pode ser excluído.");
            }

            var existingAddresses = _dataProvider.LoadAddressesByCustomerId(address.ClienteId);
            if (!CustomerCanDelete(existingAddresses, address.Id, out var deleteError)) {
                return Failure(deleteError!);
            }

            _dataProvider.RemoveAddress(address);
            _dataProvider.SaveChanges();

            EnsureDefaultDelivery(_dataProvider.LoadAddressesByCustomerId(address.ClienteId));
            _dataProvider.SaveChanges();

            return new CustomerAddressCommandResult {
                CustomerFound = true,
                Success = true
            };
        }

        private (Estado State, Cidade City, Bairro Neighborhood) ResolveLocation(string estadoInformado, string cidadeInformada, string bairroInformado) {
            var stateCode = (estadoInformado ?? string.Empty).Trim().ToUpperInvariant();
            var state = _dataProvider.LoadStateByCode(stateCode);
            if (state == null) {
                state = new Estado {
                    Nome = stateCode,
                    Sigla = stateCode
                };
                _dataProvider.AddState(state);
                _dataProvider.SaveChanges();
            }

            var cityName = (cidadeInformada ?? string.Empty).Trim();
            var city = _dataProvider.LoadCityByNameAndState(cityName, state.Id);
            if (city == null) {
                city = new Cidade {
                    Nome = cityName,
                    EstadoId = state.Id
                };
                _dataProvider.AddCity(city);
                _dataProvider.SaveChanges();
            }

            var neighborhoodName = (bairroInformado ?? string.Empty).Trim();
            var neighborhood = _dataProvider.LoadNeighborhoodByNameAndCity(neighborhoodName, city.Id);
            if (neighborhood == null) {
                neighborhood = new Bairro {
                    Nome = neighborhoodName,
                    CidadeId = city.Id
                };
                _dataProvider.AddNeighborhood(neighborhood);
                _dataProvider.SaveChanges();
            }

            return (state, city, neighborhood);
        }

        private static List<Endereco> SortSavedAddresses(IEnumerable<Endereco> addresses) {
            return addresses
                .Where(e => e.IsEntrega || e.IsCobranca)
                .OrderByDescending(e => e.IsPadrao)
                .ThenByDescending(e => e.IsEntrega)
                .ThenBy(e => e.NomeEndereco)
                .ToList();
        }

        private static bool HasAddressPurpose(bool isEntrega, bool isCobranca) {
            return isEntrega || isCobranca;
        }

        private static bool CustomerKeepsRequiredAddress(
            IReadOnlyCollection<Endereco> addresses,
            int? ignoredAddressId,
            bool isEntrega,
            bool isCobranca,
            out string? error) {
            error = null;

            var otherAddresses = addresses
                .Where(e => !ignoredAddressId.HasValue || e.Id != ignoredAddressId.Value)
                .ToList();

            var wouldKeepDelivery = isEntrega || otherAddresses.Any(e => e.IsEntrega);
            var wouldKeepBilling = isCobranca || otherAddresses.Any(e => e.IsCobranca);

            if (!wouldKeepDelivery) {
                error = "O cliente precisa manter pelo menos um endereco de entrega.";
                return false;
            }

            if (!wouldKeepBilling) {
                error = "O cliente precisa manter pelo menos um endereco de cobranca.";
                return false;
            }

            return true;
        }

        private static bool CustomerCanDelete(IReadOnlyCollection<Endereco> addresses, int addressId, out string? error) {
            error = null;

            var remaining = addresses.Where(e => e.Id != addressId).ToList();
            if (!remaining.Any(e => e.IsEntrega)) {
                error = "O cliente precisa manter pelo menos um endereco de entrega.";
                return false;
            }

            if (!remaining.Any(e => e.IsCobranca)) {
                error = "O cliente precisa manter pelo menos um endereco de cobranca.";
                return false;
            }

            return true;
        }

        private static void EnsureDefaultDelivery(IReadOnlyCollection<Endereco> addresses, int? preferredAddressId = null) {
            var ordered = addresses
                .OrderByDescending(e => e.Id == preferredAddressId)
                .ThenBy(e => e.Id)
                .ToList();

            var deliveryAddresses = ordered.Where(e => e.IsEntrega).ToList();
            if (!deliveryAddresses.Any()) {
                return;
            }

            var currentDefault = deliveryAddresses.FirstOrDefault(e => e.IsPadrao);
            if (currentDefault != null) {
                foreach (var address in ordered.Where(e => !e.IsEntrega)) {
                    address.IsPadrao = false;
                }
                return;
            }

            foreach (var address in ordered) {
                address.IsPadrao = false;
            }

            deliveryAddresses.First().IsPadrao = true;
        }

        private static void NormalizeAddress(Endereco endereco) {
            endereco.NomeEndereco = endereco.NomeEndereco?.Trim() ?? string.Empty;
            endereco.CEP = endereco.CEP?.Trim() ?? string.Empty;
            endereco.TipoLogradouro = endereco.TipoLogradouro?.Trim() ?? string.Empty;
            endereco.Logradouro = endereco.Logradouro?.Trim() ?? string.Empty;
            endereco.Numero = endereco.Numero?.Trim() ?? string.Empty;
            endereco.Complemento = string.IsNullOrWhiteSpace(endereco.Complemento) ? null : endereco.Complemento.Trim();
            endereco.TipoResidencia = endereco.TipoResidencia?.Trim() ?? string.Empty;
            endereco.Pais = string.IsNullOrWhiteSpace(endereco.Pais) ? "Brasil" : endereco.Pais.Trim();
        }

        private static CustomerAddressCommandResult Failure(string message) {
            return new CustomerAddressCommandResult {
                CustomerFound = true,
                Success = false,
                ErrorMessage = message
            };
        }
    }
}