using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIaaS.Nlp.Lib.Dtos
{
    public class NlpCbCancelTrainingInput
    {
        public string secuToken { get; set; }
        public Guid chatbotId { get; set; }
    }
}
