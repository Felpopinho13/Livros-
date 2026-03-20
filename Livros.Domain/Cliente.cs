namespace Livros.Domain {
    public class Cliente {
        public int Id { get; set; }

        // Dados pessoais
        public string Nome { get; set; }
        public string CPF { get; set; }
        public string Genero { get; set; }
        public DateTime DataNascimento { get; set; }

        // Contato
        public string Email { get; set; }
        public string Telefone { get; set; }

        // Segurança
        public string Senha { get; set; }

        // Relacionamento

        public Endereco Endereco { get; set; }
    }
}