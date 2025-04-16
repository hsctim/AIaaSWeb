using Abp.AspNetCore.Mvc.Views;

namespace AIaaS.Web.Views
{
    public abstract class AIaaSRazorPage<TModel> : AbpRazorPage<TModel>
    {
        protected AIaaSRazorPage()
        {
            LocalizationSourceName = AIaaSConsts.LocalizationSourceName;
        }
    }
}
