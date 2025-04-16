using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Threading;
using Abp.Threading.BackgroundWorkers;
using Abp.Threading.Timers;
using Abp.Timing;
using AIaaS.Authorization.Users;
using AIaaS.Editions;
using AIaaS.MultiTenancy.Payments;

namespace AIaaS.MultiTenancy
{
    public class SubscriptionExpirationCheckWorker : PeriodicBackgroundWorkerBase, ISingletonDependency
    {
        private const int CheckPeriodAsMilliseconds = 1 * 60 * 60 * 1000; //1 hour

        private readonly IRepository<Tenant> _tenantRepository;
        private readonly IRepository<SubscribableEdition> _editionRepository;
        private readonly TenantManager _tenantManager;
        private readonly UserEmailer _userEmailer;
        private readonly IRepository<SubscriptionPayment, long> _subscriptionPaymentRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public SubscriptionExpirationCheckWorker(
            AbpTimer timer,
            IRepository<Tenant> tenantRepository,
            IRepository<SubscribableEdition> editionRepository,
            TenantManager tenantManager,
            UserEmailer userEmailer,
            IRepository<SubscriptionPayment, long> subscriptionPaymentRepository,
            IUnitOfWorkManager unitOfWorkManager)
            : base(timer)
        {
            _tenantRepository = tenantRepository;
            _editionRepository = editionRepository;
            _tenantManager = tenantManager;
            _userEmailer = userEmailer;
            _subscriptionPaymentRepository = subscriptionPaymentRepository;

            Timer.Period = CheckPeriodAsMilliseconds;
            Timer.RunOnStart = true;

            LocalizationSourceName = AIaaSConsts.LocalizationSourceName;
            _unitOfWorkManager = unitOfWorkManager;
        }

        protected override void DoWork()
        {
            try
            {
                DoWork1();
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex.Message, ex);
            }

            try
            {
                CancelNotPaidAsync();
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex.Message, ex);
            }

            try
            {
                RemoveEndSubscriptionDateIsFreeEdition();
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex.Message, ex);
            }
        }

        protected void DoWork1()
        {
            _unitOfWorkManager.WithUnitOfWork(() =>
            {
                var utcNow = Clock.Now.ToUniversalTime();
                var failedTenancyNames = new List<string>();

                var subscriptionExpiredTenants = _tenantRepository.GetAllList(
                    tenant => tenant.SubscriptionEndDateUtc != null &&
                              tenant.SubscriptionEndDateUtc <= utcNow &&
                              tenant.IsActive &&
                              tenant.EditionId != null
                );

                foreach (var tenant in subscriptionExpiredTenants)
                {
                    Debug.Assert(tenant.EditionId.HasValue);

                    try
                    {

                        var edition = _editionRepository.Get(tenant.EditionId.Value);

                        Debug.Assert(tenant.SubscriptionEndDateUtc != null, "tenant.SubscriptionEndDateUtc != null");

                        var lastPayment = _subscriptionPaymentRepository.GetAllList(e => e.Status == SubscriptionPaymentStatus.Paid).OrderByDescending(e => e.CreationTime).FirstOrDefault();

                        int expiredDay = 3;
                        if (lastPayment != null && lastPayment.PaymentPeriodType != null)
                        {
                            if (lastPayment.PaymentPeriodType == PaymentPeriodType.Annual)
                                expiredDay = 30;
                        }

                        //若訂閱一個月，加3天到期，若訂閱一年，加30天後到期，轉免費
                        if (tenant.SubscriptionEndDateUtc.Value.AddDays(expiredDay) >= utcNow)
                        {
                            //Tenant is in waiting days after expire TODO: It's better to filter such entities while querying from repository!
                            continue;
                        }

                        var endSubscriptionResult = AsyncHelper.RunSync(() => _tenantManager.EndSubscriptionAsync(tenant, edition, utcNow));

                        if (endSubscriptionResult == EndSubscriptionResult.TenantSetInActive)
                        {
                            AsyncHelper.RunSync(() => _userEmailer.TryToSendSubscriptionExpireEmail(tenant.Id, utcNow));
                        }
                        else if (endSubscriptionResult == EndSubscriptionResult.AssignedToAnotherEdition)
                        {
                            AsyncHelper.RunSync(() => _userEmailer.TryToSendSubscriptionAssignedToAnotherEmail(tenant.Id, utcNow, edition.ExpiringEditionId.Value));
                        }
                    }
                    catch (Exception exception)
                    {
                        failedTenancyNames.Add(tenant.TenancyName);
                        Logger.Error($"Subscription of tenant {tenant.TenancyName} has been expired but tenant couldn't be made passive !");
                        Logger.Error(exception.Message, exception);
                    }
                }

                if (!failedTenancyNames.Any())
                {
                    return;
                }

                AsyncHelper.RunSync(() => _userEmailer.TryToSendFailedSubscriptionTerminationsEmail(failedTenancyNames, utcNow));
            });
        }

        protected void CancelNotPaidAsync()
        {
            try
            {
                _unitOfWorkManager.WithUnitOfWork(() =>
                {
                    DateTime dt = DateTime.UtcNow.AddHours(-2);

                    var oldNotPaidPaymentIds = _subscriptionPaymentRepository.GetAllList(e => e.Status == SubscriptionPaymentStatus.NotPaid && e.CreationTime <= dt);

                    foreach (var oldpayment in oldNotPaidPaymentIds)
                    {
                        //var entity = _subscriptionPaymentRepository.Get(oldpayment);
                        oldpayment.SetAsCancelled();
                        _subscriptionPaymentRepository.Update(oldpayment);
                    }
                });
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message, exception);
            }
        }


        protected void RemoveEndSubscriptionDateIsFreeEdition()
        {
            try
            {
                using (var uow = UnitOfWorkManager.Begin())
                {
                    using (CurrentUnitOfWork.SetTenantId(null))
                    {
                        using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
                        {
                            var freeEdition = _editionRepository.FirstOrDefault(e => e.Name == "Free");

                            var tenants = _tenantRepository.GetAll().Where(e => e.EditionId == freeEdition.Id && e.SubscriptionEndDateUtc != null).ToList();

                            foreach (var tenant in tenants)
                                tenant.SubscriptionEndDateUtc = null;

                            uow.Complete();
                        }
                    }
                }

            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message, exception);
            }
        }
    }
}
