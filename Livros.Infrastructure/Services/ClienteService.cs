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

        public Cliente BuscarPorEmail(string email) {
            return _context.Clientes.FirstOrDefault(c => c.Email == email);
        }

        public void Atualizar(Cliente clienteAtualizado) {
            var cliente = _context.Clientes
                .FirstOrDefault(c => c.Id == clienteAtualizado.Id);

            if (cliente == null) return;

            cliente.Nome = clienteAtualizado.Nome;
            cliente.Email = clienteAtualizado.Email;
            cliente.Telefone = clienteAtualizado.Telefone;
            cliente.CPF = clienteAtualizado.CPF;

            _context.SaveChanges();
        }

        public bool EmailExiste(string email) {
            return _context.Clientes.Any(c => c.Email == email);
        }
    }
}