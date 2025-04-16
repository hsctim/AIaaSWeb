using Abp.Modules;
using Abp.Reflection.Extensions;

namespace AIaaS
{
    [DependsOn(typeof(AIaaSXamarinSharedModule))]
    public class AIaaSXamarinAndroidModule : AbpModule
    {
        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(AIaaSXamarinAndroidModule).GetAssembly());
        }
    }
}