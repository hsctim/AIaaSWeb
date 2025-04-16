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
using AIaaS.MultiTenancy.Payments.FreeUpgrade;

namespace AIaaS.Web.Controllers
{
    public class FreeUpgradeController : AIaaSControllerBase
    {
        private readonly ISubscriptionPaymentRepository _subscriptionPaymentRepository;
        private readonly IFreeUpgradePaymentAppService _freeUpgradePaymentAppService;

        public FreeUpgradeController(
            ISubscriptionPaymentRepository subscriptionPaymentRepository,
            IFreeUpgradePaymentAppService freeUpgradePaymentAppService)
        {
            _subscriptionPaymentRepository = subscriptionPaymentRepository;
            _freeUpgradePaymentAppService = freeUpgradePaymentAppService;
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<ActionResult> Purchase(long paymentId)
        {
            try
            {
                var payment = await _subscriptionPaymentRepository.GetAsync(paymentId);
                if (payment.Status != SubscriptionPaymentStatus.NotPaid)
                    throw new ApplicationException(L("PaymentIsProcessed"));

                if (payment.TenantId != AbpSession.TenantId)
                    throw new UserFriendlyException(L("SubscriptionInputError"));

                if (payment.CreationTime.AddDays(1) < DateTime.UtcNow)
                    throw new UserFriendlyException(L("PaymentIsExpired"));

                if (payment.Amount != 0)
                    throw new UserFriendlyException(L("SubscriptionInputError"));

                await _freeUpgradePaymentAppService.CompletePayment(paymentId);

                return RedirectToAction("UpgradeSucceed", "Payment", new { PaymentId = paymentId });
            }
            catch (Exception)
            {
            }

            return RedirectToAction("UpgradeFailed", "Payment");

        }

        //[HttpPost]
        //[UnitOfWork(IsDisabled = true)]
        //
        //[ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        //public async Task<ActionResult> ConfirmPayment(long paymentId, string paypalOrderId)
        //{
        //    try
        //    {
        //        //var returnUrl = await GetSuccessUrlAsync(paymentId);
        //        var returnUrl = "Payment/PaymentCompleted";
        //        return Redirect(returnUrl);
        //    }
        //    catch (Exception exception)
        //    {
        //        Logger.Error(exception.Message, exception);

        //        var returnUrl = await GetErrorUrlAsync(paymentId);
        //        return Redirect(returnUrl);
        //    }
        //}

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