using Livros.Domain;
using Livros.Infrastructure.Data;

namespace Livros.Infrastructure.Services
{
    public class ClienteService
    {
        private readonly AppDbContext _context;

        public ClienteService(AppDbContext context)
        {
            _context = context;
        }

        public List<Cliente> Listar()
        {
            return _context.Clientes.ToList();
        }

        public void Adicionar(Cliente cliente)
        {
            _context.Clientes.Add(cliente);
            _context.SaveChanges();
        }
        public Cliente BuscarPorEmailESenha(string email, string senha) {
            return _context.Clientes
                .FirstOrDefault(c => c.Email == email && c.Senha == senha);
        }
    }
}