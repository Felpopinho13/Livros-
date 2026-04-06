using Microsoft.EntityFrameworkCore;
using Livros.Domain;

namespace Livros.Infrastructure.Data {
    public class AppDbContext : DbContext {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<Endereco>()
                .HasOne(e => e.Cidade)
                .WithMany()
                .HasForeignKey(e => e.CidadeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Endereco>()
                .HasOne(e => e.Bairro)
                .WithMany()
                .HasForeignKey(e => e.BairroId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Endereco>()
                .HasOne(e => e.Cliente)
                .WithMany(c => c.Enderecos)
                .HasForeignKey(e => e.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Cliente)
                .WithMany()
                .HasForeignKey(p => p.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Endereco)
                .WithMany()
                .HasForeignKey(p => p.EnderecoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Estoque>()
                .HasOne(e => e.Livro)
                .WithOne(l => l.Estoque)
                .HasForeignKey<Estoque>(e => e.LivroId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Troca>()
                .HasOne(t => t.Pedido)
                .WithMany()
                .HasForeignKey(t => t.PedidoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Troca>()
                .HasOne(t => t.PedidoItem)
                .WithMany()
                .HasForeignKey(t => t.PedidoItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Troca>()
                .HasOne(t => t.Cliente)
                .WithMany()
                .HasForeignKey(t => t.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Troca>()
                .HasOne(t => t.CupomDesconto)
                .WithOne(c => c.Troca)
                .HasForeignKey<Troca>(t => t.CupomDescontoId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CupomDesconto>()
                .HasOne(c => c.Cliente)
                .WithMany()
                .HasForeignKey(c => c.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CupomDesconto>()
                .HasOne(c => c.Pedido)
                .WithMany()
                .HasForeignKey(c => c.PedidoId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Endereco> Enderecos { get; set; }
        public DbSet<Estado> Estados { get; set; }
        public DbSet<Cidade> Cidades { get; set; }
        public DbSet<Bairro> Bairros { get; set; }
        public DbSet<Cartao> Cartoes { get; set; }
        public DbSet<Livro> Livros { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<PedidoItem> PedidoItens { get; set; }
        public DbSet<Pagamento> Pagamentos { get; set; }
        public DbSet<Estoque> Estoques { get; set; }
        public DbSet<Troca> Trocas { get; set; }
        public DbSet<CupomDesconto> CuponsDesconto { get; set; }
    }
}
