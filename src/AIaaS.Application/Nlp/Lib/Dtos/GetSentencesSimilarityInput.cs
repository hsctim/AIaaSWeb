using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIaaS.Nlp.Lib.Dtos
{
    internal class GetSentencesSimilarityInput
    {
        public string secuToken { get; set; }
        public string src { get; set; }

        public IList<IList<string>> refs { get; set; }
    }
}
