using System;
using System.Collections.Generic;
using System.Text;

namespace AIaaS.Nlp.Dtos.NlpCbModel
{
    public class NlpCbMIncompleteTrainingInputDto
    {
        public String SecuToken { set; get; }
        public Guid ChatbotId { set; get; }
    }
}