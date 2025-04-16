using System.Collections.Generic;
using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using Abp.Linq.Extensions;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using AIaaS.Nlp.Dtos;
using Abp.Application.Services.Dto;
using AIaaS.Authorization;
using Abp.Authorization;
using Microsoft.EntityFrameworkCore;
using Abp.Application.Services;
using Abp.UI;
using AIaaS.Nlp.Training;
using Newtonsoft.Json;
using AIaaS.Nlp.Dtos.NlpCbModel;
using System.Diagnostics;
using AIaaS.Notifications;
using AIaaS.Helpers;
using Abp.Runtime.Caching;
using ApiProtectorDotNet;
using Abp.Auditing;
using Abp.Timing;
using Abp.Threading;
using Abp.Notifications;
using Abp.Domain.Uow;
using AIaaS.Nlp.Lib;
using AIaaS.Nlp.Lib.Dtos;
using AIaaS.MultiTenancy;
using AIaaS.Auditing;
using System.Collections.Concurrent;
using AIaaS.Nlp.Dto;
using AIaaS.Editions;
using Abp.Runtime.Session;
using AutoMapper.Internal;

namespace AIaaS.Nlp
{
    [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpCbModels, AppPermissions.Pages_NlpChatbot_NlpChatbots, AppPermissions.Pages_NlpChatbot_NlpQAs)]
    public class NlpCbModelsAppService : AIaaSAppServiceBase, INlpCbModelsAppService
    {
        private readonly IRepository<NlpCbModel, Guid> _nlpCbModelRepository;
        private readonly IRepository<Tenant, int> _tenantRepository;
        private readonly IRepository<NlpQA, Guid> _nlpQARepository;

        private readonly IRepository<NlpCbTrainingData, Guid> _nlpCbTrainingDataRepository;
        private readonly IRepository<NlpCbTrainedAnswer, Guid> _nlpCbTrainedAnswerRepository;

        private readonly IRepository<NlpWorkflowState, Guid> _nlpWorkflowStateRepository;
        private readonly IRepository<NlpWorkflow, Guid> _nlpWorkflowRepository;


        private readonly NlpCbSession _nlpCbSession;
        private readonly NlpChatbotFunction _nlpChatbotFunction;
        private readonly IAppNotifier _appNotifier;
        //private readonly NlpCacheManagerHelper _nlpLRUCacheHelper;
        //private readonly NlpCbDictionariesFunction _nlpCbDictionariesFunction;
        private readonly NlpCbWebApiClient _nlpCBServiceClient;
        private readonly ICacheManager _cacheManager;

        private static ConcurrentQueue<int> _trainingModelTenantIdBags = new ConcurrentQueue<int>();    //最近訓練過的先十名Tenant

        private readonly IRepository<SubscribableEdition> _subscribableEditionRepository;


        public NlpCbModelsAppService(IRepository<NlpCbModel, Guid> nlpCbModelRepository,
            //IRepository<NlpChatbot, Guid> lookup_nlpChatbotRepository,
            IRepository<Tenant, int> tenantRepository,
            //IRepository<User, long> lookup_userRepository,
            IRepository<NlpQA, Guid> lookup_nlpQARepository,
            IRepository<NlpCbTrainingData, Guid> nlpCbTrainingDataRepository,
            IRepository<NlpCbTrainedAnswer, Guid> nlpCbTrainedAnswerRepository,
            IRepository<NlpWorkflowState, Guid> nlpWorkflowStateRepository,
            IRepository<NlpWorkflow, Guid> nlpWorkflowRepository,
            IAppNotifier appNotifier,
            //NlpCacheManagerHelper nlpLRUCacheHelper,
            NlpCbSession nlpCbSession,
            NlpChatbotFunction nlpChatbotFunction,
            //NlpCbDictionariesFunction nlpCbDictionariesFunction,
            NlpCbWebApiClient nlpCBServiceClient,
            ICacheManager cacheManager,
            IRepository<SubscribableEdition> subscribableEditionRepository
            )
        {
            _nlpCbModelRepository = nlpCbModelRepository;
            //_nlpChatbotRepository = lookup_nlpChatbotRepository;
            _tenantRepository = tenantRepository;
            //_userRepository = lookup_userRepository;
            _nlpQARepository = lookup_nlpQARepository;
            _nlpCbTrainingDataRepository = nlpCbTrainingDataRepository;
            _nlpCbTrainedAnswerRepository = nlpCbTrainedAnswerRepository;
            _nlpWorkflowStateRepository = nlpWorkflowStateRepository;
            _nlpWorkflowRepository = nlpWorkflowRepository;
            _appNotifier = appNotifier;
            //_nlpLRUCacheHelper = nlpLRUCacheHelper;
            _nlpCbSession = nlpCbSession;
            _nlpChatbotFunction = nlpChatbotFunction;
            //_nlpCbDictionariesFunction = nlpCbDictionariesFunction;
            _nlpCBServiceClient = nlpCBServiceClient;
            _cacheManager = cacheManager;
            _subscribableEditionRepository = subscribableEditionRepository;
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<PagedResultDto<GetNlpCbModelForViewDto>> GetAll(GetAllNlpCbModelsInput input)
        {
            if (input.MaxResultCount > AppConsts.MaxPageSize)
                throw new UserFriendlyException(L("Exception"));

            if (input.NlpChatbotId.HasValue == false)
                return new PagedResultDto<GetNlpCbModelForViewDto>(0, new List<GetNlpCbModelForViewDto>());

            _nlpCbSession["ChatbotId"] = input.NlpChatbotId.ToString();

            var filteredNlpCbModels = _nlpCbModelRepository.GetAll()
                        .Include(e => e.NlpChatbotFk)
                        .Include(e => e.NlpCbMTrainingCancellationUserFk)
                        .Include(e => e.NlpCbMCreatorUserFk)
                        .Where(e => e.NlpChatbotFk != null && e.NlpChatbotFk.Id == input.NlpChatbotId)
                        .WhereIf(input.NlpChatbotId.HasValue, e => e.NlpChatbotId == input.NlpChatbotId.Value);

            var pagedAndFilteredNlpCbModels = filteredNlpCbModels
                .OrderBy(input.Sorting ?? "NlpCbMCreationTime desc")
                .PageBy(input);

            var nlpCbModels = from o in pagedAndFilteredNlpCbModels
                              select new GetNlpCbModelForViewDto()
                              {
                                  NlpCbModel = new NlpCbModelDto
                                  {
                                      //NlpCbMLanguage = o.NlpCbMLanguage,
                                      NlpCbMStatus = o.NlpCbMStatus,

                                      NlpCbMTrainingStartTime = o.NlpCbMTrainingStartTime,
                                      NlpCbMTrainingCompleteTime = o.NlpCbMTrainingCompleteTime,
                                      NlpCbMTrainingCancellationTime = o.NlpCbMTrainingCancellationTime,
                                      NlpCbAccuracy = o.NlpCbAccuracy,
                                      Id = o.Id
                                  },
                                  NlpChatbotName = o.NlpChatbotFk.Name,
                                  NlpCbMTrainingCancellationUser = o.NlpCbMTrainingCancellationUserFk.Name,
                                  NlpCbMCreatorUser = o.NlpCbMCreatorUserFk.Name,
                                  NlpCbMCreationTime = o.NlpCbMCreationTime,
                              };

            var totalCount = await filteredNlpCbModels.CountAsync();

            return new PagedResultDto<GetNlpCbModelForViewDto>(
                totalCount, await nlpCbModels.ToListAsync()
            );
        }


        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpChatbots_Train)]
        [RemoteService(false)]
        public virtual async Task Create(CreateOrEditNlpCbModelDto input)
        {
            var nlpCbModel = ObjectMapper.Map<NlpCbModel>(input);

            if (AbpSession.TenantId != null)
            {
                nlpCbModel.TenantId = (int)AbpSession.TenantId;
            }

            nlpCbModel.NlpCbMStatus = NlpChatbotConsts.TrainingStatus.Queueing;
            nlpCbModel.NlpCbMInfo = L("NlpCbMInfoQueueing");
            await _nlpCbModelRepository.InsertAsync(nlpCbModel);
            await UnitOfWorkManager.Current.SaveChangesAsync();
        }

        [RemoteService(false)]
        public async Task DeleteQueueingModel(Guid chatbotId)
        {
            await _nlpCbModelRepository.GetAll()
           .Where(e => e.NlpChatbotId == chatbotId)
           .Where(e => e.NlpCbMTrainingCancellationTime == null && e.NlpCbMTrainingCompleteTime == null ||
           e.NlpCbMStatus == NlpChatbotConsts.TrainingStatus.Queueing)
           .DeleteFromQueryAsync();
        }


        [RemoteService(false)]
        public async Task CancelChatbotTrainingActivities(Guid chatbotId)
        {
            await CancelChatbotTrainingActivities(chatbotId, AbpSession.UserId);
        }

        protected async Task CancelChatbotTrainingActivities(Guid chatbotId, long? CancellationUserId)
        {
            try
            {
                NlpCbRequestTrainingResult result = await _nlpCBServiceClient.CancelTrainingAsync(chatbotId);
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex.ToString(), ex);
            }

            int nCancelled = await _nlpCbModelRepository.GetAll()
           .Where(e => e.NlpChatbotId == chatbotId)
           .Where(e => e.NlpCbMTrainingCancellationTime == null && e.NlpCbMTrainingCompleteTime == null ||
           e.NlpCbMStatus == NlpChatbotConsts.TrainingStatus.Training)
           .UpdateFromQueryAsync(e => new NlpCbModel()
           {
               NlpCbMStatus = NlpChatbotConsts.TrainingStatus.Cancelled,
               NlpCbMTrainingCancellationTime = Clock.Now,
               NlpCbMTrainingCancellationUserId = CancellationUserId,
               NlpCbMInfo = "L:NlpCbMInfoCancelled",
           });

            await _nlpCbTrainingDataRepository.GetAll()
                .Where(e => e.NlpChatbotId == chatbotId)
                .UpdateFromQueryAsync(e => new NlpCbTrainingData()
                {
                    NlpCbTDSource = null,
                });

            if (nCancelled == 0)
                return;

            var tenantId = _nlpChatbotFunction.GetChatbotDto(chatbotId)?.TenantId;
            if (tenantId.HasValue == false)
                return;

            await _appNotifier.TrainedModelChanged(tenantId, L("TrainedModelChanged_Cancelled"), NotificationSeverity.Error);
        }


        [AbpAllowAnonymous]
        [RemoteService(false)]
        public void CompleteTraining(NlpCbMCompleteTrainingInputDto input)
        {
            var trainingData = _nlpCbTrainingDataRepository.FirstOrDefault(e => e.NlpChatbotId == input.ChatbotId);

            var debugging = JsonConvert.SerializeObject(input);
            Logger.Info("CompleteTraining: " + debugging);


            //Debug.Assert(trainingData != null);
            if (trainingData == null)
            {
                Logger.Fatal(L("AnErrorOccurredMsg1", "NlpCbModelsAppService.CompleteTraining(): trainingData not found"));
                throw new UserFriendlyException(L("AnErrorOccurredMsg1", "NlpCbModelsAppService.CompleteTraining(): trainingData not found"));
            }


            //var nlpCbModel = _nlpCbModelRepository.GetAll()
            //    .Where(e => e.NlpChatbotId == trainingData.NlpChatbotId && e.NlpCbMTrainingCancellationTime == null)
            //    .OrderByDescending(e => e.NlpCbMCreationTime).FirstOrDefault();

            var nlpCbModel = _nlpCbModelRepository.FirstOrDefault(
                    e => e.NlpChatbotId == input.ChatbotId &&
                    e.NlpCbMTrainingCancellationTime == null &&
                    e.NlpCbMTrainingCompleteTime == null &&
                    e.NlpCbMStatus == NlpChatbotConsts.TrainingStatus.Training);

            Debug.Assert(nlpCbModel != null);
            if (nlpCbModel == null)
            {
                throw new UserFriendlyException(UserFriendlyExceptionCode.Code("NlpCbTrainingStatusError"), "L:NlpChatbotStatusError");
            }

            NlpCbMSourceData nlpCbMSourceData = JsonConvert.DeserializeObject<NlpCbMSourceData>(trainingData.NlpCbTDSource);

            var modelQADictionary = nlpCbMSourceData.QaSets;
            var maxNNID = modelQADictionary.Keys.Max();
            var countNNID = modelQADictionary.Keys.Count();
            if (maxNNID > countNNID * 2)
            {
                Logger.Fatal(L("AnErrorOccurredMsg1", "NlpCbModelsAppService.CompleteTraining(): Invalid NNID "));
                throw new UserFriendlyException(L("AnErrorOccurredMsg1", "NlpCbModelsAppService.CompleteTraining(): Invalid NNID"));
            }

            var nlpTrainedAnswerDictionary = _nlpCbTrainedAnswerRepository.GetAll()
                .Where(e => e.NlpCbTrainingDataId == trainingData.Id).ToDictionary(e => e.NNID);

            //update NlpCbMTrainedAnswer
            for (int n = 0; n <= maxNNID; n++)
            {
                var modelFound = modelQADictionary.ContainsKey(n);      //新訓煉的模型
                var trainedFound = nlpTrainedAnswerDictionary.ContainsKey(n);   //舊訓煉的模型

                if (modelFound && trainedFound)
                    nlpTrainedAnswerDictionary[n].NlpCbTAAnswer = JsonConvert.SerializeObject(modelQADictionary[n].a);
                else if (modelFound && !trainedFound)
                {
                    _nlpCbTrainedAnswerRepository.Insert(new NlpCbTrainedAnswer()
                    {
                        NlpCbTAAnswer = JsonConvert.SerializeObject(modelQADictionary[n].a),
                        CreationTime = Clock.Now,
                        NlpCbTrainingDataId = trainingData.Id,
                        NNID = n,
                        TenantId = trainingData.TenantId
                    });
                }
                else if (!modelFound && trainedFound)
                {
                    _nlpCbTrainedAnswerRepository.Delete(nlpTrainedAnswerDictionary[n].Id);
                }
            }

            // update NlpNNIDRepetition
            //_nlpLRUCacheHelper.SetLruCacheObject(input.NnidRepeated, input.ChatbotId.ToString(), "NlpNNIDRepetition");
            _cacheManager.Set_NlpNNIDRepetition(input.ChatbotId, input.NnidRepeated);

            UpdateNlpNNIDRepetition(input.ChatbotId, input.NnidRepeated);

            //remove cache
            //_nlpLRUCacheHelper.RemoveLruCacheObjectGroup(input.ChatbotId.ToString(), "nnid:");
            _cacheManager.Clear_NlpQADtoFromNNID(input.ChatbotId);

            _cacheManager.Clear_ChatbotPredict(input.ChatbotId);

            //var cbModel = _nlpCbModelRepository
            //    .FirstOrDefault(e => e.NlpChatbotId == input.ChatbotId &&
            //    e.NlpCbMTrainingCancellationTime == null && e.NlpCbMTrainingCompleteTime == null);

            nlpCbModel.NlpCbMStatus = NlpChatbotConsts.TrainingStatus.Trained;
            nlpCbModel.NlpCbMTrainingCompleteTime = Clock.Now;
            nlpCbModel.NlpCbMInfo = "L:NlpCbMInfoCompleted";
            nlpCbModel.NlpCbAccuracy = input.ModelAccuracy * 100;

            //if (nlpCbModel != null)
            //{
            //    nlpCbModel.NlpCbMStatus = NlpChatbotConsts.TrainingStatus.Trained;
            //    nlpCbModel.NlpCbMTrainingCompleteTime = Clock.Now;
            //    nlpCbModel.NlpCbMInfo = "L:NlpCbMInfoCompleted";
            //    nlpCbModel.NlpCbAccuracy = input.ModelAccuracy * 100;
            //}
            //else
            //{
            //    Debug.Assert(nlpCbModel != null);
            //    return;
            //}

            //int countCompleted =
            // _nlpCbModelRepository.GetAll()
            //.Where(e => e.NlpChatbotId == input.ChatbotId)
            //.Where(e => e.NlpCbMTrainingCancellationTime == null && e.NlpCbMTrainingCompleteTime == null)
            //.UpdateFromQuery(e => new NlpCbModel()
            //{
            //    NlpCbMStatus = NlpChatbotConsts.TrainingStatus.Trained,
            //    NlpCbMTrainingCompleteTime = Clock.Now,
            //    NlpCbMInfo = "L:NlpCbMInfoCompleted",
            //    NlpCbAccuracy = input.ModelAccuracy * 100
            //});

#if !DEBUG
            //刪除訓練完資料庫內不必要的訓練資料
            _nlpCbTrainingDataRepository.GetAll()
                .Where(e => e.NlpChatbotId == input.ChatbotId)
                .UpdateFromQuery(e => new NlpCbTrainingData()
                {
                    NlpCbTDSource = null,
                });
#endif

            //Debug.Assert(countCompleted == 1);
            //if (countCompleted == 0)
            //    return;

            var tenantId = _nlpChatbotFunction.GetChatbotDto(input.ChatbotId)?.TenantId;

            if (tenantId.HasValue == false)
                return;

            try
            {
                if (input.NnidRepeated != null && input.NnidRepeated.Count > 0)
                {
                    var repeatedItems = input.NnidRepeated.Values.DistinctBy(x => JsonConvert.SerializeObject(x)).ToList();

                    foreach (var repeatedItem in repeatedItems)
                    {
                        var questions = string.Empty;

                        foreach (var nnid in repeatedItem)
                        {
                            var question = GetNlpQADtofromNNID(input.ChatbotId, nnid)?.Question;

                            if (!string.IsNullOrEmpty(question))
                            {
                                if (!string.IsNullOrEmpty(questions))
                                    questions = questions + " , " + question;
                                else
                                    questions = question;
                            }
                        }

                        AsyncHelper.RunSync(() => _appNotifier.TrainedModelChanged(tenantId, L("TrainedCompleted_NNIDRepeated0", questions), NotificationSeverity.Warn));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex.Message, ex);
            }

            //更新花費時間
            if (nlpCbModel.NlpCbMTrainingCompleteTime > nlpCbModel.NlpCbMTrainingStartTime)
            {
                try
                {
                    var timeSpan = nlpCbModel.NlpCbMTrainingCompleteTime.Value - nlpCbModel.NlpCbMTrainingStartTime.Value;

                    float modelAccu = (float)input.ModelAccuracy;
                    if (modelAccu <= 0f || modelAccu > 1f)
                        modelAccu = 1f;


                    _nlpChatbotFunction.GetRepository().GetAll().Where(e => e.Id == input.ChatbotId)
                        .UpdateFromQuery(e => new NlpChatbot()
                        {
                            TrainingCostSeconds = (int)timeSpan.TotalSeconds,
                        });

                    _nlpChatbotFunction.DeleteCache(input.ChatbotId);
                }
                catch (Exception ex)
                {
                    Logger.Fatal(ex.Message, ex);
                }
            }

            AsyncHelper.RunSync(() => _appNotifier.TrainedModelChanged(tenantId, L("TrainedModelChanged_Completed"), NotificationSeverity.Success));
        }


        private NlpQADto GetNlpQADtofromNNID(Guid chatbotId, int nnid)
        {
            Debug.Assert(chatbotId != Guid.Empty);

            var nlpQADto =
                (NlpQADto)_cacheManager.Get_NlpQADtoFromNNID(chatbotId, nnid)
                ??
               (NlpQADto)_cacheManager.Set_NlpQADtoFromNNID(chatbotId, nnid,
               ObjectMapper.Map<NlpQADto>(_nlpQARepository.FirstOrDefault(e => e.NlpChatbotId == chatbotId && e.NNID == nnid)));

            return nlpQADto;
        }

        [AbpAllowAnonymous]
        [RemoteService(false)]
        public void IncompleteTraining(NlpCbMIncompleteTrainingInputDto input)
        {
            var trainingData = _nlpCbTrainingDataRepository.FirstOrDefault(e => e.NlpChatbotId == input.ChatbotId);

            //Debug.Assert(trainingData != null);
            if (trainingData == null)
            {
                Logger.Fatal(L("AnErrorOccurredMsg1", "NlpCbModelsAppService.CompleteTraining(): trainingData not found"));
                throw new UserFriendlyException(L("AnErrorOccurredMsg1", "NlpCbModelsAppService.CompleteTraining(): trainingData not found"));
            }

            int countCompleted =
             _nlpCbModelRepository.GetAll()
            .Where(e => e.NlpChatbotId == input.ChatbotId)
            .Where(e => e.NlpCbMTrainingCancellationTime == null && e.NlpCbMTrainingCompleteTime == null)
            .UpdateFromQuery(e => new NlpCbModel()
            {
                NlpCbMStatus = NlpChatbotConsts.TrainingStatus.Failed,
                NlpCbMTrainingCompleteTime = Clock.Now,
                NlpCbMInfo = "L:NlpCbMInfoFailed",
            });


            _nlpCbTrainingDataRepository.GetAll()
                .Where(e => e.NlpChatbotId == input.ChatbotId)
                .UpdateFromQuery(e => new NlpCbTrainingData()
                {
                    NlpCbTDSource = null,
                });

            Debug.Assert(countCompleted == 1);
            if (countCompleted == 0)
                return;

            var tenantId = _nlpChatbotFunction.GetChatbotDto(input.ChatbotId)?.TenantId;

            if (tenantId.HasValue == false)
                return;

            AsyncHelper.RunSync(() => _appNotifier.TrainedModelChanged(tenantId, L("TrainedModelChanged_Failed"), NotificationSeverity.Fatal));
        }


        [AbpAllowAnonymous]
        [RemoteService(false)]
        public async Task IncompleteTrainingByPreemption(NlpCbMIncompleteTrainingInputDto input)
        {
            var model = await _nlpCbModelRepository
                .FirstOrDefaultAsync(e => e.NlpChatbotId == input.ChatbotId &&
                e.NlpCbMTrainingStartTime != null && e.NlpCbMTrainingCancellationTime == null && e.NlpCbMTrainingCompleteTime == null);

            model.NlpCbMStatus = NlpChatbotConsts.TrainingStatus.Queueing;

            AsyncHelper.RunSync(() => _appNotifier.TrainedModelChanged(model.TenantId, L("NlpCbMInfoQueueing"), NotificationSeverity.Info));
        }


        [AbpAllowAnonymous]
        [RemoteService(false)]
        public async Task RestartAllOnTrainingModelAsync(bool cancelModel = false)
        {
            var modelQueryable = _nlpCbModelRepository.GetAll()
                .Where(e => e.NlpCbMTrainingStartTime != null && e.NlpCbMTrainingCancellationTime == null && e.NlpCbMTrainingCompleteTime == null);

            var modelTenantIdList = await modelQueryable.Select(e => e.TenantId).Distinct().ToListAsync();

            if (modelTenantIdList.Any())
            {
                if (cancelModel == true)
                {
                    await modelQueryable.UpdateFromQueryAsync(e => new NlpCbModel()
                    {
                        NlpCbMStatus = NlpChatbotConsts.TrainingStatus.Failed,
                        NlpCbMTrainingCompleteTime = Clock.Now,
                        NlpCbMInfo = "L:NlpCbMInfoFailed",
                    });

                    var chatbots = await modelQueryable.Select(e => e.NlpChatbotId).Distinct().ToListAsync();

                    await _nlpCbTrainingDataRepository.GetAll().Where(
                        e => e.NlpCbTDSource != null && chatbots.Contains(e.NlpChatbotId))
                        .UpdateFromQueryAsync(e => new NlpCbTrainingData()
                        {
                            NlpCbTDSource = null,
                        });
                }
                else
                {
                    await modelQueryable.UpdateFromQueryAsync(e => new NlpCbModel()
                    {
                        NlpCbMStatus = NlpChatbotConsts.TrainingStatus.Queueing,
                        //NlpCbMTrainingCompleteTime = Clock.Now,
                        //NlpCbMInfo = "L:NlpCbMInfoFailed",
                    });
                }
            }

            //保留資料給Python程式訓練
            //111111111111111111111
            //_nlpCbTrainingDataRepository.GetAll().Where(
            //    e => e.NlpCbTDSource != null)
            //    .UpdateFromQuery(e => new NlpCbTrainingData()
            //    {
            //        NlpCbTDSource = null,
            //    });
            //111111111111111111111111

            await UnitOfWorkManager.Current.SaveChangesAsync();

            if (cancelModel == true)
            {

                foreach (var tenantId in modelTenantIdList)
                {
                    AsyncHelper.RunSync(() => _appNotifier.TrainedModelChanged(tenantId, L("TrainedModelChanged_Failed"), NotificationSeverity.Fatal));
                }
            }

        }

        void UpdateNlpNNIDRepetition(Guid chatbotId, Dictionary<int, int[]> nlpNNIDRepetition)
        {
            var data = _nlpCbTrainingDataRepository.FirstOrDefault(e => e.NlpChatbotId == chatbotId);

            if (data != null)
                data.NlpNNIDRepetition = JsonConvert.SerializeObject(nlpNNIDRepetition);
        }


        /// <summary>
        /// 是否需要訓練，檢查QA是否比Model新
        /// </summary>
        /// <param name="chatbotId"></param>
        /// <returns></returns>
        [RemoteService(false)]
        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpCbModels, AppPermissions.Pages_NlpChatbot_NlpChatbots, AppPermissions.Pages_NlpChatbot_NlpQAs)]
        [DisableAuditing]
        public async Task<NlpChatbotTrainingStatus> GetChatbotTrainingStatus(Guid chatbotId, bool reEnter = false)
        {
            var tenantId = _nlpChatbotFunction.GetTenantId(chatbotId);

            DateTime? maxQaTime;
            DateTime? maxWorkflowTime;
            DateTime? maxWorkflowStateTime;

            DateTime lastTime = DateTime.UtcNow.AddMonths(-2);

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.SoftDelete))
            {
                maxQaTime = await _nlpQARepository.GetAll()
               .Where(e => e.NlpChatbotId == chatbotId && (e.DeletionTime > lastTime || e.CreationTime > lastTime || e.LastModificationTime > lastTime))
               .MaxAsync(q => q.DeletionTime != null ? q.DeletionTime : (q.LastModificationTime != null ? q.LastModificationTime : q.CreationTime));

                maxWorkflowTime = await _nlpWorkflowRepository.GetAll()
                   .Where(e => e.NlpChatbotId == chatbotId && (e.DeletionTime > lastTime || e.CreationTime > lastTime || e.LastModificationTime > lastTime))
                   .MaxAsync(q => q.DeletionTime != null ? q.DeletionTime : (q.LastModificationTime != null ? q.LastModificationTime : q.CreationTime));

                maxWorkflowStateTime = await _nlpWorkflowStateRepository.GetAll()
                   .Include(e => e.NlpWorkflowFk)
                   .Where(e => e.NlpWorkflowFk.NlpChatbotId == chatbotId && (e.DeletionTime > lastTime || e.CreationTime > lastTime || e.LastModificationTime > lastTime))
                   .MaxAsync(q => q.DeletionTime != null ? q.DeletionTime : (q.LastModificationTime != null ? q.LastModificationTime : q.CreationTime));
            }

            maxQaTime ??= DateTime.MinValue;
            maxWorkflowTime ??= DateTime.MinValue;
            maxWorkflowStateTime ??= DateTime.MinValue;

            DateTime maxTime = new DateTime[] { maxQaTime.Value, maxWorkflowTime.Value, maxWorkflowStateTime.Value }.Max();

            var nlpCbModel = await _nlpCbModelRepository.GetAll()
                .Where(e => e.NlpChatbotId == chatbotId && e.NlpCbMTrainingCancellationTime == null).OrderByDescending(e => e.NlpCbMCreationTime).FirstOrDefaultAsync();

            if (nlpCbModel == null)
                return new NlpChatbotTrainingStatus()
                {
                    TrainingStatus = NlpChatbotConsts.TrainingStatus.NotTraining,
                };

            if (nlpCbModel.NlpCbMStatus != NlpChatbotConsts.TrainingStatus.Queueing && nlpCbModel.NlpCbMStatus != NlpChatbotConsts.TrainingStatus.Training)
            {
                DateTime? maxModelCompleteTime = await _nlpCbModelRepository.GetAll()
                                .Where(e => e.NlpChatbotId == chatbotId && e.NlpCbMTrainingCancellationTime == null)
                                .MaxAsync(m => m.NlpCbMTrainingCompleteTime);
                if (maxModelCompleteTime == null)
                    return new NlpChatbotTrainingStatus()
                    {
                        TrainingStatus = NlpChatbotConsts.TrainingStatus.NotTraining,
                    };

                if (nlpCbModel.NlpCbMTrainingCompleteTime != null && maxTime > nlpCbModel.NlpCbMTrainingCompleteTime.Value)
                    return new NlpChatbotTrainingStatus()
                    {
                        TrainingStatus = NlpChatbotConsts.TrainingStatus.RequireRetraining,
                    };
            }

            //如果訓練10分鐘還未完成，取消訓練，已無用處
            //if (nlpCbModel.NlpCbMStatus == NlpChatbotConsts.TrainingStatus.Training &&
            //    nlpCbModel.NlpCbMTrainingStartTime.Value.AddMinutes(10) < Clock.Now)
            //{
            //    CancelChatbotTrainingActivities(nlpCbModel.NlpChatbotId, null);
            //    if (reEnter == false)
            //        return await GetChatbotTrainingStatus(chatbotId, true);
            //}

            var nlpChatbotTrainingStatus = new NlpChatbotTrainingStatus()
            {
                TrainingProgress = 0,
                QueueRemaining = 0,
                TrainingRemaining = 0,
                //TrainingSpent = 0,
                TrainingStatus = nlpCbModel.NlpCbMStatus
            };

            if (nlpChatbotTrainingStatus.TrainingStatus == NlpChatbotConsts.TrainingStatus.Queueing || nlpChatbotTrainingStatus.TrainingStatus == NlpChatbotConsts.TrainingStatus.Training)
            {
                var waitingStatus = NlpTrainingSchedulerWorker.GetWaitingStatus(chatbotId);
                nlpChatbotTrainingStatus.TrainingProgress = (int)Math.Floor(100.0 * waitingStatus.TrainingProgress);
                //nlpChatbotTrainingStatus.TrainingSpent = (int)Math.Round(waitingStatus.TrainingSpent.TotalSeconds);
                nlpChatbotTrainingStatus.TrainingRemaining = (int)Math.Round(waitingStatus.TrainingRemaining.TotalSeconds);
                nlpChatbotTrainingStatus.QueueRemaining = (int)Math.Round(waitingStatus.QueueRemaining.TotalSeconds);

                NlpTrainingSchedulerWorker.EnableCheck = true;
            }

            return nlpChatbotTrainingStatus;
        }

        [AbpAllowAnonymous]
        [RemoteService(false)]
        public async Task<NlpCbMTrainingDataDTO> RequestTraining()
        {
            await RestartAllOnTrainingModelAsync(false);

            DateTime dt7d = Clock.Now.AddDays(-7);

            //var modelTrainingRepository = _nlpCbModelRepository.GetAll()
            //    .Where(e => e.NlpCbMStatus == NlpChatbotConsts.TrainingStatus.Training && e.NlpCbMCreationTime >= dt7d);
            var modelQueueingRepository = _nlpCbModelRepository.GetAll()
                .Where(e => e.NlpCbMStatus == NlpChatbotConsts.TrainingStatus.Queueing && e.NlpCbMCreationTime >= dt7d);
            var tenantRepository = _tenantRepository.GetAll();
            var trainingDataRepository = _nlpCbTrainingDataRepository.GetAll();

            //Model.NlpCbMCandidate candidate = null;

            //DateTime dt30m = Clock.Now.AddMinutes(-30);
            ////取得30分鐘內在學習中但未回應的七日內要訓練的模型
            //candidate = (from m in modelTrainingRepository
            //             join t in tenantRepository on m.TenantId equals t.Id
            //             join td in trainingDataRepository on m.NlpChatbotId equals td.NlpChatbotId into jtd
            //             from std in jtd.DefaultIfEmpty()
            //             where m.NlpCbMCreationTime < dt30m
            //             orderby t.NlpPriority
            //             select new Model.NlpCbMCandidate()
            //             {
            //                 ChatbotTenantId = m.TenantId,
            //                 ChatbotId = m.NlpChatbotId,
            //                 Language = m.NlpCbMLanguage,
            //                 ModelId = m.Id,
            //                 TrainingData = std.NlpCbTDSource,
            //                 TrainingStatus = NlpChatbotConsts.TrainingStatus.Training,
            //                 RebuildModel = m.Rebuild
            //             }).FirstOrDefault();

            //取得優先權最高(值最低)的, 及最早建立的
            //if (candidate == null)
            List<int> tenantList = _trainingModelTenantIdBags.ToList();

            var candidate = await (from m in modelQueueingRepository
                             join t in tenantRepository on m.TenantId equals t.Id
                             join td in trainingDataRepository on m.NlpChatbotId equals td.NlpChatbotId into jtd
                             from std in jtd.DefaultIfEmpty()
                             where string.IsNullOrEmpty(std.NlpCbTDSource) == false
                             orderby !tenantList.Contains(m.TenantId), t.NlpPriority, m.NlpCbMCreationTime
                             //orderby t.NlpPriority, m.NlpCbMCreationTime
                             select new Model.NlpCbMCandidate()
                             {
                                 ChatbotTenantId = m.TenantId,
                                 ChatbotId = m.NlpChatbotId,
                                 Language = m.NlpCbMLanguage,
                                 ModelId = m.Id,
                                 TrainingData = std.NlpCbTDSource,
                                 TrainingStatus = NlpChatbotConsts.TrainingStatus.Queueing,
                                 RebuildModel = m.Rebuild
                             }).FirstOrDefaultAsync();

            if (candidate == null || string.IsNullOrEmpty(candidate.TrainingData))
                return new NlpCbMTrainingDataDTO() { Status = "NO_TRAINING_DATA" };

            _trainingModelTenantIdBags.Append(candidate.ChatbotTenantId);
            while (_trainingModelTenantIdBags.Count > 10)
            {
                _trainingModelTenantIdBags.TryDequeue(out int tenantId);
            }


            NlpCbMTrainingDataDTO nlpCbMTrainingDataDTO = new NlpCbMTrainingDataDTO()
            {
                Status = "00000000",
                Language = candidate.Language,
                ModelId = candidate.ModelId,
                SourceData = JsonConvert.DeserializeObject<NlpCbMSourceData>(candidate.TrainingData),
                RebuildModel = true,//candidate.RebuildModel
                IsFreeEdition = await IsFreeEdition(candidate.ChatbotTenantId)
            };

            foreach (var qas in nlpCbMTrainingDataDTO.SourceData.QaSets.Values)
                qas.a = null;


            int rows = await _nlpCbModelRepository.GetAll()
               .Where(e => e.Id == candidate.ModelId)
               .UpdateFromQueryAsync(e => new NlpCbModel()
               {
                   NlpCbMStatus = NlpChatbotConsts.TrainingStatus.Training,
                   NlpCbMTrainingStartTime = Clock.Now,
                   NlpCbMInfo = "L:NlpCbMInfoTraining",
               });

            Debug.Assert(rows == 1);
            if (rows != 1)
            {
                Logger.Fatal(L("AnErrorOccurredMsg1", $"NlpCbModelsAppService.RequestTraining(): nlpCbMTrainingData cannot be updated. rows({rows}), candidate.ModelId({candidate.ModelId})"));

                throw new UserFriendlyException(L("AnErrorOccurredMsg1", "nlpcbmtrainingdata cannot be updated"));
            }

            await _appNotifier.TrainedModelChanged(candidate.ChatbotTenantId, L("NlpCbMInfoTraining"), NotificationSeverity.Success);

            NlpTrainingSchedulerWorker.EnableCheck = true;

            return nlpCbMTrainingDataDTO;
        }

        [AbpAllowAnonymous]
        [RemoteService(false)]
        public async Task<NlpCbMTrainingDataDTO> RequestTrainingTest()     //for debugging
        {
            var modelRepository = _nlpCbModelRepository.GetAll();
            var tenantRepository = _tenantRepository.GetAll();
            var trainingDataRepository = _nlpCbTrainingDataRepository.GetAll();

            var candidate = await (from m in modelRepository
                             join t in tenantRepository on m.TenantId equals t.Id     //tenant是有效的，未被刪除的
                             join td in trainingDataRepository on m.NlpChatbotId equals td.NlpChatbotId
                             where string.IsNullOrEmpty(td.NlpCbTDSource) == false
                             orderby m.NlpCbMCreationTime descending
                             select new Model.NlpCbMCandidate()
                             {
                                 ChatbotTenantId = m.TenantId,
                                 ChatbotId = m.NlpChatbotId,
                                 Language = m.NlpCbMLanguage,
                                 ModelId = m.Id,
                                 TrainingData = td.NlpCbTDSource,
                                 TrainingStatus = NlpChatbotConsts.TrainingStatus.Training,
                                 RebuildModel = m.Rebuild
                             }).FirstOrDefaultAsync();

            if (candidate == null)
                return new NlpCbMTrainingDataDTO() { Status = "NO_TRAINING_DATA" };

            NlpCbMTrainingDataDTO nlpCbMTrainingDataDTO = new NlpCbMTrainingDataDTO()
            {
                Status = "00000000",      //tell python's program that this requestion is success.
                ModelId = candidate.ModelId,
                SourceData = JsonConvert.DeserializeObject<NlpCbMSourceData>(candidate.TrainingData),
                Language = candidate.Language,
                RebuildModel = candidate.RebuildModel,
                IsFreeEdition = await IsFreeEdition(candidate.ChatbotTenantId)
            };

            foreach (var qas in nlpCbMTrainingDataDTO.SourceData.QaSets.Values)
            {
                qas.a = null;
            }

            await _appNotifier.TrainedModelChanged(candidate.ChatbotTenantId, L("NlpCbMInfoTraining"), NotificationSeverity.Success);

            NlpTrainingSchedulerWorker.EnableCheck = true;

            return nlpCbMTrainingDataDTO;
        }

        /// <summary>
        /// 關鍵字同義字轉換，轉成Training
        /// </summary>
        /// <param name="cbmData"></param>
        /// <returns></returns>
        //private NlpCbMSourceData Synonymized(NlpCbMSourceData cbmData, int chatbotTenantId, Guid chatbotId)
        //{
        //    //Key: (流程狀態, 問題)
        //    Dictionary<(Guid, string), string> mapWfStateQuestions = new Dictionary<(Guid, string), string>();
        //    foreach (var qaSet in cbmData.QaSets)
        //    {
        //        if (qaSet.Value.q != null)
        //        {
        //            //create key
        //            foreach (var q in qaSet.Value.q)
        //                mapWfStateQuestions[(qaSet.Value.s == null ? Guid.Empty : qaSet.Value.s.Value, q)] = null;
        //        }
        //    }

        //    var arrayWfStateQuestions = mapWfStateQuestions.Keys.ToArray();

        //    //var result = _nlpCbDictionariesFunction.PrepareSynonymString(chatbotTenantId, chatbotId, arrayWfStateQuestions);

        //    for (int n = 0; n < result.sourceString.Length; n++)
        //        mapWfStateQuestions[result.sourceString[n]] = result.synonymString[n];

        //    foreach (var qaSet in cbmData.QaSets)
        //    {
        //        if (qaSet.Value.q != null)
        //        {
        //            for (int n = 0; n < qaSet.Value.q.Length; n++)
        //                qaSet.Value.q[n] = mapWfStateQuestions[(qaSet.Value.s == null ? Guid.Empty : qaSet.Value.s.Value, qaSet.Value.q[n])];
        //        }
        //    }

        //    cbmData.wordVectorDictionary = result.wordVectorDictionary;

        //    return cbmData;
        //}

        private async Task<bool> IsFreeEdition(int tenantId)
        {
            var isFree = await TenantManager.IsFreeEdition(tenantId);

            return isFree;
        }
    }
}
