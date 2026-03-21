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
                .OnDelete(DeleteBehavior.Restrict); // 🔥 AQUI

            modelBuilder.Entity<Endereco>()
                .HasOne(e => e.Bairro)
                .WithMany()
                .HasForeignKey(e => e.BairroId)
                .OnDelete(DeleteBehavior.Restrict); // 🔥 AQUI

            modelBuilder.Entity<Endereco>()
                .HasOne(e => e.Cliente)
                .WithMany(c => c.Enderecos)
                .HasForeignKey(e => e.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Endereco> Enderecos { get; set; }
        public DbSet<Estado> Estados { get; set; }
        public DbSet<Cidade> Cidades { get; set; }
        public DbSet<Bairro> Bairros { get; set; }
    }
}