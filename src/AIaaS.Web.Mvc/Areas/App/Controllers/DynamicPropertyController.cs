﻿using System.Linq;
using System.Threading.Tasks;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.DynamicEntityProperties;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Authorization;
using AIaaS.DynamicEntityProperties;
using AIaaS.DynamicEntityProperties.Dto;
using AIaaS.Web.Areas.App.Models.DynamicProperty;
using AIaaS.Web.Controllers;
using ApiProtectorDotNet;

namespace AIaaS.Web.Areas.App.Controllers
{
    [Area("App")]
    public class DynamicPropertyController : AIaaSControllerBase
    {
        private readonly IDynamicPropertyAppService _dynamicPropertyAppService;
        private readonly IDynamicEntityPropertyDefinitionManager _dynamicEntityPropertyDefinitionManager;
        private readonly IDynamicEntityPropertyAppService _dynamicEntityPropertyAppService;

        public DynamicPropertyController(
            IDynamicPropertyAppService dynamicPropertyAppService,
            IDynamicEntityPropertyDefinitionManager dynamicEntityPropertyDefinitionManager,
            IDynamicEntityPropertyAppService dynamicEntityPropertyAppService)
        {
            _dynamicPropertyAppService = dynamicPropertyAppService;
            _dynamicEntityPropertyDefinitionManager = dynamicEntityPropertyDefinitionManager;
            _dynamicEntityPropertyAppService = dynamicEntityPropertyAppService;
        }



        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public IActionResult Index()
        {
            return View();
        }

        [AbpMvcAuthorize(AppPermissions.Pages_Administration_DynamicProperties_Edit, AppPermissions.Pages_Administration_DynamicProperties_Create)]


        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<IActionResult> CreateOrEditModal(int? id)
        {
            var model = new CreateOrEditDynamicPropertyViewModel
            {
                AllowedInputTypes = _dynamicEntityPropertyDefinitionManager.GetAllAllowedInputTypeNames()
            };

            if (id.HasValue)
            {
                model.DynamicPropertyDto = await _dynamicPropertyAppService.Get(id.Value);
            }
            else
            {
                model.DynamicPropertyDto = new DynamicPropertyDto();
            }

            return PartialView("_CreateOrEditModal", model);
        }

        [AbpMvcAuthorize(AppPermissions.Pages_Administration_DynamicPropertyValue_Edit, AppPermissions.Pages_Administration_DynamicPropertyValue_Create)]


        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<IActionResult> ManageValuesModal(int id)
        {
            var dynamicProperty = await _dynamicPropertyAppService.Get(id);
            return PartialView("_ManageValuesModal", dynamicProperty);
        }

        [AbpMvcAuthorize(AppPermissions.Pages_Administration_DynamicPropertyValue_Edit, AppPermissions.Pages_Administration_DynamicPropertyValue_Create)]


        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<IActionResult> SelectAnEntityModal()
        {
            var allEntities = _dynamicEntityPropertyDefinitionManager.GetAllEntities();

            var entitiesAlreadyHasProperty =
                (await _dynamicEntityPropertyAppService.GetAllEntitiesHasDynamicProperty())
                .Items
                .Select(x => x.EntityFullName)
                .ToList();

            allEntities = allEntities.Except(entitiesAlreadyHasProperty).ToList();
            return PartialView("_SelectAnEntityModal", allEntities);
        }
    }
}