using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIaaS.Nlp.Lib.Dtos
{
    public class NlpCbGetTrainingStatus
    {
        public string ErrorCode { get; set; }
        public Guid ChatbotId { get; set; }
        public double TimeCost { get; set; }
        public double Progress { get; set; }
    }
}
