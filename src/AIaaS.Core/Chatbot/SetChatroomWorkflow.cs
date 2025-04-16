using Abp.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AIaaS.Nlp.Dtos
{

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class SetChatroomWorkflow
    {
        public Guid ChatbotId { get; set; }
        public Guid ClientId { get; set; }
        public string WorkflowName { get; set; }
        public string WorkflowStateName { get; set; }
    }
}