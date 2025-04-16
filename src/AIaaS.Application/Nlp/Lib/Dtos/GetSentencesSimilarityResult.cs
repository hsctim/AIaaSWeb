using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIaaS.Nlp.Lib.Dtos
{
    public class GetSentencesSimilarityResult
    {
        public string errorCode { get; set; }

        public IList<float> similarities { get; set; }
    }
}
