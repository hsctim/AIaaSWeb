using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.AspNetCore.Mvc.Authorization;
using ApiProtectorDotNet;
using Abp.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using AIaaS.Authorization;
using AIaaS.Authorization.Permissions;
using AIaaS.Authorization.Permissions.Dto;
using AIaaS.Authorization.Roles;
using AIaaS.Authorization.Roles.Dto;
using AIaaS.Authorization.Users;
using AIaaS.Authorization.Users.Dto;
using AIaaS.Security;
using AIaaS.Web.Areas.App.Models.Roles;
using AIaaS.Web.Areas.App.Models.Users;
using AIaaS.Web.Controllers;

namespace AIaaS.Web.Areas.App.Controllers
{
    [Area("App")]
    [AbpMvcAuthorize]
    public class UsersController : AIaaSControllerBase
    {
        private readonly IUserAppService _userAppService;
        private readonly UserManager _userManager;
        private readonly IUserLoginAppService _userLoginAppService;
        private readonly IRoleAppService _roleAppService;
        private readonly IPermissionAppService _permissionAppService;
        private readonly IPasswordComplexitySettingStore _passwordComplexitySettingStore;
        private readonly IOptions<UserOptions> _userOptions;

        public UsersController(
            IUserAppService userAppService,
            UserManager userManager,
            IUserLoginAppService userLoginAppService,
            IRoleAppService roleAppService,
            IPermissionAppService permissionAppService,
            IPasswordComplexitySettingStore passwordComplexitySettingStore,
            IOptions<UserOptions> userOptions)
        {
            _userAppService = userAppService;
            _userManager = userManager;
            _userLoginAppService = userLoginAppService;
            _roleAppService = roleAppService;
            _permissionAppService = permissionAppService;
            _passwordComplexitySettingStore = passwordComplexitySettingStore;
            _userOptions = userOptions;
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        [AbpMvcAuthorize(AppPermissions.Pages_Administration_Users)]
        public async Task<ActionResult> Index()
        {
            var roles = new List<ComboboxItemDto>();

            if (await IsGrantedAsync(AppPermissions.Pages_Administration_Roles))
            {
                var getRolesOutput = await _roleAppService.GetRoles(new GetRolesInput());
                roles = getRolesOutput.Items.Select(r => new ComboboxItemDto(r.Id.ToString(), r.DisplayName)).ToList();
            }

            roles.Insert(0, new ComboboxItemDto("", L("FilterByRole")));

            var permissions = _permissionAppService.GetAllPermissions().Items.ToList();

            var licenseUsage = await _userAppService.GetUserLicenseUsage();

            var model = new UsersViewModel
            {
                FilterText = Request.Query["filterText"],
                Roles = roles,
                Permissions = ObjectMapper.Map<List<FlatPermissionDto>>(permissions).OrderBy(p => p.DisplayName)
                    .ToList(),
                OnlyLockedUsers = false,
                WarningMessage = licenseUsage.UsageCount > licenseUsage.LicenseCount ? L("ExceedUserCount") : null,
                Usage = licenseUsage
            };

            return View(model);
        }


        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        [AbpMvcAuthorize(
            AppPermissions.Pages_Administration_Users,
            AppPermissions.Pages_Administration_Users_Create,
            AppPermissions.Pages_Administration_Users_Edit
        )]
        public async Task<PartialViewResult> CreateOrEditModal(long? id)
        {
            var output = await _userAppService.GetUserForEdit(new NullableIdDto<long> { Id = id });
            var viewModel = ObjectMapper.Map<CreateOrEditUserModalViewModel>(output);
            viewModel.PasswordComplexitySetting = await _passwordComplexitySettingStore.GetSettingsAsync();
            viewModel.AllowedUserNameCharacters = _userOptions.Value.AllowedUserNameCharacters;

            return PartialView("_CreateOrEditModal", viewModel);
        }

        [AbpMvcAuthorize(
            AppPermissions.Pages_Administration_Users,
            AppPermissions.Pages_Administration_Users_ChangePermissions
        )]

        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<PartialViewResult> PermissionsModal(long id)
        {
            var output = await _userAppService.GetUserPermissionsForEdit(new EntityDto<long>(id));

            string[] disablePermissions = {
                AppPermissions.Pages_DemoUiComponents,
                //AppPermissions.Pages_Administration_Languages,
                AppPermissions.Pages_Administration_OrganizationUnits,
                AppPermissions.Pages_Administration_WebhookSubscription,
                AppPermissions.Pages_Administration_DynamicProperties,
                AppPermissions.Pages_Administration_DynamicPropertyValue,
                AppPermissions.Pages_Administration_DynamicEntityProperties,
                AppPermissions.Pages_Administration_DynamicEntityPropertyValue,
                AppPermissions.Pages_Administration_Tenant_Settings,
                AppPermissions.Pages_Administration_MassNotification,

            //AppPermissions.Pages_Administration_Tenant_SubscriptionManagement
            };

            if (AbpSession.TenantId.HasValue && AbpSession.TenantId.Value != 1)
            {
                output.GrantedPermissionNames = output.GrantedPermissionNames.Where(p => disablePermissions.All(p2 => !p.Contains(p2))).ToList();

                output.Permissions = output.Permissions.Where(p => disablePermissions.All(p2 => !p.Name.Contains(p2))).ToList();
            }

            var viewModel = ObjectMapper.Map<UserPermissionsEditViewModel>(output);
            viewModel.User = await _userManager.GetUserByIdAsync(id);
            ;
            return PartialView("_PermissionsModal", viewModel);
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public ActionResult LoginAttempts()
        {
            var loginResultTypes = Enum.GetNames(typeof(AbpLoginResultType))
                .Select(e => new ComboboxItemDto(e, L("AbpLoginResultType_" + e)))
                .ToList();

            loginResultTypes.Insert(0, new ComboboxItemDto("", L("All")));

            return View("LoginAttempts", new UserLoginAttemptsViewModel()
            {
                LoginAttemptResults = loginResultTypes
            });
        }
    }
}
