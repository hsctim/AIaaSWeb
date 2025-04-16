using System;
using System.Collections.Generic;
using System.Text;

namespace AIaaS.Nlp.Dtos.NlpCbTrainedAnswer
{
    public class NlpCbTAChatbotTrainingInsertDto
    {
        public class NlpCbTAChatbotTrainingInsertItem
        {
            public int NNID { get; set; }
            public string Answer { get; set; }
        }

        public Guid NlpCbTrainingDataId { get; set; }
        public List<NlpCbTAChatbotTrainingInsertItem> Answers { get; set; }
    }
}