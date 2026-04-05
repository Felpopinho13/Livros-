using Livros.Domain;

public class CheckoutViewModel {
    public Livro Livro { get; set; }
    public List<Endereco> Enderecos { get; set; }
}