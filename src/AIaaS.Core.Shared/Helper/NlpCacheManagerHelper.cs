using Abp.Dependency;
using System;
using System.Linq;
using Abp.Linq.Extensions;
using System.Text;
using Abp.Runtime.Caching;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Abp.Timing;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace AIaaS.Helpers
{
    public static class NlpCacheManagerExtension
    {
        private readonly static TimeSpan _defaultTimeSpan = new TimeSpan(1, 0, 0);
        private readonly static TimeSpan _timeSpan7Days = new TimeSpan(7,0, 0, 0);


        #region LanguageManager_DisplayName
        public static Dictionary<String, String> Get_LanguageManager_DisplayName(this ICacheManager cacheManager)
        {
            return (Dictionary<String, String>)cacheManager.GetOrDefault("LanguageManager", "DisplayName");
        }

        public static Dictionary<String, String> Set_LanguageManager_DisplayName(this ICacheManager cacheManager, object value)
        {
            return (Dictionary<String, String>)cacheManager.Set("LanguageManager", "DisplayName", value);
        }

        #endregion


        #region UserProfilePicture
        public static object Get_UserProfilePicture(this ICacheManager cacheManager, long userId)
        {
            return cacheManager.GetOrDefault("UserProfilePicture", userId.ToString());
        }

        public static object Set_UserProfilePicture(this ICacheManager cacheManager, long userId, object value)
        {
            return cacheManager.Set("UserProfilePicture", userId.ToString(), value);
        }

        public static object Get_UserProfilePicture_By_PicId(this ICacheManager cacheManager, Guid picId)
        {
            return cacheManager.GetOrDefault("UserProfilePictureId", picId.ToString());
        }

        public static object Set_UserProfilePicture_By_PicId(this ICacheManager cacheManager, Guid picId, object value)
        {
            return cacheManager.Set("UserProfilePictureId", picId.ToString(), value);
        }

        #endregion


        #region External_Data
        public static object Get_ExternalData(this ICacheManager cacheManager, string jsonData)
        {
            return cacheManager.GetOrDefault("ExternalData", jsonData);
        }

        public static object Set_ExternalData(this ICacheManager cacheManager, string jsonData, object value)
        {
            return cacheManager.Set("ExternalData", jsonData, value);
        }

        #endregion

        //#region NlpCbDictKeyword
        //public static object Get_NlpCbDictKeyword(this ICacheManager cacheManager, int? tenantId, Guid? chatbotId, string language)
        //{
        //    return cacheManager.GetOrDefault(string.Join(":", "NlpCbDictKeyword", tenantId.ToString()), string.Join(":", chatbotId.ToString(), language));
        //}

        //public static object Set_NlpCbDictKeyword(this ICacheManager cacheManager, int? tenantId, Guid? chatbotId, string language, object value)
        //{
        //    return cacheManager.Set(string.Join(":", "NlpCbDictKeyword", tenantId.ToString()), string.Join(":", chatbotId.ToString(), language), value);
        //}

        //public static void Clear_NlpCbDictKeyword(this ICacheManager cacheManager, int? tenantId)
        //{
        //    cacheManager.Clear(string.Join(":", "NlpCbDictKeyword", tenantId.ToString()));
        //}
        //#endregion


        #region NlpCbDictWofkflowState
        //public static object Get_NlpCbDictWofkflowState(this ICacheManager cacheManager, Guid chatbotId)
        //{
        //    return cacheManager.GetOrDefault("NlpCbDictWofkflowState", chatbotId.ToString());
        //}

        //public static object Set_NlpCbDictWofkflowState(this ICacheManager cacheManager, Guid chatbotId, object value)
        //{
        //    return cacheManager.Set("NlpCbDictWofkflowState", chatbotId.ToString(), value);
        //}

        public static void Remove_NlpCbDictWofkflowState(this ICacheManager cacheManager, Guid chatbotId)
        {
            cacheManager.Remove("NlpCbDictWofkflowState", chatbotId.ToString());
        }
        #endregion


        #region NlpWorkflowStates
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacheManager"></param>
        /// <param name="workflowId"></param>
        /// <returns></returns>
        public static object Get_NlpWorkflowStates(this ICacheManager cacheManager, Guid workflowStateId)
        {
            return cacheManager.GetOrDefault("NlpWorkflowStates", workflowStateId.ToString());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacheManager"></param>
        /// <param name="workflowId"></param>
        /// <param name="value">NlpWorkflowStatesDto</param>
        /// <returns></returns>
        public static object Set_NlpWorkflowStates(this ICacheManager cacheManager, Guid workflowStateId, object value)
        {
            return cacheManager.Set("NlpWorkflowStates", workflowStateId.ToString(), value);
        }

        public static void Remove_NlpWorkflowStates(this ICacheManager cacheManager, Guid workflowStateId)
        {
            cacheManager.Remove("NlpWorkflowStates", workflowStateId.ToString());
        }
        #endregion


        #region ChatroomStatus
        public static object Get_ChatroomStatus(this ICacheManager cacheManager, Guid chatbotId, Guid clientId)
        {
            return cacheManager.GetOrDefault("ChatroomStatus", string.Join(":", chatbotId.ToString(), clientId.ToString()));
        }

        public static object Set_ChatroomStatus(this ICacheManager cacheManager, Guid chatbotId, Guid clientId, object value)
        {
            return cacheManager.Set("ChatroomStatus", string.Join(":", chatbotId.ToString(), clientId.ToString()), value);
        }

        #endregion



        #region NlpClientConnection_By_ChatbotId_ClientId
        public static object Get_NlpClientConnection_By_ChatbotId_ClientId(this ICacheManager cacheManager, Guid chatbotId, Guid clientId)
        {
            return cacheManager.GetOrDefault(string.Join(":", "NlpClientConnection_By_ChatbotId_ClientId", chatbotId.ToString()), clientId.ToString());

        }

        public static object Set_NlpClientConnection_By_ChatbotId_ClientId(this ICacheManager cacheManager, Guid chatbotId, Guid clientId, object value)
        {
            //TimeSpan tp = new TimeSpan(24, 0, 0);

            return cacheManager.Set(string.Join(":", "NlpClientConnection_By_ChatbotId_ClientId", chatbotId.ToString()), clientId.ToString(), value);
        }

        public static void Remove_NlpClientConnection_By_ChatbotId_ClientId(this ICacheManager cacheManager, Guid chatbotId, Guid clientId)
        {
            cacheManager.Remove(string.Join(":", "NlpClientConnection_By_ChatbotId_ClientId", chatbotId.ToString()), clientId.ToString());

        }

        #endregion


        #region NlpClientConnection_By_ConnectionId
        public static object Get_NlpClientConnection_By_ConnectionId(this ICacheManager cacheManager, string connectionId)
        {
            return cacheManager.GetOrDefault("NlpClientConnection_By_ConnectionId", connectionId);
        }

        public static object Set_NlpClientConnection_By_ConnectionId(this ICacheManager cacheManager, string connectionId, object value)
        {
            //TimeSpan tp = new TimeSpan(24, 0, 0);
            return cacheManager.Set("NlpClientConnection_By_ConnectionId", connectionId, value);
        }

        public static void Remove_NlpClientConnection_By_ConnectionId(this ICacheManager cacheManager, string connectionId)
        {
            cacheManager.Remove("NlpClientConnection_By_ConnectionId", connectionId);

        }
        #endregion


        #region NlpChatbot
        public static object Get_NlpChatbotDto(this ICacheManager cacheManager, Guid chatbotId)
        {
            return cacheManager.GetOrDefault("GetChatbot", chatbotId.ToString());
        }

        public static object Set_NlpChatbotDto(this ICacheManager cacheManager, Guid chatbotId, object data)
        {
            return cacheManager.Set("GetChatbot", chatbotId.ToString(), data);
        }

        public static void Remove_NlpChatbotDto(this ICacheManager cacheManager, Guid chatbotId)
        {
            cacheManager.Remove("GetChatbot", chatbotId.ToString());
        }
        #endregion

        #region BotPicture
        public static object Get_BotPicture(this ICacheManager cacheManager, Guid pictureId)
        {
            return cacheManager.GetOrDefault("BotPicture", pictureId.ToString());
        }

        public static object Set_BotPicture(this ICacheManager cacheManager, Guid pictureId, object data)
        {
            return cacheManager.Set("BotPicture", pictureId.ToString(), data);
        }

        public static void Remove_BotPicture(this ICacheManager cacheManager, Guid pictureId)
        {
            cacheManager.Remove("BotPicture", pictureId.ToString());
        }
        #endregion


        #region AllChatbotForSelectList
        public static object Get_AllChatbotForSelectList(this ICacheManager cacheManager, int? tenantId)
        {
            return cacheManager.GetOrDefault("AllChatbotForSelectList", tenantId?.ToString());
        }

        public static object Set_AllChatbotForSelectList(this ICacheManager cacheManager, int? tenantId, object data)
        {
            return cacheManager.Set("AllChatbotForSelectList", tenantId?.ToString(), data);
        }

        public static void Remove_AllChatbotForSelectList(this ICacheManager cacheManager, int? tenantId)
        {
            cacheManager.Remove("AllChatbotForSelectList", tenantId?.ToString());
        }
        #endregion

        #region UserLoginInfoDto
        public static object Get_UserLoginInfoDto(this ICacheManager cacheManager, long? userId)
        {
            return cacheManager.GetOrDefault("UserLoginInfoDto", userId?.ToString());
        }

        public static object Set_UserLoginInfoDto(this ICacheManager cacheManager, long? userId, object data)
        {
            return cacheManager.Set("UserLoginInfoDto", userId?.ToString(), data);
        }

        #endregion


        //#region CbQACaterogies
        //public static object Get_CbQACaterogies(this ICacheManager cacheManager, string language)
        //{
        //    return cacheManager.GetOrDefault("CbQACaterogies", string.Join(":", "Language", language));
        //}
        //public static object Set_CbQACaterogies(this ICacheManager cacheManager, string language, object data)
        //{
        //    return cacheManager.Set("CbQACaterogies", string.Join(":", "Language", language), data);
        //}
        //#endregion


        //#region CopyHostSynonymToTenantTime
        //public static DateTime? Get_CopyHostSynonymToTenantTime(this ICacheManager cacheManager, int tenantId)
        //{
        //    return (DateTime?)cacheManager.GetOrDefault("CopyHostSynonymToTenantTime", tenantId.ToString());

        //}
        //public static DateTime Set_CopyHostSynonymToTenantTime(this ICacheManager cacheManager, int tenantId, DateTime data)
        //{
        //    return (DateTime)cacheManager.Set("CopyHostSynonymToTenantTime", tenantId.ToString(), data);
        //}
        //#endregion

        #region NlpAgentConnection_By_ConnectionId
        public static object Get_NlpAgentConnection_By_ConnectionId(this ICacheManager cacheManager, string connectionId)
        {
            return cacheManager.GetOrDefault("NlpAgentConnection_By_ConnectionId", connectionId);
        }

        public static object Set_NlpAgentConnection_By_ConnectionId(this ICacheManager cacheManager, string connectionId, object data)
        {
            //TimeSpan tp = new TimeSpan(24, 0, 0);

            return cacheManager.Set("NlpAgentConnection_By_ConnectionId", connectionId, data);
        }

        public static void Remove_NlpAgentConnection_By_ConnectionId(this ICacheManager cacheManager, string connectionId)
        {
            cacheManager.Remove("NlpAgentConnection_By_ConnectionId", connectionId);
        }
        #endregion

        #region NlpAgentConnection_By_ChatbotId_ClientId_UserId
        public static object Get_NlpAgentConnection_By_ChatbotId_ClientId_UserId(this ICacheManager cacheManager, Guid chatbotId, Guid clientId, long userId)
        {
            return cacheManager.GetOrDefault("NlpAgentConnection_By_ChatbotId_ClientId_UserId", string.Join(":", chatbotId.ToString(), clientId.ToString(), userId.ToString()));
        }

        public static object Set_NlpAgentConnection_By_ChatbotId_ClientId_UserId(this ICacheManager cacheManager, Guid chatbotId, Guid clientId, long userId, object data)
        {
            return cacheManager.Set("NlpAgentConnection_By_ChatbotId_ClientId_UserId", string.Join(":", chatbotId.ToString(), clientId.ToString(), userId.ToString()), data);
        }

        public static void Remove_NlpAgentConnection_By_ChatbotId_ClientId_UserId(this ICacheManager cacheManager, Guid chatbotId, Guid clientId, long userId)
        {
            cacheManager.Remove("NlpAgentConnection_By_ChatbotId_ClientId_UserId", string.Join(":", chatbotId.ToString(), clientId.ToString(), userId.ToString()));
        }
        #endregion

        #region ChatroomByAgent
        public static object Get_ChatroomByAgent(this ICacheManager cacheManager, long agentId)
        {
            return cacheManager.GetOrDefault("ChatroomByAgent", agentId.ToString());
        }

        public static object Set_ChatroomByAgent(this ICacheManager cacheManager, long agentId, object chatroom)
        {
            return cacheManager.Set("ChatroomByAgent", agentId.ToString(), chatroom);
        }

        public static void Remove_ChatroomByAgent(this ICacheManager cacheManager, long agentId)
        {
            cacheManager.Remove("ChatroomByAgent", agentId.ToString());
        }
        #endregion


        #region NlpDtoQAFromNNID
        public static object Get_NlpQADtoFromNNID(this ICacheManager cacheManager, Guid chatbotId, int nnid)
        {
            return cacheManager.GetOrDefault(string.Join(":", "NlpQAFromNNID", chatbotId.ToString()), nnid.ToString());
        }

        public static object Set_NlpQADtoFromNNID(this ICacheManager cacheManager, Guid chatbotId, int nnid, object data)
        {
            return cacheManager.Set(string.Join(":", "NlpQAFromNNID", chatbotId.ToString()), nnid.ToString(), data);
        }

        public static void Remove_NlpQADtoFromNNID(this ICacheManager cacheManager, Guid chatbotId, int nnid)
        {
            cacheManager.Remove(string.Join(":", "NlpQAFromNNID", chatbotId.ToString()), nnid.ToString());
        }

        public static void Clear_NlpQADtoFromNNID(this ICacheManager cacheManager, Guid chatbotId)
        {
            cacheManager.Clear(string.Join(":", "NlpQAFromNNID", chatbotId.ToString()));
        }
        #endregion


        #region ChatbotPredict
        public static object Get_ChatbotPredict(this ICacheManager cacheManager, Guid chatbotId, string question)
        {
            return cacheManager.GetOrDefault(string.Join(":", "ChatbotPredict", chatbotId.ToString()), question);
        }

        public static object Set_ChatbotPredict(this ICacheManager cacheManager, Guid chatbotId, string question, object data)
        {
            return cacheManager.Set(string.Join(":", "ChatbotPredict", chatbotId.ToString()), question, data);
        }

        public static void Clear_ChatbotPredict(this ICacheManager cacheManager, Guid chatbotId)
        {
            cacheManager.Clear(string.Join(":", "ChatbotPredict", chatbotId.ToString()));
        }
        #endregion

        #region SentenceSimiliarity
        public static float? Get_SentenceSimiliarity(this ICacheManager cacheManager, string question1, IList<string> questions2)
        {
            var key = ToSHA256(string.Join(":", question1, JsonConvert.SerializeObject(questions2)));

            return (float?) cacheManager.GetOrDefault("SentenceSimiliarity", key);
        }

        public static float Set_SentenceSimiliarity(this ICacheManager cacheManager, string question1, IList<string> questions2, float data)
        {
            var key = ToSHA256(string.Join(":", question1, JsonConvert.SerializeObject(questions2)));

            return (float)cacheManager.Set("SentenceSimiliarity", key, data);
        }

        #endregion




        #region NlpNNIDRepetition
        public static object Get_NlpNNIDRepetition(this ICacheManager cacheManager, Guid chatbotId)
        {
            return cacheManager.GetOrDefault("NlpNNIDRepetition", chatbotId.ToString());
        }

        public static object Set_NlpNNIDRepetition(this ICacheManager cacheManager, Guid chatbotId, object data)
        {
            return cacheManager.Set("NlpNNIDRepetition", chatbotId.ToString(), data);
        }
        #endregion


        //NlpClientInfoDto
        #region NlpClientInfoDto
        public static object Get_NlpClientInfoDto(this ICacheManager cacheManager, int tenantId, Guid clientId)
        {
            return cacheManager.GetOrDefault("NlpClientInfoDto", string.Join(":", tenantId.ToString(), clientId.ToString()));
        }

        public static object Set_NlpClientInfoDto(this ICacheManager cacheManager, int tenantId, Guid clientId, object data)
        {
            return cacheManager.Set("NlpClientInfoDto", string.Join(":", tenantId.ToString(), clientId.ToString()), data);
        }
        #endregion

        #region ChatbotController_GetMessages_HasData
        public static bool Get_ChatbotController_GetMessages_HasData_AutoReset(this ICacheManager cacheManager, int tenantId, Guid clientId)
        {
            var data = cacheManager.GetOrDefault("ChatbotController_GetMessages_HasData", string.Join(":", tenantId.ToString(), clientId.ToString()));

            if (data == null)
            {
                Set_ChatbotController_GetMessages_HasData(cacheManager, tenantId, clientId, false);
                return true;
            }

            bool bData = (bool)data;
            if (bData == true)
                Set_ChatbotController_GetMessages_HasData(cacheManager, tenantId, clientId, false);

            return bData;
        }

        public static object Set_ChatbotController_GetMessages_HasData(this ICacheManager cacheManager, int tenantId, Guid clientId, bool bHasData)
        {
            return cacheManager.Set("ChatbotController_GetMessages_HasData", string.Join(":", tenantId.ToString(), clientId.ToString()), bHasData);
        }
        #endregion


        #region NlpLineUser
        public static object Get_NlpLineUserDto(this ICacheManager cacheManager, string lineUserId)
        {
            return cacheManager.GetOrDefault("NlpLineUser", lineUserId);
        }

        public static object Set_NlpLineUserDto(this ICacheManager cacheManager, string lineUserId, object data)
        {
            return cacheManager.Set("NlpLineUser", lineUserId, data);
        }


        public static object Get_NlpLineUserDto(this ICacheManager cacheManager, Guid clientId)
        {
            return cacheManager.GetOrDefault("NlpLineUserByClientId", clientId.ToString());
        }

        public static object Set_NlpLineUserDto(this ICacheManager cacheManager, Guid clientId, object data)
        {
            return cacheManager.Set("NlpLineUserByClientId", clientId.ToString(), data);
        }

        #endregion


        #region NlpFacebookUser
        public static object Get_NlpFacebookUserDto(this ICacheManager cacheManager, string facebookUserId)
        {
            return cacheManager.GetOrDefault("NlpFacebookUser", facebookUserId);
        }

        public static object Set_NlpFacebookUserDto(this ICacheManager cacheManager, string facebookUserId, object data)
        {
            return cacheManager.Set("NlpFacebookUser", facebookUserId, data);
        }


        public static object Get_NlpFacebookUserDto(this ICacheManager cacheManager, Guid clientId)
        {
            return cacheManager.GetOrDefault("NlpFacebookUserByClientId", clientId.ToString());
        }

        public static object Set_NlpFacebookUserDto(this ICacheManager cacheManager, Guid clientId, object data)
        {
            return cacheManager.Set("NlpFacebookUserByClientId", clientId.ToString(), data);
        }

        #endregion


        #region PUCount
        public static object Get_MaxProcessingUnitInfo(this ICacheManager cacheManager, int tenandId)
        {
            return cacheManager.GetOrDefault("MaxProcessingUnitInfo", tenandId.ToString());
        }

        public static object Set_MaxProcessingUnitInfo(this ICacheManager cacheManager, int tenandId, object data)
        {
            return cacheManager.Set("MaxProcessingUnitInfo", tenandId.ToString(), data);
        }

        #endregion

        #region TenantDashboard_SubscriptionSummary
        public static object Get_Widget(this ICacheManager cacheManager, int tenantId, string widgetName)
        {
            return cacheManager.GetOrDefault("Widget", tenantId.ToString() + widgetName);
        }

        public static object Set_Widget(this ICacheManager cacheManager, int tenantId, string widgetName, object value)
        {
            return cacheManager.Set("Widget", tenantId.ToString() + widgetName, value, new TimeSpan(0, 1, 0));
        }

        //public static void Remove_SubscriptionSummary(this ICacheManager cacheManager, int tenantId)
        //{
        //    cacheManager.Remove("SubscriptionSummary", tenantId.ToString());
        //}
        #endregion



        #region NlpToken
        public static string Get_NlpToken(this ICacheManager cacheManager, string tokenType)
        {
            var value = cacheManager.GetOrDefault("NlpToken", tokenType);
            if (value == null)
                return null;

            return (string)value;
        }

        public static string Set_NlpToken(this ICacheManager cacheManager, string tokenType, string value)
        {
            return (string)cacheManager.Set("NlpToken", tokenType, value);
        }

        public static void Remove_NlpToken(this ICacheManager cacheManager, string tokenType)
        {
            cacheManager.Remove("NlpToken", tokenType);
        }
        #endregion


        #region ChatbotControllerMuext
        //避免GetMessage搶走SendMessage的回應
        //若進入為10 ，退出為0，否則每秒減1，至0後可進入，
        //當SendMessage API進入時設為10，完成後設為0。
        //GetMessage WebAPI在0時才能往下呼叫，避免GetMessage搶走SendMessage的回應。
        public static int Get_ChatbotControllerMutex(this ICacheManager cacheManager, Guid chatbotId, Guid clientId)
        {
            var obj = cacheManager.GetOrDefault("ChatbotControllerMutex", chatbotId.ToString() + clientId.ToString());

            if (obj == null)
                return 0;

            return (int)obj;
        }

        public static int Set_ChatbotControllerMutex(this ICacheManager cacheManager, Guid chatbotId, Guid clientId, int value)
        {
            return (int)cacheManager.Set("ChatbotControllerMutex", chatbotId.ToString() + clientId.ToString(), value);
        }

        #endregion


        //#region HantToHans
        //public static string Get_HantToHans(this ICacheManager cacheManager, string Hant)
        //{
        //    var obj = cacheManager.GetOrDefault("HantToHans", Hant);

        //    if (obj == null)
        //        return null;

        //    return (string)obj;
        //}

        //public static string Set_HantToHans(this ICacheManager cacheManager, string Hant, string Hans)
        //{
        //    return (string)cacheManager.Set("HantToHans", Hant, Hans);
        //}

        //#endregion


        //先分詞
        //#region PrepareSynonymString     
        //public static object Get_PrepareSynonymString(this ICacheManager cacheManager, int tenantId, string hashKey)
        //{
        //    return cacheManager.GetOrDefault(string.Join(":", "PrepareSynonymString", tenantId.ToString()), hashKey);
        //}

        //public static object Set_PrepareSynonymString(this ICacheManager cacheManager, int tenantId, string hashKey, object data)
        //{
        //    return cacheManager.Set(string.Join(":", "PrepareSynonymString", tenantId.ToString()), hashKey, data);
        //}

        //public static void Clear_PrepareSynonymString(this ICacheManager cacheManager, int tenantId)
        //{
        //    cacheManager.Clear(string.Join(":", "PrepareSynonymString", tenantId.ToString()));
        //}

        //#endregion


        //#region TenantDashboard_SubscriptionStats
        //public static object Get_SubscriptionStats(this ICacheManager cacheManager, int tenantId)
        //{
        //    return cacheManager.GetOrDefault("SubscriptionStats", tenantId.ToString());
        //}

        //public static object Set_SubscriptionStats(this ICacheManager cacheManager, int tenantId, object value)
        //{
        //    return cacheManager.Set("SubscriptionStats", tenantId.ToString(), value, new TimeSpan(0, 1, 0));
        //}

        //public static void Remove_SubscriptionStats(this ICacheManager cacheManager, int tenantId)
        //{
        //    cacheManager.Remove("SubscriptionStats", tenantId.ToString());
        //}
        //#endregion

        #region GPTCache
        public static object Get_GPTCache(this ICacheManager cacheManager, string sha256)
        {
            var obj = cacheManager.GetOrDefault("GPT_Chat", sha256);
            return obj;
        }

        public static object Set_GPTCache(this ICacheManager cacheManager, string sha256, object answer)
        {
            return cacheManager.Set("GPT_Chat", sha256, answer, _timeSpan7Days);
        }
        #endregion


        private static object GetOrDefault(this ICacheManager cacheManager, string cacheName, string itemName)
        {
            return cacheManager.GetCache(cacheName)?.GetOrDefault(itemName);
        }

        private static object Set(this ICacheManager cacheManager, string cacheName, string itemName, object value, TimeSpan? slidingExpireTime = null)
        {
            if (slidingExpireTime == null)
                slidingExpireTime = _defaultTimeSpan;

            if (value != null)
                cacheManager.GetCache(cacheName).Set(itemName, value, slidingExpireTime);
            return value;
        }

        private static void Remove(this ICacheManager cacheManager, string cacheName, string itemName)
        {
            cacheManager.GetCache(cacheName).Remove(itemName);
        }

        private static void Clear(this ICacheManager cacheManager, string cacheName)
        {
            cacheManager.GetCache(cacheName).Clear();
        }


        private static string ToSHA256(string key)
        {
            using (var cryptoSHA256 = SHA256.Create())
            {
                IEnumerable<byte> bytes = Encoding.UTF8.GetBytes(key);

                var hash = cryptoSHA256.ComputeHash(bytes.ToArray());
                //取得 sha256
                var sha256 = BitConverter.ToString(hash).Replace("-", String.Empty);
                return sha256;
            }
        }

    }
}