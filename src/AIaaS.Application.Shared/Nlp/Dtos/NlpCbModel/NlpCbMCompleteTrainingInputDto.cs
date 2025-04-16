using System;
using System.Collections.Generic;
using System.Text;

namespace AIaaS.Nlp.Dtos.NlpCbModel
{
    public class NlpCbMCompleteTrainingInputDto
    {
        public String SecuToken { set; get; }
        public Guid ChatbotId { set; get; }

        public Dictionary<int, int[]> NnidRepeated { set; get; }

        public double ModelAccuracy { set; get; }
    }
}