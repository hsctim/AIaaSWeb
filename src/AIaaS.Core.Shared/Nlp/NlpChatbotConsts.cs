namespace AIaaS.Nlp
{
    public class NlpChatbotConsts
    {

        public const int MinNameLength = 0;
        public const int MaxNameLength = 256;

        public const int MinGreetingMsgLength = 0;
        public const int MaxGreetingMsgLength = 2048;

        public const int MinFailedMsgLength = 0;
        public const int MaxFailedMsgLength = 2048;

        public const int MinAlternativeQuestionLength = 0;
        public const int MaxAlternativeQuestionLength = 2048;

        public const int MinLanguageLength = 1;
        public const int MaxLanguageLength = 128;

        public const int MinImageFileNameLength = 0;
        public const int MaxImageFileNameLength = 256;

        public const int MaxPictureSize = 102400;

        public const int MinLineTokenLength = 0;
        public const int MaxLineTokenLength = 1024;

        public const int MinFacebookAccessTokenLength = 0;
        public const int MaxFacebookAccessTokenLength = 1024;

        public const int MinFacebookVerifyTokenLength = 0;
        public const int MaxFacebookVerifyTokenLength = 1024;

        public const int MinFacebookSecretKeyLength = 0;
        public const int MaxFacebookSecretKeyLength = 1024;

        public const int MinWebApiSecretLength = 0;
        public const int MaxWebApiSecretLength = 128;

        public const int MinWebhookSecretLength = 0;
        public const int MaxWebhookSecretLength = 128;


        public const float DefaultPredThreshold = 0.7f;
        public const float DefaultWSPredThreshold = 0.75f;
        public const float DefaultSuggestionThreshold = 0.6f;


        public const float MinPredThresholdValue = 0.1f;
        public const float MaxPredThresholdValue = 1.0f;

        public const float MinWSPredThresholdValue = 0.1f;
        public const float MaxWSPredThresholdValue = 1.0f;

        public const float MinSuggestionThresholdValue = 0.1f;
        public const float MaxSuggestionThresholdValue = 1.0f;


        public const int MinOPENAIOrgLength = 0;
        public const int MaxOPENAIOrgLength = 128;

        public const int MinOPENAIKeyLength = 0;
        public const int MaxOPENAIKeyLength = 128;

        public const int MinOpenAIParamLength = 0;
        public const int MaxOpenAIParamLength = 1024;

        public enum EnableGPTType : int
        {
            Disabled = 0,
            UsingSystem = 1,
            UsingPrivate = 2            
        }


        public class TrainingStatus
        {
            public const int Unknown = 0;
            public const int NotTraining = 1;
            public const int RequireRetraining = 10;
            public const int Queueing = 100;
            public const int Training = 200;
            public const int Trained = 1000;
            public const int Cancelled = 2000;
            public const int Failed = 2001;
        }
    }
}