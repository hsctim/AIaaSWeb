using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using AIaaS.Nlp.Dtos.NlpQA.ValidationAttribute;

namespace AIaaS.Nlp.Dtos
{
    public class CbAnswerSet
    {
        public string Answer { get; set; }
        public bool GPT { get; set; }
    }

    public class CreateOrEditNlpQADto : EntityDto<Guid?>
    {
        [StringArrayMaxLengthAttribute(NlpQAConsts.MaxQuestionLength)]
        public IEnumerable<string> Questions { get; set; }

        [StringArrayMaxLengthAttribute(NlpQAConsts.MaxAnswerLength)]
        public IEnumerable<CbAnswerSet> AnswerSets { get; set; }

        [StringLength(NlpQAConsts.MaxQuestionCategoryLength, MinimumLength = NlpQAConsts.MinQuestionCategoryLength)]
        public string QuestionCategory { get; set; }

        //public int SegmentStatus { get; set; }

        //[StringLength(1024, MinimumLength = 0)]
        //public string SegmentErrorMsg { get; set; }

        [StringLength(64, MinimumLength = 0)]
        public string NlpChatbotLanguage { get; set; }

        public int NNID { get; set; }

        public virtual int? QaType { get; set; }

        public Guid? NlpChatbotId { get; set; }
        public Guid? CurrentWfState { get; set; }
        public Guid? NextWfState { get; set; }

        //public bool EnabledWorkflow { get; set; }

    }
}