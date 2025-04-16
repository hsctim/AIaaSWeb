using AIaaS.Nlp;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Entities;

namespace AIaaS.Nlp
{
    [Table("NlpWorkflows")]
    public class NlpWorkflow : AuditedEntity<Guid>, IMustHaveTenant, IHasDeletionTime, ISoftDelete
    {
        public virtual bool IsDeleted { get; set; }
        public virtual DateTime? DeletionTime { get; set; }
        public int TenantId { get; set; }


		[StringLength(NlpWorkflowConsts.MaxNameLength, MinimumLength = NlpWorkflowConsts.MinNameLength)]
		public virtual string Name { get; set; }
		
		public virtual bool Disabled { get; set; }
		
		[StringLength(NlpWorkflowConsts.MaxVectorLength, MinimumLength = NlpWorkflowConsts.MinVectorLength)]
		public virtual string Vector { get; set; }
		

		public virtual Guid NlpChatbotId { get; set; }
		
        [ForeignKey("NlpChatbotId")]
		public NlpChatbot NlpChatbotFk { get; set; }
		
    }
}