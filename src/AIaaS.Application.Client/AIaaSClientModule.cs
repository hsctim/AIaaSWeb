using Abp.Modules;
using Abp.Reflection.Extensions;

namespace AIaaS
{
    public class AIaaSClientModule : AbpModule
    {
        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(AIaaSClientModule).GetAssembly());
        }
    }
}
