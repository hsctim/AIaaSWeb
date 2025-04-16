using AIaaS.Nlp;

using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using Abp.Linq.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using AIaaS.Nlp.Dtos;
using AIaaS.Dto;
using Abp.Application.Services.Dto;
using AIaaS.Authorization;
using Abp.Extensions;
using Abp.Authorization;
using Microsoft.EntityFrameworkCore;
using Abp.UI;
using Newtonsoft.Json;
using Abp.Application.Services;
using ApiProtectorDotNet;
using Abp.Timing;

namespace AIaaS.Nlp
{
    [AbpAuthorize(AppPermissions.Pages_NlpChatbot_NlpCbQAAccuracies)]
    public class NlpCbQAAccuraciesAppService : AIaaSAppServiceBase, INlpCbQAAccuraciesAppService
    {
        private readonly IRepository<NlpCbQAAccuracy, Guid> _nlpCbQAAccuracyRepository;
        private readonly IRepository<NlpChatbot, Guid> _lookup_nlpChatbotRepository;
        private readonly IRepository<NlpQA, Guid> _nlpQARepository;
        private readonly NlpCbSession _nlpCbSession;
        //private Dictionary<string, string> _UnanswerableQuestions;

        public NlpCbQAAccuraciesAppService(
            IRepository<NlpCbQAAccuracy, Guid> nlpCbQAAccuracyRepository,
            IRepository<NlpChatbot, Guid> lookup_nlpChatbotRepository,
            IRepository<NlpQA, Guid> nlpQARepository,
                         NlpCbSession nlpCbSession)
        {
            _nlpCbQAAccuracyRepository = nlpCbQAAccuracyRepository;
            _lookup_nlpChatbotRepository = lookup_nlpChatbotRepository;
            _nlpQARepository = nlpQARepository;
            _nlpCbSession = nlpCbSession;
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<PagedResultDto<GetNlpCbQAAccuracyForViewDto>> GetAll(GetAllNlpCbQAAccuraciesInput input)
        {
            if (input.MaxResultCount > AppConsts.MaxPageSize)
                throw new UserFriendlyException(L("Exception"));

            if (input.NlpChatbotId.HasValue == false)
                return new PagedResultDto<GetNlpCbQAAccuracyForViewDto>(0, new List<GetNlpCbQAAccuracyForViewDto>());

            _nlpCbSession["ChatbotId"] = input.NlpChatbotId.ToString();

            var filteredNlpCbQAAccuracies = _nlpCbQAAccuracyRepository.GetAll()
                        .Include(e => e.NlpChatbotFk)
                        .Include(e => e.AnswerId1Fk)
                        .Include(e => e.AnswerId2Fk)
                        .Include(e => e.AnswerId3Fk)

                        .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), e => false || e.Question.Contains(input.Filter.Trim()) ||
                        e.AnswerId1Fk.Question.Contains(input.Filter.Trim()) ||
                        e.AnswerId1Fk.Answer.Contains(input.Filter.Trim()) ||
                        e.AnswerId2Fk.Question.Contains(input.Filter.Trim()) ||
                        e.AnswerId2Fk.Answer.Contains(input.Filter.Trim()) ||
                        e.AnswerId3Fk.Question.Contains(input.Filter.Trim()) ||
                        e.AnswerId3Fk.Answer.Contains(input.Filter.Trim()))
                        .WhereIf(input.MinNlpCreationTimeFilter != null, e => e.CreationTime >= input.MinNlpCreationTimeFilter)
                        .WhereIf(input.MaxNlpCreationTimeFilter != null, e => e.CreationTime <= input.MaxNlpCreationTimeFilter)
                        .Where(e => e.NlpChatbotId == input.NlpChatbotId)
                        .Where(e => e.CreationTime >= Clock.Now.AddMonths(-12));

            var pagedAndFilteredNlpCbQAAccuracies = filteredNlpCbQAAccuracies
                .OrderBy(input.Sorting ?? "CreationTime desc")
                .PageBy(input);


            var unanswerableQuestions = GetUnanswerableQuestions(input.NlpChatbotId.Value) ?? new Dictionary<string, string>();

