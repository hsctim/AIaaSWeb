using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIaaS.Nlp.ImExport
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class NlpWorkflowImExport
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public bool Disabled { get; set; }

        public Guid NlpChatbotId { get; set; }
    }
}
