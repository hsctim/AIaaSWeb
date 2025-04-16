using System;
using System.Threading.Tasks;
using Abp.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Web.Areas.App.Models.NlpTokens;
using AIaaS.Web.Controllers;
using AIaaS.Authorization;
using AIaaS.Nlp;
using AIaaS.Nlp.Dtos;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using ApiProtectorDotNet;

namespace AIaaS.Web.Areas.App.Controllers
{
    [Area("App")]
    [AbpMvcAuthorize(AppPermissions.Pages_NlpChatbot_NlpTokens)]
    public class NlpTokensController : AIaaSControllerBase
    {
        private readonly INlpTokensAppService _nlpTokensAppService;

        public NlpTokensController(INlpTokensAppService nlpTokensAppService)
        {
            _nlpTokensAppService = nlpTokensAppService;
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public ActionResult Index()
        {
            var model = new NlpTokensViewModel
            {
                FilterText = ""
            };

            return View(model);
        }


        [AbpMvcAuthorize(AppPermissions.Pages_NlpChatbot_NlpTokens_Create, AppPermissions.Pages_NlpChatbot_NlpTokens_Edit)]

        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public PartialViewResult CreateOrEditModal(Guid? id)
        {
            GetNlpTokenForEditOutput getNlpTokenForEditOutput;

            if (id.HasValue)
            {
                getNlpTokenForEditOutput = _nlpTokensAppService.GetNlpTokenForEdit(new EntityDto<Guid> { Id = (Guid)id });
            }
            else
            {
                getNlpTokenForEditOutput = new GetNlpTokenForEditOutput
                {
                    NlpToken = new CreateOrEditNlpTokenDto()
                };
            }

            var viewModel = new CreateOrEditNlpTokenModalViewModel()
            {
                NlpToken = getNlpTokenForEditOutput.NlpToken,
            };

            return PartialView("_CreateOrEditModal", viewModel);
        }




    }
}