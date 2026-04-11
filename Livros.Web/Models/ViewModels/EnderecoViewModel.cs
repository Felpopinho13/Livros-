namespace Livros.Web.Models.ViewModels {
    public class EnderecoViewModel {
        public int Id { get; set; }

        public string NomeEndereco { get; set; }
        public string CEP { get; set; }
        public string TipoLogradouro { get; set; }
        public string Logradouro { get; set; }
        public string Numero { get; set; }
        public string? Complemento { get; set; }
        public string TipoResidencia { get; set; }
        public string Pais { get; set; }
        public bool IsEntrega { get; set; }
        public bool IsCobranca { get; set; }

        public string Bairro { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }
    }
}
