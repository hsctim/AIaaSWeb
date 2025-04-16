using System.Threading.Tasks;
using Abp.Configuration.Startup;
using Abp.Runtime.Session;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Authorization;
using AIaaS.Web.Areas.App.Models.Layout;
using AIaaS.Web.Session;
using AIaaS.Web.Views;
using AIaaS.Authorization.Users;
using AIaaS.Helpers;
using Abp.Domain.Repositories;
using System.Linq;
using System;
using AIaaS.Sessions.Dto;
using Abp.Runtime.Caching;

namespace AIaaS.Web.Areas.App.Views.Shared.Components.AppUserMenu
{
    public class AppUserMenuViewComponent : AIaaSViewComponent
    {
        private readonly IMultiTenancyConfig _multiTenancyConfig;
        private readonly IAbpSession _abpSession;
        private readonly IPerRequestSessionCache _sessionCache;
        //private readonly NlpCacheManagerHelper _nlpLRUCacheHelper;
        private readonly ICacheManager _cacheManager;

        public AppUserMenuViewComponent(
            IPerRequestSessionCache sessionCache, 
            IMultiTenancyConfig multiTenancyConfig, 
            IAbpSession abpSession,
            ICacheManager cacheManager
            //NlpCacheManagerHelper nlpLRUCacheHelper
            )
        {
            _sessionCache = sessionCache;
            _multiTenancyConfig = multiTenancyConfig;
            _abpSession = abpSession;
            //_nlpLRUCacheHelper = nlpLRUCacheHelper;
            _cacheManager = cacheManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(
            string togglerCssClass, 
            string textCssClass, 
            string symbolCssClass,
            string symbolTextCssClas,
            string anchorCssClass,
            bool renderOnlyIcon = false,
            string profileImageCssClass="")
        {
            var user = (UserLoginInfoDto)_cacheManager.Get_UserLoginInfoDto(_abpSession.UserId);

            return View(new UserMenuViewModel
            {
                LoginInformations = await _sessionCache.GetCurrentLoginInformationsAsync(),
                IsMultiTenancyEnabled = _multiTenancyConfig.IsEnabled,
                IsImpersonatedLogin = _abpSession.ImpersonatorUserId.HasValue,
                HasUiCustomizationPagePermission = await PermissionChecker.IsGrantedAsync(AppPermissions.Pages_Administration_UiCustomization),
                TogglerCssClass = togglerCssClass,
                TextCssClass = textCssClass,
                SymbolCssClass = symbolCssClass,
                SymbolTextCssClass = symbolTextCssClas,
                AnchorCssClass = anchorCssClass,
                RenderOnlyIcon = renderOnlyIcon,
                ProfilePictureId = user == null ? Guid.NewGuid().ToString() : user.ProfilePictureId
            });
        }
    }
}
