using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using Abp.Linq.Extensions;
using System.Collections.Generic;
using Abp.Domain.Repositories;
using AIaaS.Nlp.Dtos;
using Abp.Application.Services.Dto;
using AIaaS.Authorization;
using Abp.Extensions;
using Abp.Authorization;
using Abp.Localization;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Abp.Application.Services;
using AIaaS.Nlp.Lib;
using Abp.UI;
using Newtonsoft.Json;
using AIaaS.Nlp.Training;
using AIaaS.Storage;
using Abp.Auditing;
using Abp.Threading;
using AIaaS.Helpers;
using Abp.Runtime.Caching;
using ApiProtectorDotNet;
using System.Threading.Tasks;
using AIaaS.Dto;
using Abp.AspNetZeroCore.Net;
using AIaaS.Nlp.Dtos.NlpChatbot;
using System.Text;
using Abp.Timing;
using AIaaS.Features;
using AIaaS.License;
using AIaaS.Nlp.Lib.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Diagnostics;
using Abp.Domain.Uow;
using AIaaS.Nlp.ImExport;
using AIaaS.Notifications;
using Abp.Notifications;
using AIaaS.Editions;
using Abp.Runtime.Session;
using System.ComponentModel.DataAnnotations;

namespace AIaaS.Nlp
{
    [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpCbModels, AppPermissions.Pages_NlpChatbot_NlpChatbots, AppPermissions.Pages_NlpChatbot_NlpQAs)]
    public class NlpChatbotsAppService : AIaaSAppServiceBase, INlpChatbotsAppService
    {
        private readonly IRepository<NlpChatbot, Guid> _nlpChatbotRepository;
        private readonly IRepository<NlpQA, Guid> _nlpQARepository;
        private readonly IRepository<NlpWorkflow, Guid> _nlpWorkflowRepository;
        private readonly IRepository<NlpWorkflowState, Guid> _nlpWorkflowStateRepository;
        private readonly ILanguageManager _languageManager;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IBinaryObjectManager _binaryObjectManager;
        private readonly INlpPolicyAppService _nlpPolicyAppService;
        private readonly INlpCbModelsAppService _nlpCbModelsAppService;
        private readonly INlpCbTrainingDatasAppService _nlpCbTrainingDatasAppService;
        private readonly INlpCbTrainedAnswersAppService _nlpCbTrainedAnswersAppService;
        private readonly INlpQAsAppService _nlpQAsAppService;
        private readonly ICacheManager _cacheManager;
        private readonly NlpChatbotFunction _nlpChatbotFunction;
        private readonly NlpCbWebApiClient _nlpCBServiceClient;
        private readonly ITempFileCacheManager _tempFileCacheManager;

        private Dictionary<Guid, Guid> _NewGuidMap;
        private readonly IAppNotifier _appNotifier;

        public NlpChatbotsAppService(
            IRepository<NlpChatbot, Guid> nlpChatbotRepository,
            IRepository<NlpQA, Guid> nlpQARepository,
            IRepository<NlpWorkflow, Guid> nlpWorkflowRepository,
            IRepository<NlpWorkflowState, Guid> nlpWorkflowStateRepository,
            ILanguageManager languageManager,
            IWebHostEnvironment hostingEnvironment,
            INlpCbModelsAppService nlpCbModelsAppService,
            INlpCbTrainingDatasAppService nlpCbTrainingDatasAppService,
            INlpCbTrainedAnswersAppService nlpCbTrainedAnswersAppService,
            INlpQAsAppService nlpQAsAppService,
            IBinaryObjectManager binaryObjectManager,
            INlpPolicyAppService nlpPolicyAppService,
            ICacheManager cacheManager,
            NlpChatbotFunction nlpChatbotFunction,
            NlpCbWebApiClient nlpCBServiceClient,
            ITempFileCacheManager tempFileCacheManager,
            IAppNotifier appNotifier
            )
        {
            _nlpChatbotRepository = nlpChatbotRepository;
            _nlpQARepository = nlpQARepository;
            _nlpWorkflowRepository = nlpWorkflowRepository;
            _nlpWorkflowStateRepository = nlpWorkflowStateRepository;

            _languageManager = languageManager;
            _hostingEnvironment = hostingEnvironment;

            _nlpCbModelsAppService = nlpCbModelsAppService;
            _nlpCbTrainingDatasAppService = nlpCbTrainingDatasAppService;
            _nlpCbTrainedAnswersAppService = nlpCbTrainedAnswersAppService;
            _nlpQAsAppService = nlpQAsAppService;

            _nlpCBServiceClient = nlpCBServiceClient;
            _binaryObjectManager = binaryObjectManager;
            _nlpPolicyAppService = nlpPolicyAppService;

            _nlpChatbotFunction = nlpChatbotFunction;
            _cacheManager = cacheManager;

            _tempFileCacheManager = tempFileCacheManager;
            _appNotifier = appNotifier;

        }


        /// <summary>
        /// Get filename of chatbot image. Return null if file not found.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>


        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<PagedResultDto<GetNlpChatbotForViewDto>> GetAll(GetAllNlpChatbotsInput input)
        {
            var languageNameDictionary = _cacheManager.Get_LanguageManager_DisplayName()
                ??
                _cacheManager.Set_LanguageManager_DisplayName(
                (from e in _languageManager.GetActiveLanguages()
                 select new ValueTuple<string, string>()
                 {
                     Item1 = e.Name,
                     Item2 = e.DisplayName
                 }).ToDictionary(data => data.Item1, data => data.Item2));

            if (input.MaxResultCount > AppConsts.MaxPageSize)
                throw new UserFriendlyException(L("Exception"));

            var filteredNlpChatbots = _nlpChatbotRepository.GetAll();
            //.WhereIf(!string.IsNullOrWhiteSpace(input.Filter), e => false || e.Name.Contains(input.Filter.Trim()) || e.GreetingMsg.Contains(input.Filter.Trim()) || e.FailedMsg.Contains(input.Filter.Trim()) || e.AlternativeQuestion.Contains(input.Filter.Trim()));

            var pagedAndFilteredNlpChatbots = await filteredNlpChatbots.OrderBy(input.Sorting ?? "id asc").PageBy(input).ToListAsync();

            var nlpChatbots = from o in pagedAndFilteredNlpChatbots
                              select new GetNlpChatbotForViewDto()
                              {
                                  NlpChatbot = new NlpChatbotForList
                                  {
                                      Name = o.Name,
                                      GreetingMsg = o.GreetingMsg,
                                      FailedMsg = o.FailedMsg,
                                      AlternativeQuestion = o.AlternativeQuestion,
                                      //Language = languageNameDictionary.ContainsKey(o.Language) ? languageNameDictionary[o.Language] : "",
                                      Id = o.Id,
                                      ChatbotPictureId = o.ChatbotPictureId,
                                      Disabled = o.Disabled,
                                      EnableWebChat = o.EnableWebChat
                                  }
                              };

            var totalCount = nlpChatbots.Count();
            var nlpChatbotsList = nlpChatbots.ToList();

            foreach (var nlpChatbotStatus in nlpChatbotsList)
                nlpChatbotStatus.TrainingStatus = await GetChatbotTrainingStatus(nlpChatbotStatus.NlpChatbot.Id);

            return new PagedResultDto<GetNlpChatbotForViewDto>(totalCount, nlpChatbotsList);
        }


        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<IList<ChatbotTrainingStatusForListView>> GetAllTrainingStatus()
        {
            var idList = await _nlpChatbotRepository.GetAll()
                .Where(e => e.Disabled == false)
                .Select(e => e.Id).ToListAsync();

            var trainingList = new List<ChatbotTrainingStatusForListView>();

            foreach (var id in idList)
            {
                var trainingStatus = await GetChatbotTrainingStatus(id);
                trainingList.Add(new ChatbotTrainingStatusForListView()
                {
                    Id = id,
                    TrainingStatus = trainingStatus.TrainingStatus,
                    TrainingProgress = trainingStatus.TrainingProgress,
                    TrainingRemaining = trainingStatus.TrainingRemaining,
                    QueueRemaining = trainingStatus.QueueRemaining
                });
            }

            return trainingList;
        }


        [RemoteService(false)]
        [DisableAuditing]
        public async Task<List<NlpChatbotDto>> GetAllForSelectList()
        {
            var nlpChatbots = (List<NlpChatbotDto>)_cacheManager.Get_AllChatbotForSelectList(AbpSession.TenantId);

            if (nlpChatbots != null)
                return nlpChatbots;

            nlpChatbots = await (from o in _nlpChatbotRepository.GetAll()
                                 where o.TenantId == AbpSession.TenantId && o.Disabled == false
                                 select new NlpChatbotDto
                                 {
                                     Id = o.Id,
                                     Name = o.Name,
                                     Disabled = o.Disabled,
                                 }).ToListAsync();

            return (List<NlpChatbotDto>)_cacheManager.Set_AllChatbotForSelectList(AbpSession.TenantId, nlpChatbots);
        }

        [RemoteService(false)]
        [DisableAuditing]
        public async Task<GetNlpChatbotForEditOutput> GetNlpChatbotForEdit(EntityDto<Guid> input)
        {
            var nlpChatbot = await _nlpChatbotRepository.FirstOrDefaultAsync(input.Id);

            var output = new GetNlpChatbotForEditOutput { NlpChatbot = ObjectMapper.Map<CreateOrEditNlpChatbotDto>(nlpChatbot) };

            return output;
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<NlpChatbotDto> CreateOrEdit(CreateOrEditNlpChatbotDto input)
        {
            if (input.Id == null)
            {
                return await Create(input);
            }
            else
            {
                return await Update(input);
            }
        }

        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpChatbots_Create)]
        protected virtual async Task<NlpChatbotDto> Create(CreateOrEditNlpChatbotDto input)
        {
            input.FailedMsg = input.FailedMsg?.Trim();
            input.GreetingMsg = input.GreetingMsg?.Trim();
            input.ImageFileName = input.ImageFileName?.Trim();
            input.Language = input.Language?.Trim();
            input.Name = input.Name?.Trim();
            input.AlternativeQuestion = input.AlternativeQuestion?.Trim();
            input.FacebookAccessToken = input.FacebookAccessToken?.Trim();
            input.FacebookVerifyToken = input.FacebookVerifyToken?.Trim();
            input.FacebookSecretKey = input.FacebookSecretKey?.Trim();
            input.LineToken = input.LineToken?.Trim();
            input.WebApiSecret = input.WebApiSecret?.Trim();
            input.WebhookSecret = input.WebhookSecret?.Trim();
            input.OPENAIOrg = input.OPENAIOrg?.Trim();
            input.OPENAIKey = input.OPENAIKey?.Trim();

            if (input.EnableOPENAI <= 1)
            {
                input.OPENAIOrg = null;
                input.OPENAIKey = null;

                if (input.EnableOPENAI == 0)
                    input.OPENAICache = false;
                else
                    input.OPENAICache = true;
            }


            input.PredThreshold = Math.Max(Math.Min(input.PredThreshold, NlpChatbotConsts.MaxSuggestionThresholdValue), NlpChatbotConsts.MinSuggestionThresholdValue);

            input.WSPredThreshold = Math.Max(Math.Min(input.WSPredThreshold, NlpChatbotConsts.MaxWSPredThresholdValue), NlpChatbotConsts.MinWSPredThresholdValue);

            input.SuggestionThreshold = Math.Max(Math.Min(input.SuggestionThreshold, NlpChatbotConsts.MaxSuggestionThresholdValue), NlpChatbotConsts.MinSuggestionThresholdValue);


            var nlpChatbot = ObjectMapper.Map<NlpChatbot>(input);

            if (AbpSession.TenantId != null)
            {
                nlpChatbot.TenantId = (int)AbpSession.TenantId;
            }

            await _nlpPolicyAppService.CheckMaxChatbotCount(nlpChatbot.TenantId);

            await _nlpChatbotRepository.InsertAsync(nlpChatbot);
            _cacheManager.Remove_AllChatbotForSelectList(AbpSession.TenantId);

            var dto = ObjectMapper.Map<NlpChatbotDto>(nlpChatbot);
            _cacheManager.Set_NlpChatbotDto(dto.Id, dto);

            await _nlpQAsAppService.CheckNNID0Async(dto.Id);
            return dto;
        }

        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpChatbots_Edit)]
        protected virtual async Task<NlpChatbotDto> Update(CreateOrEditNlpChatbotDto input)
        {
            input.FailedMsg = input.FailedMsg?.Trim();
            input.GreetingMsg = input.GreetingMsg?.Trim();
            input.ImageFileName = input.ImageFileName?.Trim();
            input.Language = input.Language?.Trim();
            input.Name = input.Name?.Trim();
            input.AlternativeQuestion = input.AlternativeQuestion?.Trim();
            input.FacebookAccessToken = input.FacebookAccessToken?.Trim();
            input.FacebookVerifyToken = input.FacebookVerifyToken?.Trim();
            input.FacebookSecretKey = input.FacebookSecretKey?.Trim();
            input.LineToken = input.LineToken?.Trim();
            input.OPENAIOrg = input.OPENAIOrg?.Trim();
            input.OPENAIKey = input.OPENAIKey?.Trim();
            input.WebApiSecret = input.WebApiSecret?.Trim();
            input.WebhookSecret = input.WebhookSecret?.Trim();

            if (input.EnableOPENAI<=1)
            {
                input.OPENAIOrg = null;
                input.OPENAIKey = null;

                if (input.EnableOPENAI == 0)
                    input.OPENAICache = false;
                else
                    input.OPENAICache = true;
            }

            input.PredThreshold = Math.Max(Math.Min(input.PredThreshold, NlpChatbotConsts.MaxSuggestionThresholdValue), NlpChatbotConsts.MinSuggestionThresholdValue);

            input.WSPredThreshold = Math.Max(Math.Min(input.WSPredThreshold, NlpChatbotConsts.MaxWSPredThresholdValue), NlpChatbotConsts.MinWSPredThresholdValue);

            input.SuggestionThreshold = Math.Max(Math.Min(input.SuggestionThreshold, NlpChatbotConsts.MaxSuggestionThresholdValue), NlpChatbotConsts.MinSuggestionThresholdValue);

            var nlpChatbot = _nlpChatbotRepository.FirstOrDefault((Guid)input.Id);

            if (input.ChatbotPictureId == null && nlpChatbot.ChatbotPictureId != null)
                input.ChatbotPictureId = nlpChatbot.ChatbotPictureId;

            var response = ObjectMapper.Map(input, nlpChatbot);
            _cacheManager.Remove_AllChatbotForSelectList(AbpSession.TenantId);

            var dto = ObjectMapper.Map<NlpChatbotDto>(response);
            _cacheManager.Set_NlpChatbotDto(response.Id, dto);

            //CheckNNID0(dto.Id);
            await _nlpQAsAppService.CheckNNID0Async(dto.Id);
            return dto;
        }

        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpChatbots_Delete)]

        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task Delete(EntityDto<Guid> input)
        {
            await _nlpChatbotRepository.DeleteAsync(input.Id);
            _cacheManager.Remove_NlpChatbotDto(input.Id);
            _cacheManager.Remove_AllChatbotForSelectList(AbpSession.TenantId);

            await _nlpWorkflowStateRepository.DeleteAsync(e => e.NlpWorkflowFk.NlpChatbotId == input.Id);
            await _nlpWorkflowRepository.DeleteAsync(e => e.NlpChatbotId == input.Id);


            //刪除Python的H5檔
            try
            {
                await _nlpCBServiceClient.DeleteTrainingModelAsync(input.Id);
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex.Message, ex);
            }
        }

        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpChatbots_Train)]

        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task TrainChatbot(Guid chatbotId, bool rebuild = false)
        {
            //_nlpPolicy.CheckMaxModelTrainingCount(AbpSession.TenantId.Value);

            var nlpChatbot = _nlpChatbotFunction.GetChatbotDto(chatbotId);


            var trainingStatus = await GetChatbotTrainingStatus(chatbotId);

            if (trainingStatus.TrainingStatus >= NlpChatbotConsts.TrainingStatus.Queueing &&
                trainingStatus.TrainingStatus < NlpChatbotConsts.TrainingStatus.Trained)
            {
                throw new UserFriendlyException(L("NlpTrainingFailed_OnTraining"));
            }


            //Disable other Training Activities of current Chatbot.
            await _nlpCbModelsAppService.CancelChatbotTrainingActivities(chatbotId);
            await _nlpCbModelsAppService.Create(new CreateOrEditNlpCbModelDto()
            {
                NlpChatbotId = chatbotId,
                NlpCbMLanguage = nlpChatbot.Language,

                NlpCbMStatus = NlpChatbotConsts.TrainingStatus.Queueing,
                Rebuild = rebuild,

                NlpCbMCreatorUserId = AbpSession.UserId,
                NlpCbMCreationTime = Clock.Now,
                NlpCbMInfo = L("NlpCbMInfoQueueing"),
            });

            await UnitOfWorkManager.Current.SaveChangesAsync();

            await _nlpPolicyAppService.CheckMaxChatbotCount(nlpChatbot.TenantId);
            await _nlpPolicyAppService.CheckMaxQuestionCount(nlpChatbot.TenantId, nlpChatbot.Id);

            if (nlpChatbot == null)
            {
                await _nlpCbModelsAppService.DeleteQueueingModel(chatbotId);
                throw new UserFriendlyException(L("NlpTrainingFailed_UnknownChatbot"));
            }

            //var isChinese = String.Compare(nlpChatbot.Language, "ZH-HANT", true) == 0 || String.Compare(nlpChatbot.Language, "ZH-HANS", true) == 0;
            //nlpChatbot.Language.ToUpper() == "ZH-HANS";

            var nlpQAs = _nlpQARepository.GetAll()
                .Include(e => e.CurrentWfStateFk)
                .Include(e => e.CurrentWfAllFk)
                .Where(e => e.NlpChatbotId == chatbotId && string.IsNullOrEmpty(e.Question) == false);
            var nlpQACount = await nlpQAs.CountAsync();
            if (nlpQACount == 0)
            {
                await _nlpCbModelsAppService.DeleteQueueingModel(chatbotId);
                throw new UserFriendlyException(L("NlpTrainingFailed_NoQA"));
            }

            await CheckAndCompactNNID(chatbotId);

            var nlpQaList = await nlpQAs.ToListAsync();

            var nlpRawDataQas = from o in nlpQaList
                                select new
                                {
                                    i = o.NNID,
                                    s = new NlpCbMSourceData.NlpCbMSourceDataQA_DictionaryNNID()
                                    {
                                        q = JsonConvert.DeserializeObject<string[]>(o.Question),
                                        a = JsonToAnswer(o.Answer),                   
                                        w = o.CurrentWfState,
                                    }
                                };

            var nlpRawDataQasNNID = nlpRawDataQas.ToDictionary(e => e.i, e => e.s);

            if (nlpRawDataQasNNID.Keys.Count == 0 || nlpRawDataQasNNID.Keys.Max() == 0)
            {
                await _nlpCbModelsAppService.DeleteQueueingModel(chatbotId);
                throw new UserFriendlyException(L("NlpTrainingFailed"), L("NlpTrainingFailed_NoQA"));
            }

            var nlpRawData = new NlpCbMSourceData()
            {
                ChatbotId = chatbotId,
                QaSets = nlpRawDataQasNNID,
                StartTime = Clock.Now
            };


            await _nlpPolicyAppService.UpdateTenantPriority(nlpChatbot.TenantId);

            await _nlpCbTrainingDatasAppService.CreateNewTrainingDataAsync(chatbotId, nlpRawData);

            await UnitOfWorkManager.Current.SaveChangesAsync();

            var isFree = await IsFreeEdition(nlpChatbot.TenantId);

            try
            {
                //NlpCbRequestTrainingResult result = await
                _nlpCBServiceClient.RequestTrainingAsync(isFree);
                //if (result.errorCode != "success")
                //throw new UserFriendlyException(L("NlpCbMServiceNotResponding"));
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex.ToString(), ex);
                //throw new UserFriendlyException(L("NlpCbMServiceNotResponding"));
            }

            await _appNotifier.TrainedModelChanged(nlpChatbot.TenantId, L("NlpCbMInfoQueueing"), NotificationSeverity.Info);

        }
        /// <summary>
        /// 重新排列NNID
        /// 以10個為一個Package，若NNID超過(Package數*10+10)，表示中間有空的，將超過NNID的重設搬至前面空的。
        /// </summary>
        /// <param name="chatbotId"></param>
        [RemoteService(false)]
        [DisableAuditing]
        private async Task CheckAndCompactNNID(Guid chatbotId)
        {
            var nlpQAs = _nlpQARepository.GetAll().Where(e => e.NlpChatbotId == chatbotId);

            int nlpQAMax = await nlpQAs.MaxAsync(e => e.NNID);
            int nlpQACount = await nlpQAs.CountAsync();

            int nlpQAPacketSize = 10 + (nlpQACount - nlpQACount % 10);

            if (nlpQAMax > nlpQAPacketSize)
            {
                var nlpQAsList = await _nlpQARepository.GetAll().Where(e => e.NlpChatbotId == chatbotId && e.NNID >= nlpQAPacketSize).ToListAsync();

                //找出未用到的NNID，並加入Queue
                var NextNNIDQueue = await GetNextNNIDQueue(chatbotId, nlpQAPacketSize - 1);
                foreach (NlpQA qa in nlpQAsList)
                {
                    qa.NNID = NextNNIDQueue.Dequeue();
                }

                await UnitOfWorkManager.Current.SaveChangesAsync();
            }
        }

        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpChatbots_Train)]
        [DisableAuditing]
        protected async Task<Queue<int>> GetNextNNIDQueue(Guid chatbotId, int maxNNID)
        {
            var nSequence = Enumerable.Range(0, maxNNID).ToDictionary(e => e);
            var nlpQA_NNID = await _nlpQARepository.GetAll().Where(e => e.NlpChatbotId == chatbotId && e.NNID <= maxNNID)
                .Select(e => e.NNID).ToDictionaryAsync(e => e);

            var result = nSequence.Where(p => !nlpQA_NNID.Any(p2 => p2.Key == p.Key)).Select(e => e.Key);

            return new Queue<int>(result);
        }

        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpChatbots_Train)]
        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task StopTrainingChatbot(Guid chatbotId)
        {
            await _nlpCbModelsAppService.CancelChatbotTrainingActivities(chatbotId);
            await _nlpCbTrainedAnswersAppService.DeleteUnusedData();
        }

        [DisableAuditing]
        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 120, TimeWindowSeconds: 60)]
        public async Task<NlpChatbotTrainingStatus> GetChatbotTrainingStatus(Guid chatbotId)
        {
            return await _nlpCbModelsAppService.GetChatbotTrainingStatus(chatbotId);
        }

        [RemoteService(false)]
        [AbpAllowAnonymous]
        [DisableAuditing]
        public async Task<byte[]> GetProfilePicture(Guid pictureId)
        {
            Guid? pic = (await _nlpChatbotRepository.FirstOrDefaultAsync(e => e.ChatbotPictureId == pictureId || e.Id== pictureId))?.ChatbotPictureId;

            if (pic != null && pic != Guid.Empty)
            {
                var file = await _binaryObjectManager.GetOrNullAsync(pic.Value);
                if (file != null)
                {
                    return file.Bytes;
                }
            }

            //image in file
            var directory = new DirectoryInfo(Path.Combine(_hostingEnvironment.WebRootPath, "Common", "Images", "bot"));
            var lastImageFile = directory
                .GetFiles("*", SearchOption.TopDirectoryOnly)
                .Where(e => e.Name.Contains(pictureId.ToString(), StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();

            if (lastImageFile != null)
            {
                return await File.ReadAllBytesAsync(lastImageFile.FullName);
            }

            return null;
        }

        [RemoteService(false)]
        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpChatbots_Edit, AppPermissions.Pages_NlpChatbot_NlpChatbots_Create)]
        public async Task<Guid?> DeleteProfilePicture(Guid chatbotId)
        {
            var picId = _nlpChatbotRepository.FirstOrDefault(e => e.Id == chatbotId && e.ChatbotPictureId != null)?.ChatbotPictureId;


            if (picId != null)
            {
                await _binaryObjectManager.DeleteAsync(picId.Value);
                return picId;
            }

            return null;
        }

        [RemoteService(false)]
        [DisableAuditing]
        public List<string> GetDefaultProfilePictures()
        {
            var directory = new DirectoryInfo(Path.Combine(_hostingEnvironment.WebRootPath, "Common", "Images", "bot"));
            var imageFiles = directory.GetFiles("*", SearchOption.TopDirectoryOnly)
                                        .OrderBy(f => f.Name);

            Guid guid;

            var listImageFiles = new List<string>();
            foreach (var imageFile in imageFiles)
            {
                try
                {
                    if (Guid.TryParse(imageFile.Name.Left(36), out guid))
                    {
                        listImageFiles.Add(guid.ToString());
                    }
                }
                catch (Exception)
                {
                }
            }

            return listImageFiles;
        }


        /// <summary>
        /// Export To File , Mark*******************************************************
        /// </summary>
        /// <param name="chatbotId"></param>
        /// <returns></returns>
        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpChatbots_Export)]
        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 5, TimeWindowSeconds: 60)]
        public async Task<FileDto> GetNlpChatbotsToFile(Guid chatbotId)
        {
            var chatbot = _nlpChatbotFunction.GetChatbotDto(chatbotId);
            chatbot = chatbot.ShallowCopy();
            if (chatbot == null || chatbot.TenantId != AbpSession.TenantId.Value || chatbot.IsDeleted)
                throw new UserFriendlyException(L("XCanNotBeNullOrEmpty", "ChatbotId"));

            chatbot.FacebookSecretKey = null;
            chatbot.FacebookVerifyToken = null;            
            chatbot.WebApiSecret = null;
            chatbot.WebhookSecret = null;
            chatbot.LineToken = null;
            chatbot.OPENAIKey = null;
            chatbot.EnableOPENAI = 0;
            chatbot.OPENAICache=false;
            
            var nlpImportExport = new NlpImportExport();

            //chatbot
            nlpImportExport.Chatbot = ObjectMapper.Map<NlpChatbotImExport>(chatbot);
            if (chatbot.ChatbotPictureId != null && chatbot.ChatbotPictureId != Guid.Empty)
            {
                var profileFile = await _binaryObjectManager.GetOrNullAsync(chatbot.ChatbotPictureId.Value);
                if (profileFile != null && profileFile.Bytes.Length > 0)
                {
                    var base64 = Convert.ToBase64String(profileFile.Bytes);
                    nlpImportExport.Chatbot.ProfileImage = base64;
                }
            }

            //NlpWorkflowImExport
            nlpImportExport.Workflows =
                await (from o in _nlpWorkflowRepository.GetAll().Where(e => e.TenantId == AbpSession.TenantId.Value && e.NlpChatbotId == chatbotId)
                       select new NlpWorkflowImExport()
                       {
                           Id = o.Id,
                           NlpChatbotId = o.NlpChatbotId,
                           Disabled = o.Disabled,
                           Name = o.Name
                       }).ToListAsync();

            //NlpWorkflowStateImExport
            nlpImportExport.WorkflowStates =
                await (from o in _nlpWorkflowStateRepository.GetAll().Include(e => e.NlpWorkflowFk)
                       .Where(e => e.TenantId == AbpSession.TenantId.Value && e.NlpWorkflowFk.NlpChatbotId == chatbotId)
                       select new NlpWorkflowStateImExport()
                       {
                           Id = o.Id,
                           StateName = o.StateName,
                           NlpWorkflowId = o.NlpWorkflowId,
                           StateInstruction = o.StateInstruction,
                           ResponseNonWorkflowAnswer = o.ResponseNonWorkflowAnswer,
                           DontResponseNonWorkflowErrorAnswer = o.DontResponseNonWorkflowErrorAnswer,
                           OutgoingFalseOp = o.OutgoingFalseOp,
                           Outgoing3FalseOp = o.Outgoing3FalseOp
                       }).ToListAsync();

            //QAs
            nlpImportExport.QAs =
                await (from o in _nlpQARepository.GetAll()
                       .Where(e => e.TenantId == AbpSession.TenantId.Value && e.NlpChatbotId == chatbotId)
                       select new NlpQAImExport()
                       {
                           Id = o.Id,
                           Answer = o.Answer,
                           QaType = o.QaType,
                           Question = o.Question,
                           QuestionCategory = o.QuestionCategory,
                           CurrentWfState = o.CurrentWfState,
                           NextWfState = o.NextWfState,
                           //NNID = o.NNID,
                           NlpChatbotId = o.NlpChatbotId
                       }).ToListAsync();


            var json = JsonConvert.SerializeObject(nlpImportExport);
            var file = new FileDto(chatbot.Name + ".json", MimeTypeNames.ApplicationJson);

            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new StreamWriter(memoryStream, System.Text.Encoding.UTF8))
                {
                    // Various for loops etc as necessary that will ultimately do this:
                    writer.Write(json);
                    writer.Flush();
                    _tempFileCacheManager.SetFile(file.FileToken, memoryStream.ToArray());
                }
            }

            return file;
        }

        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpChatbots_Import)]
        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 5, TimeWindowSeconds: 60)]
        [RemoteService(false)]
        public async Task ImportJsonFile(string jsonData)
        {
            var nlpImportExport = JsonConvert.DeserializeObject<NlpImportExport>(jsonData);

            if (nlpImportExport == null)
                throw new UserFriendlyException(L("ImportChatbotFailed"));

            await _nlpPolicyAppService.CheckMaxChatbotCount(AbpSession.TenantId.Value, 1);

            nlpImportExport.Chatbot.Id = NewGuid(nlpImportExport.Chatbot.Id);
            var nlpChatbot = ObjectMapper.Map<NlpChatbot>(nlpImportExport.Chatbot);
            nlpChatbot.TenantId = AbpSession.TenantId.Value;


            if (nlpChatbot.PredThreshold <= 0f || nlpChatbot.PredThreshold >= 1f)
                nlpChatbot.PredThreshold = NlpChatbotConsts.DefaultPredThreshold;

            if (nlpChatbot.WSPredThreshold <= 0f || nlpChatbot.WSPredThreshold >= 1f)
                nlpChatbot.WSPredThreshold = NlpChatbotConsts.DefaultWSPredThreshold;

            if (nlpChatbot.SuggestionThreshold <= 0f || nlpChatbot.SuggestionThreshold >= 1f)
                nlpChatbot.SuggestionThreshold = NlpChatbotConsts.DefaultSuggestionThreshold;


            var list = await _nlpChatbotRepository.GetAll()
                .Where(e => e.Name.Contains(nlpChatbot.Name))
                .Select(e => e.Name).ToListAsync();

            if (list.Contains(nlpChatbot.Name))
                for (int n = 1; n < 100; n++)
                    if (list.Contains(nlpChatbot.Name + $" ({n})") == false)
                    {
                        nlpChatbot.Name = nlpChatbot.Name + $" ({n})";
                        break;
                    }

            if (nlpImportExport.Chatbot.ProfileImage.IsNullOrEmpty() == false)
            {
                var byteArray = Convert.FromBase64String(nlpImportExport.Chatbot.ProfileImage);

                var storedFile = new BinaryObject(AbpSession.TenantId, byteArray, $"Imported Chatbot {nlpChatbot.Name}. {DateTime.UtcNow}");
                await _binaryObjectManager.SaveAsync(storedFile);

                nlpChatbot.ChatbotPictureId = storedFile.Id;
            }

            nlpChatbot.FacebookSecretKey = null;
            nlpChatbot.FacebookVerifyToken = null;
            nlpChatbot.WebApiSecret = null;
            nlpChatbot.WebhookSecret = null;
            nlpChatbot.LineToken = null;
            nlpChatbot.OPENAIKey = null;
            nlpChatbot.EnableOPENAI = (int)(NlpChatbotConsts.EnableGPTType.Disabled);
            nlpChatbot.OPENAICache = false;

            await _nlpChatbotRepository.InsertAsync(nlpChatbot);

            if (nlpImportExport.Workflows != null)
            {
                foreach (var workflow in nlpImportExport.Workflows)
                {
                    workflow.Id = NewGuid(workflow.Id);
                    workflow.NlpChatbotId = NewGuid(workflow.NlpChatbotId);

                    var nlpWorkflow = ObjectMapper.Map<NlpWorkflow>(workflow);
                    nlpWorkflow.TenantId = AbpSession.TenantId.Value;
                    nlpWorkflow.Vector = JsonConvert.SerializeObject(NlpHelper.CreateNewVector());
                    await _nlpWorkflowRepository.InsertAsync(nlpWorkflow);
                }
            }

            if (nlpImportExport.WorkflowStates != null)
            {
                foreach (var workflowState in nlpImportExport.WorkflowStates)
                {
                    workflowState.Id = NewGuid(workflowState.Id);
                    workflowState.NlpWorkflowId = NewGuid(workflowState.NlpWorkflowId);
                    var nlpWorkflowState = ObjectMapper.Map<NlpWorkflowState>(workflowState);
                    nlpWorkflowState.TenantId = AbpSession.TenantId.Value;
                    nlpWorkflowState.Vector = JsonConvert.SerializeObject(NlpHelper.CreateNewVector());
                    await _nlpWorkflowStateRepository.InsertAsync(nlpWorkflowState);
                }
            }

            if (nlpImportExport.QAs != null)
            {
                int NNID = 1;

                foreach (var qa in nlpImportExport.QAs)
                {
                    qa.Id = NewGuid(qa.Id);
                    qa.NlpChatbotId = NewGuid(qa.NlpChatbotId);

                    if (qa.CurrentWfState.HasValue &&
                        qa.CurrentWfState.Value != NlpWorkflowStateConsts.WfsKeepCurrent &&
                        qa.CurrentWfState.Value != NlpWorkflowStateConsts.WfsNull)
                        qa.CurrentWfState = NewGuid(qa.CurrentWfState.Value);

                    if (qa.NextWfState.HasValue &&
                        qa.NextWfState.Value != NlpWorkflowStateConsts.WfsKeepCurrent &&
                        qa.NextWfState.Value != NlpWorkflowStateConsts.WfsNull)
                        qa.NextWfState = NewGuid(qa.NextWfState.Value);

                    var nlpQA = ObjectMapper.Map<NlpQA>(qa);

                    nlpQA.TenantId = AbpSession.TenantId.Value;
                    if (nlpQA.QaType == NlpQAConsts.QaType_Unanswerable)
                        nlpQA.NNID = 0;
                    else
                        nlpQA.NNID = NNID++;


                    var questions = JsonConvert.DeserializeObject<List<string>>(nlpQA.Question);
                    nlpQA.QuestionCount = questions.Count();

                    if ((nlpQA.NNID != 0 && nlpQA.QuestionCount > 0) || nlpQA.NNID == 0)
                        await _nlpQARepository.InsertAsync(nlpQA);
                }
            }

            await UnitOfWorkManager.Current.SaveChangesAsync();

            await _nlpPolicyAppService.CheckMaxQuestionCount(AbpSession.TenantId.Value, nlpChatbot.Id);

            _cacheManager.Remove_AllChatbotForSelectList(AbpSession.TenantId);
        }


        private Guid NewGuid(Guid id)
        {
            _NewGuidMap ??= new Dictionary<Guid, Guid>();

            Guid newId;

            if (_NewGuidMap.TryGetValue(id, out newId))
                return newId;
            else
            {
                newId = Guid.NewGuid();
                _NewGuidMap[id] = newId;

                return newId;
            }
        }

        [RemoteService(false)]
        [AbpAllowAnonymous]
        public async Task CreateChatbotSampleAsync(int tenantId, long? userId)
        {
            var languageName = _languageManager.CurrentLanguage.Name;


            using (var uow = UnitOfWorkManager.Begin())
            {
                using (CurrentUnitOfWork.SetTenantId(tenantId))
                {
                    using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.SoftDelete))
                    {
                        if (string.Compare(languageName, "zh-Hant", true) == 0)
                        {
                            if (await _nlpChatbotRepository.CountAsync(e => e.TenantId == tenantId) == 0)
                            {
                                var nlpChatbot = new NlpChatbot()
                                {
                                    Name = "ChatPal",
                                    TenantId = tenantId,
                                    CreatorUserId = userId,
                                    GreetingMsg = "我是${Chatbot.Name}，有問題可以問我",
                                    FailedMsg = "對不起，我無法回應這個問題！",
                                    AlternativeQuestion = "您是不是想問以下的問題：",
                                    Language = "zh-Hant",
                                    EnableFacebook = true,
                                    EnableWebChat = true,
                                    EnableWebAPI = true,
                                    EnableLine = true,
                                    PredThreshold = NlpChatbotConsts.DefaultPredThreshold,
                                    SuggestionThreshold = NlpChatbotConsts.DefaultSuggestionThreshold,
                                    WSPredThreshold = NlpChatbotConsts.DefaultWSPredThreshold,
                                };
                                                                
                                await _nlpChatbotRepository.InsertAsync(nlpChatbot);

                                await _nlpQARepository.InsertAsync(new NlpQA()
                                {
                                    TenantId = tenantId,
                                    CreatorUserId = userId,
                                    NlpChatbotId = nlpChatbot.Id,
                                    NNID = 1,
                                    Question = "[\"您好\"]",
                                    Answer = "[\"您好，很高興為您服務\"]",
                                    QuestionCategory = "一般問答",
                                    QuestionCount = 3,
                                    CurrentWfState = NlpWorkflowStateConsts.WfsNull,
                                    NextWfState = NlpWorkflowStateConsts.WfsKeepCurrent,
                                });

                                await _nlpQARepository.InsertAsync(new NlpQA()
                                {
                                    TenantId = tenantId,
                                    CreatorUserId = userId,
                                    NlpChatbotId = nlpChatbot.Id,
                                    NNID = 2,
                                    Question = "[\"今天的天氣如何\"]",
                                    Answer = "[\"今天天氣${\\\"Intent\\\":\\\"Weather.ZHTW\\\",\\\"Day\\\":\\\"0\\\",\\\"LocationName\\\":\\\"臺北市\\\",\\\"LocationId\\\":\\\"F-D0047-061\\\"}\"]",
                                    QuestionCategory = "一般問答",
                                    QuestionCount = 8,
                                    CurrentWfState = NlpWorkflowStateConsts.WfsNull,
                                    NextWfState = NlpWorkflowStateConsts.WfsKeepCurrent,
                                });

                                await _nlpQARepository.InsertAsync(new NlpQA()
                                {
                                    TenantId = tenantId,
                                    CreatorUserId = userId,
                                    NlpChatbotId = nlpChatbot.Id,
                                    NNID = 3,
                                    Question = "[\"明天的天氣如何\"]",
                                    Answer = "[\"明天天氣${\\\"Intent\\\":\\\"Weather.ZHTW\\\",\\\"Day\\\":\\\"1\\\",\\\"LocationName\\\":\\\"臺北市\\\",\\\"LocationId\\\":\\\"F-D0047-061\\\"}\"]",
                                    QuestionCategory = "一般問答",
                                    QuestionCount = 3,
                                    CurrentWfState = NlpWorkflowStateConsts.WfsNull,
                                    NextWfState = NlpWorkflowStateConsts.WfsKeepCurrent,
                                });

                                await _nlpQARepository.InsertAsync(new NlpQA()
                                {
                                    TenantId = tenantId,
                                    CreatorUserId = userId,
                                    NlpChatbotId = nlpChatbot.Id,
                                    NNID = 4,
                                    Question = "[\"今天的日期\"]",
                                    Answer = "[\"今天是${\\\"Intent\\\":\\\"Date.Now.ZHTW\\\"}\"]",
                                    QuestionCategory = "一般問答",
                                    QuestionCount = 3,
                                    CurrentWfState = NlpWorkflowStateConsts.WfsNull,
                                    NextWfState = NlpWorkflowStateConsts.WfsKeepCurrent,
                                });

                                await _nlpQARepository.InsertAsync(new NlpQA()
                                {
                                    TenantId = tenantId,
                                    CreatorUserId = userId,
                                    NlpChatbotId = nlpChatbot.Id,
                                    NNID = 5,
                                    Question = "[\"現在時間\"]",
                                    Answer = "[\"現在時間${\\\"Intent\\\":\\\"Time.Now.ZHTW\\\"}\"]",
                                    QuestionCategory = "一般問答",
                                    QuestionCount = 2,
                                    CurrentWfState = NlpWorkflowStateConsts.WfsNull,
                                    NextWfState = NlpWorkflowStateConsts.WfsKeepCurrent,
                                });

                                await _nlpQARepository.InsertAsync(new NlpQA()
                                {
                                    TenantId = tenantId,
                                    CreatorUserId = userId,
                                    NlpChatbotId = nlpChatbot.Id,
                                    NNID = 6,
                                    Question = "[\"營業時間\"]",
                                    Answer = "[\"營業時間為早上九點至下午六點\"]",
                                    QuestionCategory = "一般問答",
                                    QuestionCount = 1,
                                    CurrentWfState = NlpWorkflowStateConsts.WfsNull,
                                    NextWfState = NlpWorkflowStateConsts.WfsKeepCurrent,
                                });

                                await _nlpQARepository.InsertAsync(new NlpQA()
                                {
                                    TenantId = tenantId,
                                    CreatorUserId = userId,
                                    NlpChatbotId = nlpChatbot.Id,
                                    NNID = 0,
                                    QaType = 1,
                                    Question = "[\"色情\",\"暴力\"]",
                                    CurrentWfState = NlpWorkflowStateConsts.WfsNull,
                                    NextWfState = NlpWorkflowStateConsts.WfsKeepCurrent,
                                });

                                await UnitOfWorkManager.Current.SaveChangesAsync();
                            }
                        }
                    }
                    uow.Complete();
                }
            }
        }


        [RemoteService(false)]
        public async Task<int> GetChatbotCount()
        {
            return await _nlpChatbotRepository.CountAsync(e => e.TenantId == AbpSession.TenantId && e.IsDeleted == false);
        }

        [RemoteService(false)]
        public async Task<LicenseUsage> GetChatbotLicenseUsage()
        {
            var usageCount = await GetChatbotCount();

            var licenseCount = (await FeatureChecker.GetValueAsync(AbpSession.TenantId.Value, AppFeatures.MaxChatbotCount)).To<int>();

            return new LicenseUsage()
            {
                LicenseCount = licenseCount,
                UsageCount = usageCount
            };
        }


        private async Task<bool> IsFreeEdition(int tenantId)
        {
            var isFree = await TenantManager.IsFreeEdition(AbpSession.GetTenantId());

            return isFree;
        }

        //json to Answer
        private string[] JsonToAnswer(string json)
        {
            if (json == null || json.IsNullOrEmpty())
            {
                return null;
            }

            try
            {
                var obj = JsonConvert.DeserializeObject<CbAnswerSet[]>(json);

                var stringList = new List<string>();

                foreach (var item in obj)
                {
                    if (item.GPT == true)
                    {
                        stringList.Add("[GPT]" + item.Answer);
                    }
                    else
                    {
                        stringList.Add(item.Answer);
                    }
                }

                return stringList.ToArray();
            }
            catch (Exception)
            {
            }

            return JsonConvert.DeserializeObject<string[]>(json);
        }
    }
}
