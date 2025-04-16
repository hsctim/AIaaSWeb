using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIaaS.Nlp.ImExport
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class NlpImportExport
    {
        public NlpChatbotImExport Chatbot { get; set; }
        public List<NlpQAImExport> QAs { get; set; }
        public List<NlpCbDictionaryImExport> Dictionaries { get; set; }
        public List<NlpWorkflowImExport> Workflows { get; set; }
        public List<NlpWorkflowStateImExport> WorkflowStates { get; set; }
    }
}
