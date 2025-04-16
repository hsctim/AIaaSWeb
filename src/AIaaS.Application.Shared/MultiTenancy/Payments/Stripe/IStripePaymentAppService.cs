using System.Threading.Tasks;
using Abp.Application.Services;
using AIaaS.MultiTenancy.Payments.Dto;
using AIaaS.MultiTenancy.Payments.Stripe.Dto;

namespace AIaaS.MultiTenancy.Payments.Stripe
{
    public interface IStripePaymentAppService : IApplicationService
    {
        Task ConfirmPayment(StripeConfirmPaymentInput input);

        StripeConfigurationDto GetConfiguration();

        Task<SubscriptionPaymentDto> GetPaymentAsync(StripeGetPaymentInput input);

        Task<string> CreatePaymentSession(StripeCreatePaymentSessionInput input);
    }
}