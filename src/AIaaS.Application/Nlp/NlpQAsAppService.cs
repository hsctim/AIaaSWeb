using AIaaS.Nlp;
using System.Collections.Generic;

using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using Abp.Linq.Extensions;

using System.Threading.Tasks;
using Abp.Domain.Repositories;
//using AIaaS.Nlp.Exporting;
using AIaaS.Nlp.Dtos;
using AIaaS.Dto;
using Abp.Application.Services.Dto;
using AIaaS.Authorization;
using Abp.Extensions;
using Abp.Authorization;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Abp.Application.Services;
using AIaaS.Nlp.Lib;
using AIaaS.Nlp.Importing;
using AIaaS.Nlp.Importing.Dtos;
using System.Diagnostics;
using Abp.Domain.Uow;
using System.Text;
using AIaaS.Nlp.Dtos.NlpQA;
using Abp.UI;
using Abp.Logging;
using AIaaS.Helpers;
using Abp.Auditing;
using Abp.Runtime.Caching;
using ApiProtectorDotNet;
using AIaaS.Features;
using AIaaS.License;
using Abp.EntityFrameworkCore.EFPlus;
using Microsoft.AspNetCore.Mvc;
using Abp.AspNetZeroCore.Net;
using System.IO;
using AIaaS.Storage;
using AIaaS.Nlp.Dtos.NlpChatbot;
using AIaaS.MultiTenancy;

