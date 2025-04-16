using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIaaS.Nlp.ImExport
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class NlpQAImExport
    {
        public Guid Id { get; set; }
        public string Question { get; set; }

        public string Answer { get; set; }

        public string QuestionCategory { get; set; }

        //public int NNID { get; set; }

        public int? QaType { get; set; }
        //0 or null: Default Type
        //1: 系統預設的False Acceptance
        public Guid NlpChatbotId { get; set; }
        public Guid? CurrentWfState { get; set; }
        public Guid? NextWfState { get; set; }

    }
}
