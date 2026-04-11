using Livros.Domain;

namespace Livros.Web.Models.ViewModels {
    public class CartoesViewModel {
        public List<Cartao> Cartoes { get; set; } = new();
        public List<BandeiraCartao> Bandeiras { get; set; } = new();
    }
}
