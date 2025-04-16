using System;
using Abp.Application.Services.Dto;

namespace AIaaS.Nlp.Dtos
{
    public class NlpExportedQA
    {
        public string Question { get; set; }

        public string Answer { get; set; }

        public string QuestionCategory { get; set; }

        public int NNID { get; set; }

        public Guid? CurrentWfs { get; set; }
        public Guid? NextWfs { get; set; }
    }
}