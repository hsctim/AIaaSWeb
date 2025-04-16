using System;
using System.Threading.Tasks;
using Abp.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Web.Areas.App.Models.NlpChatbots;
using AIaaS.Web.Controllers;
using AIaaS.Authorization;
using AIaaS.Nlp;
using AIaaS.Nlp.Dtos;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using Abp.Localization;
using System.Linq;
//using AIaaS.Nlp.Cache;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Abp.UI;
using Abp.Web.Models;
using AIaaS.Nlp.Lib;
using Abp.IO.Extensions;
using System.Drawing.Imaging;
using AIaaS.Web.Helpers;
using AIaaS.Storage;
using Abp.Threading;
using AIaaS.Helpers;
using Abp.Runtime.Caching;
using ApiProtectorDotNet;
using AIaaS.Authorization.Users;
using AIaaS.Web.Areas.App.Models.NlpQAs;
using AIaaS.Features;
using AIaaS.Graphics;
using SkiaSharp;
using AIaaS.MultiTenancy;

namespace AIaaS.Web.Areas.App.Controllers
{
    [Area("App")]
    [AbpMvcAuthorize(AppPermissions.Pages_NlpChatbot_NlpChatbots)]
    public class NlpChatbotsController : AIaaSControllerBase
    {
        private readonly INlpChatbotsAppService _nlpChatbotsAppService;
        private readonly ILanguageManager _languageManager;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ICacheManager _cacheManager;
        private readonly IBinaryObjectManager _binaryObjectManager;
        private readonly NlpChatbotFunction _nlpChatbotFunction;
        private readonly UserManager _userManager;
        private readonly TenantManager             _tenantManager; 
        
        private readonly IImageFormatValidator _imageFormatValidator;

