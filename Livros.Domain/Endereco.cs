using Livros.Domain;

public class Endereco {
    public int Id { get; set; }

    public string NomeEndereco { get; set; }
    public string CEP { get; set; }
    public string Logradouro { get; set; }
    public string Numero { get; set; }
    public string? Complemento { get; set; }

    // RELACIONAMENTOS
    public int CidadeId { get; set; }
    public Cidade Cidade { get; set; }

    public int BairroId { get; set; }
    public Bairro Bairro { get; set; }

    // FK Cliente
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; }
}
public class Estado {
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Sigla { get; set; }

    public List<Cidade> Cidades { get; set; }
}

public class Cidade {
    public int Id { get; set; }
    public string Nome { get; set; }

    public int EstadoId { get; set; }
    public Estado Estado { get; set; }

    public List<Bairro> Bairros { get; set; }
}

public class Bairro {
    public int Id { get; set; }
    public string Nome { get; set; }

    public int CidadeId { get; set; }
    public Cidade Cidade { get; set; }
}