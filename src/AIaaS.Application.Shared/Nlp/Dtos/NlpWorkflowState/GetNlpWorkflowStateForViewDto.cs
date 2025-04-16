namespace AIaaS.Nlp.Dtos
{
    public class GetNlpWorkflowStateForViewDto
    {
        public NlpWorkflowStateDto NlpWorkflowState { get; set; }

        public string NlpChatbotName { get; set; }
        public string NlpWorkflowName { get; set; }

    }
}