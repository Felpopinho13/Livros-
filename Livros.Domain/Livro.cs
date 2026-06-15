using System.ComponentModel.DataAnnotations;

namespace Livros.Domain {
    public class Livro {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Range(1000, 2100)]
        public int Ano { get; set; }

        [Required]
        [StringLength(100)]
        public string Autor { get; set; } = string.Empty;

        public string Editora { get; set; } = string.Empty;
        public string Edicao { get; set; } = string.Empty;

        [RegularExpression(@"\d{13}", ErrorMessage = "ISBN deve ter 13 dígitos")]
        public string ISBN { get; set; } = string.Empty;

        [RegularExpression(@"\d{13}", ErrorMessage = "Código de barras inválido")]
        public string CodigoBarras { get; set; } = string.Empty;

        [Range(1, 10000)]
        public int NumeroPaginas { get; set; }
        public string Sinopse { get; set; } = string.Empty;

        [Range(0.01, 1000)]
        public decimal Altura { get; set; }
        [Range(0.01, 1000)]
        public decimal Largura { get; set; }
        [Range(0.01, 50)]
        public decimal Peso { get; set; }
        [Range(0.01, 1000)]
        public decimal Profundidade { get; set; }
        [Range(0.01, 10000)]
        public decimal Preco { get; set; }
        public string ImagemUrl { get; set; } = string.Empty;

        public bool IsAtivo { get; set; } = true;
        public List<Categoria> Categorias { get; set; } = new();
        public List<Avaliacao> Avaliacoes { get; set; } = new();
        public Estoque Estoque { get; set; } = null!;
    }
}
