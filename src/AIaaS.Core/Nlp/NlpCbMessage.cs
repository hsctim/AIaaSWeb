using AIaaS.Nlp;
using AIaaS.Authorization.Users;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Entities;

namespace AIaaS.Nlp
{
	[Table("NlpCbMessages")]
    public class NlpCbMessage : Entity<Guid> , IMustHaveTenant
    {
			public int TenantId { get; set; }
			

        public virtual Guid? ClientId { get; set; }

        [Required]
        [StringLength(NlpCbMessageConsts.MaxNlpMessageLength, MinimumLength = NlpCbMessageConsts.MinNlpMessageLength)]
        public virtual string NlpMessage { get; set; }

        [StringLength(NlpCbMessageConsts.MaxNlpMessageTypeLength, MinimumLength = NlpCbMessageConsts.MinNlpMessageTypeLength)]
        public virtual string NlpMessageType { get; set; }

        [Required]
        [StringLength(NlpCbMessageConsts.MaxNlpSenderRoleLength, MinimumLength = NlpCbMessageConsts.MinNlpSenderRoleLength)]
        public virtual string NlpSenderRole { get; set; }

        [StringLength(NlpCbMessageConsts.MaxNlpReceiverRoleLength, MinimumLength = NlpCbMessageConsts.MinNlpReceiverRoleLength)]
        public virtual string NlpReceiverRole { get; set; }

        public virtual DateTime NlpCreationTime { get; set; }

        public virtual DateTime? ClientReadTime { get; set; }

        [StringLength(NlpCbMessageConsts.MaxAlternativeQuestionLength, MinimumLength = NlpCbMessageConsts.MinAlternativeQuestionLength)]
        public virtual string AlternativeQuestion { get; set; }

        public virtual DateTime? AgentReadTime { get; set; }

        public virtual Guid? QAAccuracyId { get; set; }

        public virtual bool Invalid { get; set; }

		public virtual Guid? NlpChatbotId { get; set; }
		
        [ForeignKey("NlpChatbotId")]
		public NlpChatbot NlpChatbotFk { get; set; }
		
		public virtual long? NlpAgentId { get; set; }
		
        [ForeignKey("NlpAgentId")]
		public User NlpAgentFk { get; set; }

        [ForeignKey("ClientId")]
        public NlpLineUser NlpLineUserFk { get; set; }

        [ForeignKey("ClientId")]
        public NlpFacebookUser NlpFacebookUserFk { get; set; }
		
		public virtual Guid? QAId { get; set; }
		
        [ForeignKey("QAId")]
		public NlpQA QAFk { get; set; }
		
    }
}