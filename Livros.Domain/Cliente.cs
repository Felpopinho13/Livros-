namespace Livros.Domain {
    public class Cliente {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;

        public string? CPF { get; set; }
        public string? Telefone { get; set; }
        public string? Genero { get; set; }
        public DateTime? DataNascimento { get; set; }

        public List<Endereco> Enderecos { get; set; } = new();
        public bool IsAdmin { get; set; } = false;
        public List<Cartao> Cartoes { get; set; } = new();
        public string? CarrinhoPersistidoJson { get; set; }
        public List<ReservaCarrinho> ReservasCarrinho { get; set; } = new();
        public List<Avaliacao> Avaliacoes { get; set; } = new();
        public Wishlist? Wishlist { get; set; }
        public bool IsAtivo { get; set; } = true;
    }
}
