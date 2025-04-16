using System;
using System.Threading.Tasks;
using Abp.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Web.Areas.App.Models.NlpQAs;
using AIaaS.Web.Controllers;
using AIaaS.Authorization;
using AIaaS.Nlp;
using AIaaS.Nlp.Dtos;
using Abp.Application.Services.Dto;
using Abp.Extensions;

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
//using AIaaS.Nlp.Cache;
using Abp.UI;
using Abp.IO.Extensions;
using AIaaS.Storage;
using Abp.Web.Models;
using Abp.BackgroundJobs;
using AIaaS.Nlp.Importing;
using Abp.Domain.Repositories;
using ApiProtectorDotNet;

namespace AIaaS.Web.Areas.App.Controllers
{
    [Area("App")]
    [AbpMvcAuthorize(AppPermissions.Pages_NlpChatbot_NlpQAs)]
    public class NlpQAsController : AIaaSControllerBase
    {
        private readonly INlpQAsAppService _nlpQAsAppService;
        private readonly INlpChatbotsAppService _nlpChatbotsAppService;
        private readonly NlpCbSession _nlpCbSession;
        //private readonly NlpQACacheManager _nlpQACacheManager;

        public NlpQAsController(INlpQAsAppService nlpQAsAppService,
            INlpChatbotsAppService nlpChatbotsAppService,
            NlpCbSession nlpCbSession)
        //NlpQACacheManager nlpQACacheManager)
        {
            _nlpQAsAppService = nlpQAsAppService;
            _nlpChatbotsAppService = nlpChatbotsAppService;
            _nlpCbSession = nlpCbSession;
            //_nlpQACacheManager = nlpQACacheManager;
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<ActionResult> Index(string chatbotId)
        {
            if (chatbotId.IsNullOrEmpty())
                chatbotId = (string)_nlpCbSession["ChatbotId"];

            //PagedResultDto<GetNlpChatbotForViewDto> result = await _nlpChatbotsAppService.GetAll(new GetAllNlpChatbotsInput());

            List<NlpChatbotDto> chatbotList = await _nlpChatbotsAppService.GetAllForSelectList();

            var model = new NlpQAsViewModel
            {
                FilterText = "",
            };

            var targetList = (from o in chatbotList
                              select new SelectListItem(o.Name, o.Id.ToString(), o.Id.ToString() == chatbotId)).ToList();
            model.ChatbotSelectList = new SelectList(targetList, "Value", "Text", chatbotId);

            if (model.ChatbotSelectList.Count()>0)
            {
                chatbotId = model.ChatbotSelectList.SelectedValue?.ToString();
                if (chatbotId == null || chatbotId == Guid.Empty.ToString() || chatbotId.IsNullOrEmpty())
                    chatbotId = model.ChatbotSelectList.First().Value.ToString();

                Guid chatbot;
                Guid.TryParse(chatbotId, out chatbot);

                model.QaCount = await _nlpQAsAppService.GetQaCount(chatbot);
            }


            return View(model);
        }


        protected async Task<CreateOrEditNlpQAModalViewModel> GetNlpQAModalViewModel(Guid chatbotId, Guid? id)
        {
            CreateOrEditNlpQAModalViewModel viewModel;

            var allWorkflowStatus = await _nlpQAsAppService.GetAllNlpWorkflowStateForTableDropdown(chatbotId);

            var currentWFSSelectList = allWorkflowStatus.Select(
                    nlp => new NlpLookupTableDto
                    {
                        Id = (nlp.WfsId ?? nlp.WfId).ToString(),
                        DisplayName = nlp.WfsId == null ? L("WFStateFilter*", nlp.WfName) : (nlp.WfName + " : " + nlp.WfsName)
                    }
                ).ToList();

            var nextWFSSelectList = allWorkflowStatus.Where(e => e.WfsId != null).Select(
                    nlp => new NlpLookupTableDto
                    {
                        Id = (nlp.WfsId ?? nlp.WfId).ToString(),
                        DisplayName = nlp.WfsId == null ? (nlp.WfName + " : *") : (nlp.WfName + " : " + nlp.WfsName)
                    }
                ).ToList();

            currentWFSSelectList.Insert(0, new NlpLookupTableDto()
            {
                DisplayName = @L("WFStateFilter_All"),
                Id = NlpWorkflowStateConsts.WfsNull.ToString()
            });

            nextWFSSelectList.Insert(0, new NlpLookupTableDto()
            {
                DisplayName = @L("WFStateNull"),
                Id = NlpWorkflowStateConsts.WfsNull.ToString()
            });

            nextWFSSelectList.Insert(0, new NlpLookupTableDto()
            {
                DisplayName = @L("WFStateKeep"),
                Id = NlpWorkflowStateConsts.WfsKeepCurrent.ToString()
            });

            if (id.HasValue)
            {
                var getNlpQAForEditOutput = await _nlpQAsAppService.GetNlpQAForEdit(new EntityDto<Guid> { Id = (Guid)id });

                viewModel = new CreateOrEditNlpQAModalViewModel()
                {
                    NlpQA = getNlpQAForEditOutput.NlpQA,
                    NlpChatbotName = getNlpQAForEditOutput.NlpChatbotName,
                    NlpChatbotId = getNlpQAForEditOutput.NlpChatbotId,
                    NlpChatbotLanguage = getNlpQAForEditOutput.NlpChatbotLanguage,
                    CurrentWFSSelectList = currentWFSSelectList,
                    NextWFSSelectList = nextWFSSelectList,
                };
            }
            else
            {
                var getNlpChatbotForEditOutput = await _nlpChatbotsAppService.GetNlpChatbotForEdit(new EntityDto<Guid> { Id = chatbotId });

                var chatbotSelectList = await _nlpQAsAppService.GetAllNlpChatbotForTableDropdown();

                var getNlpQAForEditOutput = new GetNlpQAForEditOutput
                {
                    NlpQA = new CreateOrEditNlpQADto()
                };

                viewModel = new CreateOrEditNlpQAModalViewModel()
                {
                    NlpQA = getNlpQAForEditOutput.NlpQA,
                    NlpChatbotId = getNlpChatbotForEditOutput.NlpChatbot.Id.Value,
                    NlpChatbotLanguage = getNlpChatbotForEditOutput.NlpChatbot.Language,
                    ChatbotSelectList = chatbotSelectList,
                    CurrentWFSSelectList = currentWFSSelectList,
                    NextWFSSelectList = nextWFSSelectList,
                };
            }

            return viewModel;
        }


        [AbpMvcAuthorize(AppPermissions.Pages_NlpChatbot_NlpQAs, AppPermissions.Pages_NlpChatbot_NlpQAs_Create, AppPermissions.Pages_NlpChatbot_NlpQAs_Edit)]

        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<PartialViewResult> CreateOrEditModal(Guid chatbotId, Guid? id)
        {
            var viewModel = await GetNlpQAModalViewModel(chatbotId, id);
            viewModel.IsViewMode = false;

            return PartialView("_CreateOrEditModal", viewModel);
        }

        public async Task<PartialViewResult> ViewNlpWorkflowModal(Guid chatbotId, Guid? id)
        {
            var viewModel = await GetNlpQAModalViewModel(chatbotId, id);
            viewModel.IsViewMode = true;

            return PartialView("_CreateOrEditModal", viewModel);
        }


        [AbpMvcAuthorize(AppPermissions.Pages_NlpChatbot_NlpQAs,
        AppPermissions.Pages_NlpChatbot_NlpQAs_Create, AppPermissions.Pages_NlpChatbot_NlpQAs_Edit)]

        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<PartialViewResult> DiscardNlpQAModal(Guid chatbotId)
        {
            CreateOrEditNlpQAModalViewModel viewModel;

            var getNlpQAForEditOutput = await _nlpQAsAppService.DiscardNlpQAForEditAsync(new EntityDto<Guid> { Id = chatbotId });

            var chatbotSelectList = await _nlpQAsAppService.GetAllNlpChatbotForTableDropdown();

            viewModel = new CreateOrEditNlpQAModalViewModel()
            {
                NlpQA = getNlpQAForEditOutput.NlpQA,
                //NlpChatbotName = getNlpQAForEditOutput.NlpChatbotName,
                NlpChatbotId = getNlpQAForEditOutput.NlpChatbotId,
                NlpChatbotLanguage = getNlpQAForEditOutput.NlpChatbotLanguage,
                ChatbotSelectList = chatbotSelectList,
            };

            if (PermissionChecker.IsGranted("Pages.NlpChatbot.NlpQAs.Edit") == true ||
                PermissionChecker.IsGranted("Pages.NlpChatbot.NlpQAs.Create") == true)
                viewModel.IsViewMode = false;


            return PartialView("_CreateOrEditModal", viewModel);
        }


        [HttpPost]
        [AbpMvcAuthorize(AppPermissions.Pages_NlpChatbot_NlpQAs_Import)]

        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<PartialViewResult> ImportModal(Guid chatbotId)
        {
            var chatbotList = await _nlpChatbotsAppService.GetAllForSelectList();

            var targetList = (from o in chatbotList
                              select new SelectListItem(o.Name, o.Id.ToString(), o.Id == chatbotId)).ToList();

            return PartialView("_ImportModal", new NlpChatbotSelectionModel()
            {
                ChatbotSelectList = new SelectList(targetList, "Value", "Text", chatbotId)
            });
        }


        [HttpPost]
        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 60)]
        [AbpMvcAuthorize(AppPermissions.Pages_NlpChatbot_NlpQAs_Export)]
        public async Task<PartialViewResult> ExportModal(Guid chatbotId)
        {
            var chatbotList = await _nlpChatbotsAppService.GetAllForSelectList();

            var targetList = (from o in chatbotList
                              select new SelectListItem(o.Name, o.Id.ToString(), o.Id == chatbotId)).ToList();

            return PartialView("_ExportModal", new NlpChatbotSelectionModel()
            {
                ChatbotSelectList = new SelectList(targetList, "Value", "Text", chatbotId)
            });
        }

        //[HttpPost]
        //[AbpMvcAuthorize(AppPermissions.Pages_NlpChatbot_NlpQAs_Import)]
        //[ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 60)]
        //public async Task<JsonResult> ImportJsonFile(Guid chatbotId, bool ignoreDuplication)
        //{
        //    var file = Request.Form.Files.First();

        //    if (file == null)
        //    {
        //        throw new UserFriendlyException(L("File_Empty_Error"));
        //    }

        //    if (file.Length > 104857600) //100 MB
        //    {
        //        throw new UserFriendlyException(L("File_SizeLimit_Error"));
        //    }

        //    byte[] fileBytes;
        //    using (var stream = file.OpenReadStream())
        //    {
        //        fileBytes = stream.GetAllBytes();
        //    }

        //    await _nlpQAsAppService.ImportJsonFile(chatbotId, fileBytes);

        //    return Json(new AjaxResponse(true));
        //}
    }
}