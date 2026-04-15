using Livros.Infrastructure.Data;
using Livros.Domain;
using Microsoft.EntityFrameworkCore;

namespace Livros.Infrastructure.Services {
    public class LivroService {
        private const decimal ParametroMinimoVendasParaManterLivroAtivoSemEstoque = 50m;
        private readonly AppDbContext _context;

        public LivroService(AppDbContext context) {
            _context = context;
        }

        public List<Livro> Listar() {
            AplicarInativacaoAutomaticaSemEstoque();

            return _context.Livros
                .Include(l => l.Estoque)
                .Include(l => l.Categorias)
                .Where(l => l.IsAtivo)
                .ToList();
        }

        public void Criar(Livro livro) {
            _context.Livros.Add(livro);
            _context.SaveChanges();

            var estoque = new Estoque {
                LivroId = livro.Id,
                Quantidade = 0
            };

            _context.Estoques.Add(estoque);
            _context.SaveChanges();
        }

        public Livro ObterPorId(int id) {
            AplicarInativacaoAutomaticaSemEstoque();

            return _context.Livros
                .Include(l => l.Estoque)
                .Include(l => l.Categorias)
                .FirstOrDefault(l => l.Id == id && l.IsAtivo);
        }

        private void AplicarInativacaoAutomaticaSemEstoque() {
            var livrosSemEstoque = _context.Livros
                .Include(l => l.Estoque)
                .Where(l => l.IsAtivo && (l.Estoque == null || l.Estoque.Quantidade <= 0))
                .ToList();

            if (!livrosSemEstoque.Any()) {
                return;
            }

            var livroIds = livrosSemEstoque.Select(l => l.Id).ToList();
            var vendasPorLivro = _context.PedidoItens
                .Include(i => i.Pedido)
                .Where(i =>
                    livroIds.Contains(i.LivroId) &&
                    i.Pedido != null &&
                    i.Pedido.Status != "REPROVADA" &&
                    i.Pedido.Status != "PAGAMENTO RECUSADO" &&
                    i.Pedido.Status != "CANCELADO")
                .GroupBy(i => i.LivroId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(i => i.PrecoUnitario * i.Quantidade));

            var houveAlteracao = false;

            foreach (var livro in livrosSemEstoque) {
                var valorVendido = vendasPorLivro.TryGetValue(livro.Id, out var totalVendido)
                    ? totalVendido
                    : 0;

                if (valorVendido < ParametroMinimoVendasParaManterLivroAtivoSemEstoque) {
                    livro.IsAtivo = false;
                    houveAlteracao = true;
                }
            }

            if (houveAlteracao) {
                _context.SaveChanges();
            }
        }
    }
}
