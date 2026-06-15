using Livros.Domain;

public class Estoque {
    public int Id { get; set; }
    public int LivroId { get; set; }
    public Livro Livro { get; set; } = null!;
    public int Quantidade { get; set; }
    public int QuantidadeMinima { get; set; } = 10;
}
