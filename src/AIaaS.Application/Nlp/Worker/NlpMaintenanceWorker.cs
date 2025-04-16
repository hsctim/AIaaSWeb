using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Auditing;
using Abp.Authorization.Users;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore.EFPlus;
using Abp.Logging;
using Abp.Threading;
using Abp.Threading.BackgroundWorkers;
using Abp.Threading.Timers;
using Abp.Timing;
using AIaaS.Authorization.Users;
using AIaaS.MultiTenancy;
using AIaaS.Nlp;
using AIaaS.Storage;
using Microsoft.AspNetCore.Hosting;
//using static AIaaS.Nlp.NlpTenantConsts;

namespace AIaaS.Auditing
{
    public class NlpMaintenanceWorker : PeriodicBackgroundWorkerBase, ISingletonDependency
    {
        /// <summary>
        /// Set this const field to true if you want to enable ExpiredAuditLogDeleterWorker.
        /// Be careful, If you enable this, all expired logs will be permanently deleted.
        /// </summary>
        public const bool IsEnabled = true;

        private const int CheckPeriodAsMilliseconds = 8 * 1000 * 60 * 60; // 8 Hours

        //private readonly IRepository<Tenant> _tenantRepository;
        //private readonly IRepository<NlpTenant, Guid> _nlpTenantRepository;

        private readonly IRepository<NlpCbMessage, Guid> _nlpCbMessageRepository;
        private readonly IRepository<NlpCbQAAccuracy, Guid> _nlpCbQAAccuracyRepository;
        private readonly IRepository<NlpCbModel, Guid> _nlpCbModelRepository;
        private readonly IRepository<NlpClientInfo, Guid> _nlpClientInfo;
        private readonly IRepository<NlpChatbot, Guid> _nlpChatbotRepository;
        private readonly IRepository<NlpWorkflow, Guid> _nlpWorkflowRepository;
        private readonly IRepository<AuditLog, long> _auditLogRepository;
        private readonly IRepository<User, long> _userRepository;
        private readonly IRepository<Tenant, int> _tenantRepository;
        private readonly IRepository<BinaryObject, Guid> _binaryObjectRepository;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public NlpMaintenanceWorker(
            AbpTimer timer,
            IRepository<NlpCbMessage, Guid> nlpCbMessageRepository,
            IRepository<NlpCbQAAccuracy, Guid> nlpCbQAAccuracyRepository,
            IRepository<NlpCbModel, Guid> nlpCbModelRepository,
            IRepository<NlpClientInfo, Guid> nlpClientInfo,
            IRepository<NlpChatbot, Guid> nlpChatbotRepository,
            IRepository<NlpWorkflow, Guid> nlpWorkflowRepository,
            IRepository<AuditLog, long> auditLogRepository,
            IRepository<User, long> userRepository,
            IRepository<Tenant, int> tenantRepository,
            IRepository<BinaryObject, Guid> binaryObjectRepository,

            IWebHostEnvironment hostingEnvironment
            )
            : base(timer)
        {
            _nlpCbMessageRepository = nlpCbMessageRepository;
            _nlpCbQAAccuracyRepository = nlpCbQAAccuracyRepository;
            _nlpCbModelRepository = nlpCbModelRepository;
            _nlpClientInfo = nlpClientInfo;
            _nlpChatbotRepository = nlpChatbotRepository;
            _nlpWorkflowRepository = nlpWorkflowRepository;

            _auditLogRepository = auditLogRepository;

            _userRepository = userRepository;
            _tenantRepository = tenantRepository;

            _binaryObjectRepository = binaryObjectRepository;

            _hostingEnvironment = hostingEnvironment;

            Timer.Period = CheckPeriodAsMilliseconds;
            Timer.RunOnStart = false;
        }

        protected override void DoWork()
        {
            AsyncHelper.RunSync(() => DeleteOlderDatabaseAsync());
            //DeleteOlderFiles();
            //對Azure Linux App無效果
        }

        protected virtual async Task DeleteOlderDatabaseAsync()
        {
            try
            {
                DateTime dt12m = Clock.Now.AddMonths(-12);
                DateTime dt3m = Clock.Now.AddMonths(-3);
                //DateTime dt1m = DateTime.Now.AddMonths(-1);

                using (var uow = UnitOfWorkManager.Begin())
                {
                    using (CurrentUnitOfWork.SetTenantId(null))
                    {
                        using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant, AbpDataFilters.SoftDelete))
                        {
                            await _nlpCbMessageRepository.BatchDeleteAsync(e => e.NlpCreationTime < dt12m);
                            await _nlpCbQAAccuracyRepository.BatchDeleteAsync(e => e.CreationTime < dt12m);
                            await _nlpCbModelRepository.BatchDeleteAsync(e => e.NlpCbMCreationTime < dt12m);

                            await _nlpWorkflowRepository.BatchDeleteAsync(e => e.IsDeleted == true && e.DeletionTime < dt12m);

                            await _nlpChatbotRepository.BatchDeleteAsync(e => e.IsDeleted == true && e.DeletionTime < dt12m);
                            await _auditLogRepository.BatchDeleteAsync(e => e.ExecutionTime < dt3m);

                            var unusedClientIds =
                                (from o in _nlpClientInfo.GetAll()
                                 join o1 in _nlpCbMessageRepository.GetAll() on o.ClientId equals o1.ClientId into j1
                                 from s1 in j1.DefaultIfEmpty()
                                 where s1.ClientId == null
                                 select o.ClientId
                                ).Distinct();
                            await _nlpClientInfo.BatchDeleteAsync(e => unusedClientIds.Contains(e.ClientId));

                            var unusedBinaryObjects =
                                (from o in _binaryObjectRepository.GetAll()
                                 join o1 in _tenantRepository.GetAll() on o.Id equals o1.LightLogoId into j1
                                 from s1 in j1.DefaultIfEmpty()
                                 join o2 in _tenantRepository.GetAll() on o.Id equals o2.DarkLogoId into j2
                                 from s2 in j2.DefaultIfEmpty()
                                 join o3 in _userRepository.GetAll() on o.Id equals o3.ProfilePictureId into j3
                                 from s3 in j3.DefaultIfEmpty()
                                 join o4 in _nlpChatbotRepository.GetAll() on o.Id equals o4.ChatbotPictureId into j4
                                 from s4 in j4.DefaultIfEmpty()
                                 where s1.LightLogoId == null && s2.DarkLogoId == null && s3.ProfilePictureId == null && s4.ChatbotPictureId == null
                                 select o.Id);
                            await _binaryObjectRepository.BatchDeleteAsync(e => unusedBinaryObjects.Contains(e.Id));

                            uow.Complete();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log(LogSeverity.Error, $"An error occured while deleting older data on NlpMaintenanceWorker.DeleteOlderDatabase()", e);
            }
        }

        protected virtual void DeleteOlderFiles()
        {
            try
            {
                DateTime dt3m = Clock.Now.AddMonths(-3);
                //DateTime dt1m = DateTime.Now.AddMonths(-1);

                var logpath = Path.Combine(_hostingEnvironment.ContentRootPath, "logs");
                string[] files = Directory.GetFiles(logpath);

                foreach (string file in files)
                {
                    FileInfo fi = new FileInfo(file);
                    if (fi.LastAccessTime < dt3m && fi.Extension.ToUpper() == "LOG")
                        fi.Delete();
                }
            }
            catch (Exception e)
            {
                Logger.Log(LogSeverity.Error, $"An error occured while deleting older data on NlpMaintenanceWorker.DeleteOlderFiles()", e);
            }
        }
    }
}
