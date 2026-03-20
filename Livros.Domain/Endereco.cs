namespace Livros.Domain {
    public class Endereco {
        public int Id { get; set; }

        public string NomeEndereco { get; set; }
        public string CEP { get; set; }
        public string Logradouro { get; set; }
        public string Numero { get; set; }
        public string Complemento { get; set; }
        public string Bairro { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }

        // FK
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; }
    }
}