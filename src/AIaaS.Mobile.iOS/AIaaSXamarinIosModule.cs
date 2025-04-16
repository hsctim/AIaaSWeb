using Abp.Modules;
using Abp.Reflection.Extensions;

namespace AIaaS
{
    [DependsOn(typeof(AIaaSXamarinSharedModule))]
    public class AIaaSXamarinIosModule : AbpModule
    {
        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(AIaaSXamarinIosModule).GetAssembly());
        }
    }
}