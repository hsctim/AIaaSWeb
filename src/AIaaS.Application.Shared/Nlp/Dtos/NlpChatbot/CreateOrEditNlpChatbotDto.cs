using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace AIaaS.Nlp.Dtos
{
    public class CreateOrEditNlpChatbotDto : EntityDto<Guid?>
    {

        [Required]
        [StringLength(NlpChatbotConsts.MaxNameLength, MinimumLength = NlpChatbotConsts.MinNameLength)]
        public string Name { get; set; }

        [StringLength(NlpChatbotConsts.MaxGreetingMsgLength, MinimumLength = NlpChatbotConsts.MinGreetingMsgLength)]
        public string GreetingMsg { get; set; }

        [StringLength(NlpChatbotConsts.MaxFailedMsgLength, MinimumLength = NlpChatbotConsts.MinFailedMsgLength)]
        public string FailedMsg { get; set; }

        [StringLength(NlpChatbotConsts.MaxAlternativeQuestionLength, MinimumLength = NlpChatbotConsts.MinAlternativeQuestionLength)]
        public string AlternativeQuestion { get; set; }

        [Required]
        [StringLength(NlpChatbotConsts.MaxLanguageLength, MinimumLength = NlpChatbotConsts.MinLanguageLength)]
        public string Language { get; set; }

        [StringLength(NlpChatbotConsts.MaxImageFileNameLength, MinimumLength = NlpChatbotConsts.MinImageFileNameLength)]
        public string ImageFileName { get; set; }
        public bool Disabled { get; set; }

        public Guid? ChatbotPictureId { get; set; }

        [StringLength(NlpChatbotConsts.MaxLineTokenLength, MinimumLength = NlpChatbotConsts.MinLineTokenLength)]
        public string LineToken { get; set; }

        [StringLength(NlpChatbotConsts.MaxFacebookAccessTokenLength, MinimumLength = NlpChatbotConsts.MinFacebookAccessTokenLength)]
        public string FacebookAccessToken { get; set; }

        [StringLength(NlpChatbotConsts.MaxFacebookVerifyTokenLength, MinimumLength = NlpChatbotConsts.MinFacebookVerifyTokenLength)]
        public string FacebookVerifyToken { get; set; }

        [StringLength(NlpChatbotConsts.MaxFacebookSecretKeyLength, MinimumLength = NlpChatbotConsts.MinFacebookSecretKeyLength)]
        public string FacebookSecretKey { get; set; }

        public bool EnableWebChat { get; set; }


        public bool EnableFacebook { get; set; }

        public bool EnableLine { get; set; }

        public virtual bool EnableWebAPI { get; set; }

        public virtual bool EnableWebhook { get; set; }

        public virtual string WebApiSecret { get; set; }

        public virtual string WebhookSecret { get; set; }

        [Range(NlpChatbotConsts.MinPredThresholdValue, NlpChatbotConsts.MaxPredThresholdValue)]
        public virtual float PredThreshold { get; set; } = NlpChatbotConsts.DefaultPredThreshold;

        [Range(NlpChatbotConsts.MinWSPredThresholdValue, NlpChatbotConsts.MaxSuggestionThresholdValue)]
        public virtual float WSPredThreshold { get; set; } = NlpChatbotConsts.DefaultWSPredThreshold;

        [Range(NlpChatbotConsts.MinSuggestionThresholdValue, NlpChatbotConsts.MaxSuggestionThresholdValue)]
        public virtual float SuggestionThreshold { get; set; } = NlpChatbotConsts.DefaultSuggestionThreshold;

        public virtual float DefaultPredThreshold { get; set; } = NlpChatbotConsts.DefaultPredThreshold;
        public virtual float DefaultWSPredThreshold { get; set; } = NlpChatbotConsts.DefaultWSPredThreshold;
        public virtual float DefaultSuggestionThreshold { get; set; } = NlpChatbotConsts.DefaultSuggestionThreshold;


        public virtual int EnableOPENAI { get; set; }

        public virtual bool OPENAICache { get; set; }

        public virtual string OPENAIOrg { get; set; }

        public virtual string OPENAIKey { get; set; }

        public virtual string OpenAIParam { get; set; }


    }
}