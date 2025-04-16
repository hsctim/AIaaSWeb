using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Entities;

namespace AIaaS.Nlp
{
    [Table("NlpChatbots")]
    public class NlpChatbot : FullAuditedEntity<Guid>, IMustHaveTenant
    {
        public int TenantId { get; set; }

        [Required]
        [StringLength(NlpChatbotConsts.MaxNameLength, MinimumLength = NlpChatbotConsts.MinNameLength)]
        public virtual string Name { get; set; }

        [StringLength(NlpChatbotConsts.MaxGreetingMsgLength, MinimumLength = NlpChatbotConsts.MinGreetingMsgLength)]
        public virtual string GreetingMsg { get; set; }

        [StringLength(NlpChatbotConsts.MaxFailedMsgLength, MinimumLength = NlpChatbotConsts.MinFailedMsgLength)]
        public virtual string FailedMsg { get; set; }

        [StringLength(NlpChatbotConsts.MaxAlternativeQuestionLength, MinimumLength = NlpChatbotConsts.MinAlternativeQuestionLength)]
        public virtual string AlternativeQuestion { get; set; }

        [Required]
        [StringLength(NlpChatbotConsts.MaxLanguageLength, MinimumLength = NlpChatbotConsts.MinLanguageLength)]
        public virtual string Language { get; set; }

        public virtual bool Disabled { get; set; }

        public virtual Guid? ChatbotPictureId { get; set; }

        [StringLength(NlpChatbotConsts.MaxLineTokenLength, MinimumLength = NlpChatbotConsts.MinLineTokenLength)]
        public virtual string LineToken { get; set; }

        [StringLength(NlpChatbotConsts.MaxFacebookAccessTokenLength, MinimumLength = NlpChatbotConsts.MinFacebookAccessTokenLength)]
        public virtual string FacebookAccessToken { get; set; }

        [StringLength(NlpChatbotConsts.MaxFacebookVerifyTokenLength, MinimumLength = NlpChatbotConsts.MinFacebookVerifyTokenLength)]
        public virtual string FacebookVerifyToken { get; set; }

        [StringLength(NlpChatbotConsts.MaxFacebookSecretKeyLength, MinimumLength = NlpChatbotConsts.MinFacebookSecretKeyLength)]
        public virtual string FacebookSecretKey { get; set; }

        public virtual bool EnableWebChat { get; set; }

        public virtual bool EnableWebAPI { get; set; }

        public virtual bool EnableFacebook { get; set; }

        public virtual bool EnableLine { get; set; }

        public virtual int TrainingCostSeconds { get; set; }

        public virtual bool EnableWebhook { get; set; }

        [StringLength(NlpChatbotConsts.MaxWebApiSecretLength, MinimumLength = NlpChatbotConsts.MinWebApiSecretLength)]
        public virtual string WebApiSecret { get; set; }

        [StringLength(NlpChatbotConsts.MaxWebhookSecretLength, MinimumLength = NlpChatbotConsts.MinWebhookSecretLength)]
        public virtual string WebhookSecret { get; set; }

        [Range(NlpChatbotConsts.MinPredThresholdValue, NlpChatbotConsts.MaxPredThresholdValue)]
        public virtual float PredThreshold { get; set; } = NlpChatbotConsts.DefaultPredThreshold;

        [Range(NlpChatbotConsts.MinSuggestionThresholdValue, NlpChatbotConsts.MaxSuggestionThresholdValue)]
        public virtual float SuggestionThreshold { get; set; } = NlpChatbotConsts.DefaultSuggestionThreshold;

        [Range(NlpChatbotConsts.MinWSPredThresholdValue, NlpChatbotConsts.MaxWSPredThresholdValue)]
        public virtual float WSPredThreshold { get; set; }= NlpChatbotConsts.DefaultWSPredThreshold;

        public virtual int EnableOPENAI { get; set; }

        public virtual bool OPENAICache { get; set; }

        [StringLength(NlpChatbotConsts.MaxOPENAIOrgLength, MinimumLength = NlpChatbotConsts.MinOPENAIOrgLength)]
        public virtual string OPENAIOrg { get; set; }

        [StringLength(NlpChatbotConsts.MaxOPENAIKeyLength, MinimumLength = NlpChatbotConsts.MinOPENAIKeyLength)]
        public virtual string OPENAIKey { get; set; }

        [StringLength(NlpChatbotConsts.MaxOpenAIParamLength, MinimumLength = NlpChatbotConsts.MinOpenAIParamLength)]
        public virtual string OpenAIParam { get; set; }

    }
}