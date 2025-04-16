using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIaaS.Nlp.Model
{
    public class ChatbotTrainingWaitingStatus
    {
        public double TrainingProgress { get; set; }
        //public TimeSpan TrainingSpent { get; set; }
        public TimeSpan TrainingRemaining { get; set; }
        public TimeSpan QueueRemaining { get; set; }
    }
}
