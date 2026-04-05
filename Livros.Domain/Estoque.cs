using Livros.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Estoque {
    public int Id { get; set; }

    public int LivroId { get; set; }
    public Livro Livro { get; set; }

    public int Quantidade { get; set; }

    public int QuantidadeMinima { get; set; } = 10;
}
