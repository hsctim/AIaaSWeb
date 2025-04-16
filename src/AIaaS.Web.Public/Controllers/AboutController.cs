using Microsoft.AspNetCore.Mvc;
using AIaaS.Web.Controllers;

namespace AIaaS.Web.Public.Controllers
{
    public class AboutController : AIaaSControllerBase
    {
        public ActionResult Index()
        {
            return NotFound();
        }
    }
}