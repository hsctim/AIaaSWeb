using Abp.Application.Services.Dto;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIaaS.Nlp.Dtos.NlpChatbot
{

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ExportedImportedChatbotDictionaryData
    {
        public string Word { get; set; }

        public string Synonym { get; set; }

        public string Vector { get; set; }

        public bool IsDisabled { get; set; }

        public int Scope { get; set; }

        public int Type { get; set; }

        public string Language { get; set; }
    }


    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ExportedImportedChatbotData : ExportedImportedQaData
    {
        public string Name { get; set; }

        public string GreetingMsg { get; set; }

        public string FailedMsg { get; set; }

        public string AlternativeQuestion { get; set; }

        public string Language { get; set; }

        public bool Disabled { get; set; }

        public string LineToken { get; set; }

        public string FacebookAccessToken { get; set; }

        public string FacebookVerifyToken { get; set; }

        public string FacebookSecretKey { get; set; }

        public bool EnableWebChat { get; set; }

        public bool EnableHttpChat { get; set; }

        public bool EnableFacebook { get; set; }

        public bool EnableLine { get; set; }

        public string ProfileImage { get; set; }

        public List<ExportedImportedChatbotDictionaryData> DictionaryList { get; set; }
    }
}
