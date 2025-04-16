using System;
using Abp.Application.Services.Dto;

namespace AIaaS.Nlp.Dtos
{
    public class NlpWorkflowStateSelection
    {
        public Guid WfId { get; set; }
        public Guid? WfsId { get; set; }

        public string WfName { get; set; }
        public string WfsName { get; set; }

    }
}