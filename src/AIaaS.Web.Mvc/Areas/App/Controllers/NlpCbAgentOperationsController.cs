using System;
using System.Threading.Tasks;
using Abp.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Web.Areas.App.Models.NlpCbAgentOperations;
using AIaaS.Web.Controllers;
using AIaaS.Authorization;
using AIaaS.Nlp;
using AIaaS.Nlp.Dtos;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using ApiProtectorDotNet;

namespace AIaaS.Web.Areas.App.Controllers
{
    [Area("App")]
    [AbpMvcAuthorize(AppPermissions.Pages_NlpChatbot_NlpCbAgentOperations)]
    public class NlpCbAgentOperationsController : AIaaSControllerBase
    {
        private readonly INlpCbAgentOperationsAppService _nlpCbAgentOperationsAppService;
        private readonly INlpChatbotsAppService _nlpChatbotsAppService;
        private readonly NlpCbSession _nlpCbSession;

        public NlpCbAgentOperationsController(
            INlpCbAgentOperationsAppService nlpCbAgentOperationsAppService,
                        INlpChatbotsAppService nlpChatbotsAppService,
            NlpCbSession nlpCbSession)
        {
            _nlpCbAgentOperationsAppService = nlpCbAgentOperationsAppService;
            _nlpChatbotsAppService = nlpChatbotsAppService;
            _nlpCbSession = nlpCbSession;
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<ActionResult> Index()
        {
            var model = new NlpCbAgentOperationsViewModel();

            string chatbotId = (string)_nlpCbSession["ChatbotId"];

            List<NlpChatbotDto> chatbotList = await _nlpChatbotsAppService.GetAllForSelectList();

            var chatbotSelectList = (from o in chatbotList
                                     where o.Disabled == false
                                     select new SelectListItem(
                                         @L("NlpCbAgentChatbot{0}", o.Name), o.Id.ToString(), o.Id.ToString() == chatbotId)).ToList();

            chatbotSelectList.Insert(0, new SelectListItem(L("NlpCbAgentAllChatbots"), NlpCbAgentOperationsConst.TenantScope, true));

            model.ChatbotSelectList = new SelectList(chatbotSelectList, "Value", "Text", chatbotId);

            return View(model);
        }
    }
}