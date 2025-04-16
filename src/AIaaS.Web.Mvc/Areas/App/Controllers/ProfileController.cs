using System.Threading.Tasks;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.Configuration;
using Abp.MultiTenancy;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Authorization.Users.Profile;
using AIaaS.Configuration;
using AIaaS.Timing;
using AIaaS.Timing.Dto;
using AIaaS.Web.Areas.App.Models.Profile;
using AIaaS.Web.Controllers;
using ApiProtectorDotNet;

namespace AIaaS.Web.Areas.App.Controllers
{
    [Area("App")]
    [AbpMvcAuthorize]
    public class ProfileController : AIaaSControllerBase
    {
        private readonly IProfileAppService _profileAppService;
        private readonly ITimingAppService _timingAppService;
        private readonly ITenantCache _tenantCache;

        public ProfileController(
            IProfileAppService profileAppService,
            ITimingAppService timingAppService,
            ITenantCache tenantCache)
        {
            _profileAppService = profileAppService;
            _timingAppService = timingAppService;
            _tenantCache = tenantCache;
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<PartialViewResult> MySettingsModal()
        {
            var output = await _profileAppService.GetCurrentUserProfileForEdit();
            var viewModel = ObjectMapper.Map<MySettingsViewModel>(output);
            viewModel.TimezoneItems = await _timingAppService.GetTimezoneComboboxItems(new GetTimezoneComboboxItemsInput
            {
                DefaultTimezoneScope = SettingScopes.User,
                SelectedTimezoneId = output.Timezone
            });
            viewModel.SmsVerificationEnabled = await SettingManager.GetSettingValueAsync<bool>(AppSettings.UserManagement.SmsVerificationEnabled);

            return PartialView("_MySettingsModal", viewModel);
        }

        public async Task<PartialViewResult> TwoFactorAuthenticationModal()
        {
            var result = await _profileAppService.GenerateGoogleAuthenticatorKey();
            
            return PartialView("_TwoFactorAuthentication", result);
        }

        public PartialViewResult ViewRecoveryCodesModal()
        {
            return PartialView("_ViewRecoveryCodesModal");
        }
        
        public PartialViewResult RemoveAuthenticatorModal()
        {
            return PartialView("_RemoveAuthenticatorModal");
        }

        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public PartialViewResult ChangePictureModal(long? userId)
        {
            ViewBag.UserId = userId;
            return PartialView("_ChangePictureModal");
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public PartialViewResult ChangePasswordModal()
        {
            return PartialView("_ChangePasswordModal");
        }






        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public PartialViewResult SmsVerificationModal()
        {
            return PartialView("_SmsVerificationModal");
        }


        public PartialViewResult LinkedAccountsModal()
        {
            return PartialView("_LinkedAccountsModal");
        }


        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public PartialViewResult LinkAccountModal()
        {
            ViewBag.TenancyName = GetTenancyNameOrNull();
            return PartialView("_LinkAccountModal");
        }

        public PartialViewResult UserDelegationsModal()
        {
            return PartialView("_UserDelegationsModal");
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public PartialViewResult CreateNewUserDelegationModal()
        {
            return PartialView("_CreateNewUserDelegationModal");
        }

        private string GetTenancyNameOrNull()
        {
            if (!AbpSession.TenantId.HasValue)
            {
                return null;
            }

            return _tenantCache.GetOrNull(AbpSession.TenantId.Value)?.TenancyName;
        }
    }
}