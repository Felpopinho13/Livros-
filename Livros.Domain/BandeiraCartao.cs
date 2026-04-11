namespace Livros.Domain {
    public class BandeiraCartao {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public bool IsAtiva { get; set; } = true;
        public List<Cartao>? Cartoes { get; set; }
    }
}
