using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIaaS.Nlp.ImExport
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class NlpWorkflowStateImExport
    {
        public Guid Id { get; set; }

        public Guid NlpWorkflowId { get; set; }

        public string StateName { get; set; }

        public string StateInstruction { get; set; }

        public bool ResponseNonWorkflowAnswer { get; set; }

        public bool DontResponseNonWorkflowErrorAnswer { get; set; }

        public string OutgoingFalseOp { get; set; }

        public string Outgoing3FalseOp { get; set; }
    }
}
