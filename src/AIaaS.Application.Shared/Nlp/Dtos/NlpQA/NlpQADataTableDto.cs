using System;
using Abp.Application.Services.Dto;

namespace AIaaS.Nlp.Dtos
{
    public class NlpQADataTableDto : EntityDto<Guid>
    {
        public string Question { get; set; }

        public string Answer { get; set; }

        public string QuestionCategory { get; set; }

        public virtual int? QaType { get; set; }

        public string CurrentWf { get; set; }
        public string CurrentWfState { get; set; }
        public string NextWf { get; set; }
        public string NextWfState { get; set; }
        public Guid NextWfStateId { get; set; }

        //public int SegmentStatus { get; set; }


        //public string SegmentErrorMsg { get; set; }
    }
}