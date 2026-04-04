using Livros.Infrastructure.Data;
using Livros.Domain;

namespace Livros.Infrastructure.Services {
    public class LivroService {
        private readonly AppDbContext _context;

        public LivroService(AppDbContext context) {
            _context = context;
        }

        public List<Livro> Listar() {
            return _context.Livros.ToList();
        }

        public void Criar(Livro livro) {
            _context.Livros.Add(livro);
            _context.SaveChanges();
        }
    }
}