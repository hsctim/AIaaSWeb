using System;
using System.Threading.Tasks;
using Abp.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Web.Areas.App.Models.NlpCbModels;
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
    [AbpMvcAuthorize(AppPermissions.Pages_NlpChatbot_NlpCbModels)]
    public class NlpCbModelsController : AIaaSControllerBase
    {
        private readonly INlpCbModelsAppService _nlpCbModelsAppService;
        private readonly INlpChatbotsAppService _nlpChatbotsAppService;
        private readonly NlpCbSession _nlpCbSession;

        public NlpCbModelsController(
            INlpCbModelsAppService nlpCbModelsAppService,
            INlpChatbotsAppService nlpChatbotsAppService,
            NlpCbSession nlpCbSession)
        {
            _nlpCbModelsAppService = nlpCbModelsAppService;
            _nlpChatbotsAppService = nlpChatbotsAppService;
            _nlpCbSession = nlpCbSession;
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<ActionResult> Index(string chatbotId)
        {
            PagedResultDto<GetNlpChatbotForViewDto> result = await _nlpChatbotsAppService.GetAll(new GetAllNlpChatbotsInput());

            List<NlpChatbotDto> chatbotList = await _nlpChatbotsAppService.GetAllForSelectList();

            var model = new NlpCbModelsViewModel
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

        //[AbpMvcAuthorize(AppPermissions.Pages_NlpChatbot_NlpCbModels)]
        //public PartialViewResult CreateOrEditModal(Guid? id)
        //{
        //    GetNlpCbModelForEditOutput getNlpCbModelForEditOutput;

        //    if (id.HasValue)
        //    {
        //        getNlpCbModelForEditOutput = _nlpCbModelsAppService.GetNlpCbModelForEdit(new EntityDto<Guid> { Id = (Guid)id });
        //    }
        //    else
        //    {
        //        getNlpCbModelForEditOutput = new GetNlpCbModelForEditOutput
        //        {
        //            NlpCbModel = new CreateOrEditNlpCbModelDto()
        //        };
        //        getNlpCbModelForEditOutput.NlpCbModel.NlpCbMTrainingStartTime = DateTime.Now;
        //        getNlpCbModelForEditOutput.NlpCbModel.NlpCbMTrainingCompleteTime = DateTime.Now;
        //        getNlpCbModelForEditOutput.NlpCbModel.NlpCbMTrainingCancellationTime = DateTime.Now;
        //    }

        //    string TrainingStatus = L("NlpTrainingStatus_NotTraining");
        //    int code = getNlpCbModelForEditOutput.NlpCbModel.NlpCbMStatus;

        //    if (code >= NlpChatbotConsts.TrainingStatus.Queueing && code < NlpChatbotConsts.TrainingStatus.Training)
        //        TrainingStatus = L("NlpTrainingStatus_Queueing");
        //    else if (code >= NlpChatbotConsts.TrainingStatus.Training && code < NlpChatbotConsts.TrainingStatus.Trained)
        //        TrainingStatus = L("NlpTrainingStatus_Training");
        //    else if (code == NlpChatbotConsts.TrainingStatus.Trained)
        //        TrainingStatus = L("NlpTrainingStatus_Trained");
        //    else if (code == NlpChatbotConsts.TrainingStatus.Cancelled)
        //        TrainingStatus = L("NlpTrainingStatus_Cancelled");
        //    else if (code == NlpChatbotConsts.TrainingStatus.RequireRetraining)
        //        TrainingStatus = L("NlpTrainingStatus_RequireRetraining");
        //    TrainingStatus = L("NlpTrainingStatus_NotTraining");

        //    var viewModel = new CreateOrEditNlpCbModelModalViewModel()
        //    {
        //        NlpCbModel = getNlpCbModelForEditOutput.NlpCbModel,
        //        NlpChatbotName = getNlpCbModelForEditOutput.NlpChatbotName,

        //        NlpCbMTrainingCancellationUser = getNlpCbModelForEditOutput.NlpCbMTrainingCancellationUser,
        //        NlpCbMCreatorUser = getNlpCbModelForEditOutput.NlpCbMCreatorUser,
        //        NlpCbMTraininigStatus = TrainingStatus
        //    };

        //    return PartialView("_CreateOrEditModal", viewModel);
        //}
    }
}