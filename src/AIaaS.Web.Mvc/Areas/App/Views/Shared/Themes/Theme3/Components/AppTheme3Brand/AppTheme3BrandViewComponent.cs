﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Web.Areas.App.Models.Layout;
using AIaaS.Web.Session;
using AIaaS.Web.Views;

namespace AIaaS.Web.Areas.App.Views.Shared.Themes.Theme3.Components.AppTheme3Brand
{
    public class AppTheme3BrandViewComponent : AIaaSViewComponent
    {
        private readonly IPerRequestSessionCache _sessionCache;

        public AppTheme3BrandViewComponent(IPerRequestSessionCache sessionCache)
        {
            _sessionCache = sessionCache;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var headerModel = new HeaderViewModel
            {
                LoginInformations = await _sessionCache.GetCurrentLoginInformationsAsync()
            };

            return View(headerModel);
        }
    }
}
