using Abp;
using Abp.Application.Services;
using Abp.Auditing;
using Abp.Collections.Extensions;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore.EFPlus;
using Abp.Extensions;
using Abp.RealTime;
using Abp.Runtime.Caching;
using Abp.Timing;
using Abp.UI;
//using static AIaaS.Nlp.NlpCbDictionariesFunction;
using AIaaS.Authorization;
using AIaaS.Chatbot;
using AIaaS.Helper;
using AIaaS.Helpers;
using AIaaS.Nlp;
using AIaaS.Nlp.Dtos;
using AIaaS.Nlp.Dtos.NlpCbMessage;
using AIaaS.Nlp.External;
using AIaaS.Nlp.Lib;
using AIaaS.Nlp.Lib.Dtos;
using AIaaS.Sessions;
using AIaaS.Sessions.Dto;
using AIaaS.Web.Chatbot;
using AIaaS.Web.Chatbot.Dto;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ReflectSoftware.Facebook.Messenger.Client;
using ReflectSoftware.Facebook.Messenger.Common.Models;
using ReflectSoftware.Facebook.Messenger.Common.Models.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;


namespace AIaaS.Web.Chatbot
{
    public partial class ChatbotMessageManager : ApplicationService, IChatbotMessageManager
    {

        public async Task<string> ReplaceCustomStringAsync(string text, Guid chatbotId)
        {
            string result = text;

            if (text.Contains("${") == true && text.Contains("}") == true)
            {

                if (text.Contains("${Chatbot.Name}", StringComparison.OrdinalIgnoreCase))
                {
                    var nlpChatbotName = _nlpChatbotFunction.GetChatbotName(chatbotId);
                    text = text.Replace("${Chatbot.Name}", nlpChatbotName);
                }

                var list = NlpDataParserHelper.ParserJson(text);
                StringBuilder sb = new StringBuilder();

                foreach (var item in list)
                {
                    if (item.name == "text")
                        sb.Append(await _externalCustomData.GetCustomDataAsync2(text));
                    else if (item.name == "json")
                        sb.Append(await _externalCustomData.GetCustomDataAsync((string)item.value));
                }

                result = sb.ToString();
            }

            //result = await _openAIClient.Chat(chatbotId, result);

            return result;
        }

        private async Task<IList<string>> ReplaceCustomStringAsync(IList<string> texts, Guid chatbotId)
        {
            var newList = new List<string>();

            foreach (var text in texts)
                newList.Add(
                    //await ChatGPT(chatbotId
                    await ReplaceCustomStringAsync(text, chatbotId));

            return newList;
        }

        private string StripHTML(string input)
        {
            if (input.IsNullOrEmpty())
                return input;

            input = input.Replace("<br>", "\n").Replace("<BR>", "\n");
            return Regex.Replace(input, "<.*?>", String.Empty);
        }

        private static void InferenctSlime(SemaphoreSlim slim, int Count = 1)
        {
            if (slim == null)
                return;

            _ = Task.Run(async () =>
            {
                for (int n = 0; n < Count; n++)
                {
                    try
                    {
                        await slim.WaitAsync(_SemaphoreSlimWaitTimeOut);
                        await Task.Delay(1000);
                    }
                    finally
                    {
                        slim.Release();
                    }
                }
            });
        }

        //json to Answer
        private IList<string> JsonToAnswer(string json)
        {
            if (json == null || json.IsNullOrEmpty())
            {
                return null;
            }

            try
            {
                var obj = JsonConvert.DeserializeObject<CbAnswerSet[]>(json);

                var stringList = new List<string>();

                foreach (var item in obj)
                {
                    if (item.GPT == true)
                    {
                        stringList.Add("[GPT]" + item.Answer);
                    }
                    else
                    {
                        stringList.Add(item.Answer);
                    }
                }

                return stringList;
            }
            catch (Exception)
            {
            }

            try
            {
                return JsonConvert.DeserializeObject<IList<string>>(json);
            }
            catch (Exception)
            {
                return null;
            }

        }

