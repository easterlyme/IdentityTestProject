using Microsoft.AspNetCore.Mvc;

namespace IdentityTestProject.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
