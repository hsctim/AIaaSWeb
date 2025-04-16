using System;
using Abp.Application.Services.Dto;

namespace AIaaS.Nlp.Dtos
{
    public class NlpWorkflowStateDto : EntityDto<Guid>
    {
        public string StateName { get; set; }

        public string StateInstruction { get; set; }

        public bool ResponseNonWorkflowAnswer { get; set; }

        public bool DontResponseNonWorkflowErrorAnswer { get; set; }

        public string OutgoingFalseOp { get; set; }
        public string Outgoing3FalseOp { get; set; }

        public Guid NlpWorkflowId { get; set; }

    }
}