        private async Task<NlpQADto> GetNlpQADtofromNNID(Guid chatbotId, int nnid)
        {
            Debug.Assert(chatbotId != Guid.Empty);

            var nlpQADto =
                (NlpQADto)_cacheManager.Get_NlpQADtoFromNNID(chatbotId, nnid)
                ??
               (NlpQADto)_cacheManager.Set_NlpQADtoFromNNID(chatbotId, nnid,
               ObjectMapper.Map<NlpQADto>(await _nlpQARepository.FirstOrDefaultAsync(e => e.NlpChatbotId == chatbotId && e.NNID == nnid)));

            return nlpQADto;
        }

        protected async Task<string> GetAnswerFromNNIDRepetition(Guid chatbotId, int nnid)
        {
            List<int> nnidLists;

            var nnidRepetition = await GetAnswerFromNNIDRepetition(chatbotId);

            if (nnidRepetition != null && nnidRepetition.ContainsKey(nnid) == true)
                nnidLists = nnidRepetition[nnid].ToList();
            else
            {
                nnidLists = new List<int>();
                nnidLists.Add(nnid);
            }

            List<string> answers = new List<string>();

            foreach (var nnidListItem in nnidLists)
            {
                var nlpQADto = await GetNlpQADtofromNNID(chatbotId, nnidListItem);
                string jsonAnswers = nlpQADto?.Answer;

                if (jsonAnswers != null)
                {
                    answers.AddRange(JsonToAnswer(jsonAnswers));
                }
            }

            if (answers.Count == 0)
                return null;

            Random random = new Random();
            int start = random.Next(0, answers.Count);
            return answers[start];
        }

        public async Task<ChatroomWorkflowInfo> GetChatbotWorkflowState(NlpChatroom chatroomId)
        {
            var chatroomStatus = await GetChatroomStatus(chatroomId.ChatbotId, chatroomId.ClientId);
            if (chatroomStatus == null)
                return null;

            //var WfsStatus = chatroomStatus.WfState;
            var workflowStatus = await GetNlpWorkflowStateInfo(chatroomStatus.WfState);

            chatroomStatus.WfState = workflowStatus?.Id ?? Guid.Empty;

            var data = new ChatroomWorkflowInfo()
            {
                ChatbotId = chatroomStatus.ChatbotId,
                ClientId = chatroomStatus.ClientId,
                WorkflowId = workflowStatus?.NlpWorkflowId ?? null,
                WorkflowName = workflowStatus?.NlpWorkflowName ?? null,
                WorkflowStateId = workflowStatus?.Id ?? null,
                WorkflowStateName = workflowStatus?.StateName ?? null,
            };

            if (data.WorkflowName.IsNullOrEmpty() || data.WorkflowName.IsNullOrWhiteSpace())
                data.WorkflowName = null;

            if (data.WorkflowStateName.IsNullOrEmpty() || data.WorkflowStateName.IsNullOrWhiteSpace())
                data.WorkflowStateName = null;

            return data;
        }



        private async Task<NlpChatroomAgent> GetAgentNamePicture(long userId)
        {
            if (__userLoginInfoDtoCache == null || __userLoginInfoDtoCache.Id != userId)
                __userLoginInfoDtoCache = (UserLoginInfoDto)_cacheManager.Get_UserLoginInfoDto(userId);

            if (AbpSession.UserId.HasValue && __userLoginInfoDtoCache == null)
            {
                var info = await _sessionAppService.GetCurrentLoginInformations();
                __userLoginInfoDtoCache = info.User;
            }

            if (__userLoginInfoDtoCache != null)
            {
                Guid? profilePictureId = null;
                try
                {
                    profilePictureId = Guid.Parse(__userLoginInfoDtoCache.ProfilePictureId);
                }
                catch (Exception)
                {
                }

                return new NlpChatroomAgent()
                {
                    AgentId = userId,
                    AgentName = __userLoginInfoDtoCache.Name,
                    AgentPictureId = profilePictureId
                };
            }
            else
            {
                return new NlpChatroomAgent()
                {
                    AgentId = userId,
                    AgentName = "",
                    AgentPictureId = null
                };
            }
        }


