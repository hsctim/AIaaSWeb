using AIaaS.Nlp.Dtos;
using AIaaS.Nlp.Lib.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
//using static AIaaS.Nlp.NlpCbDictionariesFunction;

namespace AIaaS.Web.Chatbot.Dto
{
    public class AllPredictMessages
    {
        public Guid InputState { get; set; }    //wf or wfs

        //public PrepareSynonymStringOutput ChatbotPredictInput { get; set; }

        public (Guid?, string)[] ChatbotPredictInput { get; set; }


        public NlpCbGetChatbotPredictResultItem ChatbotPredictResult { get; set; }

        public NlpQADto NlpQADto { get; set; }

        //public double probability { get; set; }

        public bool inWorkflowState { get; set; } = false;

        public bool inWorkflow { get; set; } = false;

        public bool inPredictionThreshold { get; set; } = false;

        public bool inSuggestionThreshold { get; set; } = false;
    }
}
