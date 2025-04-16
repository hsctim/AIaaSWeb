using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;
using Newtonsoft.Json;

namespace AIaaS.Nlp.Dtos
{
    public class NlpQADto : EntityDto<Guid>
    {
        public int TenantId { get; set; }

        public string Question { get; set; }

        public string Answer { get; set; }

        public string QuestionCategory { get; set; }

        public int NNID { get; set; }

        public int? QaType { get; set; }
        //0 or null: Default Type
        //1: 系統預設的False Acceptance
        public Guid NlpChatbotId { get; set; }
        public Guid? CurrentWfState { get; set; }
        public Guid? NextWfState { get; set; }

        public List<string> GetQuestionList()
        {
            try
            {
                return JsonConvert.DeserializeObject<List<string>>(Question);

            }
            catch (Exception)
            {
                return new List<string>();
            }
        }
    }
}