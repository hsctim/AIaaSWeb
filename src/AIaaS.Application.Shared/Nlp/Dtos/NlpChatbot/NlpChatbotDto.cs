using System;
using Abp.Application.Services.Dto;
using Newtonsoft.Json;
using static AIaaS.Nlp.NlpChatbotConsts;

namespace AIaaS.Nlp.Dtos
{
    public class NlpChatbotDto : EntityDto<Guid>
    {
        public NlpChatbotDto ShallowCopy()
        {
            return (NlpChatbotDto)this.MemberwiseClone();
        }

        public int TenantId { get; set; }

        public string Name { get; set; }

        public string GreetingMsg { get; set; }

        public string FailedMsg { get; set; }

        public string AlternativeQuestion { get; set; }

        public string Language { get; set; }

        public bool Disabled { get; set; }

        //public int TrainingStatus { get; set; }
        public bool IsDeleted { get; set; }

        public Guid? ChatbotPictureId { get; set; }

        public string LineToken { get; set; }

        public string FacebookAccessToken { get; set; }

        public string FacebookVerifyToken { get; set; }

        public string FacebookSecretKey { get; set; }

        public bool EnableWebChat { get; set; }


        public bool EnableFacebook { get; set; }

        public bool EnableLine { get; set; }

        //public int TrainingCostSeconds { get; set; }
        //不需要，否則會一直更新快取

        public virtual bool EnableWebAPI { get; set; }

        public virtual bool EnableWebhook { get; set; }

        public virtual string WebApiSecret { get; set; }

        public virtual string WebhookSecret { get; set; }
        public virtual float PredThreshold { get; set; } = NlpChatbotConsts.DefaultPredThreshold;

        public virtual float WSPredThreshold { get; set; } = NlpChatbotConsts.DefaultWSPredThreshold;

        public virtual float SuggestionThreshold { get; set; } = NlpChatbotConsts.DefaultSuggestionThreshold;

        public virtual int EnableOPENAI { get; set; }

        public virtual bool OPENAICache { get; set; }
        public virtual string OPENAIOrg { get; set; }
        public virtual string OPENAIKey { get; set; }
		
        public virtual string OpenAIParam { get; set; }		
    }
}