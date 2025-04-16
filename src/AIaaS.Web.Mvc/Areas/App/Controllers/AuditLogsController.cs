using System.Threading.Tasks;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.Auditing;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Auditing;
using AIaaS.Auditing.Dto;
using AIaaS.Authorization;
using AIaaS.Web.Areas.App.Models.AuditLogs;
using AIaaS.Web.Controllers;
using ApiProtectorDotNet;

namespace AIaaS.Web.Areas.App.Controllers
{
    [Area("App")]
    [DisableAuditing]
    [AbpMvcAuthorize(AppPermissions.Pages_Administration_AuditLogs)]
    public class AuditLogsController : AIaaSControllerBase
    {
        private readonly IAuditLogAppService _auditLogAppService;

        public AuditLogsController(IAuditLogAppService auditLogAppService)
        {
            _auditLogAppService = auditLogAppService;
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public ActionResult Index()
        {
            return View();
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<PartialViewResult> EntityChangeDetailModal(EntityChangeListDto entityChangeListDto)
        {
            var output = await _auditLogAppService.GetEntityPropertyChanges(entityChangeListDto.Id);

            var viewModel = new EntityChangeDetailModalViewModel(output, entityChangeListDto);

            return PartialView("_EntityChangeDetailModal", viewModel);
        }
    }
}