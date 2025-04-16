using Abp.Application.Services.Dto;
using System;

namespace AIaaS.Nlp.Dtos
{
    public class GetAllNlpTokensInput : PagedAndSortedResultRequestDto
    {
        public string Filter { get; set; }
    }
}