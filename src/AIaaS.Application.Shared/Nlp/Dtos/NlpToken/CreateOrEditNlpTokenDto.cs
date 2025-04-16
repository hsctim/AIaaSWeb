
using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace AIaaS.Nlp.Dtos
{
    public class CreateOrEditNlpTokenDto : EntityDto<Guid?>
    {

		[Required]
		[StringLength(NlpTokenConsts.MaxNlpTokenTypeLength, MinimumLength = NlpTokenConsts.MinNlpTokenTypeLength)]
		public string NlpTokenType { get; set; }
		
		
		[Required]
		[StringLength(NlpTokenConsts.MaxNlpTokenValueLength, MinimumLength = NlpTokenConsts.MinNlpTokenValueLength)]
		public string NlpTokenValue { get; set; }
		
		

    }
}