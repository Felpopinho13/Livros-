using Livros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Livros.Infrastructure.Services {
    public class EnderecoService {
        private readonly AppDbContext _context;

        public EnderecoService(AppDbContext context) {
            _context = context;
        }

        public List<Endereco> ListarPorCliente(int clienteId) {
            return _context.Enderecos
                .Where(e => e.ClienteId == clienteId && (e.IsEntrega || e.IsCobranca))
                .Include(e => e.Bairro)
                .Include(e => e.Cidade)
                    .ThenInclude(c => c.Estado)
                .ToList();
        }
    }
}

