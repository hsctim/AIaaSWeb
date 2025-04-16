using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIaaS.Nlp.Model
{
    public class NlpTrainingModelPriority
    {
        public int Tenant { get; set; }
        public double Priority { get; set; }
        public Guid ChatbotId { get; set; }
        public double TrainingCost { get; set; }
    }
}
