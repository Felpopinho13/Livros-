using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Livros.Web.Controllers {
    public class EstoqueController : Controller {
        private readonly EstoqueService _service;

        public EstoqueController(EstoqueService service) {
            _service = service;
        }

        public IActionResult Index() {
            var estoques = _service.Listar();
            return View(estoques);
        }

        [HttpPost]
        public IActionResult Adicionar(int livroId, int quantidade) {
            _service.AdicionarEstoque(livroId, quantidade);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Ajustar(int livroId, int quantidade) {
            _service.AjustarEstoque(livroId, quantidade);
            return RedirectToAction("Index");
        }
    }
}
