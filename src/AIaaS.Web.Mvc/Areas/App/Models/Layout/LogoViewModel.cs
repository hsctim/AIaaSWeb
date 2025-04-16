using AIaaS.Sessions.Dto;
using Abp.Extensions;

namespace AIaaS.Web.Areas.App.Models.Layout
{
    public class LogoViewModel
    {
        public GetCurrentLoginInformationsOutput LoginInformations { get; set; }

        public string LogoSkinOverride { get; set; }

        public string LogoClassOverride { get; set; }

        public string GetLogoUrl(string appPath, string logoSkin)
        {
            if (!LogoSkinOverride.IsNullOrEmpty())
            {
                logoSkin = LogoSkinOverride;
            }

            if (LoginInformations?.Tenant == null || !LoginInformations.Tenant.HasLogo())
            {
                return appPath + $"Common/Images/app-logo-on-{logoSkin}.svg?";
            }

            //id parameter is used to prevent caching only.
            return appPath + "TenantCustomization/GetTenantLogo?tenantId=" + LoginInformations?.Tenant?.Id + "&skin=" +
                   logoSkin;
    }
}
}