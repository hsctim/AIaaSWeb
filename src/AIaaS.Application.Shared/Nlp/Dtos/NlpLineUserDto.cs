using System;
using Abp.Application.Services.Dto;

namespace AIaaS.Nlp.Dtos
{
    public class NlpLineUserDto : EntityDto<Guid>
    {
        public virtual string UserId { get; set; }
        public virtual string UserName { get; set; }
        public virtual string PictureUrl { get; set; }
    }
}