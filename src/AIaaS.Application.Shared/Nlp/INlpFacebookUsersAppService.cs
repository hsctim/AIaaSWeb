using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using AIaaS.Nlp.Dtos;
using AIaaS.Dto;

namespace AIaaS.Nlp
{
    public interface INlpFacebookUsersAppService : IApplicationService
    {
        NlpFacebookUserDto GetNlpFacebookUserDto(Guid clientId);

        Task<NlpFacebookUserDto> GetNlpFacebookUserDtoAsync(Guid chatbotId, string facebookUserId);

    }
}