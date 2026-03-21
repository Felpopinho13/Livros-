namespace Livros.Domain {
    public class Cartao {
        public int Id { get; set; }

        public string NomeImpresso { get; set; }
        public string Numero { get; set; }
        public string Validade { get; set; }
        public string CVV { get; set; }

        public bool IsPadrao { get; set; } = false;

        // FK
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; }
    }
}