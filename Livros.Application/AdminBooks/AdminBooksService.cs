using Livros.Application.Common.Logging;
using Livros.Domain;

namespace Livros.Application.AdminBooks {
    public sealed class AdminBooksService {
        private readonly IAdminBooksDataProvider _dataProvider;
        private readonly IAppLogger<AdminBooksService> _logger;

        public AdminBooksService(IAdminBooksDataProvider dataProvider, IAppLogger<AdminBooksService> logger) {
            _dataProvider = dataProvider;
            _logger = logger;
        }

        public AdminBooksCatalogResult BuildCatalog(AdminBooksCatalogQuery? query = null) {
            var normalizedQuery = new AdminBooksCatalogQuery {
                Busca = query?.Busca?.Trim(),
                CategoriaId = query?.CategoriaId,
                Status = NormalizeStatus(query?.Status)
            };

            return new AdminBooksCatalogResult {
                Busca = normalizedQuery.Busca ?? string.Empty,
                CategoriaId = normalizedQuery.CategoriaId,
                Status = normalizedQuery.Status ?? "ativos",
                CategoriasDisponiveis = _dataProvider.LoadCategories(),
                Livros = _dataProvider.LoadBooksWithStockAndCategories(normalizedQuery)
            };
        }

        public AdminBookOperationResult Create(AdminBookCreateCommand command) {
            var categoriasSelecionadas = NormalizeCategoryIds(command.CategoriasIds);
            if (categoriasSelecionadas.Count == 0) {
                _logger.LogWarning("Tentativa de cadastrar livro sem categorias. Titulo: {Titulo}", command.Livro.Titulo);
                return new AdminBookOperationResult {
                    Succeeded = false,
                    Message = "Selecione pelo menos uma categoria para o livro."
                };
            }

            var categorias = _dataProvider.LoadCategoriesByIds(categoriasSelecionadas);
            if (categorias.Count == 0) {
                _logger.LogWarning("Tentativa de cadastrar livro com categorias invalidas. Titulo: {Titulo}", command.Livro.Titulo);
                return new AdminBookOperationResult {
                    Succeeded = false,
                    Message = "Selecione pelo menos uma categoria para o livro."
                };
            }

            command.Livro.IsAtivo = true;
            command.Livro.Categorias = categorias;
            _dataProvider.AddBook(command.Livro);
            _dataProvider.SaveChanges();

            _dataProvider.AddStock(new Estoque {
                LivroId = command.Livro.Id,
                Quantidade = 0
            });
            _dataProvider.SaveChanges();

            _logger.LogInformation(
                "Livro cadastrado no admin. LivroId: {LivroId}, Titulo: {Titulo}, Categorias: {Categorias}",
                command.Livro.Id,
                command.Livro.Titulo,
                string.Join(", ", categorias.Select(c => c.Nome)));

            return new AdminBookOperationResult {
                Succeeded = true,
                Message = "Livro cadastrado com sucesso!",
                LivroTitulo = command.Livro.Titulo
            };
        }

        public AdminBookOperationResult UpdateCategories(AdminBookCategoryUpdateCommand command) {
            var categoriasSelecionadas = NormalizeCategoryIds(command.CategoriasIds);
            if (categoriasSelecionadas.Count == 0) {
                _logger.LogWarning("Tentativa de atualizar categorias sem selecao. LivroId: {LivroId}", command.LivroId);
                return new AdminBookOperationResult {
                    Succeeded = false,
                    Message = "Selecione pelo menos uma categoria para o livro."
                };
            }

            var livro = _dataProvider.LoadBookByIdWithCategories(command.LivroId);
            if (livro == null) {
                _logger.LogWarning("Livro nao encontrado ao atualizar categorias. LivroId: {LivroId}", command.LivroId);
                return new AdminBookOperationResult {
                    Succeeded = false,
                    Message = "Livro nao encontrado."
                };
            }

            var categorias = _dataProvider.LoadCategoriesByIds(categoriasSelecionadas);
            livro.Categorias ??= new List<Categoria>();
            livro.Categorias.Clear();

            foreach (var categoria in categorias) {
                livro.Categorias.Add(categoria);
            }

            _dataProvider.SaveChanges();

            _logger.LogInformation(
                "Categorias atualizadas no admin. LivroId: {LivroId}, Titulo: {Titulo}, Categorias: {Categorias}",
                livro.Id,
                livro.Titulo,
                string.Join(", ", categorias.Select(c => c.Nome)));

            return new AdminBookOperationResult {
                Succeeded = true,
                Message = $"Categorias do livro \"{livro.Titulo}\" atualizadas com sucesso!",
                LivroTitulo = livro.Titulo
            };
        }

        private static List<int> NormalizeCategoryIds(IReadOnlyCollection<int> categoryIds) {
            return categoryIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();
        }

        private static string NormalizeStatus(string? status) {
            return string.IsNullOrWhiteSpace(status)
                ? "ativos"
                : status.Trim().ToLowerInvariant();
        }
    }
}
