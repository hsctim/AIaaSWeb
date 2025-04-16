using System;

namespace AIaaS.Nlp.Dtos
{
    public class GetNlpCbModelForViewDto
    {
        public NlpCbModelDto NlpCbModel { get; set; }

        public string NlpChatbotName { get; set; }

        public string NlpCbMTrainingCancellationUser { get; set; }

        public string NlpCbMCreatorUser { get; set; }

        public DateTime? NlpCbMCreationTime { get; set; }

        public double? NlpCbAccuracy { get; set; }
    }
}