namespace Livros.Domain {
    public class Cartao {
        public int Id { get; set; }

        public string NomeImpresso { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string Validade { get; set; } = string.Empty;
        public string CVV { get; set; } = string.Empty;
        public int BandeiraCartaoId { get; set; }
        public BandeiraCartao BandeiraCartao { get; set; } = null!;

        public bool IsPadrao { get; set; } = false;
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;
    }
}
