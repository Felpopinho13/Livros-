using Livros.Infrastructure.Data;
using Livros.Domain;
using Microsoft.EntityFrameworkCore;

namespace Livros.Infrastructure.Services {
    public class LivroService {
        private readonly AppDbContext _context;

        public LivroService(AppDbContext context) {
            _context = context;
        }

        public List<Livro> Listar() {
            return _context.Livros
                .Include(l => l.Estoque)
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
            return _context.Livros
                .Include(l => l.Estoque)
                .FirstOrDefault(l => l.Id == id && l.IsAtivo);
        }
    }
}
