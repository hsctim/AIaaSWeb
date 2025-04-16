using AIaaS.Nlp;
using AIaaS.Nlp.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIaaS.Web.Chatbot.Dto
{
    public class NlpCbMessageEx
    {

        public NlpCbMessageEx()
        {
        }

        public NlpCbMessageEx(NlpCbMessage nlpCbMessage)
        {
            NlpCbMessage = nlpCbMessage;
        }

        public NlpCbMessageEx(NlpCbMessage nlpCbMessage, IList<string> suggestedAnswers)
        {
            NlpCbMessage = nlpCbMessage;
            SuggestedAnswers = suggestedAnswers;
        }


        public IList<string> SuggestedAnswers { get; set; }

        public NlpCbMessage NlpCbMessage;
    }
}
