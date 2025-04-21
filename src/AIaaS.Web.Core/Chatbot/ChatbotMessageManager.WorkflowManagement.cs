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


        public async Task<ChatroomWorkflowInfo> SetChatbotWorkflowState(SetChatroomWorkflow workflow)
        {
            var chatroomStatus = await GetChatroomStatus(workflow.ChatbotId, workflow.ClientId);
            if (chatroomStatus == null)
                return null;

            if (workflow.WorkflowName.IsNullOrEmpty() || workflow.WorkflowName.IsNullOrWhiteSpace() || workflow.WorkflowStateName.IsNullOrEmpty() || workflow.WorkflowStateName.IsNullOrWhiteSpace())
                chatroomStatus.WfState = Guid.Empty;
            else
            {
                Guid wfpStatus = await _nlpWorkflowStateRepository.GetAll()
                     .Include(e => e.NlpWorkflowFk)
                     .Where(e => e.StateName == workflow.WorkflowStateName && e.NlpWorkflowFk.Name == workflow.WorkflowName && e.NlpWorkflowFk.NlpChatbotId == workflow.ChatbotId).Select(e => e.Id).FirstOrDefaultAsync();

                if (chatroomStatus.WfState != wfpStatus)
                {
                    chatroomStatus.WfState = wfpStatus;
                    chatroomStatus.IncorrectAnswerCount = 0;
                }
            }

            UpdateChatroomStatusCache(workflow.ChatbotId, workflow.ClientId, chatroomStatus);
            return await GetChatbotWorkflowState(new NlpChatroom(workflow.ChatbotId, workflow.ClientId));
        }


        public async Task<NlpWfsFalsePredictionOpDto> GetNlpWfsFalsePredictionOpDto(Guid chatbotId, Guid clientId, string json)
        {
            try
            {
                var value = JsonConvert.DeserializeObject<NlpWfsFalsePredictionOpDto>(json);

                if (value.NextStatus == NlpWorkflowStateConsts.WfsKeepCurrent)
                {
                    var chatroomInfo = await GetChatroomStatus(chatbotId, clientId);
                    value.NextStatus = chatroomInfo.WfState;
                }

                return value;

            }
            catch (Exception)
            {
                return null;
            }
        }

    }
}