        private async Task<NlpWorkflowStateInfo> GetNlpWorkflowStateInfo(Guid workflowStateId)
        {
            if (workflowStateId == Guid.Empty)
                return null;

            if (__nlpWorkflowStateInfoCache == null || __nlpWorkflowStateInfoCache.Id != workflowStateId)
                __nlpWorkflowStateInfoCache = (NlpWorkflowStateInfo)_cacheManager.Get_NlpWorkflowStates(workflowStateId);

            if (__nlpWorkflowStateInfoCache == null)
            {
                var nlpWorkflowStateInfo = await (from o in _nlpWorkflowStateRepository.GetAll().Include(e => e.NlpWorkflowFk).Where(e => e.Id == workflowStateId)
                                                  select new NlpWorkflowStateInfo()
                                                  {
                                                      Id = o.Id,
                                                      ResponseNonWorkflowAnswer = o.ResponseNonWorkflowAnswer,
                                                      DontResponseNonWorkflowErrorAnswer = o.DontResponseNonWorkflowErrorAnswer,
                                                      NlpWorkflowId = o.NlpWorkflowId,
                                                      Outgoing3FalseOp = o.Outgoing3FalseOp,
                                                      OutgoingFalseOp = o.OutgoingFalseOp,
                                                      StateInstruction = o.StateInstruction,
                                                      StateName = o.StateName,
                                                      NlpWorkflowName = o.NlpWorkflowFk.Name
                                                  }).FirstOrDefaultAsync();

                __nlpWorkflowStateInfoCache = (NlpWorkflowStateInfo)_cacheManager.Set_NlpWorkflowStates(workflowStateId, nlpWorkflowStateInfo);
            }

            return __nlpWorkflowStateInfoCache;
        }



        /// <summary>
        /// 取得Client, Agent或Chatbot的頭像或名字
        /// </summary>
        /// <param name="role"></param>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private async Task<NlpUserNameImage> GetNameImage(string role, Guid? id, long? userId, string channel)
        {
            NlpUserNameImage nameImage = new NlpUserNameImage();

            switch (role)
            {
                case "client":
                    nameImage.Name = "";
                    nameImage.Image = "/Common/Images/default-profile-picture.png";

                    if (channel == "line")
                    {
                        var lineUser = _nlpLineUsersAppService.GetNlpLineUserDto(id.Value);
                        if (lineUser != null)
                        {
                            if (lineUser.UserName.IsNullOrEmpty() == false)
                                nameImage.Name = lineUser.UserName;
                            if (lineUser.PictureUrl.IsNullOrEmpty() == false)
                                nameImage.Image = lineUser.PictureUrl;
                        }
                    }
                    else if (channel == "facebook")
                    {
                        var facebookUser = _nlpFacebookUsersAppService.GetNlpFacebookUserDto(id.Value);
                        if (facebookUser != null)
                        {
                            if (facebookUser.UserName.IsNullOrEmpty() == false)
                                nameImage.Name = facebookUser.UserName;
                            if (facebookUser.PictureUrl.IsNullOrEmpty() == false)
                                nameImage.Image = facebookUser.PictureUrl;
                        }
                    }


                    break;
                case "chatbot":
                    var nlpChatbot = _nlpChatbotFunction.GetChatbotDto(id.Value);
                    if (nlpChatbot != null)
                    {
                        nameImage.Name = nlpChatbot.Name;
                        nameImage.Image = "/Chatbot/ProfilePicture/" + nlpChatbot.ChatbotPictureId.ToString();
                    }
                    else
                    {
                        nameImage.Name = "/Chatbot/ProfilePicture";
                        nameImage.Image = "";
                    }
                    break;
                case "agent":
                    var agent = await GetAgentNamePicture(userId.Value);
                    nameImage.Name = agent.AgentName;
                    nameImage.Image = "/Chatbot/GetProfilePictureById/" + agent.AgentPictureId.ToString();
                    break;
            }
            return nameImage;
        }


        private async Task<NlpCbMessage> SaveUnfoundMessage(Guid chatbotId, Guid clientId, string question, List<string> alternativeQuestion, Guid? qaAccuracyId)
        {
            var nlpCbMessage = await GetUnfoundMessageAsync(chatbotId, clientId, question, alternativeQuestion, qaAccuracyId);

            if (nlpCbMessage == null)
                return null;

            await _nlpCbMessageRepository.InsertAsync(nlpCbMessage);
            //CurrentUnitOfWork.SaveChanges();
            return nlpCbMessage;
        }



    }
}
