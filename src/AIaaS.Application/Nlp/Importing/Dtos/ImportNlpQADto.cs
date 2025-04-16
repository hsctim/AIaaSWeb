using System;
using System.Collections.Generic;

namespace AIaaS.Nlp.Importing.Dtos
{
    public class ImportNlpQADto
    {
        public string ImportId { get; set; }
        public string Category { get; set; }
        public string Question { get; set; }

        public string Answer { get; set; }

        public string CurrentWorkflow { get; set; }
        public string CurrentWorkflowState { get; set; }
        public string NextWorkflow { get; set; }
        public string NextWorkflowState { get; set; }

        public Guid CurrentWorkflowStateId { get; set; }
        public Guid NextWorkflowStateId { get; set; }

        /// <summary>
        /// Can be set when reading data from excel or when importing user
        /// </summary>
        public string Exception { get; set; }

        public bool CanBeImported()
        {
            return string.IsNullOrEmpty(Exception);
        }

        public Guid? QAId { get; set; }

        public List<string> QuestionList { get; set; }
        public List<string> AnswerList { get; set; }
    }
}