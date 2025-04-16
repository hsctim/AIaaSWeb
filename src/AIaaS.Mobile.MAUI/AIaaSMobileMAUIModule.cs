using Abp.AutoMapper;
using Abp.Configuration.Startup;
using Abp.Modules;
using Abp.Reflection.Extensions;
using AIaaS.ApiClient;
using AIaaS.Mobile.MAUI.Core.ApiClient;

namespace AIaaS
{
    [DependsOn(typeof(AIaaSClientModule), typeof(AbpAutoMapperModule))]

    public class AIaaSMobileMAUIModule : AbpModule
    {
        public override void PreInitialize()
        {
            Configuration.Localization.IsEnabled = false;
            Configuration.BackgroundJobs.IsJobExecutionEnabled = false;

            Configuration.ReplaceService<IApplicationContext, MAUIApplicationContext>();
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(AIaaSMobileMAUIModule).GetAssembly());
        }
    }
}