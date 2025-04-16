using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AIaaS.Nlp.Dtos.NlpQA
{
    public class CreaetQAForAccuracyDto
    {
        [StringLength(NlpQAConsts.MaxAnswerLength, MinimumLength = NlpQAConsts.MinAnswerLength)]
        public string NlpQuestion { get; set; }

        public Guid NlpQAId { get; set; }
    }
}
