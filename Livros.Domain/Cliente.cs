namespace Livros.Domain {
    public class Cliente {
        public int Id { get; set; }

        public string Nome { get; set; }
        public string Email { get; set; }
        public string Senha { get; set; }

        public string? CPF { get; set; }
        public string? Telefone { get; set; }
        public string? Genero { get; set; }
        public DateTime? DataNascimento { get; set; }

        // RELACIONAMENTO
        public List<Endereco>? Enderecos { get; set; }

        public bool IsAdmin { get; set; } = false;

        public List<Cartao>? Cartoes { get; set; }
    }
}