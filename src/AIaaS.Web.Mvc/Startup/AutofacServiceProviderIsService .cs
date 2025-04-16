using Abp.Dependency;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AIaaS.Web.Startup
{
    public class AutofacServiceProviderIsService : IServiceProviderIsService, ISingletonDependency
    {
        private readonly IIocManager iocManager;

        public AutofacServiceProviderIsService(IIocManager iocManager)
        {
            this.iocManager = iocManager;
        }
        public bool IsService(Type serviceType)
        {
            return iocManager.IsRegistered(serviceType);
        }
    }
}