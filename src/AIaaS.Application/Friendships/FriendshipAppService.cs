using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Authorization;
using Abp.Extensions;
using Abp.MultiTenancy;
using Abp.RealTime;
using Abp.Runtime.Session;
using Abp.UI;
using ApiProtectorDotNet;
using AIaaS.Authorization.Users;
using AIaaS.Chat;
using AIaaS.Friendships.Dto;

namespace AIaaS.Friendships
{
    [AbpAuthorize]
    public class FriendshipAppService : AIaaSAppServiceBase, IFriendshipAppService
    {
        private readonly IFriendshipManager _friendshipManager;
        private readonly IOnlineClientManager<ChatChannel> _onlineClientManager;
        private readonly IChatCommunicator _chatCommunicator;
        private readonly ITenantCache _tenantCache;
        private readonly IChatFeatureChecker _chatFeatureChecker;

        public FriendshipAppService(
            IFriendshipManager friendshipManager,
            IOnlineClientManager<ChatChannel> onlineClientManager,
            IChatCommunicator chatCommunicator,
            ITenantCache tenantCache,
            IChatFeatureChecker chatFeatureChecker)
        {
            _friendshipManager = friendshipManager;
            _onlineClientManager = onlineClientManager;
            _chatCommunicator = chatCommunicator;
            _tenantCache = tenantCache;
            _chatFeatureChecker = chatFeatureChecker;
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task<FriendDto> CreateFriendshipRequest(CreateFriendshipRequestInput input)
        {
            var userIdentifier = AbpSession.ToUserIdentifier();
            var probableFriend = new UserIdentifier(input.TenantId, input.UserId);

            _chatFeatureChecker.CheckChatFeatures(userIdentifier.TenantId, probableFriend.TenantId);

            if (await _friendshipManager.GetFriendshipOrNullAsync(userIdentifier, probableFriend) != null)
            {
                throw new UserFriendlyException(L("YouAlreadySentAFriendshipRequestToThisUser"));
            }

            var user = await UserManager.FindByIdAsync(AbpSession.GetUserId().ToString());

            User probableFriendUser;
            using (CurrentUnitOfWork.SetTenantId(input.TenantId))
            {
                probableFriendUser = await UserManager.FindByIdAsync(input.UserId.ToString());
            }

            // Friend requester
            var friendTenancyName = await GetTenancyNameAsync(probableFriend.TenantId);
            var sourceFriendship = new Friendship(userIdentifier, probableFriend, friendTenancyName,
                probableFriendUser.UserName, probableFriendUser.ProfilePictureId, FriendshipState.Accepted);
            await _friendshipManager.CreateFriendshipAsync(sourceFriendship);

            // Target friend
            var userTenancyName = await GetTenancyNameAsync(user.TenantId);
            var targetFriendship = new Friendship(probableFriend, userIdentifier, userTenancyName, user.UserName,
                user.ProfilePictureId, FriendshipState.Accepted);

            if (await _friendshipManager.GetFriendshipOrNullAsync(probableFriend, userIdentifier) == null)
            {
            await _friendshipManager.CreateFriendshipAsync(targetFriendship);

            var clients = _onlineClientManager.GetAllByUserId(probableFriend);
            if (clients.Any())
            {
                var isFriendOnline = _onlineClientManager.IsOnline(sourceFriendship.ToUserIdentifier());
                    await _chatCommunicator.SendFriendshipRequestToClient(clients, targetFriendship, false,
                        isFriendOnline);
            }
            }

            var senderClients = _onlineClientManager.GetAllByUserId(userIdentifier);
            if (senderClients.Any())
            {
                var isFriendOnline = _onlineClientManager.IsOnline(targetFriendship.ToUserIdentifier());
                await _chatCommunicator.SendFriendshipRequestToClient(senderClients, sourceFriendship, true,
                    isFriendOnline);
            }

            var sourceFriendshipRequest = ObjectMapper.Map<FriendDto>(sourceFriendship);
            sourceFriendshipRequest.IsOnline = _onlineClientManager.GetAllByUserId(probableFriend).Any();

            return sourceFriendshipRequest;
        }

        private async Task<string> GetTenancyNameAsync(int? tenantId)
        {
            if (tenantId.HasValue)
            {
                var tenant = await _tenantCache.GetAsync(tenantId.Value);
                return tenant.TenancyName;
            }

            return null;
        }

        public async Task<FriendDto> CreateFriendshipWithDifferentTenant(CreateFriendshipWithDifferentTenantInput input)
        {
            var probableFriend = await GetUserIdentifier(input.TenancyName, input.UserName);
            return await CreateFriendshipRequest(new CreateFriendshipRequestInput
            {
                TenantId = probableFriend.TenantId,
                UserId = probableFriend.UserId
            });
        }

        public async Task<FriendDto> CreateFriendshipForCurrentTenant(CreateFriendshipForCurrentTenantInput input)
        {
            using (CurrentUnitOfWork.SetTenantId(AbpSession.TenantId))
            {
                var user = await UserManager.FindByNameOrEmailAsync(input.UserName);
                if (user == null)
                {
                    throw new UserFriendlyException(L("ThereIsNoUserRegisteredWithNameOrEmail{0}", input.UserName));
                }

                var probableFriend = user.ToUserIdentifier();
                
                return await CreateFriendshipRequest(new CreateFriendshipRequestInput
                {
                    TenantId = probableFriend.TenantId,
                    UserId = probableFriend.UserId
                });
            }

        }

        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task BlockUser(BlockUserInput input)
        {
            var userIdentifier = AbpSession.ToUserIdentifier();
            var friendIdentifier = new UserIdentifier(input.TenantId, input.UserId);
            await _friendshipManager.BanFriendAsync(userIdentifier, friendIdentifier);

            var clients = _onlineClientManager.GetAllByUserId(userIdentifier);
            if (clients.Any())
            {
                await _chatCommunicator.SendUserStateChangeToClients(clients, friendIdentifier,
                    FriendshipState.Blocked);
            }
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task UnblockUser(UnblockUserInput input)
        {
            var userIdentifier = AbpSession.ToUserIdentifier();
            var friendIdentifier = new UserIdentifier(input.TenantId, input.UserId);
            await _friendshipManager.AcceptFriendshipRequestAsync(userIdentifier, friendIdentifier);

            var clients = _onlineClientManager.GetAllByUserId(userIdentifier);
            if (clients.Any())
            {
                await _chatCommunicator.SendUserStateChangeToClients(clients, friendIdentifier,
                    FriendshipState.Accepted);
            }
        }



        [ApiProtector(ApiProtectionType.ByIdentity, Limit: 10, TimeWindowSeconds: 20)]
        public async Task AcceptFriendshipRequest(AcceptFriendshipRequestInput input)
        {
            var userIdentifier = AbpSession.ToUserIdentifier();
            var friendIdentifier = new UserIdentifier(input.TenantId, input.UserId);
            await _friendshipManager.AcceptFriendshipRequestAsync(userIdentifier, friendIdentifier);

            var clients = _onlineClientManager.GetAllByUserId(userIdentifier);
            if (clients.Any())
            {
                await _chatCommunicator.SendUserStateChangeToClients(clients, friendIdentifier,
                    FriendshipState.Blocked);
            }
        }

        private async Task<UserIdentifier> GetUserIdentifier(string tenancyName, string userName)
        {
            int? tenantId = null;
            
            await CheckFeatures(tenancyName);

            if (!tenancyName.IsNullOrEmpty())
            {
                using (CurrentUnitOfWork.SetTenantId(null))
                {
                    var tenant = await TenantManager.FindByTenancyNameAsync(tenancyName);
                    if (tenant == null)
                    {
                        throw new UserFriendlyException(L("ThereIsNoTenantDefinedWithName{0}", tenancyName));
                    }

                    tenantId = tenant.Id;
                }
            }

            using (CurrentUnitOfWork.SetTenantId(tenantId))
            {
                var user = await UserManager.FindByNameOrEmailAsync(userName);
                if (user == null)
                {
                    throw new UserFriendlyException(L("ThereIsNoUserRegisteredWithNameOrEmail{0}", userName));
                }

                return user.ToUserIdentifier();
            }
        }

        private async Task CheckFeatures(string tenancyName)
        {
            if (AbpSession.TenantId == null)
            {
                return;
            }
            
            var tenantToTenantAllowed = await FeatureChecker.IsEnabledAsync
                ("App.ChatFeature.TenantToTenant");
            
            var tenantToHostAllowed = await FeatureChecker.IsEnabledAsync
                ("App.ChatFeature.TenantToHost");

            if (tenancyName.IsNullOrEmpty())
            {
                if (!tenantToHostAllowed)
                {
                    throw new UserFriendlyException(L("TenantToHostChatIsNotEnabled"));
                }
            }
            else
            {
                if (!tenantToTenantAllowed)
                {
                    throw new UserFriendlyException(L("TenantToTenantChatIsNotEnabled"));
                }
            }
        }

        public async Task RemoveFriend(RemoveFriendInput input)
        {
            var userIdentifier = AbpSession.ToUserIdentifier();
            var friendIdentifier = new UserIdentifier(input.TenantId, input.UserId);
            await _friendshipManager.RemoveFriendAsync(userIdentifier, friendIdentifier);
        }
}
}