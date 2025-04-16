using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using AIaaS.Nlp.Dtos;
using AIaaS.Dto;
using System.Collections.Generic;

namespace AIaaS.Nlp
{
    public interface INlpCbMessagesAppService : IApplicationService
    {
        Task<PagedResultDto<GetNlpCbMessageForViewDto>> GetAll(GetAllNlpCbMessagesInput input);

        Task<List<NlpChatbotDto>> GetAllForSelectList();

    }
}