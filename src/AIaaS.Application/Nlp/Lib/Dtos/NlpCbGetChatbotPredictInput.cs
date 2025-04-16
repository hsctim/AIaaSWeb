using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIaaS.Nlp.Lib.Dtos
{
    public class NlpCbGetChatbotPredictInput
    {
        public Guid chatbotId { get; set; }
        //public string language { get; set; }
        public string question { get; set; }
        public string state { get; set; }
        public string secuToken { get; set; }
    }
}
