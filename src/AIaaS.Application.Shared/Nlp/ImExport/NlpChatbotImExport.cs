using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIaaS.Nlp.ImExport
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class NlpChatbotImExport
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string GreetingMsg { get; set; }

        public string FailedMsg { get; set; }

        public string AlternativeQuestion { get; set; }

        public string Language { get; set; }

        public bool Disabled { get; set; }

        //public string LineToken { get; set; }

        //public string FacebookAccessToken { get; set; }

        //public string FacebookVerifyToken { get; set; }

        //public string FacebookSecretKey { get; set; }

        public bool EnableWebChat { get; set; }

        public bool EnableHttpChat { get; set; }

        public bool EnableFacebook { get; set; }

        public bool EnableLine { get; set; }

        public string ProfileImage { get; set; }    //base64 image

        public virtual float PredThreshold { get; set; } = NlpChatbotConsts.DefaultPredThreshold;

        public virtual float WSPredThreshold { get; set; } = NlpChatbotConsts.DefaultWSPredThreshold;

        public virtual float SuggestionThreshold { get; set; } = NlpChatbotConsts.DefaultSuggestionThreshold;

    }
}
