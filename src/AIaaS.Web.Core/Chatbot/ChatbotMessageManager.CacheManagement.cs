using Abp.Application.Services;
using Abp.Timing;
using AIaaS.Chatbot;
using AIaaS.Helpers;
using AIaaS.Nlp;
using AIaaS.Nlp.Dtos;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace AIaaS.Web.Chatbot
{
    public partial class ChatbotMessageManager : ApplicationService, IChatbotMessageManager
    {
        public async Task<NlpClientInfoDto> GetNlpClientInfoDtosCache(int tenantId, Guid clientId)
        {
            if (__nlpClientInfoDtoCache != null && __nlpClientInfoDtoCache.TenantId == tenantId && __nlpClientInfoDtoCache.ClientId == clientId)
                return __nlpClientInfoDtoCache;

            __nlpClientInfoDtoCache = (NlpClientInfoDto)_cacheManager.Get_NlpClientInfoDto(tenantId, clientId);
            if (__nlpClientInfoDtoCache != null)
                return __nlpClientInfoDtoCache;

            var nlpClientInfo = await _nlpClientInfo.FirstOrDefaultAsync(e => e.TenantId == tenantId && e.ClientId == clientId);

            if (nlpClientInfo == null)
                return null;

            __nlpClientInfoDtoCache = ObjectMapper.Map<NlpClientInfoDto>(nlpClientInfo);
            _cacheManager.Set_NlpClientInfoDto(tenantId, clientId, __nlpClientInfoDtoCache);
            return __nlpClientInfoDtoCache;
        }

        public async Task<NlpClientInfoDto> SetNlpClientInfoDtosCache(NlpClientInfoDto dto)
        {
            if (dto != null && __nlpClientInfoDtoCache != null && dto.isSame(__nlpClientInfoDtoCache) == true)
                return dto;

            __nlpClientInfoDtoCache = dto;
            _cacheManager.Set_NlpClientInfoDto(dto.TenantId, dto.ClientId, dto);

            var nlpClientInfo = await _nlpClientInfo.FirstOrDefaultAsync(e => e.TenantId == dto.TenantId && e.ClientId == dto.ClientId);

            if (nlpClientInfo != null)
            {
                dto.Id = nlpClientInfo.Id;
                dto.UpdatedTime = Clock.Now;

                ObjectMapper.Map(dto, nlpClientInfo);
            }
            else
            {
                await _nlpClientInfo.InsertAsync(ObjectMapper.Map<NlpClientInfo>(dto));
            }

            return dto;
        }


        protected async Task<Dictionary<int, int[]>> GetAnswerFromNNIDRepetition(Guid chatbotId)
        {
            var nnidRepetition = (Dictionary<int, int[]>)_cacheManager.Get_NlpNNIDRepetition(chatbotId);

            if (nnidRepetition == null)
            {
                var data = await _nlpCbTrainingDataRepository.FirstOrDefaultAsync(e => e.NlpChatbotId == chatbotId);
                if (data != null && string.IsNullOrEmpty(data.NlpNNIDRepetition) == false)
                {
                    nnidRepetition = JsonConvert.DeserializeObject<Dictionary<int, int[]>>(data.NlpNNIDRepetition);
                    _cacheManager.Set_NlpNNIDRepetition(chatbotId, nnidRepetition);
                }
            }

            return nnidRepetition;
        }
    }
}
