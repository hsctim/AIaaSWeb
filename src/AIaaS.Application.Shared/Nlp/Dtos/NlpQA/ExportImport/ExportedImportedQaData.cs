using Abp.Application.Services.Dto;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIaaS.Nlp.Dtos.NlpChatbot
{
    public class WorkflowCollection
    {
        //Workflow
        public int WfNo { get; set; }
        public Guid? WfId { get; set; }
        public string WfName { get; set; }
        public bool Disabled { get; set; }
    }


    public class WorkflowStateCollection : WorkflowCollection
    {
        //WorkflowState
        public int WfsNo { get; set; }
        public Guid? WfsId { get; set; }
        public string WfsName { get; set; }
        public string StateInstruction { get; set; }
        public string OutgoingFalseOp { get; set; }
        public string Outgoing3FalseOp { get; set; }
        public bool ResponseNonWorkflowAnswer { get; set; }
        public bool DontResponseNonWorkflowErrorAnswer { get; set; }
    }


    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ExportedImportedChatbotWorkflowStateData
    {
        public int No { get; set; }
        public string StateName { get; set; }
        public string StateInstruction { get; set; }
        public string OutgoingFalseOp { get; set; }
        public string Outgoing3FalseOp { get; set; }
        public bool ResponseNonWorkflowAnswer { get; set; }
        public bool DontResponseNonWorkflowErrorAnswer { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ExportedImportedChatbotWorkflowData
    {
        public int No { get; set; }
        public string WorkflowName { get; set; }
        public bool Disabled { get; set; }

        public List<ExportedImportedChatbotWorkflowStateData> StatusList { get; set; }
    }



    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ExportedImportedChatbotQAData
    {
        public string Question { get; set; }

        public string Answer { get; set; }

        public string QuestionCategory { get; set; }

        //public int NNID { get; set; }

        /// <summary>
        /// 0 or null: Default Type
        /// 1: 系統預設的False Acceptance, 並且設定NNID=0
        /// </summary>
        public int? QaType { get; set; }

        public int? currentWfsId { get; set; }
        public int? nextWfsId { get; set; }

        public Guid? currentWfsUuid { get; set; }
        public Guid? nextWfsUuid { get; set; }

    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ExportedImportedQaData
    {

        public List<ExportedImportedChatbotQAData> QAList { get; set; }

        public List<ExportedImportedChatbotWorkflowData> WorkflowList { get; set; }
    }
}
