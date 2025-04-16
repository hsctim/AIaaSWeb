using Microsoft.AspNetCore.Mvc;
using AIaaS.Web.Controllers;

namespace AIaaS.Web.Public.Controllers
{
    public class HomeController : AIaaSControllerBase
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}