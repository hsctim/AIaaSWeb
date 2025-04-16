using Abp.AutoMapper;
using Abp.Modules;
using Abp.Reflection.Extensions;
using AIaaS.Authorization;

namespace AIaaS
{
    /// <summary>
    /// Application layer module of the application.
    /// </summary>
    [DependsOn(
        typeof(AIaaSApplicationSharedModule),
        typeof(AIaaSCoreModule)
        )]
    public class AIaaSApplicationModule : AbpModule
    {
        public override void PreInitialize()
        {
            //Adding authorization providers
            Configuration.Authorization.Providers.Add<AppAuthorizationProvider>();

            //Adding custom AutoMapper configuration
            Configuration.Modules.AbpAutoMapper().Configurators.Add(CustomDtoMapper.CreateMappings);
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(AIaaSApplicationModule).GetAssembly());
        }
    }
}