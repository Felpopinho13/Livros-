using Livros.Domain;

namespace Livros.Application.AdminBooks {
    public sealed class AdminBooksService {
        private readonly IAdminBooksDataProvider _dataProvider;

        public AdminBooksService(IAdminBooksDataProvider dataProvider) {
            _dataProvider = dataProvider;
        }

        public AdminBooksCatalogResult BuildCatalog() {
            return new AdminBooksCatalogResult {
                CategoriasDisponiveis = _dataProvider.LoadCategories(),
                Livros = _dataProvider.LoadActiveBooksWithStockAndCategories()
            };
        }

        public AdminBookOperationResult Create(AdminBookCreateCommand command) {
            var categoriasSelecionadas = NormalizeCategoryIds(command.CategoriasIds);
            if (categoriasSelecionadas.Count == 0) {
                return new AdminBookOperationResult {
                    Succeeded = false,
                    Message = "Selecione pelo menos uma categoria para o livro."
                };
            }

            var categorias = _dataProvider.LoadCategoriesByIds(categoriasSelecionadas);
            if (categorias.Count == 0) {
                return new AdminBookOperationResult {
                    Succeeded = false,
                    Message = "Selecione pelo menos uma categoria para o livro."
                };
            }

            command.Livro.Categorias = categorias;
            _dataProvider.AddBook(command.Livro);
            _dataProvider.SaveChanges();

            _dataProvider.AddStock(new Estoque {
                LivroId = command.Livro.Id,
                Quantidade = 0
            });
            _dataProvider.SaveChanges();

            return new AdminBookOperationResult {
                Succeeded = true,
                Message = "Livro cadastrado com sucesso!",
                LivroTitulo = command.Livro.Titulo
            };
        }

        public AdminBookOperationResult UpdateCategories(AdminBookCategoryUpdateCommand command) {
            var categoriasSelecionadas = NormalizeCategoryIds(command.CategoriasIds);
            if (categoriasSelecionadas.Count == 0) {
                return new AdminBookOperationResult {
                    Succeeded = false,
                    Message = "Selecione pelo menos uma categoria para o livro."
                };
            }

            var livro = _dataProvider.LoadBookByIdWithCategories(command.LivroId);
            if (livro == null) {
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
    }
}