        public NlpChatbotsController(
            INlpChatbotsAppService nlpChatbotsAppService,
            ILanguageManager languageManager,
            IWebHostEnvironment hostingEnvironment,
            ICacheManager cacheManager,
            IBinaryObjectManager binaryObjectManager,
            NlpChatbotFunction nlpChatbotFunction,
            UserManager userManager,
            TenantManager tenantManager,
            IImageFormatValidator imageFormatValidator
            )
        {
            _nlpChatbotsAppService = nlpChatbotsAppService;
            _languageManager = languageManager;
            _hostingEnvironment = hostingEnvironment;
            _cacheManager = cacheManager;
            _binaryObjectManager = binaryObjectManager;
            _nlpChatbotFunction = nlpChatbotFunction;
            _userManager = userManager;
            _tenantManager = tenantManager;
            _imageFormatValidator = imageFormatValidator;
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<ActionResult> Index()
        {
            var licenseUsage = await _nlpChatbotsAppService.GetChatbotLicenseUsage();

            var model = new NlpChatbotsViewModel
            {
                FilterText = "",
                WarningMessage = licenseUsage.UsageCount > licenseUsage.LicenseCount ? L("ExceedChatbotCount") : null,
                Usage = licenseUsage
            };

            return View(model);
        }


        protected async Task<CreateOrEditNlpChatbotModalViewModel> GetNlpChatbotModel(Guid? id)
        {
            GetNlpChatbotForEditOutput getNlpChatbotForEditOutput;
            string language = "";

            if (id.HasValue)
            {
                getNlpChatbotForEditOutput = await _nlpChatbotsAppService.GetNlpChatbotForEdit(new EntityDto<Guid> { Id = (Guid)id });

                language = getNlpChatbotForEditOutput.NlpChatbot.Language;
            }
            else
            {
                getNlpChatbotForEditOutput = new GetNlpChatbotForEditOutput
                {
                    NlpChatbot = new CreateOrEditNlpChatbotDto()
                    {
                        EnableFacebook=true,
                        EnableLine=true,
                        EnableWebChat=true,
                        EnableWebAPI = true,
                        EnableWebhook=true,
                        EnableOPENAI=0,
                       
                    }
                };

                language = _languageManager.CurrentLanguage.Name;
            }

            var selectedLanguage = getNlpChatbotForEditOutput.NlpChatbot.Language;
            var currentLanguage = _languageManager.CurrentLanguage;

            var languages = from e in _languageManager.GetActiveLanguages()
                            orderby e.Name != selectedLanguage, e.Name != currentLanguage.Name, e.Name
                            select new SelectListItem()
                            {
                                Text = e.DisplayName,
                                Value = e.Name,
                                Selected = (e.Name == selectedLanguage),
                            };

            var tenant =await _tenantManager.GetByIdAsync(AbpSession.TenantId.Value);

            var gptOptions = new List<SelectListItem>();

            gptOptions.Add(new SelectListItem() {
                Text = L("GPT_NotUse"),
                Value = ((int)NlpChatbotConsts.EnableGPTType.Disabled).ToString()
            });

            gptOptions.Add(new SelectListItem() { Text = L("GPT_UseMyChatGPT"), 
                Value = ((int)NlpChatbotConsts.EnableGPTType.UsingPrivate).ToString()
            });

            if (tenant.EnableSysGpt)
            {
                gptOptions.Add(new SelectListItem()
                {
                    Text = L("GPT_UseChatPal"),
                    Value = ((int)NlpChatbotConsts.EnableGPTType.UsingSystem).ToString()
                });
            }
            else
            {
                if (getNlpChatbotForEditOutput.NlpChatbot.EnableOPENAI== (int)NlpChatbotConsts.EnableGPTType.UsingSystem)
                {
                    getNlpChatbotForEditOutput.NlpChatbot.EnableOPENAI = (int)NlpChatbotConsts.EnableGPTType.Disabled;
                }
            }

            
            var viewModel = new CreateOrEditNlpChatbotModalViewModel()
            {
                NlpChatbot = getNlpChatbotForEditOutput.NlpChatbot,
                LanguageSelectList = new SelectList(languages, "Value", "Text", selectedLanguage),

                GPTOptionsList = new SelectList(gptOptions, "Value", "Text", ((int)getNlpChatbotForEditOutput.NlpChatbot.EnableOPENAI).ToString()),

                //GPTOptionsList = new SelectList()

                PictureList = _nlpChatbotsAppService.GetDefaultProfilePictures().ToList(),
            };

            return viewModel;
        }


        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        [AbpMvcAuthorize(AppPermissions.Pages_NlpChatbot_NlpChatbots_Create, AppPermissions.Pages_NlpChatbot_NlpChatbots_Edit)]
        public async Task<PartialViewResult> CreateOrEditModal(Guid? id)
        {
            var viewModel = await GetNlpChatbotModel(id);
            viewModel.IsViewMode = false;

            if (id.HasValue)
                _cacheManager.Remove_BotPicture(id.Value);

            return PartialView("_CreateOrEditModal", viewModel);
        }


        public async Task<PartialViewResult> ViewNlpChatbotModal(Guid? id)
        {
            var viewModel = await GetNlpChatbotModel(id);
            viewModel.IsViewMode = true;

            if (id.HasValue)
                _cacheManager.Remove_BotPicture(id.Value);

            return PartialView("_CreateOrEditModal", viewModel);
        }


        //private List<SelectListItem> GetDefault

        [AbpMvcAuthorize(AppPermissions.Pages_NlpChatbot_NlpChatbots_Create, AppPermissions.Pages_NlpChatbot_NlpChatbots_Edit)]
        [RequestSizeLimit(102400)]
        [HttpPost]


        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<JsonResult> CreateOrEdit(CreateOrEditNlpChatbotDto dto)//dto是表單數據
        {
            var imageFiles = Request.Form.Files;//文件此時需要這樣獲取
                                                //string targetFileName = "";
                                                //string targetFilePath = "";
            if (imageFiles != null && imageFiles.Count >= 1)        //upload image
            {
                var imageFile = imageFiles[0];

                if (imageFile.Length > NlpChatbotConsts.MaxPictureSize)
                {
                    //return Json(new AjaxResponse(new ErrorInfo(L("ChatbotUploadLargeFile"), L("ChatbotUploadLargeFileDetail")), false));
                    throw new UserFriendlyException(L("ChatbotUploadLargeFile"), L("ChatbotUploadLargeFileDetail"));
                }

                byte[] fileBytes;
                using (var stream = imageFile.OpenReadStream())
                {
                    fileBytes = stream.GetAllBytes();
                    _imageFormatValidator.Validate(fileBytes);
                }

                if (dto.Id != null)
                {
                    Guid? deletedId = await _nlpChatbotsAppService.DeleteProfilePicture(dto.Id.Value);

                    _cacheManager.Remove_BotPicture(deletedId.Value);
                    _cacheManager.Remove_BotPicture(dto.Id.Value);
                }


                var fileObject = new BinaryObject(AbpSession.TenantId, fileBytes, $"Chatbot profile, uploaded file {DateTime.UtcNow}");

                await _binaryObjectManager.SaveAsync(fileObject);

                dto.ChatbotPictureId = fileObject.Id;
                _cacheManager.Set_BotPicture(dto.ChatbotPictureId.Value, File(fileBytes, Abp.AspNetZeroCore.Net.MimeTypeNames.ImageJpeg));

                //_nlpLRUCacheHelper.SetLruCacheObject(File(fileBytes, Abp.AspNetZeroCore.Net.MimeTypeNames.ImageJpeg), dto.ChatbotPictureId.Value.ToString(), "BotPicture");
            }
            else if (dto.ImageFileName != null && dto.ImageFileName.Length > 0)
            {
                try
                {
                    Guid guidChatbot;

                    if (Guid.TryParse(dto.ImageFileName.Left(36), out guidChatbot))
                        dto.ChatbotPictureId = guidChatbot;
                }
                catch (Exception)
                {
                }
            }
            var result = await _nlpChatbotsAppService.CreateOrEdit(dto);

            return Json(new AjaxResponse(true));
        }


        [AbpMvcAuthorize(AppPermissions.Pages_NlpChatbot_NlpChatbots, AppPermissions.Pages_NlpChatbot_NlpChatbots_Delete)]


        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public PartialViewResult DeleteModal(Guid id)
        {
            var chatbot = _nlpChatbotFunction.GetChatbotDto(id);

            var user = _userManager.GetUserOrNull(new Abp.UserIdentifier(AbpSession.TenantId, AbpSession.UserId.Value));

            var viewModel = new DeleteNlpChatbotModalViewModel()
            {
                ChatbotId = chatbot.Id,
                ChatbotName = chatbot.Name,
                UserEmail = user.EmailAddress,
            };

            return PartialView("_DeleteModal", viewModel);
        }


        [HttpPost]
        [AbpMvcAuthorize(AppPermissions.Pages_NlpChatbot_NlpChatbots_Import)]

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
        [AbpMvcAuthorize(AppPermissions.Pages_NlpChatbot_NlpChatbots_Export)]
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


        [HttpPost]
        [AbpMvcAuthorize(AppPermissions.Pages_NlpChatbot_NlpChatbots_Import)]
        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 60)]
        public async Task<JsonResult> ImportJsonFile(Guid chatbotId, bool ignoreDuplication)
        {
            var file = Request.Form.Files.First();

            if (file == null)
            {
                throw new UserFriendlyException(L("File_Empty_Error"));
            }

            if (file.Length > 104857600) //100 MB
            {
                throw new UserFriendlyException(L("File_SizeLimit_Error"));
            }

            string jsonText;
            using (var stream = file.OpenReadStream())
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    //This allows you to do one Read operation.
                    jsonText = sr.ReadToEnd();
                }
            }

            await _nlpChatbotsAppService.ImportJsonFile(jsonText);
            //_nlpQAsAppService.ImportExcelFile(chatbotId, fileBytes);

            return Json(new AjaxResponse(true));
        }

    }
}