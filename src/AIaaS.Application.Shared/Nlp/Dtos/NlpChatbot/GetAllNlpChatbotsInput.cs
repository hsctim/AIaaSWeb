using Abp.Application.Services.Dto;
using System;

namespace AIaaS.Nlp.Dtos
{
    public class GetAllNlpChatbotsInput : PagedAndSortedResultRequestDto
    {
        public string Filter { get; set; }

    }
}
