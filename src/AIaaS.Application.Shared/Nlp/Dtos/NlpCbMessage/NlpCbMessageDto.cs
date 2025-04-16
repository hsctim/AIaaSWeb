using System;
using Abp.Application.Services.Dto;

namespace AIaaS.Nlp.Dtos
{
    public class NlpCbMessageDto : EntityDto<Guid>
    {
        public string NlpMessage { get; set; }

        public string NlpSenderRole { get; set; }

        public string NlpReceiverRole { get; set; }

        public DateTime NlpCreationTime { get; set; }

        public Guid? NlpChatbotId { get; set; }

        public long? NlpStaffId { get; set; }

        public long? NlpUserId { get; set; }

        public Guid? ClientId { get; set; }

        public Guid? QAId { get; set; }

    }
}