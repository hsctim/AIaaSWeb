using Abp.AutoMapper;
using Abp.Modules;
using Abp.Reflection.Extensions;

namespace AIaaS
{
    [DependsOn(typeof(AIaaSClientModule), typeof(AbpAutoMapperModule))]
    public class AIaaSXamarinSharedModule : AbpModule
    {
        public override void PreInitialize()
        {
            Configuration.Localization.IsEnabled = false;
            Configuration.BackgroundJobs.IsJobExecutionEnabled = false;
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(AIaaSXamarinSharedModule).GetAssembly());
        }
    }
}