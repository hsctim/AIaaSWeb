using Abp.Domain.Services;

namespace AIaaS
{
    public abstract class AIaaSDomainServiceBase : DomainService
    {
        /* Add your common members for all your domain services. */

        protected AIaaSDomainServiceBase()
        {
            LocalizationSourceName = AIaaSConsts.LocalizationSourceName;
        }
    }
}
