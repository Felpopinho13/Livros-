using Livros.Domain;

namespace Livros.Application.CustomerAddresses {
    public sealed class CustomerAddressListQuery {
        public string Email { get; init; } = string.Empty;
    }

    public sealed class CustomerAddressEditQuery {
        public string Email { get; init; } = string.Empty;
        public int AddressId { get; init; }
    }

    public sealed class CustomerAddressEditData {
        public int Id { get; init; }
        public string NomeEndereco { get; init; } = string.Empty;
        public string CEP { get; init; } = string.Empty;
        public string TipoLogradouro { get; init; } = string.Empty;
        public string Logradouro { get; init; } = string.Empty;
        public string Numero { get; init; } = string.Empty;
        public string? Complemento { get; init; }
        public string TipoResidencia { get; init; } = string.Empty;
        public string Pais { get; init; } = string.Empty;
        public bool IsEntrega { get; init; }
        public bool IsCobranca { get; init; }
        public string Bairro { get; init; } = string.Empty;
        public string Cidade { get; init; } = string.Empty;
        public string Estado { get; init; } = string.Empty;
    }

    public sealed class CustomerAddressListResult {
        public bool CustomerFound { get; init; }
        public List<Endereco> Addresses { get; init; } = new();
    }

    public sealed class CustomerAddressDetailsResult {
        public bool Found { get; init; }
        public CustomerAddressEditData? Address { get; init; }
    }

    public sealed class CustomerAddressCreateCommand {
        public int ClienteId { get; init; }
        public string NomeEndereco { get; init; } = string.Empty;
        public string CEP { get; init; } = string.Empty;
        public string TipoLogradouro { get; init; } = string.Empty;
        public string Logradouro { get; init; } = string.Empty;
        public string Numero { get; init; } = string.Empty;
        public string? Complemento { get; init; }
        public string TipoResidencia { get; init; } = string.Empty;
        public string Pais { get; init; } = string.Empty;
        public bool IsEntrega { get; init; }
        public bool IsCobranca { get; init; }
        public string Bairro { get; init; } = string.Empty;
        public string Cidade { get; init; } = string.Empty;
        public string Estado { get; init; } = string.Empty;
    }

    public sealed class CustomerAddressUpdateCommand {
        public string Email { get; init; } = string.Empty;
        public int AddressId { get; init; }
        public string NomeEndereco { get; init; } = string.Empty;
        public string CEP { get; init; } = string.Empty;
        public string TipoLogradouro { get; init; } = string.Empty;
        public string Logradouro { get; init; } = string.Empty;
        public string Numero { get; init; } = string.Empty;
        public string? Complemento { get; init; }
        public string TipoResidencia { get; init; } = string.Empty;
        public string Pais { get; init; } = string.Empty;
        public bool IsEntrega { get; init; }
        public bool IsCobranca { get; init; }
        public string Bairro { get; init; } = string.Empty;
        public string Cidade { get; init; } = string.Empty;
        public string Estado { get; init; } = string.Empty;
    }

    public sealed class CustomerAddressSetDefaultCommand {
        public string Email { get; init; } = string.Empty;
        public int AddressId { get; init; }
    }

    public sealed class CustomerAddressDeleteCommand {
        public string Email { get; init; } = string.Empty;
        public int AddressId { get; init; }
    }

    public sealed class CustomerAddressCommandResult {
        public bool Success { get; init; }
        public bool CustomerFound { get; init; }
        public bool AddressFound { get; init; } = true;
        public string? ErrorMessage { get; init; }
        public int? EnderecoId { get; init; }
    }
}