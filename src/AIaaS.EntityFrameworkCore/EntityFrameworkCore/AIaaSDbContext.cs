using Abp.IdentityServer4vNext;
using Abp.Zero.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AIaaS.Authorization.Delegation;
using AIaaS.Authorization.Roles;
using AIaaS.Authorization.Users;
using AIaaS.Chat;
using AIaaS.Editions;
using AIaaS.Friendships;
using AIaaS.MultiTenancy;
using AIaaS.MultiTenancy.Accounting;
using AIaaS.MultiTenancy.Payments;
using AIaaS.Storage;
using AIaaS.Nlp;

namespace AIaaS.EntityFrameworkCore
{
    public class AIaaSDbContext : AbpZeroDbContext<Tenant, Role, User, AIaaSDbContext>, IAbpPersistedGrantDbContext
    {
        public virtual DbSet<ChatGptText> ChatGptTexts { get; set; }

        public virtual DbSet<NlpWorkflowState> NlpWorkflowStates { get; set; }

        public virtual DbSet<NlpWorkflow> NlpWorkflows { get; set; }

        public virtual DbSet<NlpCbTrainedAnswer> NlpCbTrainedAnswers { get; set; }

        public virtual DbSet<NlpCbTrainingData> NlpCbTrainingDatas { get; set; }

        public virtual DbSet<NlpCbQAAccuracy> NlpCbQAAccuracies { get; set; }

        public virtual DbSet<NlpCbModel> NlpCbModels { get; set; }

        public virtual DbSet<NlpCbMessage> NlpCbMessages { get; set; }

        public virtual DbSet<NlpClientInfo> NlpClientInfos { get; set; }

        public virtual DbSet<NlpLineUser> NlpLineUsers { get; set; }

        public virtual DbSet<NlpFacebookUser> NlpFacebookUsers { get; set; }

        public virtual DbSet<NlpToken> NlpTokens { get; set; }

        public virtual DbSet<NlpQA> NlpQAs { get; set; }

        public virtual DbSet<NlpChatbot> NlpChatbots { get; set; }

        /* Define an IDbSet for each entity of the application */

        public virtual DbSet<BinaryObject> BinaryObjects { get; set; }

        public virtual DbSet<Friendship> Friendships { get; set; }

        public virtual DbSet<ChatMessage> ChatMessages { get; set; }

        public virtual DbSet<SubscribableEdition> SubscribableEditions { get; set; }

        public virtual DbSet<SubscriptionPayment> SubscriptionPayments { get; set; }

        public virtual DbSet<Invoice> Invoices { get; set; }

        public virtual DbSet<PersistedGrantEntity> PersistedGrants { get; set; }

        public virtual DbSet<SubscriptionPaymentExtensionData> SubscriptionPaymentExtensionDatas { get; set; }

        public virtual DbSet<UserDelegation> UserDelegations { get; set; }

        public virtual DbSet<RecentPassword> RecentPasswords { get; set; }

        public AIaaSDbContext(DbContextOptions<AIaaSDbContext> options)
            : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<NlpChatbot>(n =>
                       {
                           n.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<NlpWorkflowState>(n =>
                       {
                           n.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<NlpWorkflowState>(n =>
                       {
                           n.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<NlpQA>(n =>
                       {
                           n.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<NlpCbQAAccuracy>(n =>
                       {
                           n.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<NlpCbModel>(n =>
                       {
                           n.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<NlpCbTrainingData>(n =>
                       {
                           n.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<NlpCbTrainedAnswer>(n =>
                       {
                           n.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<NlpWorkflowState>(n =>
                       {
                           n.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<NlpWorkflow>(n =>
                       {
                           n.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<NlpClientInfo>(n =>
                       {
                           n.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<NlpCbTrainingData>(n =>
                       {
                           n.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<NlpCbTrainedAnswer>(n =>
                       {
                           n.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<NlpCbQAAccuracy>(n =>
                       {
                           n.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<NlpCbMessage>(n =>
                       {
                           n.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<NlpCbModel>(n =>
                       {
                           n.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<NlpQA>(n =>
                       {
                           n.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<NlpChatbot>(n =>
                       {
                           n.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<BinaryObject>(b =>
                       {
                           b.HasIndex(e => new { e.TenantId });
                       });

            modelBuilder.Entity<ChatMessage>(b =>
            {
                b.HasIndex(e => new { e.TenantId, e.UserId, e.ReadState });
                b.HasIndex(e => new { e.TenantId, e.TargetUserId, e.ReadState });
                b.HasIndex(e => new { e.TargetTenantId, e.TargetUserId, e.ReadState });
                b.HasIndex(e => new { e.TargetTenantId, e.UserId, e.ReadState });
            });

            modelBuilder.Entity<Friendship>(b =>
            {
                b.HasIndex(e => new { e.TenantId, e.UserId });
                b.HasIndex(e => new { e.TenantId, e.FriendUserId });
                b.HasIndex(e => new { e.FriendTenantId, e.UserId });
                b.HasIndex(e => new { e.FriendTenantId, e.FriendUserId });
            });

            modelBuilder.Entity<Tenant>(b =>
            {
                b.HasIndex(e => new { e.SubscriptionEndDateUtc });
                b.HasIndex(e => new { e.CreationTime });
            });

            modelBuilder.Entity<SubscriptionPayment>(b =>
            {
                b.HasIndex(e => new { e.Status, e.CreationTime });
                b.HasIndex(e => new { PaymentId = e.ExternalPaymentId, e.Gateway });
            });

            modelBuilder.Entity<SubscriptionPaymentExtensionData>(b =>
            {
                b.HasQueryFilter(m => !m.IsDeleted)
                    .HasIndex(e => new { e.SubscriptionPaymentId, e.Key, e.IsDeleted })
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0");
            });

            modelBuilder.Entity<UserDelegation>(b =>
            {
                b.HasIndex(e => new { e.TenantId, e.SourceUserId });
                b.HasIndex(e => new { e.TenantId, e.TargetUserId });
            });

            modelBuilder.ConfigurePersistedGrantEntity();
        }
    }
}
