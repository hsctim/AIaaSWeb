using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIaaS.Nlp.ImExport
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class NlpCbDictionaryImExport
    {
        public Guid Id { get; set; }

        public Guid NlpChatbotId { get; set; }

        public string Word { get; set; }

        public string Synonym { get; set; }

        public string Vector { get; set; }

        public bool IsDisabled { get; set; }

        public int Scope { get; set; }

        public int Type { get; set; }

        public string Language { get; set; }
    }
}
