using System;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.DynamicEntityProperties;
using Abp.Extensions;
using Abp.UI;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Authorization;
using AIaaS.Web.Areas.App.Models.DynamicEntityPropertyValues;
using AIaaS.Web.Controllers;
using ApiProtectorDotNet;

namespace AIaaS.Web.Areas.App.Controllers
{
    [Area("App")]
    [AbpMvcAuthorize(AppPermissions.Pages_Administration_DynamicEntityPropertyValue)]
    public class DynamicEntityPropertyValuesController : AIaaSControllerBase
    {
        private readonly IDynamicEntityPropertyDefinitionManager _dynamicEntityPropertyDefinitionManager;

        public DynamicEntityPropertyValuesController(IDynamicEntityPropertyDefinitionManager dynamicEntityPropertyDefinitionManager)
        {
            _dynamicEntityPropertyDefinitionManager = dynamicEntityPropertyDefinitionManager;
        }

        [AbpMvcAuthorize(AppPermissions.Pages_Administration_DynamicEntityPropertyValue_Edit)]
        [AbpMvcAuthorize(AppPermissions.Pages_Administration_DynamicEntityPropertyValue_Create)]
        [HttpGet("/App/DynamicEntityPropertyValue/ManageAll/{entityFullName}/{entityId}")]


        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public IActionResult ManageAll(string entityFullName, string entityId)
        {
            if (entityFullName.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(entityFullName));
            }

            if (entityId.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(entityId));
            }

            if (!_dynamicEntityPropertyDefinitionManager.ContainsEntity(entityFullName))
            {
                throw new UserFriendlyException(L("UnknownEntityType", entityFullName));
            }

            return View(new DynamicEntityPropertyValueManageAllViewModel
            {
                EntityFullName = entityFullName,
                EntityId = entityId
            });
        }

        [AbpMvcAuthorize(AppPermissions.Pages_Administration_DynamicEntityPropertyValue_Edit)]
        [AbpMvcAuthorize(AppPermissions.Pages_Administration_DynamicEntityPropertyValue_Create)]


        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public IActionResult ManageDynamicEntityPropertyValuesModal(string entityFullName, string rowId)
        {
            if (entityFullName.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(entityFullName));
            }

            if (rowId.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(rowId));
            }

            if (!_dynamicEntityPropertyDefinitionManager.ContainsEntity(entityFullName))
            {
                throw new UserFriendlyException(L("UnknownEntityType", entityFullName));
            }

            return PartialView("_ManageDynamicEntityPropertyValuesModal", new DynamicEntityPropertyValueManageAllViewModel
            {
                EntityFullName = entityFullName,
                EntityId = rowId
            });
        }
    }
}