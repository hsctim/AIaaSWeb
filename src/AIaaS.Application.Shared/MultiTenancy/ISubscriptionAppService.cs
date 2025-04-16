using System.Threading.Tasks;
using Abp.Application.Services;

namespace AIaaS.MultiTenancy
{
    public interface ISubscriptionAppService : IApplicationService
    {
        Task DisableRecurringPayments();

        Task EnableRecurringPayments();
    }
}
