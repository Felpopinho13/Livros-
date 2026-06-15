using Livros.Domain;

namespace Livros.Web.Models.ViewModels {
    public sealed class LivroDetalhesViewModel {
        public Livro Livro { get; set; } = null!;
        public decimal MediaAvaliacoes { get; set; }
        public int QuantidadeAvaliacoes { get; set; }
        public List<LivroDetalhesComentarioViewModel> Comentarios { get; set; } = new();
        public bool PossuiAvaliacoes => QuantidadeAvaliacoes > 0;
        public bool PossuiComentarios => Comentarios.Any(comment => !string.IsNullOrWhiteSpace(comment.Comentario));
    }

    public sealed class LivroDetalhesComentarioViewModel {
        public string NomeCliente { get; set; } = string.Empty;
        public int Nota { get; set; }
        public string? Comentario { get; set; }
        public DateTime DataAvaliacao { get; set; }
    }
}
