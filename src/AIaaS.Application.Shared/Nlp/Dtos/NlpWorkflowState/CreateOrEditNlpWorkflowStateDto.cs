using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace AIaaS.Nlp.Dtos
{
    public class CreateOrEditNlpWorkflowStateDto : EntityDto<Guid?>
    {

        [Required]
        [StringLength(NlpWorkflowStateConsts.MaxStateNameLength, MinimumLength = NlpWorkflowStateConsts.MinStateNameLength)]
        public string StateName { get; set; }

        [StringLength(NlpWorkflowStateConsts.MaxStateInstructionLength, MinimumLength = NlpWorkflowStateConsts.MinStateInstructionLength)]
        public string StateInstruction { get; set; }

        //[StringLength(NlpWorkflowStateConsts.MaxVectorLength, MinimumLength = NlpWorkflowStateConsts.MinVectorLength)]
        //public string Vector { get; set; }

        //[StringLength(NlpWorkflowStateConsts.MaxAcceptIncomingStatusLength, MinimumLength = NlpWorkflowStateConsts.MinAcceptIncomingStatusLength)]
        //public string AcceptIncomingStatus { get; set; }

        [StringLength(NlpWorkflowStateConsts.MaxOutgoingFalseOpLength, MinimumLength = NlpWorkflowStateConsts.MinOutgoingFalseOpLength)]
        public string OutgoingFalseOp { get; set; }

        [StringLength(NlpWorkflowStateConsts.MaxOutgoing3FalseOpLength, MinimumLength = NlpWorkflowStateConsts.MinOutgoing3FalseOpLength)]
        public string Outgoing3FalseOp { get; set; }

        public bool ResponseNonWorkflowAnswer { get; set; }

        public bool DontResponseNonWorkflowErrorAnswer { get; set; }

        public Guid NlpWorkflowId { get; set; }



        //"FalsePredict1ResponseMsg": "",
        //"FalsePredict1Select": "00000000-0000-0000-0000-000000000001",
        //"FalsePredict3ResponseMsg": "",
        //"FalsePredict3Select": "00000000-0000-0000-0000-000000000000",
        [StringLength(NlpWorkflowStateConsts.MaxStateInstructionLength, MinimumLength = NlpWorkflowStateConsts.MinStateInstructionLength)]
        public string FalsePredict1ResponseMsg { get; set; }
        public Guid FalsePredict1Select { get; set; }

        [StringLength(NlpWorkflowStateConsts.MaxStateInstructionLength, MinimumLength = NlpWorkflowStateConsts.MinStateInstructionLength)]
        public string FalsePredict3ResponseMsg { get; set; }
        public Guid FalsePredict3Select { get; set; }


    }
}