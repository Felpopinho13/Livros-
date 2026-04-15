using Microsoft.EntityFrameworkCore;
using Livros.Domain;

namespace Livros.Infrastructure.Data {
    public class AppDbContext : DbContext {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<Categoria>()
                .HasData(
                    new Categoria { Id = 1, Nome = "Romance" },
                    new Categoria { Id = 2, Nome = "Ficcao" },
                    new Categoria { Id = 3, Nome = "Fantasia" },
                    new Categoria { Id = 4, Nome = "Drama" },
                    new Categoria { Id = 5, Nome = "Biografia" },
                    new Categoria { Id = 6, Nome = "Negocios" },
                    new Categoria { Id = 7, Nome = "Tecnologia" },
                    new Categoria { Id = 8, Nome = "Classicos" }
                );

            modelBuilder.Entity<BandeiraCartao>()
                .HasIndex(b => b.Codigo)
                .IsUnique();

            modelBuilder.Entity<BandeiraCartao>()
                .HasData(
                    new BandeiraCartao { Id = 1, Nome = "Visa", Codigo = "VISA", IsAtiva = true },
                    new BandeiraCartao { Id = 2, Nome = "Mastercard", Codigo = "MASTERCARD", IsAtiva = true },
                    new BandeiraCartao { Id = 3, Nome = "Elo", Codigo = "ELO", IsAtiva = true },
                    new BandeiraCartao { Id = 4, Nome = "Hipercard", Codigo = "HIPERCARD", IsAtiva = true },
                    new BandeiraCartao { Id = 5, Nome = "American Express", Codigo = "AMEX", IsAtiva = true }
                );

            modelBuilder.Entity<Cartao>()
                .HasOne(c => c.BandeiraCartao)
                .WithMany(b => b.Cartoes)
                .HasForeignKey(c => c.BandeiraCartaoId)
                .OnDelete(DeleteBehavior.Restrict);

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

            modelBuilder.Entity<ReservaCarrinho>()
                .HasOne(r => r.Livro)
                .WithMany()
                .HasForeignKey(r => r.LivroId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReservaCarrinho>()
                .HasOne(r => r.Cliente)
                .WithMany(c => c.ReservasCarrinho)
                .HasForeignKey(r => r.ClienteId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ReservaCarrinho>()
                .HasIndex(r => new { r.LivroId, r.ExpiraEm });

            modelBuilder.Entity<ReservaCarrinho>()
                .HasIndex(r => new { r.ClienteId, r.SessionKey });
        }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Endereco> Enderecos { get; set; }
        public DbSet<Estado> Estados { get; set; }
        public DbSet<Cidade> Cidades { get; set; }
        public DbSet<Bairro> Bairros { get; set; }
        public DbSet<BandeiraCartao> BandeirasCartao { get; set; }
        public DbSet<Cartao> Cartoes { get; set; }
        public DbSet<Livro> Livros { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<PedidoItem> PedidoItens { get; set; }
        public DbSet<Pagamento> Pagamentos { get; set; }
        public DbSet<Estoque> Estoques { get; set; }
        public DbSet<Troca> Trocas { get; set; }
        public DbSet<CupomDesconto> CuponsDesconto { get; set; }
        public DbSet<ReservaCarrinho> ReservasCarrinho { get; set; }
    }
}
