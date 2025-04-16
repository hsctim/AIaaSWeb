using System;
using System.Threading.Tasks;
using Abp.Extensions;
using Abp.Runtime.Session;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Editions;
using AIaaS.MultiTenancy;
using AIaaS.MultiTenancy.Payments;
using AIaaS.MultiTenancy.Payments.Stripe;
using AIaaS.MultiTenancy.Payments.Stripe.Dto;
using AIaaS.Url;
using AIaaS.Web.Models.Stripe;
using ApiProtectorDotNet;
using Abp.UI;

namespace AIaaS.Web.Controllers
{
    public class StripeController : StripeControllerBase
    {
        private readonly StripePaymentGatewayConfiguration _stripeConfiguration;
        private readonly IPaymentAppService _paymentAppService;
        private readonly IWebUrlService _webUrlService;
        private readonly StripeGatewayManager _stripeGatewayManager;
        private readonly TenantManager _tenantManager;

        public StripeController(
            StripeGatewayManager stripeGatewayManager,
            StripePaymentGatewayConfiguration stripeConfiguration,
            IStripePaymentAppService stripePaymentAppService,
            IPaymentAppService paymentAppService,
            IWebUrlService webUrlService, TenantManager tenantManager)
            : base(stripeGatewayManager, stripeConfiguration, stripePaymentAppService)
        {
            _stripeGatewayManager = stripeGatewayManager;
            _stripeConfiguration = stripeConfiguration;
            _paymentAppService = paymentAppService;
            _webUrlService = webUrlService;
            _tenantManager = tenantManager;
        }

        public async Task<ActionResult> Purchase(long paymentId, int? tenantId)
        {
            if (tenantId.HasValue)
        {
                SetTenantIdCookie(tenantId);
            }

            var payment = await _paymentAppService.GetPaymentAsync(paymentId);
            if (payment.Status != SubscriptionPaymentStatus.NotPaid)
            {
                throw new ApplicationException("This payment is processed before");
            }

            var model = new StripePurchaseViewModel
            {
                PaymentId = payment.Id,
                Amount = payment.Amount,
                Description = payment.Description,
                IsRecurring = payment.IsRecurring,
                Configuration = _stripeConfiguration,
                UpdateSubscription = payment.EditionPaymentType == EditionPaymentType.Upgrade
            };

            var sessionId = await StripePaymentAppService.CreatePaymentSession(new StripeCreatePaymentSessionInput
            {
                PaymentId = paymentId,
                SuccessUrl = _webUrlService.GetSiteRootAddress().EnsureEndsWith('/') + "Stripe/GetPaymentResult",
                CancelUrl = _webUrlService.GetSiteRootAddress().EnsureEndsWith('/') + "Stripe/PaymentCancel",
            });

            if (payment.IsRecurring && payment.EditionPaymentType == EditionPaymentType.Upgrade)
            {
                model.IsRecurring = false;
            }

            model.SessionId = sessionId;

            return View(model);
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public IActionResult PaymentCancel()
        {
            return Forbid();

            //return View();
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<ActionResult> GetPaymentResult(string sessionId)
        {
            return Forbid();

            var payment = await StripePaymentAppService.GetPaymentAsync(
                new StripeGetPaymentInput
                {
                    StripeSessionId = sessionId
                });

            using (CurrentUnitOfWork.SetTenantId(null))
            {
                var tenant = await _tenantManager.GetByIdAsync(payment.TenantId);
                await _stripeGatewayManager.UpdateCustomerDescriptionAsync(sessionId, tenant.TenancyName);
            }

            if (payment.TenantId != AbpSession.TenantId)
            {
                return new NotFoundResult();
            }

            ViewBag.PaymentId = payment.Id;
            return View();
        }
    }
}
