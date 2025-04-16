using AIaaS.Nlp;
using System.Collections.Generic;

using System;
using System.Linq;
using System.Linq.Dynamic.Core;

using System.Threading.Tasks;
using Abp.Domain.Repositories;
using AIaaS.Nlp.Dtos;
using Abp.Application.Services;
using AIaaS.Nlp.Dtos.NlpCbTrainedAnswer;
using Microsoft.EntityFrameworkCore;

namespace AIaaS.Nlp
{
    //[AbpAuthorize(AppPermissions.Pages_NlpCbTrainedAnswers)]
    [RemoteService(false)]
    public class NlpCbTrainedAnswersAppService : AIaaSAppServiceBase, INlpCbTrainedAnswersAppService
    {
        private readonly IRepository<NlpCbTrainedAnswer, Guid> _nlpCbTrainedAnswerRepository;
        private readonly IRepository<NlpCbTrainingData, Guid> _lookup_nlpCbTrainingDataRepository;

        public NlpCbTrainedAnswersAppService(IRepository<NlpCbTrainedAnswer, Guid> nlpCbTrainedAnswerRepository, IRepository<NlpCbTrainingData, Guid> lookup_nlpCbTrainingDataRepository)
        {
            _nlpCbTrainedAnswerRepository = nlpCbTrainedAnswerRepository;
            _lookup_nlpCbTrainingDataRepository = lookup_nlpCbTrainingDataRepository;
        }

        [RemoteService(false)]
        public void Create(NlpCbTAChatbotTrainingInsertDto input)
        {
            foreach (var i in input.Answers)
            {
                CreateOrEditNlpCbTrainedAnswerDto input2 = new CreateOrEditNlpCbTrainedAnswerDto()
                {
                    NlpCbTAAnswer = i.Answer,
                    NNID = i.NNID,
                    NlpCbTrainingDataId = input.NlpCbTrainingDataId
                };

                Create(input2);
            }
        }

        [RemoteService(false)]
        public async Task DeleteUnusedData()
        {
            //var nlpUnused =
            //    (from o in _nlpCbTrainedAnswerRepository.GetAll().Include(e => e.NlpCbTrainingDataFk)
            //     where o.NlpCbTrainingDataFk == null
            //     select o).DeleteFromQuery();


            await _nlpCbTrainedAnswerRepository.GetAll().Where(e => e.NlpCbTrainingDataFk == null).DeleteFromQueryAsync();
        }

        [RemoteService(false)]
        public void Delete(Guid trainingDataId)
        {
            _nlpCbTrainedAnswerRepository.GetAll()
               .Where(e => e.NlpCbTrainingDataId == trainingDataId)
               .DeleteFromQuery();
        }


        [RemoteService(false)]
        protected virtual void Create(CreateOrEditNlpCbTrainedAnswerDto input)
        {
            var nlpCbTrainedAnswer = ObjectMapper.Map<NlpCbTrainedAnswer>(input);

            if (AbpSession.TenantId != null)
            {
                nlpCbTrainedAnswer.TenantId = (int)AbpSession.TenantId;
            }

            _nlpCbTrainedAnswerRepository.Insert(nlpCbTrainedAnswer);
        }
    }
}