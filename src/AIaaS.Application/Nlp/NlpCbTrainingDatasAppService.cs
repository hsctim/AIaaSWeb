using System.Collections.Generic;

using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using Abp.Linq.Extensions;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using AIaaS.Nlp.Dtos;
using Abp.Application.Services.Dto;
using Microsoft.EntityFrameworkCore;
using Abp.Application.Services;
using AIaaS.Nlp.Training;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using Abp.UI;

//type 0: training
//type 1: complete

namespace AIaaS.Nlp
{
    [RemoteService(false)]
    public class NlpCbTrainingDatasAppService : AIaaSAppServiceBase, INlpCbTrainingDatasAppService
    {
        private readonly IRepository<NlpCbTrainingData, Guid> _nlpCbTrainingDataRepository;
        private readonly IRepository<NlpChatbot, Guid> _nlpChatbotRepository;

        //private readonly RawSQLRepository<NlpCbTrainingData, Guid> _rawSQLRepository;

        public NlpCbTrainingDatasAppService(IRepository<NlpCbTrainingData, Guid> nlpCbTrainingDataRepository,
            IRepository<NlpChatbot, Guid> nlpChatbotRepository
            )
        {
            _nlpCbTrainingDataRepository = nlpCbTrainingDataRepository;
            _nlpChatbotRepository = nlpChatbotRepository;
        }


        [RemoteService(false)]
        public async Task<Guid> CreateNewTrainingDataAsync(Guid chatbotId, NlpCbMSourceData nlpCbMRawData)
        {
            var data = await _nlpCbTrainingDataRepository.FirstOrDefaultAsync(e => e.NlpChatbotId == chatbotId);

            if (data == null)
            {
                NlpCbTrainingData result = await _nlpCbTrainingDataRepository.InsertAsync(
                    new NlpCbTrainingData()
                    {
                        TenantId = AbpSession.TenantId.Value,
                        NlpChatbotId = chatbotId,
                        NlpCbTDSource = JsonConvert.SerializeObject(nlpCbMRawData),
                    });

                return result.Id;
            }
            else
            {
                data.TenantId = AbpSession.TenantId.Value;
                data.NlpChatbotId = chatbotId;
                data.NlpCbTDSource = JsonConvert.SerializeObject(nlpCbMRawData);
                return data.Id;
            }
        }
    }
}
