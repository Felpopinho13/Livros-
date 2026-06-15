using Livros.Application.BookReviews;
using Livros.Web.Services;
using Microsoft.AspNetCore.Mvc;

public sealed class AvaliacaoController : Controller {
    private readonly BookReviewService _bookReviewService;
    private readonly UserSessionService _userSessionService;

    public AvaliacaoController(
        BookReviewService bookReviewService,
        UserSessionService userSessionService) {
        _bookReviewService = bookReviewService;
        _userSessionService = userSessionService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Criar(int pedidoId, int pedidoItemId, int nota, string? comentario) {
        var customerId = _userSessionService.GetCustomerId(HttpContext.Session);
        if (!customerId.HasValue) {
            return RedirectToAction("Login", "Auth", new {
                returnUrl = Url.Action("DetalhesPedido", "Pedido", new { id = pedidoId })
            });
        }

        var result = _bookReviewService.Create(new BookReviewCreateCommand {
            CustomerId = customerId.Value,
            OrderId = pedidoId,
            OrderItemId = pedidoItemId,
            Rating = nota,
            Comment = comentario
        });

        if (!result.OrderItemFound) {
            TempData["ErroAvaliacao"] = "Nao foi possivel localizar o item para avaliacao.";
            return RedirectToAction("DetalhesPedido", "Pedido", new { id = pedidoId });
        }

        if (!result.Success) {
            TempData["ErroAvaliacao"] = result.ErrorMessage;
            return RedirectToAction("DetalhesPedido", "Pedido", new { id = pedidoId });
        }

        TempData["SucessoAvaliacao"] = result.SuccessMessage;
        return RedirectToAction("DetalhesPedido", "Pedido", new { id = pedidoId });
    }
}
