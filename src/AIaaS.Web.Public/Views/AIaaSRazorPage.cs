using Abp.AspNetCore.Mvc.Views;
using Abp.Runtime.Session;
using Microsoft.AspNetCore.Mvc.Razor.Internal;

namespace AIaaS.Web.Public.Views
{
    public abstract class AIaaSRazorPage<TModel> : AbpRazorPage<TModel>
    {
        [RazorInject]
        public IAbpSession AbpSession { get; set; }

        protected AIaaSRazorPage()
        {
            LocalizationSourceName = AIaaSConsts.LocalizationSourceName;
        }
    }
}
