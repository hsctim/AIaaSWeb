using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AIaaS.Web.Areas.App.Models.ContactUs;
using ApiProtectorDotNet;
using Abp.Net.Mail;
using Microsoft.Extensions.Configuration;
using AIaaS.Configuration;
using System.Net.Mail;
using AIaaS.MultiTenancy;
using AIaaS.Authorization.Users;

namespace AIaaS.Web.Controllers
{
    public class ContactUsController : AIaaSControllerBase
    {
        private readonly IEmailSender _emailSender;
        private readonly IConfigurationRoot _appConfiguration;
        private readonly UserManager _userManager;
        private readonly TenantManager _tenantManager;

        public ContactUsController(
            IEmailSender emailSender,
            IAppConfigurationAccessor configurationAccessor,
            UserManager userManager,
            TenantManager tenantManager
            )
        {
            _emailSender = emailSender;
            _appConfiguration = configurationAccessor.Configuration;
            _userManager = userManager;
            _tenantManager = tenantManager;
        }


        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<ActionResult> Index()
        {
            var model = new ContactUsViewModel()
            {
                Name = (await GetCurrentUserAsync())?.FullName,
                EMail = (await GetCurrentUserAsync())?.EmailAddress,
            };

            return View(model);
        }

        [ApiProtector(ApiProtectionType.ByIpAddress, Limit: 10, TimeWindowSeconds: 60)]
        public async Task SendEmail(SendEmailModal sendEmailModal)
        {
            var supportEMail = _appConfiguration["App:ContactUsEmail"];
            var user = await GetCurrentUserAsync();
            var tenant = await GetCurrentTenantAsync();

            //emailTemplate.Replace("{EMAIL_BODY}", mailMessage.ToString());
            string emailBody = $"UserId: {user?.Id}\nUserName: {user?.Name}\nUserFullName: {user?.FullName}\n\nTenantId: {tenant?.Id}\nTenantName: {tenant?.Name}\n\nName: {sendEmailModal.Name} \nEMail: {sendEmailModal.EMail} \nMessage:\n{sendEmailModal.Message}";

            await _emailSender.SendAsync(new MailMessage
            {
                To = { supportEMail },
                Subject = "Contact Us",
                Body = emailBody,
                IsBodyHtml = false,
            });
        }

        protected virtual async Task<User> GetCurrentUserAsync()
        {
            if (!AbpSession.UserId.HasValue)
                return null;

            var user = await _userManager.GetUserByIdAsync(AbpSession.UserId.Value);
            return user;
        }

        protected virtual async Task<Tenant> GetCurrentTenantAsync()
        {
            if (!AbpSession.TenantId.HasValue)
                return null;

            using (CurrentUnitOfWork.SetTenantId(null))
            {
                return await _tenantManager.GetByIdAsync(AbpSession.TenantId.Value);
            }
        }
    }
}