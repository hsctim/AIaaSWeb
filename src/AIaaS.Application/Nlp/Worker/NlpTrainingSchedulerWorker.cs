using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
using Abp.Notifications;
using Abp.Threading;
using Abp.Threading.BackgroundWorkers;
using Abp.Threading.Timers;
using Abp.Timing;
using AIaaS.Authorization.Users;
using AIaaS.MultiTenancy;
using AIaaS.Nlp;
using AIaaS.Nlp.Lib;
using AIaaS.Nlp.Lib.Dtos;
using AIaaS.Nlp.Model;
using AIaaS.Notifications;
using AIaaS.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
//using static AIaaS.Nlp.NlpTenantConsts;

namespace AIaaS.Auditing
{
    public class NlpTrainingSchedulerWorker : PeriodicBackgroundWorkerBase, ISingletonDependency
    {
        /// <summary>
        /// Set this const field to true if you want to enable ExpiredAuditLogDeleterWorker.
        /// Be careful, If you enable this, all expired logs will be permanently deleted.
        /// </summary>
        public const bool IsEnabled = true;
        private const int CheckPeriodAsMilliseconds = 5 * 1000; // 10 Seconds
        public static bool EnableCheck = true;

        private static object _lock = new object();

        private readonly NlpCbWebApiClient _nlpCBServiceClient;
        private readonly IRepository<NlpCbModel, Guid> _nlpCbModelRepository;
        private readonly IRepository<Tenant, int> _tenantRepository;
        private readonly IAppNotifier _appNotifier;

        private static NlpCbGetTrainingStatus _nlpCbTrainingStatus = new NlpCbGetTrainingStatus();
        private static List<NlpTrainingModelPriority> _nlpTrainingModelPriorityList = new List<NlpTrainingModelPriority>();


        public NlpTrainingSchedulerWorker(
            AbpTimer timer,
            NlpCbWebApiClient nlpCBServiceClient,
            IRepository<Tenant, int> tenantRepository,
            IRepository<NlpCbModel, Guid> nlpCbModelRepository,
            IAppNotifier appNotifier)
            : base(timer)
        {
            Timer.Period = CheckPeriodAsMilliseconds;
            Timer.RunOnStart = false;

            _nlpCBServiceClient = nlpCBServiceClient;
            _tenantRepository = tenantRepository;
            _nlpCbModelRepository = nlpCbModelRepository;
            _appNotifier = appNotifier;
        }

        protected override void DoWork()
        {
            if (EnableCheck == false)
                return;

            try
            {
                AsyncHelper.RunSync(() => DoWorkAsync());
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex.ToString(), ex);
            }
        }


