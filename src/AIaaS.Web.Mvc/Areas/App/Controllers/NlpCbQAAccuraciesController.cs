using System;
using System.Threading.Tasks;
using Abp.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Web.Areas.App.Models.NlpCbQAAccuracies;
using AIaaS.Web.Controllers;
using AIaaS.Authorization;
using AIaaS.Nlp;
using AIaaS.Nlp.Dtos;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using ApiProtectorDotNet;

namespace AIaaS.Web.Areas.App.Controllers
{
    [Area("App")]
    [AbpMvcAuthorize(AppPermissions.Pages_NlpChatbot_NlpCbQAAccuracies)]
    public class NlpCbQAAccuraciesController : AIaaSControllerBase
    {
        private readonly INlpCbQAAccuraciesAppService _nlpCbQAAccuraciesAppService;
        private readonly INlpChatbotsAppService _nlpChatbotsAppService;
        private readonly NlpCbSession _nlpCbSession;

        public NlpCbQAAccuraciesController(
            INlpCbQAAccuraciesAppService nlpCbQAAccuraciesAppService,
                    INlpChatbotsAppService nlpChatbotsAppService,
                    NlpCbSession nlpCbSession)
        {
            _nlpCbQAAccuraciesAppService = nlpCbQAAccuraciesAppService;
            _nlpChatbotsAppService = nlpChatbotsAppService;
            _nlpCbSession = nlpCbSession;
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<ActionResult> Index(string chatbotId)
        {
            List<NlpChatbotDto> chatbotList = await _nlpChatbotsAppService.GetAllForSelectList();

            var model = new NlpCbQAAccuraciesViewModel
            {
                FilterText = ""
            };

            if (chatbotId.IsNullOrEmpty())
                chatbotId = (string)_nlpCbSession["ChatbotId"];

            var targetList = (from o in chatbotList
                              select new SelectListItem(o.Name, o.Id.ToString(), o.Id.ToString() == chatbotId)).ToList();
            model.ChatbotSelectList = new SelectList(targetList, "Value", "Text", chatbotId);

            return View(model);

        }

        //[AbpMvcAuthorize(AppPermissions.Pages_NlpCbQAAccuracies_Create)]
        //public async Task<PartialViewResult> CreateOrEditModal(Guid? id)
        //{
        //    GetNlpCbQAAccuracyForEditOutput getNlpCbQAAccuracyForEditOutput;

        //    if (id.HasValue)
        //    {
        //        //getNlpCbQAAccuracyForEditOutput = await _nlpCbQAAccuraciesAppService.GetNlpCbQAAccuracyForEdit(new EntityDto<Guid> { Id = (Guid)id });
        //    }
        //    else
        //    {
        //        getNlpCbQAAccuracyForEditOutput = new GetNlpCbQAAccuracyForEditOutput
        //        {
        //            NlpCbQAAccuracy = new CreateOrEditNlpCbQAAccuracyDto()
        //        };
        //    }

        //    var viewModel = new CreateOrEditNlpCbQAAccuracyModalViewModel()
        //    {
        //        ////NlpCbQAAccuracy = getNlpCbQAAccuracyForEditOutput.NlpCbQAAccuracy,
        //        //NlpChatbotName = getNlpCbQAAccuracyForEditOutput.NlpChatbotName,
        //        //NlpCbQAAccuracyNlpChatbotList = await _nlpCbQAAccuraciesAppService.GetAllNlpChatbotForTableDropdown(),
        //    };

        //    return PartialView("_CreateOrEditModal", viewModel);
        //}

    }
}