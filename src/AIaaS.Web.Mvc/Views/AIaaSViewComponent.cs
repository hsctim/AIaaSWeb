using Abp.AspNetCore.Mvc.ViewComponents;

namespace AIaaS.Web.Views
{
    public abstract class AIaaSViewComponent : AbpViewComponent
    {
        protected AIaaSViewComponent()
        {
            LocalizationSourceName = AIaaSConsts.LocalizationSourceName;
        }
    }
}