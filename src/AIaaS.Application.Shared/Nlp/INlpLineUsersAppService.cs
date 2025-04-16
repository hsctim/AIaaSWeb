using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using AIaaS.Nlp.Dtos;
using AIaaS.Dto;

namespace AIaaS.Nlp
{
    public interface INlpLineUsersAppService : IApplicationService
    {
        NlpLineUserDto GetNlpLineUserDto(Guid clientId);

        NlpLineUserDto GetNlpLineUserDto(string lineUserId, string channelAccessToken);
    }
}