using System;
using System.Threading;
using System.Threading.Tasks;
using Abp.Domain.Policies;
using AIaaS.Nlp.Dto;

namespace AIaaS.Nlp
{
    //public enum UpdateNlpTenantType
    //{
    //    UpdatePriority = 1,
    //    UpdateAmount = 2,
    //    UpdateAll = 3,
    //}


    public interface INlpPolicyAppService : IPolicy
    {


        Task CheckMaxChatbotCount(int tenantId, int offset = 0);
        Task CheckMaxQuestionCount(int tenantId, Guid chatbotId);
        //void CheckMaxModelTrainingCount(int tenantId);
        //Task CheckMaxAnswerSendCount(int tenantId);
        //Task<bool> IsExceedingMaxAnswerSendCount(int tenantId);
        Task<SemaphoreSlim> GetMessageSendQuotaSemaphoreSlim(int tenantId);

        Task<SemaphoreSlim> Get_GetMessageQuotaSemaphoreSlim(int tenantId);

        Task<NlpTenantCoreDto> UpdateTenantPriority(int tenantId);
    }
}
