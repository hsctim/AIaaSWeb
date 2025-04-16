using System;
using System.Collections.Generic;
using System.Text;

namespace AIaaS.Nlp.Dtos.NlpCbMessage
{
    public class PredictedQAMessage
    {
        public Guid QaId { get; set; }
        public string Message { get; set; }
    }
}
