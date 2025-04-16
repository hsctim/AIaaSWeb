using Abp.AspNetCore.Mvc.ViewComponents;

namespace AIaaS.Web.Public.Views
{
    public abstract class AIaaSViewComponent : AbpViewComponent
    {
        protected AIaaSViewComponent()
        {
            LocalizationSourceName = AIaaSConsts.LocalizationSourceName;
        }
    }
}