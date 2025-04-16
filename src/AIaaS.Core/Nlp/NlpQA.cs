using AIaaS.Nlp;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Entities;

namespace AIaaS.Nlp
{
    [Table("NlpQAs")]
    public class NlpQA : AuditedEntity<Guid>, IMustHaveTenant, IHasDeletionTime, ISoftDelete
    {
        public virtual bool IsDeleted { get; set; }
        public virtual DateTime? DeletionTime { get; set; }

        public int TenantId { get; set; }

		[Required]
		[StringLength(NlpQAConsts.MaxQuestionLength, MinimumLength = NlpQAConsts.MinQuestionLength)]
		public virtual string Question { get; set; }
		
		[StringLength(NlpQAConsts.MaxAnswerLength, MinimumLength = NlpQAConsts.MinAnswerLength)]
		public virtual string Answer { get; set; }
		
		[StringLength(NlpQAConsts.MaxQuestionCategoryLength, MinimumLength = NlpQAConsts.MinQuestionCategoryLength)]
		public virtual string QuestionCategory { get; set; }
		
		
		[Required]
		public virtual int NNID { get; set; }
		
		public virtual int QaType { get; set; }
		
		public virtual int QuestionCount { get; set; }
		

		public virtual Guid NlpChatbotId { get; set; }
		
        [ForeignKey("NlpChatbotId")]
		public NlpChatbot NlpChatbotFk { get; set; }
		
		public virtual Guid? CurrentWfState { get; set; }
		
        [ForeignKey("CurrentWfState")]
        public NlpWorkflowState CurrentWfStateFk { get; set; }

        [ForeignKey("CurrentWfState")]
        public NlpWorkflow CurrentWfAllFk { get; set; }
        //自助服務牆流程 : *

        public virtual Guid? NextWfState { get; set; }

        [ForeignKey("NextWfState")]
        public NlpWorkflowState NextWfStateFk { get; set; }

    }
}