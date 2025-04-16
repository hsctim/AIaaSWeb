using System;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Microsoft.AspNetCore.Mvc;
using AIaaS.MultiTenancy.Payments;
using AIaaS.MultiTenancy.Payments.Paypal;
using AIaaS.MultiTenancy.Payments.PayPal;
using AIaaS.Web.Models.Paypal;
using ApiProtectorDotNet;
using Abp.UI;
using AIaaS.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace AIaaS.Web.Controllers
{
    public class PayPalController : AIaaSControllerBase
    {
        private readonly PayPalPaymentGatewayConfiguration _payPalConfiguration;
        private readonly ISubscriptionPaymentRepository _subscriptionPaymentRepository;
        private readonly IPayPalPaymentAppService _payPalPaymentAppService;
        private readonly TenantManager _tenantManager;

        public PayPalController(
            PayPalPaymentGatewayConfiguration payPalConfiguration,
            ISubscriptionPaymentRepository subscriptionPaymentRepository,
            IPayPalPaymentAppService payPalPaymentAppService,
            TenantManager tenantManager)
        {
            _payPalConfiguration = payPalConfiguration;
            _subscriptionPaymentRepository = subscriptionPaymentRepository;
            _payPalPaymentAppService = payPalPaymentAppService;
            _payPalConfiguration = payPalConfiguration;
            _tenantManager = tenantManager;
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<ActionResult> Purchase(long paymentId, int? tenantId)
        {
            if (tenantId.HasValue)
            {
                SetTenantIdCookie(tenantId);
            }

            var payment = await _subscriptionPaymentRepository.GetAsync(paymentId);
            if (payment.Status != SubscriptionPaymentStatus.NotPaid)
                throw new ApplicationException(L("PaymentIsProcessed"));

            if (payment.TenantId != AbpSession.TenantId)
                throw new UserFriendlyException(L("SubscriptionInputError"));

            if (payment.CreationTime.AddDays(1) < DateTime.UtcNow)
                throw new UserFriendlyException(L("PaymentIsExpired"));

            var editionLoginInfo = await _tenantManager.Tenants.Include(t => t.Edition).FirstAsync(t => t.Id == AbpSession.TenantId);

            //若訂閱期還有一年，則不能延長
            if (editionLoginInfo.SubscriptionEndDateUtc != null && editionLoginInfo.SubscriptionEndDateUtc > DateTime.UtcNow.AddYears(1))
                throw new UserFriendlyException(L("SubscriptionInputError"));

            if (payment.IsRecurring)
            {
                throw new ApplicationException("PayPal integration doesn't support recurring payments !");
            }

            decimal Amount = 0;

            if (string.Compare(AIaaSConsts.Currency, "TWD", true) == 0)
                Amount = Decimal.Round(payment.Amount);
            else
                Amount = Decimal.Round(payment.Amount, 2);

            var model = new PayPalPurchaseViewModel
            {
                PaymentId = payment.Id,
                Amount = Amount,
                Description = payment.Description,
                Configuration = _payPalConfiguration
            };

            return View(model);
        }

        [HttpPost]
        //[UnitOfWork(IsDisabled = true)]

        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<ActionResult> ConfirmPayment(long paymentId, string paypalOrderId)
        {
            try
            {
                await _payPalPaymentAppService.ConfirmPayment(paymentId, paypalOrderId);

                var returnUrl = await GetSuccessUrlAsync(paymentId);
                return Redirect(returnUrl);
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message, exception);

                var returnUrl = await GetErrorUrlAsync(paymentId);
                return Redirect(returnUrl);
            }
        }

        private async Task<string> GetSuccessUrlAsync(long paymentId)
        {
            var payment = await _subscriptionPaymentRepository.GetAsync(paymentId);
            return payment.SuccessUrl + (payment.SuccessUrl.Contains("?") ? "&" : "?") + "paymentId=" + paymentId;
        }

        private async Task<string> GetErrorUrlAsync(long paymentId)
        {
            var payment = await _subscriptionPaymentRepository.GetAsync(paymentId);
            return payment.ErrorUrl + (payment.ErrorUrl.Contains("?") ? "&" : "?") + "paymentId=" + paymentId;
        }
    }
}