using Abp.Auditing;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIaaS.Authorization.Accounts.Dto
{
    public class GetAllUsersByEmailPasswordDto
    {
        public string EmailAddress { set; get; }


        [DisableAuditing]
        public string Password { set; get; }
    }
}
