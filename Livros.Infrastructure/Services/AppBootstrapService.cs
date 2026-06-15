using System.Globalization;
using System.Text;
using Livros.Domain;
using Livros.Infrastructure.Data;

namespace Livros.Infrastructure.Services {
    public sealed class AppBootstrapService {
        private readonly AppDbContext _context;

        public AppBootstrapService(AppDbContext context) {
            _context = context;
        }

        public void Initialize() {
            var existingCategories = _context.Categorias.ToList();
            var categoriesUpdated = false;

            foreach (var defaultCategory in CategoriaCatalogo.Itens) {
                var existingCategory = existingCategories.FirstOrDefault(category =>
                    NormalizeCategory(category.Nome) == NormalizeCategory(defaultCategory.Nome));

                if (existingCategory == null) {
                    _context.Categorias.Add(new Categoria {
                        Nome = defaultCategory.Nome
                    });
                    categoriesUpdated = true;
                    continue;
                }

                if (!string.Equals(existingCategory.Nome, defaultCategory.Nome, StringComparison.Ordinal)) {
                    existingCategory.Nome = defaultCategory.Nome;
                    categoriesUpdated = true;
                }
            }

            if (categoriesUpdated) {
                _context.SaveChanges();
            }

            if (_context.Clientes.Any(customer => customer.Email == "admin@admin.com")) {
                return;
            }

            var admin = new Cliente {
                Nome = "Admin",
                Email = "admin@admin.com",
                Senha = BCrypt.Net.BCrypt.HashPassword("123456"),
                IsAdmin = true,
                IsAtivo = true
            };

            _context.Clientes.Add(admin);
            _context.SaveChanges();
        }

        private static string NormalizeCategory(string? name) {
            if (string.IsNullOrWhiteSpace(name)) {
                return string.Empty;
            }

            var normalizedText = name.Trim().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();

            foreach (var character in normalizedText) {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark) {
                    builder.Append(char.ToLowerInvariant(character));
                }
            }

            return builder
                .ToString()
                .Normalize(NormalizationForm.FormC);
        }
    }
}
