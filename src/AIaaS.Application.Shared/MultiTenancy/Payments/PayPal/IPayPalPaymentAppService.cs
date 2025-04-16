using System.Threading.Tasks;
using Abp.Application.Services;
using AIaaS.MultiTenancy.Payments.PayPal.Dto;

namespace AIaaS.MultiTenancy.Payments.PayPal
{
    public interface IPayPalPaymentAppService : IApplicationService
    {
        Task ConfirmPayment(long paymentId, string paypalOrderId);

        PayPalConfigurationDto GetConfiguration();
    }
}