        protected async Task DoWorkAsync()
        {


            using (var uow = UnitOfWorkManager.Begin())
            {
                using (CurrentUnitOfWork.SetTenantId(null))
                {
                    var status = await _nlpCBServiceClient.GetTrainingStatusAsync();
                    lock (_lock)
                    {
                        _nlpCbTrainingStatus = status;
                    }

                    using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant, AbpDataFilters.SoftDelete))
                    {
                        DateTime expiredDtc = DateTime.UtcNow.AddMinutes(10);

                        //取得目前正在訓練的模型，取消標示正在訓練的模型
                        var invalidModels = _nlpCbModelRepository.GetAll()
                            .Where(e => e.NlpChatbotId != status.ChatbotId && e.NlpCbMTrainingStartTime != null && expiredDtc > e.NlpCbMTrainingStartTime && e.NlpCbMTrainingCancellationTime == null && e.NlpCbMTrainingCompleteTime == null);

                        int cancellationCount = await invalidModels.UpdateFromQueryAsync(e => new NlpCbModel()
                        {
                            NlpCbMStatus = NlpChatbotConsts.TrainingStatus.Failed,
                            NlpCbMTrainingCompleteTime = Clock.Now,
                            NlpCbMInfo = "L:NlpCbMInfoFailed",
                        });

                        var invalidModelList = invalidModels.ToList();
                        foreach (var model in invalidModelList)
                        {
                            await _appNotifier.TrainedModelChanged(model.TenantId, L("TrainedModelChanged_Failed"), NotificationSeverity.Fatal);
                        }


                        DateTime dt7d = Clock.Now.AddDays(-7);
                        var modelQueueingRepository = _nlpCbModelRepository.GetAll()
                            .Where(e => (e.NlpCbMStatus == NlpChatbotConsts.TrainingStatus.Queueing || e.NlpCbMStatus == NlpChatbotConsts.TrainingStatus.Training) && e.NlpCbMCreationTime >= dt7d);
                        var tenantRepository = _tenantRepository.GetAll();

                        var nlpTrainingModelPriorityList = (from m in modelQueueingRepository
                                                            join t in tenantRepository on m.TenantId equals t.Id
                                                            orderby t.NlpPriority
                                                            select new NlpTrainingModelPriority()
                                                            {
                                                                ChatbotId = m.NlpChatbotId,
                                                                Priority = t.NlpPriority,
                                                                Tenant = m.TenantId,
                                                                TrainingCost = m.NlpChatbotFk.TrainingCostSeconds
                                                            }).ToList();

                        lock (_lock)
                        {
                            _nlpTrainingModelPriorityList = nlpTrainingModelPriorityList;
                        }

                        //如果已無訓練模型，則不再檢查進度
                        if (_nlpCbTrainingStatus.ChatbotId == Guid.Empty && _nlpTrainingModelPriorityList.Count == 0)
                            EnableCheck = false;
                    }
                }
            }
        }

        public static ChatbotTrainingWaitingStatus GetWaitingStatus(Guid chatbotId)
        {
            NlpCbGetTrainingStatus nlpCbTrainingStatus = null;
            List<NlpTrainingModelPriority> nlpTrainingModelPriorityList = null;

            lock (_lock)
            {
                nlpCbTrainingStatus = _nlpCbTrainingStatus;
                nlpTrainingModelPriorityList = _nlpTrainingModelPriorityList;
            }

            if (chatbotId == nlpCbTrainingStatus.ChatbotId)
            {
                var span = new TimeSpan(0, 0, 0, 0, (int)(1000.0* nlpCbTrainingStatus.TimeCost));
                var progress = nlpCbTrainingStatus.Progress;

                //Debug.WriteLine("Raw nlpCbTrainingStatus.Progress: {0}", nlpCbTrainingStatus.Progress);

                progress = Math.Min(Math.Max(0.0, progress), 100.0);
                progress = Math.Sqrt(progress);

                if (progress>0.01)
                    progress = Math.Pow(100.0, progress) / 100.0;

                if (span > TimeSpan.MaxValue / 100000.0)
                    span = TimeSpan.MaxValue / 100000.0;

                //if (progress < 0.001)
                //    progress = 0.001;

                TimeSpan remainCost = TimeSpan.Zero;

                if (nlpCbTrainingStatus.Progress > 0.1)
                {
                    TimeSpan allCost = span / progress;
                    remainCost = allCost - span;
                }
                else
                {
                    ////var cost = nlpTrainingModelPriorityList.FirstOrDefault(e => e.ChatbotId == chatbotId)?.TrainingCost;

                    ////if (cost.HasValue)
                    remainCost = new TimeSpan(0);
                }

                //Debug.WriteLine(" ChatbotTrainingWaitingStatus.TrainingProgress: {0}", progress);

                return new ChatbotTrainingWaitingStatus()
                {
                    TrainingProgress = progress,
                    TrainingRemaining = remainCost,
                    QueueRemaining = TimeSpan.Zero
                };
            }
            else
            {
                var QueueRemaining = TimeSpan.Zero;
                var TrainingRemaining = TimeSpan.Zero;

                foreach (var model in nlpTrainingModelPriorityList)
                {
                    if (model.ChatbotId == chatbotId)
                    {
                        TrainingRemaining = new TimeSpan(0, 0, 0, 0, (int)(model.TrainingCost * 1000.0));
                        break;
                    }

                    if (model.ChatbotId == nlpCbTrainingStatus.ChatbotId && nlpCbTrainingStatus.Progress > 0.2)
                    {
                        var span = new TimeSpan(0, 0, 0, 0, (int)(1000.0 * nlpCbTrainingStatus.TimeCost));

                        var progress = nlpCbTrainingStatus.Progress;
                        progress = Math.Min(Math.Max(0.0, progress), 1.0);
                        progress = Math.Sqrt(progress);

                        if (progress > 0.01)
                            progress = Math.Pow(100.0, progress) / 100.0;

                        if (span > TimeSpan.MaxValue / 100000.0)
                            span = TimeSpan.MaxValue / 100000.0;

                        if (progress < 0.001)
                            progress = 0.001;

                        TimeSpan allCost = span / progress;
                        TimeSpan remainCost = allCost - span;

                        QueueRemaining += remainCost;
                    }
                    else
                    {
                        var cost = new TimeSpan(0, 0, 0, 0, (int)(model.TrainingCost * 1000.0));
                        QueueRemaining += cost;
                    }
                }

                //Debug.WriteLine(" ChatbotTrainingWaitingStatus.TrainingProgress: {0}", 0);

                return new ChatbotTrainingWaitingStatus()
                {
                    TrainingProgress = 0,
                    //TrainingSpent = TimeSpan.Zero,
                    TrainingRemaining = TrainingRemaining,
                    QueueRemaining = QueueRemaining
                };
            }
        }
    }
}
