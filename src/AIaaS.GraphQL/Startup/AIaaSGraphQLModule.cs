using Abp.AutoMapper;
using Abp.Modules;
using Abp.Reflection.Extensions;
using AIaaS.Localization;

namespace AIaaS.Startup
{
    [DependsOn(typeof(AIaaSCoreModule))]
    public class AIaaSGraphQLModule : AbpModule
    {
        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(AIaaSGraphQLModule).GetAssembly());
        }

        public override void PreInitialize()
        {
            base.PreInitialize();

            //重新設置語系來源
            AIaaSLocalizationConfigurer.Configure2(Configuration.Localization);

            //Adding custom AutoMapper configuration
            Configuration.Modules.AbpAutoMapper().Configurators.Add(CustomDtoMapper.CreateMappings);
        }
    }
}