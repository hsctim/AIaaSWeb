using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.UI;
using ApiProtectorDotNet;
using AIaaS.MultiTenancy.Payments.FreeUpgrade;


namespace AIaaS.MultiTenancy.Payments
{
    public class FreeUpgradePaymentAppService : AIaaSAppServiceBase, IFreeUpgradePaymentAppService
    {
        private readonly ISubscriptionPaymentRepository _subscriptionPaymentRepository;
        private readonly IPaymentAppService _paymentAppService;

        public FreeUpgradePaymentAppService(ISubscriptionPaymentRepository subscriptionPaymentRepository,
            IPaymentAppService paymentAppService)
        {
            _subscriptionPaymentRepository = subscriptionPaymentRepository;
            _paymentAppService = paymentAppService;
        }

        [RemoteService(false)]
        public async Task CompletePayment(long paymentId)
        {
            var payment = await _subscriptionPaymentRepository.GetAsync(paymentId);

            if (payment.Amount != 0)
                throw new UserFriendlyException(L("SubscriptionInputError"));

            payment.Gateway = SubscriptionPaymentGatewayType.FreeUpgrade;

            payment.SetAsPaid();

            //await _paymentAppService.UpgradeSucceed(paymentId);
        }
    }
}