using Livros.Domain;
using System.Text.RegularExpressions;

namespace Livros.Application.Checkout {
    public sealed class CheckoutAddressService {
        private readonly ICheckoutAddressDataProvider _dataProvider;

        public CheckoutAddressService(ICheckoutAddressDataProvider dataProvider) {
            _dataProvider = dataProvider;
        }

        public CheckoutAddressResolutionResult Resolve(CheckoutAddressResolutionRequest request) {
            if (request.EnderecoId > 0) {
                var enderecoExistente = _dataProvider.LoadDeliveryAddress(request.ClienteId, request.EnderecoId);
                if (enderecoExistente == null) {
                    return new CheckoutAddressResolutionResult {
                        Errors = new List<string> { "Selecione um endereco de entrega valido." }
                    };
                }

                return new CheckoutAddressResolutionResult {
                    EnderecoId = enderecoExistente.Id
                };
            }

            if (string.IsNullOrWhiteSpace(request.CEP)
                || string.IsNullOrWhiteSpace(request.Logradouro)
                || string.IsNullOrWhiteSpace(request.Numero)
                || string.IsNullOrWhiteSpace(request.Bairro)
                || string.IsNullOrWhiteSpace(request.Cidade)
                || string.IsNullOrWhiteSpace(request.Estado)) {
                return new CheckoutAddressResolutionResult {
                    Errors = new List<string> { "Preencha todos os campos obrigatorios do novo endereco." }
                };
            }

            var cepNormalizado = NormalizeDigits(request.CEP);
            if (cepNormalizado.Length != 8) {
                return new CheckoutAddressResolutionResult {
                    Errors = new List<string> { "O CEP deve conter exatamente 8 digitos." }
                };
            }

            var estadoSigla = request.Estado.Trim().ToUpperInvariant();
            if (!Regex.IsMatch(estadoSigla, "^[A-Z]{2}$")) {
                return new CheckoutAddressResolutionResult {
                    Errors = new List<string> { "Informe uma UF valida com 2 letras." }
                };
            }

            var estadoEntity = _dataProvider.LoadStateByCode(estadoSigla);
            if (estadoEntity == null) {
                estadoEntity = new Estado {
                    Nome = estadoSigla,
                    Sigla = estadoSigla
                };
                _dataProvider.AddState(estadoEntity);
                _dataProvider.SaveChanges();
            }

            var cidadeNome = request.Cidade.Trim();
            var cidadeEntity = _dataProvider.LoadCityByNameAndState(cidadeNome, estadoEntity.Id);
            if (cidadeEntity == null) {
                cidadeEntity = new Cidade {
                    Nome = cidadeNome,
                    EstadoId = estadoEntity.Id
                };
                _dataProvider.AddCity(cidadeEntity);
                _dataProvider.SaveChanges();
            }

            var bairroNome = request.Bairro.Trim();
            var bairroEntity = _dataProvider.LoadNeighborhoodByNameAndCity(bairroNome, cidadeEntity.Id);
            if (bairroEntity == null) {
                bairroEntity = new Bairro {
                    Nome = bairroNome,
                    CidadeId = cidadeEntity.Id
                };
                _dataProvider.AddNeighborhood(bairroEntity);
                _dataProvider.SaveChanges();
            }

            var endereco = new Endereco {
                NomeEndereco = string.IsNullOrWhiteSpace(request.NomeEndereco) ? "Novo Endereco" : request.NomeEndereco.Trim(),
                CEP = cepNormalizado,
                TipoLogradouro = string.IsNullOrWhiteSpace(request.TipoLogradouro) ? "Rua" : request.TipoLogradouro.Trim(),
                Logradouro = request.Logradouro.Trim(),
                Numero = request.Numero.Trim(),
                Complemento = request.Complemento?.Trim(),
                TipoResidencia = string.IsNullOrWhiteSpace(request.TipoResidencia) ? "Casa" : request.TipoResidencia.Trim(),
                Pais = string.IsNullOrWhiteSpace(request.Pais) ? "Brasil" : request.Pais.Trim(),
                BairroId = bairroEntity.Id,
                CidadeId = cidadeEntity.Id,
                ClienteId = request.ClienteId,
                IsPadrao = false,
                IsEntrega = request.SalvarNoPerfil,
                IsCobranca = false
            };

            _dataProvider.AddAddress(endereco);
            _dataProvider.SaveChanges();

            return new CheckoutAddressResolutionResult {
                EnderecoId = endereco.Id
            };
        }

        private static string NormalizeDigits(string? valor) {
            if (string.IsNullOrWhiteSpace(valor)) {
                return string.Empty;
            }

            return new string(valor.Where(char.IsDigit).ToArray());
        }
    }
}
