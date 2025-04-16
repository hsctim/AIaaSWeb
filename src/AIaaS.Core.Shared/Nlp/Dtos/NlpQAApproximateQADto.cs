using System;
using System.Collections.Generic;
using System.Text;

namespace AIaaS.Nlp.Dtos
{
    public class NlpQAApproximateQADto
    {
        public string Question;
        public List<string> ApproximateQuestion;

        public string Answer;
        public List<string> ApproximateAnswer;
    }
}