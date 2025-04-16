using System.Threading.Tasks;
using Abp.Application.Services;


namespace AIaaS.MultiTenancy.Payments.FreeUpgrade
{
    public interface IFreeUpgradePaymentAppService : IApplicationService
    {
        Task CompletePayment(long paymentId);
    }
}
