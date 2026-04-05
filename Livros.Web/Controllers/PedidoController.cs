using Livros.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Livros.Web.Controllers {
    public class PedidoController : Controller {
        private readonly LivroService _livroService;
        private readonly EnderecoService _enderecoService;

        public PedidoController(LivroService livroService, EnderecoService enderecoService) {
            _livroService = livroService;
            _enderecoService = enderecoService;
        }

        public IActionResult Checkout(int id, int quantidade = 1) {
            var clienteIdStr = HttpContext.Session.GetString("ClienteId");

            if (string.IsNullOrEmpty(clienteIdStr)) {
                return RedirectToAction("Login", "Auth", new {
                    returnUrl = Url.Action("Checkout", "Pedido", new { id = id, quantidade = quantidade })
                });
            }

            var clienteId = int.Parse(clienteIdStr);

            var livro = _livroService.ObterPorId(id);

            var enderecos = _enderecoService.ListarPorCliente(clienteId);

            var vm = new CheckoutViewModel {
                Livro = livro,
                Enderecos = enderecos ?? new List<Endereco>(),
                Quantidade = quantidade // ✅ agora funciona
            };

            return View(vm);
        }
    }
}
