
using System;
using Abp.Application.Services.Dto;

namespace AIaaS.Nlp.Dtos
{
    public class NlpTokenDto : EntityDto<Guid>
    {
		public string NlpTokenType { get; set; }

		public string NlpTokenValue { get; set; }



    }
}