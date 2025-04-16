using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace AIaaS.Nlp.Dtos
{
    public class GetNlpTokenForEditOutput
    {
		public CreateOrEditNlpTokenDto NlpToken { get; set; }


    }
}