using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;

namespace AIaaS.Nlp.Dtos
{
    public class NlpCbQAAccuracyDto : EntityDto<Guid>
    {
        public class NlpCbQAAccuracyAnswerDto
        {
            public Guid? AnswerId { get; set; }
            public double? AnswerAcc { get; set; }

            public IList<string> Question { get; set; }
            public IList<CbAnswerSet> Answer { get; set; }

        };

        public string Question { get; set; }

        public List<NlpCbQAAccuracyAnswerDto> AnswerPredict { get; set; }

        public Guid NlpChatbotId { get; set; }

        public DateTime CreationTime { get; set; }

        public bool UnanswerableQuestion { get; set; }

    }
}