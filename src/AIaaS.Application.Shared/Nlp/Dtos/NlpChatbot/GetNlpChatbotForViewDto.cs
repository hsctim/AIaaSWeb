using AIaaS.Nlp.Lib.Dtos;

namespace AIaaS.Nlp.Dtos
{
    public class GetNlpChatbotForViewDto
    {
        public NlpChatbotForList NlpChatbot { get; set; }

        public NlpChatbotTrainingStatus TrainingStatus { get; set; }


    }
}