using System;
using System.Collections.Generic;
using System.Text;

namespace AIaaS.Nlp.Model
{
    public class NlpCbMCandidate
    {
        public int ChatbotTenantId { get; set; }

        public Guid ChatbotId { get; set; }

        public string Language { get; set; }
        public Guid ModelId { get; set; }
        public string TrainingData { get; set; }

        public int TrainingStatus { set; get; }

        public bool RebuildModel { get; set; }
    }
}