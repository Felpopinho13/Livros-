using System.ComponentModel.DataAnnotations;

namespace Livros.Domain { 
    public class Livro {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Titulo { get; set; }

        [Range(1000, 2100)]
        public int Ano { get; set; }
        [Required]
        [StringLength(100)]
        public string Autor { get; set; }
        public string Editora { get; set; }
        public string Edicao { get; set; }

        [RegularExpression(@"\d{13}", ErrorMessage = "ISBN deve ter 13 dígitos")]
        public string ISBN { get; set; }

        [RegularExpression(@"\d{13}", ErrorMessage = "Código de barras inválido")]
        public string CodigoBarras { get; set; }

        [Range(1, 10000)]
        public int NumeroPaginas { get; set; }
        public string Sinopse { get; set; }

        // Dimensões
        [Range(0.01, 1000)]
        public decimal Altura { get; set; }
        [Range(0.01, 1000)]
        public decimal Largura { get; set; }
        [Range(0.01, 50)]
        public decimal Peso { get; set; }
        [Range(0.01, 1000)]
        public decimal Profundidade { get; set; }
        // Preço (simplificado por enquanto)
        [Range(0.01, 10000)]
        public decimal Preco { get; set; }
        public string ImagemUrl { get; set; }

        public bool IsAtivo { get; set; } = true;

        public List<Categoria>? Categorias { get; set; }
    }
}