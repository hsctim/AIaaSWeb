using Abp.Modules;
using Abp.Reflection.Extensions;

namespace AIaaS
{
    [DependsOn(typeof(AIaaSCoreSharedModule))]
    public class AIaaSApplicationSharedModule : AbpModule
    {
        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(AIaaSApplicationSharedModule).GetAssembly());
        }
    }
}