using Livros.Domain;

namespace Livros.Application.Authentication {
    public sealed class CustomerRegistrationCommand {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public string CPF { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Genero { get; set; } = string.Empty;
        public DateTime? DataNascimento { get; set; }
        public string NomeEndereco { get; set; } = string.Empty;
        public string CEP { get; set; } = string.Empty;
        public string TipoLogradouro { get; set; } = string.Empty;
        public string Logradouro { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string Complemento { get; set; } = string.Empty;
        public string TipoResidencia { get; set; } = string.Empty;
        public string Pais { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }

    public sealed class CustomerRegistrationResult {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Cliente? Customer { get; set; }
    }

    public sealed class CustomerLoginCartMergeCommand {
        public int CustomerId { get; set; }
        public string? PersistedCartJson { get; set; }
        public string? CurrentSessionCartJson { get; set; }
        public string SessionKey { get; set; } = string.Empty;
    }

    public sealed class CustomerLoginCartMergeResult {
        public bool HasItems { get; set; }
        public string? MergedCartJson { get; set; }
    }
}
