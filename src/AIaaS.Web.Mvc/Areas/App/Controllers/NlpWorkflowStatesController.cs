using System;
using System.Threading.Tasks;
using Abp.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Web.Areas.App.Models.NlpWorkflows;
using AIaaS.Web.Areas.App.Models.NlpWorkflowStates;
using AIaaS.Web.Controllers;
using AIaaS.Authorization;
using AIaaS.Nlp;
using AIaaS.Nlp.Dtos;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Abp.UI;
using Newtonsoft.Json;

namespace AIaaS.Web.Areas.App.Controllers
{
    [Area("App")]
    [AbpMvcAuthorize(AppPermissions.Pages_NlpChatbot_NlpWorkflows)]
    public class NlpWorkflowStatesController : AIaaSControllerBase
    {
        private readonly INlpWorkflowStatesAppService _nlpWorkflowStatesAppService;
        private readonly INlpWorkflowsAppService _nlpWorkflowsAppService;

        public NlpWorkflowStatesController(
            INlpWorkflowStatesAppService nlpWorkflowStatesAppService,
            INlpWorkflowsAppService nlpWorkflowsAppService)
        {
            _nlpWorkflowStatesAppService = nlpWorkflowStatesAppService;
            _nlpWorkflowsAppService = nlpWorkflowsAppService;
        }

        [Route("App/NlpWorkflows/NlpWorkflowStates")]
        [Route("App/NlpWorkflows/NlpWorkflowStates/{workflowId}")]
        public async Task<ActionResult> Index(string workflowId)
        {
            Guid workflowUUID = Guid.Empty;
            NlpWorkflowChatbotDto workflowDto = null;

            if (string.IsNullOrEmpty(workflowId))
                workflowId=Guid.Empty.ToString();

            if (Guid.TryParse(workflowId, out workflowUUID))
            {
                workflowDto = await _nlpWorkflowsAppService.GetNlpWorkflowDto(workflowUUID);
            }

            //if workflowDto is null, redirect to parent folder
            if (workflowDto == null)
                  return RedirectToAction("Index", "NlpWorkflows");

            var model = new NlpWorkflowStatesViewModel
            {
                NlpWorkflowChatbot = workflowDto
            };

            return View(model);
        }


        protected async Task<CreateOrEditNlpWorkflowStateModalViewModel> GetNlpWorkflowStateModel(Guid? id, Guid? workflowId)
        {
            GetNlpWorkflowStateForEditOutput getNlpWorkflowStateForEditOutput;

            if (id.HasValue)
            {
                getNlpWorkflowStateForEditOutput = await _nlpWorkflowStatesAppService.GetNlpWorkflowStateForEdit(new EntityDto<Guid> { Id = (Guid)id });
            }
            else
            {
                if (!workflowId.HasValue)
                    throw new UserFriendlyException(L("ErrorWorkFlowId"));

                var workflowDto = await _nlpWorkflowsAppService.GetNlpWorkflowDto(workflowId.Value);

                getNlpWorkflowStateForEditOutput = new GetNlpWorkflowStateForEditOutput
                {
                    NlpWorkflowState = new CreateOrEditNlpWorkflowStateDto()
                    {
                        NlpWorkflowId = workflowDto.Id,
                        ResponseNonWorkflowAnswer = true,
                        DontResponseNonWorkflowErrorAnswer = true,
                    },
                    NlpWorkflowName = workflowDto.Name,
                    NlpChatbotName = workflowDto.ChatbotName,
                };
            }

            var viewModel = new CreateOrEditNlpWorkflowStateModalViewModel()
            {
                NlpWorkflowState = getNlpWorkflowStateForEditOutput.NlpWorkflowState,
                NlpWorkflowName = getNlpWorkflowStateForEditOutput.NlpWorkflowName,
                NlpChatbotName = getNlpWorkflowStateForEditOutput.NlpChatbotName,
                WorkflowStateList = await _nlpWorkflowStatesAppService.GetAllNlpWorkflowStateForTableDropdown(),
                IsViewMode = false,
            };

            try
            {
                viewModel.FalsePrediction1_Op = JsonConvert.DeserializeObject<NlpWfsFalsePredictionOpDto>(viewModel.NlpWorkflowState.OutgoingFalseOp);
            }
            catch (Exception)
            {
                viewModel.FalsePrediction1_Op = new NlpWfsFalsePredictionOpDto
                {
                    ResponseMsg = viewModel.NlpWorkflowState.OutgoingFalseOp.IsNullOrEmpty() ? "" : viewModel.NlpWorkflowState.OutgoingFalseOp,
                    NextStatus = NlpWorkflowStateConsts.WfsNull
                };
            }

            try
            {
                viewModel.FalsePrediction3_Op = JsonConvert.DeserializeObject<NlpWfsFalsePredictionOpDto>(viewModel.NlpWorkflowState.Outgoing3FalseOp);
            }
            catch (Exception)
            {
                viewModel.FalsePrediction3_Op = new NlpWfsFalsePredictionOpDto
                {
                    ResponseMsg = viewModel.NlpWorkflowState.Outgoing3FalseOp.IsNullOrEmpty() ? "" : viewModel.NlpWorkflowState.Outgoing3FalseOp,
                    NextStatus = NlpWorkflowStateConsts.WfsNull
                };
            }

            return viewModel;

            //return PartialView("_CreateOrEditModal", viewModel);
        }

        [AbpMvcAuthorize(AppPermissions.Pages_NlpChatbot_NlpWorkflows_Create, AppPermissions.Pages_NlpChatbot_NlpWorkflows_Edit)]
        public async Task<PartialViewResult> CreateOrEditModal(Guid? id, Guid? workflowId)
        {
            var viewModel = await GetNlpWorkflowStateModel(id, workflowId);
            viewModel.IsViewMode = false;

            return PartialView("_CreateOrEditModal", viewModel);
        }


        public async Task<PartialViewResult> ViewNlpWorkflowStateModal(Guid id)
        {
            var viewModel = await GetNlpWorkflowStateModel(id, null);
            viewModel.IsViewMode = true;

            return PartialView("_CreateOrEditModal", viewModel);
            //var getNlpWorkflowStateForViewDto = await _nlpWorkflowStatesAppService.GetNlpWorkflowStateForView(id);

            //var model = new NlpWorkflowStateViewModel()
            //{
            //    NlpWorkflowState = getNlpWorkflowStateForViewDto.NlpWorkflowState,
            //    NlpWorkflowName = getNlpWorkflowStateForViewDto.NlpWorkflowName,
            //    NlpChatbotName = getNlpWorkflowStateForViewDto.NlpChatbotName
            //};

            //return PartialView("_ViewNlpWorkflowStateModal", model);
        }
    }
}
