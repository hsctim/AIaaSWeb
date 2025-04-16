using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Authorization;
using AIaaS.Authorization.Permissions;
using AIaaS.Authorization.Permissions.Dto;
using AIaaS.Authorization.Roles;
using AIaaS.Web.Areas.App.Models.Roles;
using AIaaS.Web.Controllers;
using ApiProtectorDotNet;

namespace AIaaS.Web.Areas.App.Controllers
{
    [Area("App")]
    [AbpMvcAuthorize(AppPermissions.Pages_Administration_Roles)]
    public class RolesController : AIaaSControllerBase
    {
        private readonly IRoleAppService _roleAppService;
        private readonly IPermissionAppService _permissionAppService;

        public RolesController(
            IRoleAppService roleAppService,
            IPermissionAppService permissionAppService)
        {
            _roleAppService = roleAppService;
            _permissionAppService = permissionAppService;
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public ActionResult Index()
        {
            var permissions = _permissionAppService.GetAllPermissions().Items.ToList();

            var model = new RoleListViewModel
            {
                Permissions = ObjectMapper.Map<List<FlatPermissionDto>>(permissions).OrderBy(p => p.DisplayName).ToList(),
                GrantedPermissionNames = new List<string>()
            };

            return View(model);
        }

        [AbpMvcAuthorize(AppPermissions.Pages_Administration_Roles_Create, AppPermissions.Pages_Administration_Roles_Edit)]

        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<PartialViewResult> CreateOrEditModal(int? id)
        {
            var output = await _roleAppService.GetRoleForEdit(new NullableIdDto { Id = id });

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

            var viewModel = ObjectMapper.Map<CreateOrEditRoleModalViewModel>(output);

            return PartialView("_CreateOrEditModal", viewModel);
        }
    }
}