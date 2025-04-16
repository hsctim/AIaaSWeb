using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Web.Areas.App.Models.Layout;
using AIaaS.Web.Views;

namespace AIaaS.Web.Areas.App.Views.Shared.Components.AppToggleDarkMode
{
    public class AppToggleDarkModeViewComponent : AIaaSViewComponent
    {
        public Task<IViewComponentResult> InvokeAsync(string cssClass, bool isDarkModeActive)
        {
            return Task.FromResult<IViewComponentResult>(View(new ToggleDarkModeViewModel(cssClass, isDarkModeActive)));
        }
    }
}