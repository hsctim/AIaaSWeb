using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIaaS.Nlp.Lib.Dtos
{
    public class NlpCbGetChatbotPredictResultItem
    {
        public int nnid { get; set; }
        public double probability { get; set; }

        //public bool InWorkflowResult { get; set; }

        public Guid QaId { get; set; }

        //public double probabilityFitW
        //{
        //    get
        //    {
        //        if (InWorkflowResult)
        //            return Math.Max(0, probability - 0.3);
        //        else
        //            return probability;
        //    }
        //}
    }

    public class NlpCbGetChatbotPredictResult
    {

        public string errorCode { get; set; }

        public NlpCbGetChatbotPredictResultItem[] result { get; set; }
    }
}
