namespace Livros.Web.Models.ViewModels {
    public class EnderecoViewModel {
        public int Id { get; set; }
        public string NomeEndereco { get; set; } = string.Empty;
        public string CEP { get; set; } = string.Empty;
        public string TipoLogradouro { get; set; } = string.Empty;
        public string Logradouro { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string? Complemento { get; set; }
        public string TipoResidencia { get; set; } = string.Empty;
        public string Pais { get; set; } = string.Empty;
        public bool IsEntrega { get; set; }
        public bool IsCobranca { get; set; }
        public string Bairro { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }
}
