﻿using System.Threading.Tasks;
using Abp.Domain.Policies;

namespace AIaaS.Authorization.Users
{
    public interface IUserPolicy : IPolicy
    {
        Task CheckMaxUserCountAsync(int tenantId);
    }
}