            var nlpCbQAAccuracies = from o in pagedAndFilteredNlpCbQAAccuracies
                                    select new GetNlpCbQAAccuracyForViewDto()
                                    {
                                        NlpCbQAAccuracy = new NlpCbQAAccuracyDto
                                        {
                                            Question = o.Question,
                                            AnswerPredict = GetNlpCbQAAccuracyAnswerDto(
                                                o.AnswerId1, o.AnswerId2, o.AnswerId3,
                                                o.AnswerAcc1, o.AnswerAcc2, o.AnswerAcc3,
                                                o.AnswerId1Fk.Question,
                                                o.AnswerId2Fk.Question,
                                                o.AnswerId3Fk.Question,
                                                o.AnswerId1Fk.Answer,
                                                o.AnswerId2Fk.Answer,
                                                o.AnswerId3Fk.Answer),
                                            Id = o.Id,
                                            CreationTime = o.CreationTime,
                                            NlpChatbotId = input.NlpChatbotId.Value,
                                            UnanswerableQuestion = unanswerableQuestions.ContainsKey(o.Question.Trim())
                                        },
                                    };

            var totalCount = await filteredNlpCbQAAccuracies.CountAsync();

            var nlpCbQAAccuraciesList = await nlpCbQAAccuracies.ToListAsync();

            return new PagedResultDto<GetNlpCbQAAccuracyForViewDto>(
                totalCount,
                nlpCbQAAccuraciesList
            );
        }


        protected static List<NlpCbQAAccuracyDto.NlpCbQAAccuracyAnswerDto> GetNlpCbQAAccuracyAnswerDto(Guid? AnswerId1, Guid? AnswerId2, Guid? AnswerId3, double? AnswerAcc1, double? AnswerAcc2, double? AnswerAcc3,
            string nlpQ1, string nlpQ2, string nlpQ3, string nlpA1, string nlpA2, string nlpA3)
        {
            List<NlpCbQAAccuracyDto.NlpCbQAAccuracyAnswerDto> dto = new List<NlpCbQAAccuracyDto.NlpCbQAAccuracyAnswerDto>();

            if (AnswerAcc1.HasValue && !string.IsNullOrEmpty(nlpQ1))
                dto.Add(new NlpCbQAAccuracyDto.NlpCbQAAccuracyAnswerDto()
                {
                    AnswerId = AnswerId1,
                    AnswerAcc = AnswerAcc1,
                    Question = JsonConvert.DeserializeObject<List<string>>(nlpQ1),
                    //Answer = nlpA1.IsNullOrEmpty() ? new List<string>() : JsonConvert.DeserializeObject<List<string>>(nlpA1),
                    Answer= nlpA1.IsNullOrEmpty() ? new List<CbAnswerSet>() : JsonToAnswer(nlpA1),
                });

            if (AnswerAcc2.HasValue && !string.IsNullOrEmpty(nlpQ2))
                dto.Add(new NlpCbQAAccuracyDto.NlpCbQAAccuracyAnswerDto()
                {
                    AnswerId = AnswerId2,
                    AnswerAcc = AnswerAcc2,
                    Question = JsonConvert.DeserializeObject<List<string>>(nlpQ2),
                    Answer = nlpA2.IsNullOrEmpty() ? new List<CbAnswerSet>() : JsonToAnswer(nlpA2),
                });

            if (AnswerAcc3.HasValue && !string.IsNullOrEmpty(nlpQ3))
                dto.Add(new NlpCbQAAccuracyDto.NlpCbQAAccuracyAnswerDto()
                {
                    AnswerId = AnswerId3,
                    AnswerAcc = AnswerAcc3,
                    Question = JsonConvert.DeserializeObject<List<string>>(nlpQ3),
                    Answer = nlpA3.IsNullOrEmpty() ? new List<CbAnswerSet>() : JsonToAnswer(nlpA3),
                });

            return dto;
        }

        private Dictionary<string, string> GetUnanswerableQuestions(Guid chatbotId)
        {
            var qa = _nlpQARepository.FirstOrDefault(e => e.NlpChatbotId == chatbotId && e.QaType == NlpQAConsts.QaType_Unanswerable && e.NNID == 0);

            if (qa == null || qa.Question.IsNullOrEmpty())
                return null;

            return JsonConvert.DeserializeObject<List<string>>(qa.Question).Distinct().ToDictionary<string, string>(e => e);
        }

        private static IList<CbAnswerSet> JsonToAnswer(string json)
        {
            if (json == null || json.IsNullOrEmpty())
                return null;

            try
            {
                var obj = JsonConvert.DeserializeObject<IList<string>>(json);

                var stringList = new List<CbAnswerSet>();

                foreach (var item in obj)
                {
                    stringList.Add(
                        new CbAnswerSet()
                        {
                            GPT = false,
                            Answer = item
                        });
                }

                return stringList;
            }
            catch (Exception)
            {
            }

            try
            {
                return JsonConvert.DeserializeObject<IList<CbAnswerSet>>(json);
            }
            catch (Exception)
            {
                return null;
            }

        }
    }
}