namespace AIaaS.Nlp
{
    [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpQAs)]
    public class NlpQAsAppService : AIaaSAppServiceBase, INlpQAsAppService
    {
        private readonly TenantManager _tenantManager;
        private readonly IRepository<NlpQA, Guid> _nlpQARepository;
        private readonly IRepository<NlpChatbot, Guid> _lookup_nlpChatbotRepository;
        private readonly NlpCbWebApiClient _nlpCBServiceClient;
        private readonly INlpPolicyAppService _nlpPolicyAppService;
        private readonly NlpCbSession _nlpCbSession;
        private readonly NlpChatbotFunction _nlpChatbotFunction;
        //private readonly NlpCbDictionariesFunction _nlpCbDictionariesFunction;
        private readonly ICacheManager _cacheManager;
        //private readonly INlpQAsExcelExporter _nlpQAsExcelExporter;
        private readonly IRepository<NlpWorkflowState, Guid> _nlpWorkflowState;
        private readonly ITempFileCacheManager _tempFileCacheManager;



        public NlpQAsAppService(
            TenantManager tenantManager,
            IRepository<NlpQA, Guid> nlpQARepository,
            IRepository<NlpChatbot, Guid> lookup_nlpChatbotRepository,
            NlpCbWebApiClient nlpCBServiceClient,
            INlpPolicyAppService nlpPolicyAppService,
            NlpCbSession nlpCbSession,
            NlpChatbotFunction nlpChatbotFunction,
            //NlpCbDictionariesFunction nlpCbDictionariesFunction,
            ICacheManager cacheManager,
            //INlpQAsExcelExporter nlpQAsExcelExporter,
            IRepository<NlpWorkflowState, Guid> nlpWorkflowState,
            ITempFileCacheManager tempFileCacheManager
            )
        {
            _tenantManager = tenantManager;
            _nlpQARepository = nlpQARepository;
            _lookup_nlpChatbotRepository = lookup_nlpChatbotRepository;
            _nlpCBServiceClient = nlpCBServiceClient;
            _nlpPolicyAppService = nlpPolicyAppService;
            _nlpCbSession = nlpCbSession;
            _nlpChatbotFunction = nlpChatbotFunction;
            //_nlpCbDictionariesFunction = nlpCbDictionariesFunction;
            _cacheManager = cacheManager;
            //_nlpQAsExcelExporter = nlpQAsExcelExporter;
            _nlpWorkflowState = nlpWorkflowState;
            _tempFileCacheManager = tempFileCacheManager;
        }


        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<PagedResultDto<GetNlpQAForViewDto>> GetAll(GetAllNlpQAsInput input)
        {
            if (input.MaxResultCount > AppConsts.MaxPageSize)
                throw new UserFriendlyException(L("Exception"));

            _nlpCbSession["ChatbotId"] = input.NlpChatbotGuidFilter;

            if (input.CategoryFilter.IsNullOrEmpty())
                _nlpCbSession["Category"] = "";
            else
                _nlpCbSession["Category"] = input.CategoryFilter;

            input.Filter = input.Filter?.Trim();
            input.CategoryFilter = input.CategoryFilter?.Trim();

            var filteredNlpQAs = _nlpQARepository.GetAll()
                        .Include(e => e.NlpChatbotFk)
                        .Include(e => e.CurrentWfStateFk).ThenInclude(e => e.NlpWorkflowFk)
                        .Include(e => e.NextWfStateFk).ThenInclude(e => e.NlpWorkflowFk)
                        .Include(e => e.CurrentWfAllFk)
                        .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), e => false ||
                        e.Question.Contains(input.Filter) || e.Answer.Contains(input.Filter) ||
                        e.CurrentWfStateFk.StateName.Contains(input.Filter) || e.CurrentWfStateFk.NlpWorkflowFk.Name.Contains(input.Filter) ||
                        e.NextWfStateFk.StateName.Contains(input.Filter) || e.NextWfStateFk.NlpWorkflowFk.Name.Contains(input.Filter) ||
                        e.CurrentWfAllFk.Name.Contains(input.Filter)
                        )
                        .WhereIf(!string.IsNullOrWhiteSpace(input.CategoryFilter), e => e.QuestionCategory.Contains(input.CategoryFilter))
                        .Where(e => e.NlpChatbotFk != null && e.NlpChatbotFk.Id.ToString() == input.NlpChatbotGuidFilter && e.QaType == NlpQAConsts.QaType_Answerable);

            if (input.Sorting.IsNullOrEmpty() == false && input.Sorting.IndexOf("workflow", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (input.Sorting.IndexOf("asc", StringComparison.OrdinalIgnoreCase) >= 0)
                    input.Sorting = "currentWfStateFk.nlpWorkflowFk.name asc, currentWfStateFk.stateName asc , nextWfStateFk.nlpWorkflowFk.name asc , nextWfStateFk.stateName asc";
                else
                    input.Sorting = "currentWfStateFk.nlpWorkflowFk.name desc, currentWfStateFk.stateName desc , nextWfStateFk.nlpWorkflowFk.name desc , nextWfStateFk.stateName desc";
            }

            var pagedAndFilteredNlpQAs = filteredNlpQAs
                .OrderBy(input.Sorting ?? "id asc")
                .PageBy(input);

            var nlpQAs = from o in pagedAndFilteredNlpQAs
                         select new GetNlpQAForViewDto()
                         {
                             NlpQA = new NlpQADataTableDto
                             {
                                 Question = o.Question,
                                 Answer = o.Answer,
                                 QuestionCategory = o.QuestionCategory.Replace(":", " : "),
                                 Id = o.Id,
                                 QaType = o.QaType,
                                 CurrentWf = o.CurrentWfStateFk.NlpWorkflowFk.Name + o.CurrentWfAllFk.Name,
                                 CurrentWfState = o.CurrentWfStateFk.StateName,
                                 NextWfState = o.NextWfStateFk.StateName,
                                 NextWf = o.NextWfStateFk.NlpWorkflowFk.Name,
                                 NextWfStateId = (o.NextWfState == null ? NlpWorkflowStateConsts.WfsKeepCurrent : o.NextWfState.Value)
                             },
                         };

            var totalCount = await filteredNlpQAs.CountAsync();

            return new PagedResultDto<GetNlpQAForViewDto>(totalCount, await nlpQAs.ToListAsync());
        }

        /// <summary>
        /// Export To File , Mark*******************************************************
        /// </summary>
        /// <param name="chatbotId"></param>
        /// <returns></returns>

        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpQAs_Export)]
        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<FileDto> GetNlpQAsToFile(Guid chatbotId)
        {
            var chatbot = _nlpChatbotFunction.GetChatbotDto(chatbotId);
            if (chatbot == null || chatbot.TenantId != AbpSession.TenantId.Value || chatbot.IsDeleted)
                throw new UserFriendlyException(L("ChatbotDoesNotExists"));

            var exportData = new ExportedImportedQaData();

            var QAs = _nlpQARepository.GetAll()
                .Include(e => e.CurrentWfStateFk).ThenInclude(e => e.NlpWorkflowFk)
                .Include(e => e.NextWfStateFk).ThenInclude(e => e.NlpWorkflowFk)
                .Include(e => e.CurrentWfAllFk)
                .Where(e => e.NlpChatbotId == chatbotId && e.QaType != NlpQAConsts.QaType_Unanswerable)
                ;

            var QaDataQueryList = await (from o in QAs
                              select new
                              {
                                  QuestionCategory = o.QuestionCategory,
                                  Question = o.Question,
                                  Answer = o.Answer,
                                  NNID = o.NNID,
                                  CurrentWfAll = o.CurrentWfAllFk,
                                  CurrentWfState = o.CurrentWfStateFk,
                                  NextWfState = o.NextWfStateFk,
                                  QaType = o.QaType,
                              }).ToListAsync();

            exportData.QAList = (from o in QaDataQueryList.OrderBy(e => e.QuestionCategory).ThenBy(e => e.NNID)
                                       select new ExportedImportedChatbotQAData()
                                       {
                                           Answer = o.Answer,
                                           QaType = o.QaType,
                                           Question = o.Question,
                                           QuestionCategory = o.QuestionCategory,
                                           currentWfsUuid = o.CurrentWfState?.Id,
                                           nextWfsUuid = o.NextWfState?.Id,
                                       }).ToList();

            var wfList = (from o in (from q in QaDataQueryList
                                           where q.CurrentWfState != null && q.CurrentWfState.NlpWorkflowFk != null && string.IsNullOrEmpty(q.CurrentWfState.NlpWorkflowFk.Name) == false
                                     select q.CurrentWfState.NlpWorkflowFk)
                          select new WorkflowCollection
                          {
                              WfId = o.Id,
                              WfNo = 0,
                              WfName = o.Name,
                              Disabled = o.Disabled
                          }).GroupBy(i => i.WfId).Select(i => i.FirstOrDefault()).ToList();

            int i = 0;
            foreach (var wf in wfList)
                wf.WfNo = i++;

            var wfDictionary = wfList.ToDictionary(e => e.WfId);

            var wfsList = (from o in (from q in QaDataQueryList
                                      where q.CurrentWfState != null && string.IsNullOrEmpty(q.CurrentWfState.StateName) == false
                                      select q.CurrentWfState)
                        .UnionBy(from q in QaDataQueryList
                                 where q.NextWfState != null && string.IsNullOrEmpty(q.NextWfState.StateName)
                                 select q.NextWfState, q => q.Id)
                           select new WorkflowStateCollection
                           {
                               WfId = o.NlpWorkflowFk.Id,
                               WfName = o.NlpWorkflowFk.Name,
                               Disabled = o.NlpWorkflowFk.Disabled,
                               WfsId = o.Id,
                               WfsName = o.StateName,
                               StateInstruction = o.StateInstruction,
                               OutgoingFalseOp = o.OutgoingFalseOp,
                               Outgoing3FalseOp = o.Outgoing3FalseOp,
                               ResponseNonWorkflowAnswer = o.ResponseNonWorkflowAnswer,
                               DontResponseNonWorkflowErrorAnswer = o.DontResponseNonWorkflowErrorAnswer,
                           }).ToList();

            i = 0;
            foreach (var wfsItem in wfsList)
            {
                wfsItem.WfNo = wfDictionary[wfsItem.WfId].WfNo;
                wfsItem.WfsNo = i++;
            }

            exportData.WorkflowList = (from o in wfList
                                       select new ExportedImportedChatbotWorkflowData
                                       {
                                           No = o.WfNo,
                                           WorkflowName = o.WfName,
                                           Disabled = o.Disabled,
                                           StatusList = (from s in wfsList
                                                         where s.WfId == o.WfId
                                                         select new ExportedImportedChatbotWorkflowStateData
                                                         {
                                                             DontResponseNonWorkflowErrorAnswer = s.DontResponseNonWorkflowErrorAnswer,
                                                             ResponseNonWorkflowAnswer = s.ResponseNonWorkflowAnswer,
                                                             No = s.WfsNo,
                                                             StateInstruction = s.StateInstruction,
                                                             StateName = s.WfsName,
                                                             Outgoing3FalseOp = s.Outgoing3FalseOp,
                                                             OutgoingFalseOp = s.OutgoingFalseOp
                                                         }
                                                         ).ToList(),
                                       }).ToList();



            var chatbotName = _nlpChatbotFunction.GetChatbotName(chatbotId);
            if (chatbotName.IsNullOrEmpty())
                chatbotName = "MyChatbot";

            var json = JsonConvert.SerializeObject(exportData);
            var file = new FileDto(chatbotName + "_QAs.json", MimeTypeNames.ApplicationJson);

            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new StreamWriter(memoryStream, System.Text.Encoding.UTF8))
                {
                    // Various for loops etc as necessary that will ultimately do this:
                    await writer.WriteAsync(json);
                    await writer.FlushAsync();
                    _tempFileCacheManager.SetFile(file.FileToken, memoryStream.ToArray());
                }
            }

            return file;

        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<GetNlpQAForEditOutput> GetNlpQAForEdit(EntityDto<Guid> input)
        {
            var nlpQA = await _nlpQARepository.FirstOrDefaultAsync(input.Id);

            var output = new GetNlpQAForEditOutput { NlpQA = ObjectMapper.Map<CreateOrEditNlpQADto>(nlpQA) };

            output.NlpQA.Questions = JsonConvert.DeserializeObject<List<string>>(nlpQA.Question ?? "");
                       
            try
            {
                output.NlpQA.AnswerSets = JsonConvert.DeserializeObject<List<CbAnswerSet>>(nlpQA.Answer ?? "");
            }
            catch (Exception)
            {
                var answers = JsonConvert.DeserializeObject <List<string>>(nlpQA.Answer ?? "");

                output.NlpQA.AnswerSets = answers.Select(e => new CbAnswerSet { Answer = e, GPT = false }).ToList();
            }

            if (output.NlpQA.NlpChatbotId != null)
            {
                var _lookupNlpChatbot = _nlpChatbotFunction.GetChatbotDto(output.NlpQA.NlpChatbotId.Value);
                output.NlpChatbotName = _lookupNlpChatbot?.Name?.ToString();
                output.NlpChatbotLanguage = _lookupNlpChatbot?.Language;
                output.NlpChatbotId = output.NlpQA.NlpChatbotId.Value;
            }

            return output;
        }


        //無效的QA，不進Training
        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpQAs)]

        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<GetNlpQAForEditOutput> DiscardNlpQAForEditAsync(EntityDto<Guid> chatbotId)
        {
            await CheckNNID0Async(chatbotId.Id);

            NlpQA nlpQA = await _nlpQARepository.FirstOrDefaultAsync(e => e.NlpChatbotId == chatbotId.Id && e.NNID == 0);
            var output = new GetNlpQAForEditOutput { NlpQA = ObjectMapper.Map<CreateOrEditNlpQADto>(nlpQA) };

            output.NlpQA ??= new CreateOrEditNlpQADto();

            List<string> questionsList = JsonConvert.DeserializeObject<List<string>>(nlpQA.Question ?? "");

            if (questionsList.Any() == false)
                questionsList.Add("");

            output.NlpQA.Questions = questionsList;

            output.NlpQA.AnswerSets = null;

            if (output.NlpQA.NlpChatbotId != null)
            {
                var _lookupNlpChatbot = _nlpChatbotFunction.GetChatbotDto(output.NlpQA.NlpChatbotId.Value);
                output.NlpChatbotName = _lookupNlpChatbot?.Name?.ToString();
                output.NlpChatbotLanguage = _lookupNlpChatbot?.Language;
                output.NlpChatbotId = output.NlpQA.NlpChatbotId.Value;
            }

            return output;
        }


        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpQAs_Create)]

        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task CreateOrEdit(CreateOrEditNlpQADto input)
        {
            await _CreateOrEdit(input);
        }


        //這個是不對外的
        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpQAs_Create, AppPermissions.Pages_NlpChatbot_NlpQAs_Import)]
        protected async Task _CreateOrEdit(CreateOrEditNlpQADto input)
        {
            if (input.CurrentWfState == null)
                input.CurrentWfState = NlpWorkflowStateConsts.WfsNull;

            if (input.NextWfState == null)
                input.NextWfState = NlpWorkflowStateConsts.WfsKeepCurrent;

            if (input.QaType == NlpQAConsts.QaType_Unanswerable)
            {
                input.CurrentWfState = NlpWorkflowStateConsts.WfsNull;
                input.NextWfState = NlpWorkflowStateConsts.WfsKeepCurrent;
            }


            if (input.Id == null)
            {
                await Create(input);
            }
            else
            {
                await Update(input);
            }
        }


        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpQAs_Create, AppPermissions.Pages_NlpChatbot_NlpQAs_Import)]
        protected async Task<NlpQA> Create(CreateOrEditNlpQADto input)
        {
            if (input.QaType != NlpQAConsts.QaType_Unanswerable)
            {
                if (input.Questions == null || input.Questions.Count() == 0 || input.AnswerSets == null || input.AnswerSets.Count() == 0)
                    throw new UserFriendlyException(L("InvalidFormMessage"));

                await _nlpPolicyAppService.CheckMaxQuestionCount((int)AbpSession.TenantId, input.NlpChatbotId.Value);
            }

            NlpQA nlpQA = ObjectMapper.Map<NlpQA>(input);

            if (AbpSession.TenantId != null)
            {
                nlpQA.TenantId = (int)AbpSession.TenantId;
            }

            nlpQA.NNID = GetNextNNID(input.NlpChatbotId.Value);

            var questions = input.Questions.Select(s => s.Trim()).Where(s => s.Length > 0);

            var answers = input.AnswerSets.Select(
                s => new CbAnswerSet()
                {
                    Answer = s.Answer.Trim(),
                    GPT = s.GPT
                }).Where(s => s?.Answer?.Length > 0).ToList();

            //var answers = input.Answers.Select(s => s.Trim()).Where(s => s.Length > 0);

            if (input.QaType != NlpQAConsts.QaType_Unanswerable && (questions.Count() == 0 || answers.Count() == 0))
                throw new UserFriendlyException(L("InvalidFormMessage"));

            nlpQA.Question = JsonConvert.SerializeObject(questions);
            nlpQA.Answer = (answers != null && answers.Any()) ? JsonConvert.SerializeObject(answers) : null;

            nlpQA.QuestionCount = input.QaType == NlpQAConsts.QaType_Unanswerable ? 0 : questions.Count();

            await _nlpQARepository.InsertAsync(nlpQA);
            await CurrentUnitOfWork.SaveChangesAsync();

            DeleteLRUCache(nlpQA.NlpChatbotId, nlpQA.NNID);
            return nlpQA;
        }

        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpQAs_Edit, AppPermissions.Pages_NlpChatbot_NlpQAs_Import)]
        protected async Task<NlpQA> Update(CreateOrEditNlpQADto input)
        {
            if (input.QaType != NlpQAConsts.QaType_Unanswerable)
            {
                if (input.Questions == null || input.Questions.Count() == 0 || input.AnswerSets == null || input.AnswerSets.Count() == 0)
                    throw new UserFriendlyException(L("InvalidFormMessage"));
            }

            var nlpQA = _nlpQARepository.FirstOrDefault((Guid)input.Id);

            if (input.Questions.Count() >= nlpQA.QuestionCount && input.QaType != NlpQAConsts.QaType_Unanswerable)
                await _nlpPolicyAppService.CheckMaxQuestionCount((int)AbpSession.TenantId, input.NlpChatbotId.Value);

            //input = await GetSegments(input);

            List<string> questions = null;
            List<CbAnswerSet> answers = null;
            //int questionCount = 0;

            if (input.Questions != null)
            {
                questions = input.Questions.Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
                //questionCount = qs.Count();
                //questions = JsonConvert.SerializeObject(qs);

                if (questions.Count() == 0 && input.QaType != NlpQAConsts.QaType_Unanswerable)
                    throw new UserFriendlyException(L("InvalidFormMessage"));
            }

            if (input.AnswerSets != null)
            {
                answers = input.AnswerSets.Select(
                    s => new CbAnswerSet()
                    {
                        Answer = s.Answer.Trim(),
                        GPT = s.GPT
                    }).Where(s => s.Answer.Length > 0).ToList();

                //answers = input.Answers.Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

                if (answers.Count() == 0 && input.QaType != NlpQAConsts.QaType_Unanswerable)
                    throw new UserFriendlyException(L("InvalidFormMessage"));
            }

            string questionsJson = JsonConvert.SerializeObject(questions);
            string answersJson = (answers != null && answers.Any()) ? JsonConvert.SerializeObject(answers) : null;

            input.NNID = nlpQA.NNID;
            DeleteLRUCache(nlpQA.NlpChatbotId, nlpQA.NNID);

            if (input.QaType == NlpQAConsts.QaType_Unanswerable)
            {
                Debug.Assert(input.NNID == 0);
                input.NNID = 0;
            }
            else
            {
                if (questionsJson != nlpQA.Question)
                {
                    int nnID = GetNextNNID(input.NlpChatbotId.Value);
                    if (nnID < input.NNID)
                        input.NNID = nnID;
                }
            }

            nlpQA = ObjectMapper.Map(input, nlpQA);
            nlpQA.Question = questionsJson;
            nlpQA.Answer = answersJson;
            nlpQA.QuestionCount = input.QaType == NlpQAConsts.QaType_Unanswerable ? 0 : questions.Count();

            DeleteLRUCache(nlpQA.NlpChatbotId, nlpQA.NNID);
            return nlpQA;
        }


        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpQAs_Create, AppPermissions.Pages_NlpChatbot_NlpQAs_Edit)]


        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public void CreaetQAForAccuracy(CreaetQAForAccuracyDto input)
        {
            if (input.NlpQuestion.IsNullOrEmpty() || input.NlpQuestion.IsNullOrWhiteSpace())
                return;

            var nlpQA = _nlpQARepository.FirstOrDefault(input.NlpQAId);
            if (nlpQA == null)
                return;

            List<string> questions = JsonConvert.DeserializeObject<List<string>>(nlpQA.Question);
            foreach (var question in questions)
            {
                if (input.NlpQuestion == question)
                    return;
            }

            questions.Add(input.NlpQuestion);
            nlpQA.Question = JsonConvert.SerializeObject(questions);
            nlpQA.QuestionCount = questions.Count();
        }


        //protected async Task<CreateOrEditNlpQADto> GetSegments(CreateOrEditNlpQADto input)
        //{
        //    if (input.NlpChatbotLanguage.IsNullOrEmpty())
        //        input.NlpChatbotLanguage = _nlpChatbotFunction.GetChatbotLanguage(input.NlpChatbotId.Value);

        //    if (string.Compare(input.NlpChatbotLanguage, "EN", true) == 0)
        //        return input;

        //    var questionList = input.Questions.ToList();
        //    var query = input.Questions.Select(c => new Tuple<string, string>(Guid.Empty.ToString(), c));

        //    var segments = await _nlpCBServiceClient.GetSegmentResult(new Lib.Dtos.NlpCbGetSegmentInput()
        //    {
        //        language = input.NlpChatbotLanguage,
        //        sentences = _nlpCbDictionariesFunction.PrepareSynonymString(AbpSession.TenantId.Value, input.NlpChatbotId.Value, input.NlpChatbotLanguage, input.Questions.Select(c => (Guid.Empty, c)).ToArray())
        //    }); ;

        //    if (segments == null)
        //        throw new UserFriendlyException(L("NlpCbMServiceNotResponding"));

        //    var vector = from e in segments.result select e.spacySegments;
        //    var unvector = from e in segments.result select e.unSpacySegments;

        //    List<string> unVectorWords = new List<string>();

        //    foreach (var stringList in unvector)
        //    {
        //        foreach (var stringItem in stringList)
        //        {
        //            unVectorWords.Add(stringItem);
        //        }
        //    }

        //    input.SegmentStatus = NlpQAConsts.SegmentStatus.SegmentSuccess;

        //    if (segments.errorCode != "success")
        //    {
        //        input.SegmentStatus = NlpQAConsts.SegmentStatus.SegmentError;
        //        input.SegmentErrorMsg = L(segments.errorCode);
        //    }
        //    else if (unVectorWords.Count > 0)
        //    {
        //        input.SegmentStatus = NlpQAConsts.SegmentStatus.SegmentError;
        //        StringBuilder unvectorString = new StringBuilder();

        //        foreach (var seg in unVectorWords)
        //        {
        //            if (unvectorString.Length > 0)
        //                unvectorString.Append(" " + seg);
        //            else
        //                unvectorString.Append(seg);
        //        }

        //        input.SegmentErrorMsg = L("nlpcb_tokenizer_error_segments", unvectorString.ToString());
        //    }

        //    return input;
        //}


        //[ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        //public async Task<QueryQuestionSegmentsOutput> QueryQuestionSegments(QueryQuestionSegmentsInput input)
        //{
        //    try
        //    {
        //        return await _nlpCbDictionariesFunction.QueryQuestionSegments(input);
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex.Message, ex);
        //        return new QueryQuestionSegmentsOutput();
        //    }

        //}


        [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpQAs_Delete)]
        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public void Delete(EntityDto<Guid> input)
        {
            Guid id = input.Id;

            int nCount = _nlpQARepository.Count(e => e.Id == id && e.QaType != NlpQAConsts.QaType_Unanswerable);

            if (nCount == 0)
                throw new UserFriendlyException(L("CanNotEditOrDeleteQA"));

            _nlpQARepository.Delete(id);
        }

        //[AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpQAs_Import)]
        //[RemoteService(false)]
        //public async Task ImportJsonFile(Guid chatbotId, byte[] excelData)
        //{
        //    return;

        //    var newQAList = _nlpQAListExcelDataReader.GetNlpQAsFromExcel(excelData);

        //    //將資料庫的舊資料放至MAP(question string, question id)
        //    var oldQAQueryable = await _nlpQARepository.GetAll()
        //        .Include(e => e.CurrentWfStateFk).ThenInclude(e => e.NlpWorkflowFk)
        //        .Include(e => e.NextWfStateFk).ThenInclude(e => e.NlpWorkflowFk)
        //        .Where(e => e.NlpChatbotId == chatbotId).ToListAsync();

        //    var oldMapIdToQAEntity = oldQAQueryable.ToDictionary(e => e.Id);

        //    Dictionary<(string, string), NlpWorkflowStateInfo> workflowDictionary = null;



        //    // 問題、目前流程、目前流程狀態、下個流程、下個流程狀態
        //    var oldMapQuestionToId = new Dictionary<(string, string, string, string, string), Guid>();
        //    foreach (var item in oldQAQueryable)
        //    {
        //        try
        //        {
        //            if (item.Question.IsNullOrEmpty() == false)
        //            {
        //                List<string> questionList = JsonConvert.DeserializeObject<List<string>>(item.Question);

        //                foreach (var item2 in questionList)
        //                    oldMapQuestionToId[(item2,
        //                        item.CurrentWfStateFk?.NlpWorkflowFk?.Name,
        //                        item.CurrentWfStateFk?.StateName,
        //                        item.NextWfStateFk?.NlpWorkflowFk?.Name,
        //                        item.NextWfStateFk?.StateName
        //                        )] = item.Id;
        //            }
        //        }
        //        catch (Exception)
        //        {
        //        }
        //    }

        //    ImportNlpQADto previous = new ImportNlpQADto() { ImportId = "********" };
        //    ImportNlpQADto current = null;
        //    Dictionary<Guid, NlpQA> newData = new Dictionary<Guid, NlpQA>();
        //    Dictionary<Guid, bool> newEntityGuid = new Dictionary<Guid, bool>();

        //    foreach (var item in newQAList)
        //    {
        //        try
        //        {
        //            if (item.ImportId.IsNullOrWhiteSpace()) item.ImportId = previous.ImportId;
        //            if (item.Category.IsNullOrWhiteSpace()) item.Category = previous.Category;
        //            if (item.Question.IsNullOrWhiteSpace()) item.Question = previous.Question;
        //            if (item.CurrentWorkflow.IsNullOrWhiteSpace()) item.CurrentWorkflow = previous.CurrentWorkflow;
        //            if (item.CurrentWorkflowState.IsNullOrWhiteSpace()) item.CurrentWorkflowState = previous.CurrentWorkflowState;
        //            if (item.NextWorkflow.IsNullOrWhiteSpace()) item.NextWorkflow = previous.NextWorkflow;
        //            if (item.NextWorkflowState.IsNullOrWhiteSpace()) item.NextWorkflowState = previous.NextWorkflowState;

        //            if (item.Question.IsNullOrWhiteSpace() && item.Answer.IsNullOrWhiteSpace())
        //                continue;

        //            if (item.CurrentWorkflow.IsNullOrWhiteSpace() == false ||
        //                item.CurrentWorkflowState.IsNullOrWhiteSpace() == false ||
        //                item.NextWorkflow.IsNullOrWhiteSpace() == false ||
        //                item.NextWorkflowState.IsNullOrWhiteSpace() == false)
        //            {
        //                //Flow內有空白的，不處理
        //                if (item.CurrentWorkflow.IsNullOrWhiteSpace() ||
        //                item.CurrentWorkflowState.IsNullOrWhiteSpace() ||
        //                item.NextWorkflow.IsNullOrWhiteSpace() ||
        //                item.NextWorkflowState.IsNullOrWhiteSpace())
        //                {
        //                    continue;
        //                }

        //                workflowDictionary ??= (from o in
        //                                           _nlpWorkflowState.GetAll().Include(e => e.NlpWorkflowFk)
        //                                        select new NlpWorkflowStateInfo()
        //                                        {
        //                                            NlpWorkflowName = o.NlpWorkflowFk.Name,
        //                                            StateName = o.StateName,
        //                                            Id = o.Id
        //                                        }).ToDictionary(e => (e.NlpWorkflowName, e.StateName));

        //                if (workflowDictionary.ContainsKey((item.CurrentWorkflow, item.CurrentWorkflowState)))
        //                    item.CurrentWorkflowStateId = workflowDictionary[(item.CurrentWorkflow, item.CurrentWorkflowState)].Id;

        //                if (workflowDictionary.ContainsKey((item.NextWorkflow, item.NextWorkflowState)))
        //                    item.NextWorkflowStateId = workflowDictionary[(item.NextWorkflow, item.NextWorkflowState)].Id;
        //            }


        //            if (item.ImportId != previous.ImportId)
        //            {
        //                Guid QAId;
        //                if (oldMapQuestionToId.TryGetValue((item.Question, item.CurrentWorkflow, item.CurrentWorkflowState, item.NextWorkflow, item.NextWorkflowState), out QAId))
        //                {
        //                    var foundQAEntity = oldMapIdToQAEntity[QAId];
        //                    current = new ImportNlpQADto()
        //                    {
        //                        ImportId = item.ImportId,
        //                        Category = foundQAEntity.QuestionCategory,
        //                        QAId = foundQAEntity.Id,
        //                        QuestionList = JsonConvert.DeserializeObject<List<string>>(foundQAEntity.Question),
        //                        AnswerList = JsonConvert.DeserializeObject<List<string>>(foundQAEntity.Answer),
        //                        CurrentWorkflowStateId = item.CurrentWorkflowStateId,
        //                        NextWorkflowStateId = item.NextWorkflowStateId,
        //                    };
        //                }
        //                else
        //                {
        //                    current = new ImportNlpQADto()
        //                    {
        //                        ImportId = item.ImportId,
        //                        Category = item.Category,
        //                        QuestionList = new List<string>(),
        //                        AnswerList = new List<string>(),
        //                        CurrentWorkflowStateId = item.CurrentWorkflowStateId,
        //                        NextWorkflowStateId = item.NextWorkflowStateId,
        //                    };
        //                }
        //            }
        //            else
        //            {
        //                item.QAId = current.QAId;
        //            }

        //            bool bChangedQuestion = (item.Question.IsNullOrWhiteSpace() == false && current.QuestionList.Contains(item.Question) == false);
        //            bool bChangedAnswer = (item.Answer.IsNullOrWhiteSpace() == false && current.AnswerList.Contains(item.Answer) == false);
        //            bool bChangedCategory = (item.Category != current.Category);

        //            if (bChangedQuestion || bChangedAnswer || bChangedCategory)
        //            {
        //                if (bChangedQuestion)
        //                    current.QuestionList.Add(item.Question);
        //                else
        //                    item.QAId = current.QAId;

        //                if (bChangedAnswer)
        //                    current.AnswerList.Add(item.Answer);

        //                if (bChangedCategory)
        //                    current.Category = item.Category;

        //                NlpQA nlpQA = new NlpQA()
        //                {
        //                    QuestionCategory = item.Category,
        //                    NlpChatbotId = chatbotId,
        //                    Question = Serialize(current.Question, current.QuestionList),
        //                    Answer = Serialize(current.Answer, current.AnswerList),
        //                    QuestionCount = 1 + current.QuestionList.Count,
        //                    CurrentWfState = item.CurrentWorkflowStateId == Guid.Empty ? null : item.CurrentWorkflowStateId,
        //                    NextWfState = item.NextWorkflowStateId == Guid.Empty ? null : item.NextWorkflowStateId,
        //                };

        //                if (current != null && current.QuestionList != null)
        //                    nlpQA.QuestionCount = current.QuestionList.Count + 1;

        //                if (item.QAId == null)
        //                {
        //                    nlpQA.Id = Guid.NewGuid();
        //                    newEntityGuid[nlpQA.Id] = true;
        //                }
        //                else
        //                    nlpQA.Id = item.QAId.Value;

        //                newData[nlpQA.Id] = nlpQA;
        //                item.QAId = nlpQA.Id;

        //                if (item.Question != null)
        //                    oldMapQuestionToId[(item.Question, item.CurrentWorkflow, item.CurrentWorkflowState, item.NextWorkflow, item.NextWorkflowState)] = item.QAId.Value;

        //                oldMapIdToQAEntity[item.QAId.Value] = nlpQA;
        //                current.QAId = nlpQA.Id;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Log(LogSeverity.Error, $"An error occured while NlpQAsAppService.ImportExcelFile()", ex);
        //            Debugger.Break();
        //            throw;
        //        }
        //        finally
        //        {
        //            previous = current;
        //        }
        //    }

        //    foreach (var item in newData.Values)
        //    {
        //        var createOrEditNlpQADto = new CreateOrEditNlpQADto()
        //        {
        //            Questions = JsonConvert.DeserializeObject<List<string>>(item.Question),
        //            Answers = JsonConvert.DeserializeObject<List<string>>(item.Answer),
        //            QuestionCategory = item.QuestionCategory,
        //            NlpChatbotId = item.NlpChatbotId
        //        };

        //        if (newEntityGuid.ContainsKey(item.Id))
        //            createOrEditNlpQADto.Id = item.Id;

        //        await _CreateOrEdit(createOrEditNlpQADto);
        //    }

        //    DeleteLRUCache(chatbotId, null);
        //}



        [RemoteService(false)]
        protected int GetNextNNID(Guid chatbotId)
        {
            try
            {
                var countNNID = _nlpQARepository.Count(e => e.NlpChatbotId == chatbotId);

                if (countNNID == 0)
                    return 1;

                var maxNNID = _nlpQARepository.GetAll().Where(e => e.NlpChatbotId == chatbotId).Max(e => e.NNID);

                if (maxNNID <= countNNID)
                    return maxNNID + 1;

                var nlpQA_NNID = from o in _nlpQARepository.GetAll()
                                 where o.NlpChatbotId == chatbotId
                                 orderby o.NNID
                                 select o.NNID;

                var interruptNNID = nlpQA_NNID.AsEnumerable<int>().Select((u, index) => new
                {
                    NNID = u,
                    idx = index,
                }).FirstOrDefault(e => e.idx != e.NNID);

                if (interruptNNID != null)
                {
                    return interruptNNID.idx;
                }

                throw new UserFriendlyException("nlp_qa_table_error");
            }
            catch (Exception)
            {
                throw;
            }
        }

        protected void DeleteLRUCache(Guid chatbotId, int? nnid)
        {
            if (nnid == null)
                _cacheManager.Clear_NlpQADtoFromNNID(chatbotId);
            else
                _cacheManager.Remove_NlpQADtoFromNNID(chatbotId, nnid.Value);
        }

        protected static string Serialize(string main, IEnumerable<string> approximations)
        {
            try
            {
                List<string> stringList = new List<string>();

                if (main.IsNullOrWhiteSpace() == false)
                    stringList.Add(main);

                if (approximations != null)
                    stringList.AddRange(approximations);

                return JsonConvert.SerializeObject(stringList);
            }
            catch (Exception)
            {
                List<string> stringList = new List<string>();
                stringList.Add(main);
                return JsonConvert.SerializeObject(stringList);
            }
        }


        [RemoteService(false)]
        public async Task CheckNNID0Async(Guid chatbotId)
        {
            try
            {
                var nlpQADto = (NlpQADto)_cacheManager.Get_NlpQADtoFromNNID(chatbotId, 0) ?? ObjectMapper.Map<NlpQADto>(_nlpQARepository.FirstOrDefault(e => e.NlpChatbotId == chatbotId && e.NNID == 0));

                if (nlpQADto != null && (nlpQADto.QaType == null || nlpQADto.QaType == 0))
                {
                    await _nlpQARepository.BatchDeleteAsync(e => e.TenantId == AbpSession.TenantId && (
                    e.Id == nlpQADto.Id || e.NNID == 0));
                    _cacheManager.Remove_NlpQADtoFromNNID(chatbotId, 0);
                    nlpQADto = null;
                }

                if (nlpQADto == null)
                {
                    var nlpQA = new NlpQA()
                    {
                        QaType = NlpQAConsts.QaType_Unanswerable,
                        NNID = 0,
                        TenantId = AbpSession.TenantId.Value,
                        Question = "[]",
                        Answer = null,
                        NlpChatbotId = chatbotId,
                        QuestionCount = 0
                    };

                    await _nlpQARepository.InsertAsync(nlpQA);

                    await CurrentUnitOfWork.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString(), ex);
            }
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<GetCaterogiesOutput> GetCaterogies(Guid chatbotId)
        {
            return new GetCaterogiesOutput()
            {
                SelectItem = (string)_nlpCbSession["Category"],
                Caterogies = await _nlpQARepository.GetAll().Where(e => e.NlpChatbotId == chatbotId && string.IsNullOrEmpty(e.QuestionCategory) == false).Select(e => e.QuestionCategory.Trim()).Distinct().ToListAsync()
            };
        }


        [RemoteService(false)]
        public async Task<LicenseUsage> GetQuestionLicenseUsage(Guid chatbotId)
        {
            var usageCount = _nlpQARepository.GetAll()
                .Where(e => e.NlpChatbotId == chatbotId && e.QaType != NlpQAConsts.QaType_Unanswerable)
                .Sum(e => e.QuestionCount <= 0 ? 1 : e.QuestionCount);

            var licenseCount = (await FeatureChecker.GetValueAsync(AbpSession.TenantId.Value, AppFeatures.MaxQuestionCount)).To<int>();

            //licenseCount = 5;

            return new LicenseUsage()
            {
                LicenseCount = licenseCount,
                UsageCount = usageCount
            };
        }

        [RemoteService(false)]
        public async Task<List<NlpWorkflowStateSelection>> GetAllNlpWorkflowStateForTableDropdown(Guid chatbotId)
        {
            Debug.Assert(chatbotId != Guid.Empty);

            if (chatbotId== Guid.Empty)
                throw new UserFriendlyException(nameof(chatbotId));


            var a = await _nlpWorkflowState.GetAll()
                .Include(e => e.NlpWorkflowFk)
                .Where(e=>e.NlpWorkflowFk.NlpChatbotId == chatbotId)
                .Select(workflowStatus => new NlpWorkflowStateSelection
                {
                    WfId = workflowStatus.NlpWorkflowId,
                    WfsId = workflowStatus.Id,
                    WfName = workflowStatus.NlpWorkflowFk.Name,
                    WfsName = workflowStatus.StateName
                }).ToListAsync();

            var b = a.GroupBy(wfs => new
            {
                WfId = wfs.WfId,
                WfName = wfs.WfName,
            }).Select(wfs => new NlpWorkflowStateSelection
            {
                WfId = wfs.Key.WfId,
                WfsId = null,
                WfName = wfs.Key.WfName,
                WfsName = null
            });

            var c = a.Union(b).OrderBy(wfs => wfs.WfId).ThenBy(wfs => wfs.WfsId).ToList();

            //var d = c.Select(
            //        nlp => new NlpLookupTableDto
            //        {
            //            Id = (nlp.WfsId ?? nlp.WfId).ToString(),
            //            DisplayName = nlp.WfsId == null ? (nlp.WfName + " : *") : (nlp.WfName + " : " + nlp.WfsName)
            //        }
            //    ).ToList();

            return c;
        }

        [RemoteService(false)]
        public async Task<List<NlpLookupTableDto>> GetAllNlpChatbotForTableDropdown()
        {
            return await _lookup_nlpChatbotRepository.GetAll()
                .Select(nlpChatbot => new NlpLookupTableDto
                {
                    Id = nlpChatbot.Id.ToString(),
                    DisplayName = nlpChatbot == null || nlpChatbot.Name == null ? "" : nlpChatbot.Name.ToString()
                }).ToListAsync();
        }

        [HttpPost]
        public async Task DeleteSelections(DeleteSelectionInput input)
        {
            await _nlpQARepository.DeleteAsync(e => input.Ids.Contains(e.Id));
        }


        public async Task<int> GetQaCount(Guid chatbotId)
        {
            var qaCount = await _nlpQARepository.GetAll()
                     .Where(e => e.NlpChatbotId == chatbotId && e.QaType != NlpQAConsts.QaType_Unanswerable)
                     .SumAsync(e => e.QuestionCount <= 0 ? 1 : e.QuestionCount);
            return qaCount;
        }
    }
}
