﻿using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.Configuration;
using Abp.Configuration.Startup;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.UI;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Authorization;
using AIaaS.Authorization.Users;
using AIaaS.Configuration;
using AIaaS.Debugging;
using AIaaS.Identity;
using AIaaS.MultiTenancy;
using AIaaS.MultiTenancy.Dto;
using AIaaS.MultiTenancy.Payments;
using AIaaS.Security;
using AIaaS.Url;
using AIaaS.Web.Security.Recaptcha;
using System.Threading.Tasks;
using Abp.Collections.Extensions;
using AIaaS.Editions;
using AIaaS.MultiTenancy.Payments.Dto;
using AIaaS.Web.Models.TenantRegistration;
using AIaaS.Nlp;
using ApiProtectorDotNet;
using System;

namespace AIaaS.Web.Controllers
{
    public class TenantRegistrationController : AIaaSControllerBase
    {
        private readonly IMultiTenancyConfig _multiTenancyConfig;
        private readonly UserManager _userManager;
        private readonly AbpLoginResultTypeHelper _abpLoginResultTypeHelper;
        private readonly LogInManager _logInManager;
        private readonly SignInManager _signInManager;
        private readonly IWebUrlService _webUrlService;
        private readonly ITenantRegistrationAppService _tenantRegistrationAppService;
        private readonly IPasswordComplexitySettingStore _passwordComplexitySettingStore;
        private readonly INlpChatbotsAppService _nlpChatbotAppService;

        public TenantRegistrationController(
            IMultiTenancyConfig multiTenancyConfig,
            UserManager userManager,
            AbpLoginResultTypeHelper abpLoginResultTypeHelper,
            LogInManager logInManager,
            SignInManager signInManager,
            IWebUrlService webUrlService,
            ITenantRegistrationAppService tenantRegistrationAppService,
            IPasswordComplexitySettingStore passwordComplexitySettingStore,
            INlpChatbotsAppService nlpChatbotAppService)
        {
            _multiTenancyConfig = multiTenancyConfig;
            _userManager = userManager;
            _abpLoginResultTypeHelper = abpLoginResultTypeHelper;
            _logInManager = logInManager;
            _signInManager = signInManager;
            _webUrlService = webUrlService;
            _tenantRegistrationAppService = tenantRegistrationAppService;
            _passwordComplexitySettingStore = passwordComplexitySettingStore;
            _nlpChatbotAppService = nlpChatbotAppService;
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<ActionResult> SelectEdition()
        {
            CheckTenantRegistrationIsEnabled();

            var output = await _tenantRegistrationAppService.GetEditionsForSelect();
            if (!AbpSession.UserId.HasValue && output.EditionsWithFeatures.IsNullOrEmpty())
            {
                return RedirectToAction("Register", "TenantRegistration");
            }

            var model = ObjectMapper.Map<EditionsSelectViewModel>(output);

            return View(model);
        }

        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 60)]
        public async Task<ActionResult> Register(int? editionId, SubscriptionStartType? subscriptionStartType = null)
        {
            CheckTenantRegistrationIsEnabled();

            var model = new TenantRegisterViewModel
            {
                PasswordComplexitySetting = await _passwordComplexitySettingStore.GetSettingsAsync(),
                SubscriptionStartType = subscriptionStartType,
                EditionPaymentType = EditionPaymentType.NewRegistration
            };

            if (editionId.HasValue)
            {
                model.EditionId = editionId.Value;
                model.Edition = await _tenantRegistrationAppService.GetEdition(editionId.Value);
            }

            var editionName = model.Edition.Name;

            if (editionName != "Free" && editionName != "Basic" && editionName != "Pro" && editionName != "Business")
            {
                //return Forbid();
                return RedirectToAction("SelectEdition", "TenantRegistration");
            }

            ViewBag.UseCaptcha = UseCaptchaOnRegistration();

            return View(model);
        }

