using Abp.Zero.Ldap.Authentication;
using Abp.Zero.Ldap.Configuration;
using AIaaS.Authorization.Users;
using AIaaS.MultiTenancy;

namespace AIaaS.Authorization.Ldap
{
    public class AppLdapAuthenticationSource : LdapAuthenticationSource<Tenant, User>
    {
        public AppLdapAuthenticationSource(ILdapSettings settings, IAbpZeroLdapModuleConfig ldapModuleConfig)
            : base(settings, ldapModuleConfig)
        {
        }
    }
}