namespace Livros.Application.Checkout {
    public sealed class CheckoutAddressResolutionRequest {
        public int ClienteId { get; init; }
        public int EnderecoId { get; init; }
        public string? NomeEndereco { get; init; }
        public string? CEP { get; init; }
        public string? TipoLogradouro { get; init; }
        public string? Logradouro { get; init; }
        public string? Numero { get; init; }
        public string? Complemento { get; init; }
        public string? TipoResidencia { get; init; }
        public string? Pais { get; init; }
        public string? Bairro { get; init; }
        public string? Cidade { get; init; }
        public string? Estado { get; init; }
        public bool SalvarNoPerfil { get; init; }
    }
    public sealed class CheckoutAddressResolutionResult {
        public int? EnderecoId { get; init; }
        public List<string> Errors { get; init; } = new();
    }
}

