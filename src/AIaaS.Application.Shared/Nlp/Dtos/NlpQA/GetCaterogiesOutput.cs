using System;
using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace AIaaS.Nlp.Dtos
{
    public class GetCaterogiesOutput
    {
        public string SelectItem { get; set; }

        public List<string> Caterogies { get; set; }
    }
}