using AIaaS.Nlp;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Entities;

namespace AIaaS.Nlp
{
    [Table("NlpWorkflowStates")]
    public class NlpWorkflowState : AuditedEntity<Guid>, IMustHaveTenant, IHasDeletionTime, ISoftDelete
    {
        public virtual bool IsDeleted { get; set; }
        public virtual DateTime? DeletionTime { get; set; }
        public int TenantId { get; set; }


        [Required]
        [StringLength(NlpWorkflowStateConsts.MaxStateNameLength, MinimumLength = NlpWorkflowStateConsts.MinStateNameLength)]
        public virtual string StateName { get; set; }

        [StringLength(NlpWorkflowStateConsts.MaxStateInstructionLength, MinimumLength = NlpWorkflowStateConsts.MinStateInstructionLength)]
        public virtual string StateInstruction { get; set; }

        [StringLength(NlpWorkflowStateConsts.MaxVectorLength, MinimumLength = NlpWorkflowStateConsts.MinVectorLength)]
        public virtual string Vector { get; set; }

        [StringLength(NlpWorkflowStateConsts.MaxOutgoingFalseOpLength, MinimumLength = NlpWorkflowStateConsts.MinOutgoingFalseOpLength)]
        public virtual string OutgoingFalseOp { get; set; }

        [StringLength(NlpWorkflowStateConsts.MaxOutgoing3FalseOpLength, MinimumLength = NlpWorkflowStateConsts.MinOutgoing3FalseOpLength)]
        public virtual string Outgoing3FalseOp { get; set; }

        public virtual bool ResponseNonWorkflowAnswer { get; set; }

        public virtual bool DontResponseNonWorkflowErrorAnswer { get; set; }


        public virtual Guid NlpWorkflowId { get; set; }

        [ForeignKey("NlpWorkflowId")]
        public NlpWorkflow NlpWorkflowFk { get; set; }

    }
}