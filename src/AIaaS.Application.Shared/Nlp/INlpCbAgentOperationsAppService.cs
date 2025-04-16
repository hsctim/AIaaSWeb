using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using AIaaS.Nlp.Dtos;
using AIaaS.Dto;
using System.Collections.Generic;

namespace AIaaS.Nlp
{
    public interface INlpCbAgentOperationsAppService : IApplicationService
    {

        //List<NlpChatroomStatus> GetAllChatrooms(Guid? chatbotId);
    }
}