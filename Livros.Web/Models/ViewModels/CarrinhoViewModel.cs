using Livros.Domain;

namespace Livros.Web.Models.ViewModels {
    public class CarrinhoItemViewModel {
        public int LivroId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Autor { get; set; } = string.Empty;
        public string ImagemUrl { get; set; } = string.Empty;
        public decimal PrecoUnitario { get; set; }
        public int Quantidade { get; set; }
        public bool EmEstoque { get; set; }
        public int EstoqueDisponivel { get; set; }
        public DateTime? ReservaExpiraEm { get; set; }
        public bool ReservaExpirando { get; set; }
        public string? AvisoReserva { get; set; }
        public decimal TotalItem => PrecoUnitario * Quantidade;
    }

    public class CarrinhoViewModel {
        public List<CarrinhoItemViewModel> Itens { get; set; } = new();
        public List<string> Avisos { get; set; } = new();
        public decimal Subtotal => Itens.Sum(i => i.TotalItem);
        public int QuantidadeItens => Itens.Sum(i => i.Quantidade);
        public bool CarrinhoVazio => !Itens.Any();
    }

    public class CarrinhoSessionItem {
        public int LivroId { get; set; }
        public int Quantidade { get; set; }
    }
}
