using System;
using System.Threading.Tasks;
using Abp.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Web.Areas.App.Models.NlpWorkflows;
using AIaaS.Web.Controllers;
using AIaaS.Authorization;
using AIaaS.Nlp;
using AIaaS.Nlp.Dtos;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;

namespace AIaaS.Web.Areas.App.Controllers
{
    [Area("App")]
    [AbpMvcAuthorize(AppPermissions.Pages_NlpChatbot_NlpWorkflows)]
    public class NlpWorkflowsController : AIaaSControllerBase
    {
        private readonly INlpWorkflowsAppService _nlpWorkflowsAppService;
        private readonly INlpChatbotsAppService _nlpChatbotsAppService;
        private readonly NlpChatbotFunction _nlpChatbotFunction;
        private readonly NlpCbSession _nlpCbSession;

        public NlpWorkflowsController(INlpWorkflowsAppService nlpWorkflowsAppService,
            INlpChatbotsAppService nlpChatbotsAppService,
            NlpChatbotFunction nlpChatbotFunction,
            NlpCbSession nlpCbSession)
        {
            _nlpWorkflowsAppService = nlpWorkflowsAppService;
            _nlpChatbotsAppService = nlpChatbotsAppService;
            _nlpChatbotFunction = nlpChatbotFunction;
            _nlpCbSession = nlpCbSession;
        }

        public async Task<ActionResult> Index(string chatbotId)
        {
            if (chatbotId.IsNullOrEmpty())
                chatbotId = (string)_nlpCbSession["ChatbotId"];
            else
                _nlpCbSession["ChatbotId"] = chatbotId?.Trim();

            chatbotId = chatbotId?.Trim();

            PagedResultDto<GetNlpChatbotForViewDto> result = await _nlpChatbotsAppService.GetAll(new GetAllNlpChatbotsInput());

            List<NlpChatbotDto> chatbotList = await _nlpChatbotsAppService.GetAllForSelectList();

            var targetList = (from o in chatbotList
                              select new SelectListItem(o.Name, o.Id.ToString(), string.Compare(o.Id.ToString(), chatbotId, false) == 0)).ToList();


            targetList.Insert(0, new SelectListItem()
            {
                Text = "-",
                Value = "",
                Selected = targetList.Count(e => e.Selected) == 0
            });

            var model = new NlpWorkflowsViewModel
            {
                ChatbotSelectList = new SelectList(targetList, "Value", "Text", chatbotId),
            };

            return View(model);
        }

        protected async Task<CreateOrEditNlpWorkflowModalViewModel> GetNlpWorkflowModel(Guid? chatbotId, Guid? id)
        {
            GetNlpWorkflowForEditOutput getNlpWorkflowForEditOutput;

            if (id.HasValue)
            {
                getNlpWorkflowForEditOutput = await _nlpWorkflowsAppService.GetNlpWorkflowForEdit(new EntityDto<Guid> { Id = (Guid)id });
            }
            else
            {
                getNlpWorkflowForEditOutput = new GetNlpWorkflowForEditOutput
                {
                    NlpWorkflow = new CreateOrEditNlpWorkflowDto(),
                };
            }

            NlpChatbotDto nlpChatbotDto = null;
            if (chatbotId.HasValue)
                nlpChatbotDto = _nlpChatbotFunction.GetChatbotDto(chatbotId.Value);

            var viewModel = new CreateOrEditNlpWorkflowModalViewModel()
            {
                NlpWorkflow = getNlpWorkflowForEditOutput.NlpWorkflow,
                NlpWorkflowNlpChatbotList = await _nlpWorkflowsAppService.GetAllNlpChatbotForTableDropdown(),
                NlpChatbot = nlpChatbotDto
            };

            return viewModel;
        }

        [AbpMvcAuthorize(AppPermissions.Pages_NlpChatbot_NlpWorkflows_Create, AppPermissions.Pages_NlpChatbot_NlpWorkflows_Edit)]
        public async Task<PartialViewResult> CreateOrEditModal(Guid? chatbotId, Guid? id)
        {
            var viewModel = await GetNlpWorkflowModel(chatbotId, id);
            viewModel.IsViewMode = false;

            return PartialView("_CreateOrEditModal", viewModel);
        }

        public async Task<PartialViewResult> ViewNlpWorkflowModal(Guid id)
        {
            var viewModel = await GetNlpWorkflowModel(null, id);
            viewModel.IsViewMode = true;

            return PartialView("_CreateOrEditModal", viewModel);

        }

    }
}
