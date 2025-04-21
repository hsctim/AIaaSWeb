using Abp.Application.Services;
using Abp.Auditing;
using Abp.Timing;
using Abp.UI;
using AIaaS.Chatbot;
using AIaaS.Nlp.Dtos;
using AIaaS.Nlp;
using AIaaS.Web.Chatbot;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Abp.Extensions;
using Abp.EntityFrameworkCore.EFPlus;
using AIaaS.Web.Chatbot.Dto;
using System.Linq;
using System.Threading;
using ReflectSoftware.Facebook.Messenger.Common.Models;
using ReflectSoftware.Facebook.Messenger.Client;
using ReflectSoftware.Facebook.Messenger.Common.Models.Client;
using Abp.Domain.Uow;
using AIaaS.Nlp.Dtos.NlpCbMessage;
using System.Diagnostics;
using AIaaS.Nlp.Lib.Dtos;

namespace AIaaS.Web.Chatbot
{
    public partial class ChatbotMessageManager : ApplicationService, IChatbotMessageManager
    {

        private async Task<List<NlpCbMessageEx>> ProcessReceiveMessage(ChatbotMessageManagerMessageDto input)
        {
            if (input.Message.Length > 256)
                throw new UserFriendlyException(ChatErrorCode.Error_InvalidInputParameter, "Invalid input parameter");

            var nlpChatbot = _nlpChatbotFunction.GetChatbotDto(input.ChatbotId.Value);
            if (nlpChatbot == null || nlpChatbot.Disabled)
                throw new UserFriendlyException(ChatErrorCode.Error_InvalidChatbotId, "ChatbotId should be a valid guid.");
            //return null;

            input.SenderTime ??= Clock.Now;

            List<NlpCbMessageEx> nlpCbMessageExList = null;
            NlpCbMessageEx nlpCbMessageEx = new NlpCbMessageEx();

            try
            {
                using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MustHaveTenant, AbpDataFilters.MayHaveTenant))
                {
                    nlpCbMessageEx.NlpCbMessage = await _nlpCbMessageRepository.InsertAsync(new NlpCbMessage()
                    {
                        TenantId = nlpChatbot.TenantId,
                        ClientId = input.ClientId,
                        NlpChatbotId = input.ChatbotId,
                        NlpMessage = input.Message,
                        NlpMessageType = input.MessageType,
                        NlpCreationTime = input.SenderTime.Value,
                        NlpSenderRole = input.SenderRole,
                        NlpReceiverRole = input.ReceiverRole,
                        NlpAgentId = input.AgentId,
                    });

                    //CurrentUnitOfWork.SaveChanges();

                    //加入Message到Chatroom
                    if (input.SenderRole == "client" || input.ReceiverRole == "client")
                    {
                        await AddMessageToChatroomStatus(input.ChatbotId.Value, input.ClientId.Value, new NlpChatroomMessage()
                        {
                            IsClientSent = input.SenderRole == "client" ? true : false,
                            Message = input.Message
                        });
                    }

                    //送至Chatbot AI
                    if (input.ReceiverRole == "chatbot" && input.ChatbotId.HasValue && input.MessageType == "text")
                    {
                        try
                        {
                            nlpCbMessageExList = await GetChatbotReplyMessage(nlpChatbot, input.ClientId.Value, nlpCbMessageEx.NlpCbMessage);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex.ToString(), ex);

                            var chatroomStatus = await GetChatroomStatus(nlpChatbot.Id, input.ClientId.Value);

                            if (input.SenderRole == "agent" || (chatroomStatus.ResponseConfirmEnabled == true && chatroomStatus.ChatroomAgents.Count > 0))
                            {
                                await DeferredSendAgentUnfoundMessageAnswer(input.ChatbotId.Value, input.ClientId.Value);
                                return nlpCbMessageExList;
                            }
                            else
                            {
                                throw;
                            }
                        }

                        //加入Message到Chatroom

                        if (nlpCbMessageExList != null)
                        {
                            foreach (var msg in nlpCbMessageExList)
                            {
                                if (msg != null && nlpCbMessageEx.NlpCbMessage != null && msg.NlpCbMessage.NlpReceiverRole == "client")
                                    await AddMessageToChatroomStatus(input.ChatbotId.Value, input.ClientId.Value, new NlpChatroomMessage() { IsClientSent = false, Message = msg.NlpCbMessage.NlpMessage });

                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal($"An error occured while ProcessReceiveMessage", ex);

                nlpCbMessageEx.NlpCbMessage = await SaveUnfoundMessage(nlpChatbot.Id, input.ClientId.Value, null, null, null);
                if (nlpCbMessageEx?.NlpCbMessage != null)
                    await AddMessageToChatroomStatus(input.ChatbotId.Value, input.ClientId.Value, new NlpChatroomMessage() { IsClientSent = false, Message = input.Message });
            }

            return nlpCbMessageExList;
        }


        private async Task<List<NlpCbMessageEx>> GetChatbotReplyMessage(NlpChatbotDto nlpChatbot, Guid clientId, NlpCbMessage nlpCbMessage)
        {
            var nlpCbMessageExList = new List<NlpCbMessageEx>();
            var allPredictMessages = new List<AllPredictMessages>();

            //var threshold_predict = nlpChatbot.PredThreshold;
            //var threshold_suggestion = nlpChatbot.SuggestionThreshold;

            //NlpCbMessageEx nlpCbMessageEx = null;
            var inputMessage = nlpCbMessage.NlpMessage.Trim();

            //var questionWebAPITask = _nlpCbDictionariesFunction.GetQuestionSegmentsHashForComparison(nlpChatbot.TenantId, nlpChatbot.Id, nlpChatbot.Language, inputMessage);

            var chatroomStatus = await GetChatroomStatus(nlpChatbot.Id, clientId);

            //if (chatroomStatus.WfState != Guid.Empty)
            //threshold_predict = nlpChatbot.WSPredThreshold;

            //var threshold_same = threshold_predict / 2;      //檢查Segment是否相同，但accu要大於threadhold_same才檢查

            var workflowStatus = await GetNlpWorkflowStateInfo(chatroomStatus.WfState);

            //先檢查在流程內的WorkflowState
            if (nlpCbMessage.NlpSenderRole != "agent" && workflowStatus != null)
            {
                //先檢查在流程內的WorkflowState
                //var message1 = _nlpCbDictionariesFunction.PrepareSynonymString(nlpChatbot.TenantId, nlpChatbot.Id, nlpChatbot.Language, (workflowStatus.Id, inputMessage));

                var predictResult1 = await ChatbotPredict(nlpChatbot.Id, inputMessage, workflowStatus.Id);
                if (predictResult1.errorCode == "success" && predictResult1.result != null && predictResult1.result.Any())
                {
                    foreach (var item in predictResult1.result)
                    {
                        //只加入相同workflowstate的回應
                        var nlpQADto = await GetNlpQADtofromNNID(nlpChatbot.Id, item.nnid);
                        if (nlpQADto == null)
                            continue;

                        if (nlpQADto.CurrentWfState != null && nlpQADto.CurrentWfState != Guid.Empty && nlpQADto.CurrentWfState.Value == workflowStatus.Id)
                        {
                            item.QaId = nlpQADto.Id;

                            allPredictMessages.Add(new AllPredictMessages()
                            {
                                ChatbotPredictInput = new (Guid?, string)[] { (workflowStatus.Id, inputMessage) },
                                ChatbotPredictResult = item,
                                InputState = workflowStatus == null ? Guid.Empty : workflowStatus.Id,
                                NlpQADto = nlpQADto,
                                inPredictionThreshold = item.probability > nlpChatbot.WSPredThreshold,
                                inWorkflowState = true,
                                inSuggestionThreshold = item.probability > nlpChatbot.SuggestionThreshold
                            });
                        }
                    }
                }

                //再檢查在流程內的Workflow，workflow內有多個workflowstates

                predictResult1 = await ChatbotPredict(nlpChatbot.Id, inputMessage, workflowStatus.NlpWorkflowId);
                if (predictResult1.errorCode == "success" && predictResult1.result != null && predictResult1.result.Any())
                {
                    foreach (var item in predictResult1.result)
                    {
                        var nlpQADto = await GetNlpQADtofromNNID(nlpChatbot.Id, item.nnid);
                        if (nlpQADto == null)
                            continue;

                        if (nlpQADto.CurrentWfState != null && nlpQADto.CurrentWfState != Guid.Empty && nlpQADto.CurrentWfState.Value == workflowStatus.NlpWorkflowId)
                        {
                            if (nlpQADto.CurrentWfState == workflowStatus.NlpWorkflowId)
                            {
                                item.QaId = nlpQADto.Id;

                                //只加入相同workflow的回應
                                allPredictMessages.Add(new AllPredictMessages()
                                {
                                    //ChatbotPredictInput = message1,
                                    ChatbotPredictInput = new (Guid?, string)[] { (workflowStatus.NlpWorkflowId, nlpCbMessage.NlpMessage) },
                                    ChatbotPredictResult = item,
                                    InputState = workflowStatus == null ? Guid.Empty : workflowStatus.Id,
                                    NlpQADto = nlpQADto,
                                    inPredictionThreshold = item.probability > nlpChatbot.WSPredThreshold,
                                    inWorkflow = true,
                                    inSuggestionThreshold = item.probability > nlpChatbot.SuggestionThreshold

                                });
                            }
                        }
                    }
                }
            }

            //非流程或在流程但可以問非流程問題
            if (workflowStatus == null || workflowStatus.ResponseNonWorkflowAnswer)
            {
                //var message2 = _nlpCbDictionariesFunction.PrepareSynonymString(nlpChatbot.TenantId, nlpChatbot.Id, nlpChatbot.Language, (Guid.Empty, nlpCbMessage.NlpMessage));

                var predictResult2 = await ChatbotPredict(nlpChatbot.Id, nlpCbMessage.NlpMessage, Guid.Empty);
                if (predictResult2.errorCode == "success" && predictResult2.result != null && predictResult2.result.Any())
                {
                    foreach (var item in predictResult2.result)
                    {
                        //只加入相同workflowstatus==null的回應
                        var nlpQADto = await GetNlpQADtofromNNID(nlpChatbot.Id, item.nnid);
                        if (nlpQADto == null)
                            continue;

                        if (nlpQADto.CurrentWfState == Guid.Empty || nlpQADto.CurrentWfState == null)
                        {
                            item.QaId = nlpQADto.Id;

                            allPredictMessages.Add(new AllPredictMessages()
                            {
                                ChatbotPredictInput = new (Guid?, string)[] { (Guid.Empty, nlpCbMessage.NlpMessage) },
                                ChatbotPredictResult = item,
                                InputState = workflowStatus == null ? Guid.Empty : workflowStatus.Id,
                                NlpQADto = nlpQADto,
                                inPredictionThreshold = (workflowStatus != null ? item.probability > nlpChatbot.WSPredThreshold : item.probability > nlpChatbot.PredThreshold),
                                inSuggestionThreshold = item.probability > nlpChatbot.SuggestionThreshold
                            });
                        }
                    }
                }
            }

            //Distinct 去掉多餘且可能性低的相同NNID回應
            var orderPredictMessages = allPredictMessages.Where(e => e.ChatbotPredictResult.nnid != 0)
                .OrderByDescending(e => e.ChatbotPredictResult.probability > .99)
                .ThenByDescending(e => e.ChatbotPredictResult.probability > .95)
                .ThenByDescending(e => e.inPredictionThreshold)
                .ThenByDescending(e => e.ChatbotPredictResult.probability * (e.inWorkflowState ? 1.1 : 1.0) * (e.inWorkflow ? 1.05 : 1.0));

            var distinctPredictMessages = new List<AllPredictMessages>();
            foreach (var item in orderPredictMessages)
            {
                if (distinctPredictMessages.Any(e => e.ChatbotPredictResult.nnid == item.ChatbotPredictResult.nnid) == false)
                {
                    distinctPredictMessages.Add(item);

                    if (distinctPredictMessages.Count >= 3)
                        break;
                }
            }

            //設定連續無法回答問題的數目
            if (nlpCbMessage.NlpSenderRole != "agent")
            {
                chatroomStatus.IncorrectAnswerCount = (distinctPredictMessages.Count > 0 && distinctPredictMessages.First().inPredictionThreshold) ? 0 : chatroomStatus.IncorrectAnswerCount + 1;
            }


            //設定新的流程狀態，若命中問題
            if (distinctPredictMessages.Count > 0 && distinctPredictMessages.First().inPredictionThreshold)
            {
                var nlpQADto = distinctPredictMessages.First().NlpQADto;
                if (nlpQADto.NextWfState != null && nlpQADto.NextWfState != NlpWorkflowStateConsts.WfsKeepCurrent)
                {
                    chatroomStatus.WfState = (nlpQADto.NextWfState == null) ? Guid.Empty : nlpQADto.NextWfState.Value;
                    Debug.WriteLine("WorkflowState : " + chatroomStatus.WfState.ToString());
                }
            }


            //設定無法命中的回應，當錯誤>=1次或>=3次時設定
            string PredictionErrorMessage = null;
            if (workflowStatus != null && chatroomStatus.IncorrectAnswerCount >= 1)
            {
                if (chatroomStatus.IncorrectAnswerCount >= 3)
                {
                    var nlpWfsOp = await GetNlpWfsFalsePredictionOpDto(nlpChatbot.Id, clientId, workflowStatus.Outgoing3FalseOp);
                    if (nlpWfsOp != null)
                    {
                        PredictionErrorMessage = nlpWfsOp.ResponseMsg;
                        chatroomStatus.WfState = nlpWfsOp.NextStatus;
                        Debug.WriteLine("WorkflowState : " + chatroomStatus.WfState.ToString());
                    }
                }
                else if (chatroomStatus.IncorrectAnswerCount >= 1)
                {
                    var nlpWfsOp = await GetNlpWfsFalsePredictionOpDto(nlpChatbot.Id, clientId, workflowStatus.OutgoingFalseOp);
                    if (nlpWfsOp != null)
                    {
                        PredictionErrorMessage = nlpWfsOp.ResponseMsg;
                        chatroomStatus.WfState = nlpWfsOp.NextStatus;
                        Debug.WriteLine("WorkflowState : " + chatroomStatus.WfState.ToString());
                    }
                }
            }

            var nlpCbQAAccuracy = await SaveNlpCbQAAccuracy(nlpCbMessage, distinctPredictMessages);

            //若是Agent監控
            if (nlpCbMessage.NlpSenderRole == "agent" || (chatroomStatus.ResponseConfirmEnabled == true && chatroomStatus.ChatroomAgents.Count > 0))
            {
                if (PredictionErrorMessage.IsNullOrEmpty() == false)
                {
                    var sendNlpCbMessage = new NlpCbMessageEx(new NlpCbMessage()
                    {
                        TenantId = nlpChatbot.TenantId,
                        ClientId = clientId,
                        NlpChatbotId = nlpChatbot.Id,
                        NlpMessage = PredictionErrorMessage,
                        NlpMessageType = "text.workflow.error",
                        NlpCreationTime = Clock.Now,
                        NlpSenderRole = "chatbot",
                        NlpReceiverRole = "agent",
                        NlpAgentId = nlpCbMessage.NlpAgentId,
                        AlternativeQuestion = null,
                        QAAccuracyId = null
                    });

                    await _nlpCbMessageRepository.InsertAsync(sendNlpCbMessage.NlpCbMessage);
                    //CurrentUnitOfWork.SaveChanges();
                    nlpCbMessageExList.Add(sendNlpCbMessage);
                }

                var suggestedAnswers = new List<string>();

                var unfoundMessage = (await GetUnfoundMessageWithoutGPTAsync(nlpChatbot.Id, clientId, inputMessage, null, nlpCbQAAccuracy?.Id))?.NlpMessage;

                if (unfoundMessage.IsNullOrEmpty() == false)
                    suggestedAnswers.Add(unfoundMessage);


                foreach (var result in distinctPredictMessages)
                {
                    if (result.inSuggestionThreshold)
                    {
                        try
                        {
                            var nlpQADto = result.NlpQADto;
                            var answers = JsonConvert.DeserializeObject<string[]>(nlpQADto.Answer);

                            foreach (var answer in answers)
                                suggestedAnswers.Add(await ReplaceCustomStringAsync(answer, nlpChatbot.Id));
                        }
                        catch (Exception ex)
                        {
                            Logger.Fatal(ex.ToString(), ex);
                        }
                    }
                    else
                        break;
                }

                nlpCbMessageExList.Add(new NlpCbMessageEx(new NlpCbMessage()
                {
                    TenantId = nlpChatbot.TenantId,
                    ClientId = clientId,
                    NlpChatbotId = nlpChatbot.Id,
                    NlpMessage = "",
                    NlpMessageType = chatroomStatus.WfState == Guid.Empty ? "text" : "text.workflow",
                    NlpCreationTime = Clock.Now,
                    NlpSenderRole = "chatbot",
                    NlpReceiverRole = "agent",
                    NlpAgentId = nlpCbMessage.NlpAgentId,
                    AlternativeQuestion = null,
                    QAAccuracyId = nlpCbQAAccuracy?.Id
                }, suggestedAnswers
                ));

                return nlpCbMessageExList;
            }
            else
            {
                //User至Chatbot 或 Agent端不監控，直接由Chatbot回應至User

                if (PredictionErrorMessage.IsNullOrEmpty() == false)
                {
                    var sendNlpCbMessage2 = new NlpCbMessageEx(new NlpCbMessage()
                    {
                        TenantId = nlpChatbot.TenantId,
                        ClientId = clientId,
                        NlpChatbotId = nlpChatbot.Id,
                        NlpMessage = PredictionErrorMessage,
                        NlpMessageType = "text.workflow.error",
                        NlpCreationTime = Clock.Now,
                        NlpSenderRole = "chatbot",
                        NlpReceiverRole = "client",
                        NlpAgentId = null,
                        AlternativeQuestion = null,
                        QAAccuracyId = null
                    });

                    await _nlpCbMessageRepository.InsertAsync(sendNlpCbMessage2.NlpCbMessage);
                    //CurrentUnitOfWork.SaveChanges();
                    nlpCbMessageExList.Add(sendNlpCbMessage2);
                }

                List<string> alternativeQuestion = null;


                var firstIndex = 0;
                if (distinctPredictMessages.FirstOrDefault().inPredictionThreshold)
                    firstIndex = 1;

                foreach (var result in distinctPredictMessages.Skip(firstIndex))
                {
                    //if (result.ChatbotPredictResult.probability >= threshold_high)
                    //    break;

                    if (result.inSuggestionThreshold)
                    {
                        try
                        {
                            var nlpQADto = result.NlpQADto;
                            var questions = nlpQADto.GetQuestionList();
                            alternativeQuestion ??= new List<string>();
                            alternativeQuestion.Add(await ReplaceCustomStringAsync(questions.First(), nlpChatbot.Id));
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

                if (distinctPredictMessages.First().inPredictionThreshold == false && nlpCbMessage.NlpSenderRole != "agent")
                {
                    if (PredictionErrorMessage.IsNullOrEmpty() == true || (PredictionErrorMessage.IsNullOrEmpty() == false && workflowStatus != null && workflowStatus.DontResponseNonWorkflowErrorAnswer == false))
                    {
                        var unfoundMessage = await SaveUnfoundMessage(nlpChatbot.Id, clientId, inputMessage, alternativeQuestion, nlpCbQAAccuracy?.Id);

                        if (unfoundMessage != null)
                            nlpCbMessageExList.Add(new NlpCbMessageEx(unfoundMessage));
                    }

                    return nlpCbMessageExList;
                }

                //回傳預測答案及QaId
                PredictedQAMessage output = new PredictedQAMessage();

                if (AbpSession.TenantId == 1)
                {
                    foreach (var i in distinctPredictMessages)
                    {
                        output ??= new PredictedQAMessage();

                        if (output.Message.IsNullOrWhiteSpace() == false && output.Message.Length > 0)
                            output.Message += "<br>";

                        output.QaId = distinctPredictMessages.First().ChatbotPredictResult.QaId;

                        output.Message += (100.0 * i.ChatbotPredictResult.probability).ToString("N2") + "___" + i.ChatbotPredictResult.nnid.ToString() + "___" + (await GetAnswerFromNNIDRepetition(nlpChatbot.Id, i.ChatbotPredictResult.nnid));
                    }
                }
                else
                {
                    output.Message = await GetAnswerFromNNIDRepetition(nlpChatbot.Id, distinctPredictMessages.First().ChatbotPredictResult.nnid);

                    output.QaId = distinctPredictMessages.First().ChatbotPredictResult.QaId;
                }

                //更新變數
                if (output.Message.IsNullOrEmpty() == false)
                {
                    output.Message = await ReplaceCustomStringAsync(output.Message, nlpChatbot.Id);
                    output.Message = await ChatGPT(nlpChatbot.Id, inputMessage, output.Message);
                }

                if (output.Message.IsNullOrEmpty())
                {
                    var unfoundMessage = await SaveUnfoundMessage(nlpChatbot.Id, clientId, inputMessage, alternativeQuestion, nlpCbQAAccuracy?.Id);

                    if (unfoundMessage != null)
                        nlpCbMessageExList.Add(new NlpCbMessageEx(unfoundMessage));

                    return nlpCbMessageExList;
                }

                NlpCbMessage sendNlpCbMessage = new NlpCbMessage()
                {
                    TenantId = nlpChatbot.TenantId,
                    ClientId = clientId,
                    NlpChatbotId = nlpChatbot.Id,
                    NlpMessage = output.Message.Substring(0, Math.Min(output.Message.Length, 1024)),
                    QAId = output.QaId,
                    NlpMessageType = chatroomStatus.WfState == Guid.Empty ? "text" : "text.workflow",
                    NlpCreationTime = Clock.Now,
                    NlpSenderRole = "chatbot",
                    NlpReceiverRole = "client",
                    NlpAgentId = null,
                    AlternativeQuestion = (alternativeQuestion == null) ? null : JsonConvert.SerializeObject(alternativeQuestion),
                    QAAccuracyId = nlpCbQAAccuracy?.Id
                };

                await _nlpCbMessageRepository.InsertAsync(sendNlpCbMessage);
                //CurrentUnitOfWork.SaveChanges();

                nlpCbMessageExList.Add(new NlpCbMessageEx(sendNlpCbMessage));
                return nlpCbMessageExList;
            }
        }

        public async Task<NlpCbGetChatbotPredictResult> ChatbotPredict(Guid chatbotId, string question, Guid? workflowState)
        {
            NlpCbGetChatbotPredictResult result = await _lpCbWebApiClient.ChatbotPredict(chatbotId, question, workflowState);

            return result;
        }



        public async Task<NlpCbGetChatbotPredictResult> ChatbotPredictBySimilarity(Guid chatbotId, string question, Guid? workflowState)
        {
            NlpCbGetChatbotPredictResult result = await _lpCbWebApiClient.ChatbotPredict(chatbotId, question, workflowState);

            if (result.errorCode != "success")
                return result;

            var predictQuestions = new List<IList<string>>();
            foreach (var resultItem in result.result)
            {
                var nlpQADto = await GetNlpQADtofromNNID(chatbotId, resultItem.nnid);

                var state1 = workflowState;
                var state2 = Guid.Empty;

                if (nlpQADto != null && nlpQADto.CurrentWfState != null)
                    state2 = nlpQADto.CurrentWfState.Value;

                if (nlpQADto == null || state1 != state2)
                {
                    predictQuestions.Add(null);
                    continue;
                }
                else
                {
                    IList<string> questions = null;
                    try
                    {
                        questions = JsonConvert.DeserializeObject<IList<string>>(nlpQADto.Question);
                    }
                    catch (Exception)
                    {
                    }

                    predictQuestions.Add(questions);
                }
            }

            var similarities = await _lpCbWebApiClient.GetSentencesSimilarityAsync(question, predictQuestions);
            var similarities2 = similarities.similarities.ToArray();

            int i = 0;
            foreach (var resultItem in result.result)
            {
                resultItem.probability = similarities2[i];
                i++;
            }

            result.result = result.result.OrderByDescending(e => e.probability).ToArray();

            return result;
        }


    }
}
