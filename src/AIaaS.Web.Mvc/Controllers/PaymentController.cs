using Abp.Extensions;
using Abp.Runtime.Session;
using Abp.Web.Models;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Editions;
using AIaaS.MultiTenancy;
using AIaaS.MultiTenancy.Dto;
using AIaaS.MultiTenancy.Payments;
using AIaaS.MultiTenancy.Payments.Dto;
using AIaaS.Url;
using AIaaS.Web.Models.Payment;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using AIaaS.Authorization;
using AIaaS.Authorization.Roles;
using AIaaS.Authorization.Users;
using AIaaS.Identity;
using ApiProtectorDotNet;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using Abp.Zero.Configuration;
using Abp.Configuration;
using AIaaS.Nlp;

namespace AIaaS.Web.Controllers
{
    public class PaymentController : AIaaSControllerBase
    {
        private readonly IPaymentAppService _paymentAppService;
        private readonly ITenantRegistrationAppService _tenantRegistrationAppService;
        private readonly TenantManager _tenantManager;
        private readonly EditionManager _editionManager;
        private readonly IWebUrlService _webUrlService;
        private readonly ISubscriptionPaymentRepository _subscriptionPaymentRepository;
        private readonly UserClaimsPrincipalFactory<User, Role> _userClaimsPrincipalFactory;
        private readonly UserManager _userManager;
        private readonly SignInManager _signInManager;
        private readonly INlpPolicyAppService _nlpPolicyAppService;

