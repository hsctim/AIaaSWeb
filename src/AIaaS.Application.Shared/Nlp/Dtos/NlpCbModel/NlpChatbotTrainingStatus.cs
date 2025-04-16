using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIaaS.Nlp.Lib.Dtos
{
    public class NlpChatbotTrainingStatus
    {
        public int TrainingStatus { get; set; }
        public int TrainingProgress { get; set; }
        //public int TrainingSpent { get; set; }  //seconds
        public int TrainingRemaining { get; set; } //seconds
        public int QueueRemaining { get; set; } //seconds

    }
}
