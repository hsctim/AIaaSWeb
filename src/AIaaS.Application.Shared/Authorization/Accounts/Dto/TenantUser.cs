using System;
using System.Collections.Generic;
using System.Text;



namespace AIaaS.Authorization.Accounts.Dto
{
    public class TenantUser
    {

        public int? TenantId { set; get; }
        public string TenantName { set; get; }

        public long UserId { set; get; }
        public string UserName { set; get; }

        public string EmailAddress { set; get; }


    }
}
