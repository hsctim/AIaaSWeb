using Abp.Modules;
using Abp.Reflection.Extensions;
using Castle.Windsor.MsDependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using AIaaS.Configure;
using AIaaS.Startup;
using AIaaS.Test.Base;

namespace AIaaS.GraphQL.Tests
{
    [DependsOn(
        typeof(AIaaSGraphQLModule),
        typeof(AIaaSTestBaseModule))]
    public class AIaaSGraphQLTestModule : AbpModule
    {
        public override void PreInitialize()
        {
            IServiceCollection services = new ServiceCollection();
            
            services.AddAndConfigureGraphQL();

            WindsorRegistrationHelper.CreateServiceProvider(IocManager.IocContainer, services);
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(AIaaSGraphQLTestModule).GetAssembly());
        }
    }
}