        [HttpPost]
        [UnitOfWork]
        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public virtual async Task<ActionResult> Register(RegisterTenantInput model)
        {
            try
            {
                if (UseCaptchaOnRegistration())
                {
                    model.CaptchaResponse = HttpContext.Request.Form[RecaptchaValidator.RecaptchaResponseKey];
                }

                var result = await _tenantRegistrationAppService.RegisterTenant(model);

                CurrentUnitOfWork.SetTenantId(result.TenantId);

                var user = await _userManager.GetAdminAsync();

                try
                {
                    await _nlpChatbotAppService.CreateChatbotSampleAsync(result.TenantId, user.Id);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message, ex);
                }

                //Directly login if possible
                if (result.IsTenantActive &&
                    result.IsActive &&
                    !result.IsEmailConfirmationRequired &&
                    !_webUrlService.SupportsTenancyNameInUrl)
                {
                    var loginResult = await GetLoginResultAsync(user.UserName, model.AdminPassword, model.TenancyName);

                    if (loginResult.Result == AbpLoginResultType.Success)
                    {
                        await _signInManager.SignOutAsync();
                        await _signInManager.SignInAsync(loginResult.Identity, false);

                        SetTenantIdCookie(result.TenantId);

                        return Redirect(Url.Action("Index", "Home", new { area = "App" }));
                    }

                    Logger.Warn("New registered user could not be login. This should not be normally. login result: " + loginResult.Result);
                }

                //Show result page
                var resultModel = ObjectMapper.Map<TenantRegisterResultViewModel>(result);

                resultModel.TenantLoginAddress = _webUrlService.SupportsTenancyNameInUrl
                    ? _webUrlService.GetSiteRootAddress(model.TenancyName).EnsureEndsWith('/') + "Account/Login"
                    : "";

                if (model.SubscriptionStartType == SubscriptionStartType.Paid)
                {
                    return RedirectToAction("Buy", "Payment", new
                    {
                        tenantId = result.TenantId,
                        editionId = model.EditionId,
                        subscriptionStartType = (int)model.SubscriptionStartType
                    });
                }

                return View("RegisterResult", resultModel);
            }
            catch (UserFriendlyException ex)
            {
                ViewBag.UseCaptcha = UseCaptchaOnRegistration();
                ViewBag.ErrorMessage = ex.Message;

                var viewModel = new TenantRegisterViewModel
                {
                    PasswordComplexitySetting = await _passwordComplexitySettingStore.GetSettingsAsync(),
                    EditionId = model.EditionId,
                    SubscriptionStartType = model.SubscriptionStartType,
                    EditionPaymentType = EditionPaymentType.NewRegistration
                };

                if (model.EditionId.HasValue)
                {
                    viewModel.Edition = await _tenantRegistrationAppService.GetEdition(model.EditionId.Value);
                    viewModel.EditionId = model.EditionId.Value;
                }

                return View("Register", viewModel);
            }
        }

        private bool IsSelfRegistrationEnabled()
        {
            return SettingManager.GetSettingValueForApplication<bool>(AppSettings.TenantManagement.AllowSelfRegistration);
        }

        private void CheckTenantRegistrationIsEnabled()
        {
            if (!IsSelfRegistrationEnabled())
            {
                throw new UserFriendlyException(L("SelfTenantRegistrationIsDisabledMessage_Detail"));
            }

            if (!_multiTenancyConfig.IsEnabled)
            {
                throw new UserFriendlyException(L("MultiTenancyIsNotEnabled"));
            }
        }

        private bool UseCaptchaOnRegistration()
        {
            return SettingManager.GetSettingValueForApplication<bool>(AppSettings.TenantManagement.UseCaptchaOnRegistration);
        }

        private async Task<AbpLoginResult<Tenant, User>> GetLoginResultAsync(string usernameOrEmailAddress, string password, string tenancyName)
        {
            var loginResult = await _logInManager.LoginAsync(usernameOrEmailAddress, password, tenancyName);

            switch (loginResult.Result)
            {
                case AbpLoginResultType.Success:
                    return loginResult;
                default:
                    throw _abpLoginResultTypeHelper.CreateExceptionForFailedLoginAttempt(loginResult.Result, usernameOrEmailAddress, tenancyName);
            }
        }
    }
}
