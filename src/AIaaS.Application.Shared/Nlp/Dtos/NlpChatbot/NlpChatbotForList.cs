using System;
using Abp.Application.Services.Dto;
using Newtonsoft.Json;

namespace AIaaS.Nlp.Dtos
{
    public class NlpChatbotForList : EntityDto<Guid>
    {
        public string Name { get; set; }

        public string GreetingMsg { get; set; }

        public string FailedMsg { get; set; }

        public string AlternativeQuestion { get; set; }

        //public string Language { get; set; }

        public Guid? ChatbotPictureId { get; set; }

        public bool Disabled { get; set; }

        public bool EnableWebChat { get; set; }

    }
}