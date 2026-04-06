using Livros.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class EstoqueService {
    private readonly AppDbContext _context;

    public EstoqueService(AppDbContext context) {
        _context = context;
    }

    public List<Estoque> Listar() {
        var livrosSemEstoque = _context.Livros
            .Where(l => l.IsAtivo && !_context.Estoques.Any(e => e.LivroId == l.Id))
            .ToList();

        if (livrosSemEstoque.Any()) {
            var novosEstoques = livrosSemEstoque.Select(l => new Estoque {
                LivroId = l.Id,
                Quantidade = 0
            });

            _context.Estoques.AddRange(novosEstoques);
            _context.SaveChanges();
        }

        return _context.Estoques
            .Include(e => e.Livro)
            .Where(e => e.Livro.IsAtivo)
            .ToList();
    }

    public void AdicionarEstoque(int livroId, int quantidade) {
        var estoque = _context.Estoques.FirstOrDefault(e => e.LivroId == livroId);

        if (estoque != null) {
            estoque.Quantidade += quantidade;
        }

        _context.SaveChanges();
    }

    public void AjustarEstoque(int livroId, int novoValor) {
        var estoque = _context.Estoques.FirstOrDefault(e => e.LivroId == livroId);

        if (estoque != null) {
            estoque.Quantidade = novoValor;
        }

        _context.SaveChanges();
    }
}
