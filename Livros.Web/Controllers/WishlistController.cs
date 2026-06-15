using Livros.Application.CustomerWishlist;
using Livros.Web.Services;
using Microsoft.AspNetCore.Mvc;

public sealed class WishlistController : Controller {
    private readonly CustomerWishlistService _wishlistService;
    private readonly UserSessionService _userSessionService;

    public WishlistController(
        CustomerWishlistService wishlistService,
        UserSessionService userSessionService) {
        _wishlistService = wishlistService;
        _userSessionService = userSessionService;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken = default) {
        var customerId = _userSessionService.GetCustomerId(HttpContext.Session);
        var result = await _wishlistService.BuildAsync(customerId, cancellationToken);

        return Json(new {
            isAuthenticated = result.IsAuthenticated,
            count = result.Count,
            items = result.Items.Select(item => new {
                livroId = item.LivroId,
                titulo = item.Titulo,
                preco = item.Preco.ToString("N2"),
                imagemUrl = item.ImagemUrl,
                dataAdicao = item.DataAdicao.ToString("O")
            })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] WishlistBookRequest request, CancellationToken cancellationToken = default) {
        var customerId = _userSessionService.GetCustomerId(HttpContext.Session);
        var result = await _wishlistService.AddAsync(customerId, request.BookId, cancellationToken);

        if (result.RequiresAuthentication) {
            return Unauthorized(new {
                succeeded = result.Succeeded,
                message = result.Message,
                requiresAuthentication = true
            });
        }

        return Json(new {
            succeeded = result.Succeeded,
            message = result.Message,
            count = result.Count,
            isInWishlist = result.IsInWishlist
        });
    }

    [HttpPost]
    public async Task<IActionResult> Remove([FromBody] WishlistBookRequest request, CancellationToken cancellationToken = default) {
        var customerId = _userSessionService.GetCustomerId(HttpContext.Session);
        var result = await _wishlistService.RemoveAsync(customerId, request.BookId, cancellationToken);

        if (result.RequiresAuthentication) {
            return Unauthorized(new {
                succeeded = result.Succeeded,
                message = result.Message,
                requiresAuthentication = true
            });
        }

        return Json(new {
            succeeded = result.Succeeded,
            message = result.Message,
            count = result.Count,
            isInWishlist = result.IsInWishlist
        });
    }

    public sealed class WishlistBookRequest {
        public int BookId { get; set; }
    }
}