        public PaymentController(
            IPaymentAppService paymentAppService,
            ITenantRegistrationAppService tenantRegistrationAppService,
            TenantManager tenantManager,
            EditionManager editionManager,
            IWebUrlService webUrlService,
            ISubscriptionPaymentRepository subscriptionPaymentRepository,
            UserClaimsPrincipalFactory<User, Role> userClaimsPrincipalFactory,
            UserManager userManager,
            SignInManager signInManager,
            INlpPolicyAppService nlpPolicyAppService)
        {
            _paymentAppService = paymentAppService;
            _tenantRegistrationAppService = tenantRegistrationAppService;
            _tenantManager = tenantManager;
            _editionManager = editionManager;
            _webUrlService = webUrlService;
            _subscriptionPaymentRepository = subscriptionPaymentRepository;
            _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
            _userManager = userManager;
            _signInManager = signInManager;
            _nlpPolicyAppService = nlpPolicyAppService;
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<IActionResult> Buy(int tenantId, int editionId, int? subscriptionStartType, int? editionPaymentType)
        {
            if (!AbpSession.TenantId.HasValue)
            {
            SetTenantIdCookie(tenantId);
            }

            var edition = await _tenantRegistrationAppService.GetEdition(editionId);

            var model = new BuyEditionViewModel
            {
                Edition = edition,
                PaymentGateways = _paymentAppService.GetActiveGateways(new GetActiveGatewaysInput())
            };

            if (editionPaymentType.HasValue)
            {
                model.EditionPaymentType = (EditionPaymentType)editionPaymentType.Value;
            }

            if (subscriptionStartType.HasValue)
            {
                model.SubscriptionStartType = (SubscriptionStartType)subscriptionStartType.Value;
            }

            return View("Buy", model);
        }

        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<IActionResult> Upgrade(int upgradeEditionId)
        {
            if (!AbpSession.TenantId.HasValue)
            {
                throw new ArgumentNullException();
            }

            var upgradeEdition = await _editionManager.GetByIdAsync(upgradeEditionId);
            if (upgradeEdition.Name != "Free" && upgradeEdition.Name != "Basic" && upgradeEdition.Name != "Pro" && upgradeEdition.Name != "Business")
            {
                return RedirectToAction("Index", "SubscriptionManagement", new { area = "App" });
            }


            SubscriptionPaymentType subscriptionPaymentType;
            Tenant tenant;

            var hasAnyPayment = await _paymentAppService.HasAnyPayment();

            using (CurrentUnitOfWork.SetTenantId(null))
            {
                tenant = await _tenantManager.GetByIdAsync(AbpSession.GetTenantId());
                subscriptionPaymentType = tenant.SubscriptionPaymentType;

                if (tenant.EditionId.HasValue)
                {
                    var currentEdition = await _editionManager.GetByIdAsync(tenant.EditionId.Value);

                    if (((SubscribableEdition)currentEdition).IsFree ||
                        (tenant.SubscriptionEndDateUtc != null && tenant.SubscriptionEndDateUtc.Value < DateTime.UtcNow)
                        )
                    {
                        //var upgradeEdition = await _editionManager.GetByIdAsync(upgradeEditionId);

                        if (((SubscribableEdition)upgradeEdition).IsFree)
                        {
                            await _paymentAppService.SwitchBetweenFreeEditions(upgradeEditionId);
                            return RedirectToAction("Index", "SubscriptionManagement", new { area = "App" });
                        }

                        return RedirectToAction("Buy", "Payment", new
                        {
                            tenantId = AbpSession.GetTenantId(),
                            editionId = upgradeEditionId,
                            editionPaymentType = (int)EditionPaymentType.BuyNow
                        });
                    }

                    if (!hasAnyPayment)
                    {
                    using (CurrentUnitOfWork.SetTenantId(AbpSession.GetTenantId()))
                    {
                        if (!await _paymentAppService.HasAnyPayment())
                        {
                            return RedirectToAction("Buy", "Payment", new
                            {
                                tenantId = AbpSession.GetTenantId(),
                                editionId = upgradeEditionId,
                                editionPaymentType = (int)EditionPaymentType.BuyNow
                            });
                        }
                    }
                    }
                }
            }

            var paymentInfo = await _paymentAppService.GetPaymentInfo(new PaymentInfoInput { UpgradeEditionId = upgradeEditionId });

            if (paymentInfo.IsLessThanMinimumUpgradePaymentAmount() && paymentInfo.AdditionalPrice > 0)
            {
                await _paymentAppService.UpgradeSubscriptionCostsLessThenMinAmount(upgradeEditionId);
                return RedirectToAction("Index", "SubscriptionManagement", new { area = "App" });
            }
            var edition = await _tenantRegistrationAppService.GetEdition(upgradeEditionId);

            var lastPayment = await _subscriptionPaymentRepository.GetLastCompletedPaymentOrDefaultAsync(
                tenantId: AbpSession.GetTenantId(),
                gateway: null,
                isRecurring: null);

            var model = new UpgradeEditionViewModel
            {
                Edition = edition,
                AdditionalPrice = paymentInfo.AdditionalPrice,
                SubscriptionPaymentType = subscriptionPaymentType,
                PaymentPeriodType = lastPayment.GetPaymentPeriodType(),
                SubscriptionEndDateUtc = tenant.SubscriptionEndDateUtc,
            };

            if (subscriptionPaymentType.IsRecurring())
            {
                model.PaymentGateways = new List<PaymentGatewayModel>
                {
                    new PaymentGatewayModel
                    {
                        GatewayType = lastPayment.Gateway,
                        SupportsRecurringPayments = true
                    }
                };
            }
            else
            {
                model.PaymentGateways = _paymentAppService.GetActiveGateways(new GetActiveGatewaysInput());
            }

            return View("Upgrade", model);
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<IActionResult> Extend(int upgradeEditionId, EditionPaymentType editionPaymentType)
        {
            //if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Development")
            //    return Forbid();

            var editionLoginInfo = await _tenantManager.Tenants.Include(t => t.Edition).FirstAsync(t => t.Id == AbpSession.TenantId);

            if (editionLoginInfo.EditionId != upgradeEditionId || editionLoginInfo.SubscriptionEndDateUtc == null || ((SubscribableEdition)editionLoginInfo.Edition).IsFree == true)
                throw new UserFriendlyException(L("SubscriptionInputError"));

            //若訂閱期還有一年，則不能延長
            if (editionLoginInfo.SubscriptionEndDateUtc != null && editionLoginInfo.SubscriptionEndDateUtc > DateTime.UtcNow.AddYears(1))
                throw new UserFriendlyException(L("SubscriptionCannotExtendByDate"));

            var edition = await _tenantRegistrationAppService.GetEdition(upgradeEditionId);

            var model = new ExtendEditionViewModel
            {
                Edition = edition,
                PaymentGateways = _paymentAppService.GetActiveGateways(new GetActiveGatewaysInput())
            };

            return View("Extend", model);
        }

        [HttpPost]
        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<JsonResult> CreatePayment(CreatePaymentModel model)
        {
            var paymentId = await _paymentAppService.CreatePayment(new CreatePaymentDto
            {
                PaymentPeriodType = model.PaymentPeriodType,
                EditionId = model.EditionId,
                EditionPaymentType = model.EditionPaymentType,
                RecurringPaymentEnabled = model.RecurringPaymentEnabled.HasValue && model.RecurringPaymentEnabled.Value,
                SubscriptionPaymentGatewayType = model.Gateway,
                SuccessUrl = _webUrlService.GetSiteRootAddress().EnsureEndsWith('/') + "Payment/" + model.EditionPaymentType + "Succeed",
                ErrorUrl = _webUrlService.GetSiteRootAddress().EnsureEndsWith('/') + "Payment/PaymentFailed"
            });

            return Json(new AjaxResponse
            {
                TargetUrl = Url.Action("Purchase", model.Gateway.ToString(), new
                {
                    paymentId = paymentId,
                    isUpgrade = model.EditionPaymentType == EditionPaymentType.Upgrade
                })
            });
        }

        [HttpPost]
        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task CancelPayment(CancelPaymentModel model)
        {
            await _paymentAppService.CancelPayment(new CancelPaymentDto
            {
                Gateway = model.Gateway,
                PaymentId = model.PaymentId
            });
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<IActionResult> BuyNowSucceed(long paymentId)
        {
            await _paymentAppService.BuyNowSucceed(paymentId);

            //return RedirectToAction("Index", "SubscriptionManagement", new { area = "App" });
            return View();
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<IActionResult> NewRegistrationSucceed(long paymentId)
        {
            await _paymentAppService.NewRegistrationSucceed(paymentId);

            await LoginAdminAsync();

            //return RedirectToAction("Index", "SubscriptionManagement", new { area = "App" });
            return View();
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<IActionResult> UpgradeSucceed(long paymentId)
        {
            await _paymentAppService.UpgradeSucceed(paymentId);

            try
            {
                if (AbpSession.TenantId.HasValue)
                    await _nlpPolicyAppService.UpdateTenantPriority(AbpSession.TenantId.Value);
            }
            catch (Exception)
            {
            }

            //return RedirectToAction("Index", "SubscriptionManagement", new { area = "App" });
            return View();
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<IActionResult> ExtendSucceed(long paymentId)
        {
            await _paymentAppService.ExtendSucceed(paymentId);
            return View();
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<IActionResult> PaymentFailed(long paymentId)
        {
            await _paymentAppService.PaymentFailed(paymentId);
            return View();
        }

        private async Task LoginAdminAsync()
        {
            var user = await _userManager.GetAdminAsync();
            var principal = await _userClaimsPrincipalFactory.CreateAsync(user);

            await _signInManager.SignOutAsync();
            await _signInManager.SignInAsync(principal.Identity as ClaimsIdentity, false);
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<IActionResult> PaymentCompleted()
        {
            try
            {
                //檢查新使用者是否有Mail啟用
                if (AbpSession.UserId.HasValue)
                {
                    var user = await _userManager.GetUserAsync(AbpSession.ToUserIdentifier());
                    var isEmailConfirmationRequiredForLogin = await SettingManager.GetSettingValueAsync<bool>(AbpZeroSettingNames.UserManagement.IsEmailConfirmationRequiredForLogin);

                    if (isEmailConfirmationRequiredForLogin == true && user.IsEmailConfirmed == false)
                    {
                        await _signInManager.SignOutAsync();
                    }
                }
            }
            catch (Exception)
            {
            }

            return View();
        }


        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public IActionResult UpgradeFailed()
        {
            return View();
        }
    }
}