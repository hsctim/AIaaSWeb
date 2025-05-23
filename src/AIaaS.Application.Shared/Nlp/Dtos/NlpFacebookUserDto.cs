﻿using System;
using Abp.Application.Services.Dto;

namespace AIaaS.Nlp.Dtos
{
    public class NlpFacebookUserDto : EntityDto<Guid>
    {
        public string UserId { get; set; }

        public string UserName { get; set; }

        public string PictureUrl { get; set; }
    }
}