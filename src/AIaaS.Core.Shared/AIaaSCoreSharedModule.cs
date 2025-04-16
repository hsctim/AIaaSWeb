using Abp.Modules;
using Abp.Reflection.Extensions;

namespace AIaaS
{
    public class AIaaSCoreSharedModule : AbpModule
    {
        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(AIaaSCoreSharedModule).GetAssembly());
        }
    }
}