using System;
using System.Threading.Tasks;
using Abp.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Web.Areas.App.Models.NlpCbMessages;
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
using Microsoft.EntityFrameworkCore;

namespace AIaaS.Web.Areas.App.Controllers
{
    [Area("App")]
    [AbpMvcAuthorize(AppPermissions.Pages_NlpChatbot_NlpCbMessages)]
    public class NlpCbMessagesController : AIaaSControllerBase
    {
        private readonly INlpCbMessagesAppService _nlpCbMessagesAppService;
        //private readonly INlpChatbotsAppService _nlpChatbotsAppService;
        private readonly NlpCbSession _nlpCbSession;

        public NlpCbMessagesController(
            INlpCbMessagesAppService nlpCbMessagesAppService,
            //INlpChatbotsAppService nlpChatbotsAppService,
            NlpCbSession nlpCbSession)
        {
            _nlpCbMessagesAppService = nlpCbMessagesAppService;
            //_nlpChatbotsAppService = nlpChatbotsAppService;
            _nlpCbSession = nlpCbSession;
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<ActionResult> Index()
        {
            var model = new NlpCbMessagesViewModel
            {
                FilterText = ""
            };


            string chatbotId = (string)_nlpCbSession["ChatbotId"];

            List<NlpChatbotDto> chatbotList = await _nlpCbMessagesAppService.GetAllForSelectList();

            var targetList = (from o in chatbotList
                              select new SelectListItem(o.Name, o.Id.ToString(), o.Id.ToString() == chatbotId)).ToList();

            if (targetList.Count > 0)
                targetList[0].Selected = true;

            model.ChatbotSelectList = new SelectList(targetList, "Value", "Text", chatbotId);

            return View(model);
        }



    }
}