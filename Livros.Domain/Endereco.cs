using Livros.Domain;

public class Endereco {
    public int Id { get; set; }

    public string NomeEndereco { get; set; } = string.Empty;
    public string CEP { get; set; } = string.Empty;
    public string TipoLogradouro { get; set; } = "Rua";
    public string Logradouro { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? Complemento { get; set; }
    public string TipoResidencia { get; set; } = "Casa";
    public string Pais { get; set; } = "Brasil";

    public bool IsPadrao { get; set; } = false;
    public bool IsEntrega { get; set; } = true;
    public bool IsCobranca { get; set; } = true;

    public int CidadeId { get; set; }
    public Cidade Cidade { get; set; } = null!;

    public int BairroId { get; set; }
    public Bairro Bairro { get; set; } = null!;

    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
}

public class Estado {
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Sigla { get; set; } = string.Empty;
    public List<Cidade> Cidades { get; set; } = new();
}

public class Cidade {
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int EstadoId { get; set; }
    public Estado Estado { get; set; } = null!;
    public List<Bairro> Bairros { get; set; } = new();
}

public class Bairro {
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int CidadeId { get; set; }
    public Cidade Cidade { get; set; } = null!;
}
