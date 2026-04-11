using Livros.Domain;

public class CheckoutResumoItemViewModel {
    public int LivroId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Autor { get; set; } = string.Empty;
    public string ImagemUrl { get; set; } = string.Empty;
    public decimal PrecoUnitario { get; set; }
    public int Quantidade { get; set; }
    public decimal TotalItem => PrecoUnitario * Quantidade;
}

public class CheckoutViewModel {
    public Livro? Livro { get; set; }
    public List<CheckoutResumoItemViewModel> Itens { get; set; } = new();
    public List<Endereco> Enderecos { get; set; } = new();
    public List<Cartao> Cartoes { get; set; } = new();
    public int Quantidade { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Frete { get; set; }
    public decimal Desconto { get; set; }
    public decimal Total { get; set; }
    public bool OrigemCarrinho { get; set; }
    public bool PermiteAlterarQuantidade { get; set; }
    public CheckoutFormData Form { get; set; } = new();
}

public class CheckoutFormData {
    public int LivroId { get; set; }
    public int Quantidade { get; set; } = 1;
    public bool UsarCarrinho { get; set; }

    public int EnderecoId { get; set; }
    public bool SalvarNovoEndereco { get; set; }
    public string? NomeEndereco { get; set; }
    public string? CEP { get; set; }
    public string? TipoLogradouro { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? TipoResidencia { get; set; }
    public string? Pais { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Estado { get; set; }

    public string? Cupom { get; set; }

    public string? Metodo1 { get; set; }
    public decimal? Valor1 { get; set; }
    public int? CartaoId1 { get; set; }
    public bool SalvarNovoCartao1 { get; set; }
    public string? NomeCartao1 { get; set; }
    public string? NumeroCartao1 { get; set; }
    public string? CVV1 { get; set; }
    public string? Validade1 { get; set; }

    public string? Metodo2 { get; set; }
    public decimal? Valor2 { get; set; }
    public int? CartaoId2 { get; set; }
    public bool SalvarNovoCartao2 { get; set; }
    public string? NomeCartao2 { get; set; }
    public string? NumeroCartao2 { get; set; }
    public string? CVV2 { get; set; }
    public string? Validade2 { get; set; }
}
