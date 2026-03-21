using Microsoft.AspNetCore.Mvc;

namespace Livros.Web.Controllers {
    public class AdminController : Controller {
        public IActionResult Dashboard() {
            var isAdmin = HttpContext.Session.GetString("IsAdmin");

            if (isAdmin != "True") {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }
    }
}
