(function () {
    $(function () {

        var _nlpService = abp.services.app.nlpCbAgentOperations;
        var _$nlpPage = $('div[name="NlpCbAgentOperationsPage"]');
        var _permissions = {
            'send': abp.auth.hasPermission('Pages.NlpChatbot.NlpCbAgentOperations.SendMessage')
        };

        var showLeftListPane = true;

        var isToday = function (someDate) {
            var today = new Date()
            return someDate.getDate() == today.getDate() &&
                someDate.getMonth() == today.getMonth() &&
                someDate.getFullYear() == today.getFullYear()
        }

        var isNew = function (inputTimeString) {
            var latestTime = new Date(_$nlpPage.find('#chatroom-latestTime').val());
            var inputTime = new Date(inputTimeString);

            if (latestTime > inputTime)
                return false;
            else {
                _$nlpPage.find('#chatroom-latestTime').val(inputTimeString);
                return true;
            }
        }


        var getChatroomHtml = function (data, css1) {

            var unreadMessageCount = "";
            if (data.unreadMessageCount > 0)
                unreadMessageCount = "" + data.unreadMessageCount;

            var latestTime = new Date(data.latestMessageTime);
            var timeString = "";
            var dateString = "";
            if (isToday(latestTime))
                timeString = latestTime.toLocaleTimeString();
            else {
                timeString = latestTime.toLocaleTimeString();
                dateString = latestTime.toLocaleDateString();
            }

            var msgHtml = "";

            //最後兩筆訊號

            if (data.latestMessages) {
                for (var j = 0; j < data.latestMessages.length; j++) {
                    if (data.latestMessages[j].isClientSent == false) {
                        msgHtml +=
                            "<div class='pt-1 text-end'>" +
                            "   <div class='d-inline-block text-wrap text-start msg_cotainer_send'>" +
                            htmlStr(data.latestMessages[j].message) +
                            /*                        "       <i class='ms-3 fas fa-chevron-circle-left text-white align-middle'></i>" +*/
                            "   </div>" +
                            "</div>";
                    }
                    else {

                        msgHtml +=
                            "<div class='pt-1'>" +
                            "   <div class='d-inline-block text-wrap text-start msg_cotainer'>" +
                            /*                        "       <i class='fas fa-chevron-circle-right text-white align-middle me-3 '></i>" +*/
                            htmlStr(data.latestMessages[j].message) +
                            "   </div>" +
                            "</div>";
                    }
                }
            }

            msgHtml = "<div class='div-2msg'>" + msgHtml + "</div>";

            var agentHtml = "";
            if (data.chatroomAgents) {
                for (var n = 0; n < data.chatroomAgents.length; n++) {
                    if (data.chatroomAgents[n].agentId == abp.session.userId) {
                        agentHtml +=
                            "<div class='text-center img_cont div-agent ms-2'>" +
                            "   <img src='/Chatbot/GetProfilePictureById/" +
                            (data.chatroomAgents[n].agentPictureId || '') +
                            "' class='rounded-circle user_img' > " +
                            "   <div class = 'user_name'>" + htmlStr(data.chatroomAgents[n].agentName || '') +
                            "   </div>" +
                            "</div>";
                        break;
                    }
                }

                for (var n = 0; n < data.chatroomAgents.length; n++) {
                    if (data.chatroomAgents[n].agentId != abp.session.userId) {
                        agentHtml +=
                            "<div class='text-center img_cont div-agent ms-2'>" +
                            "   <img src='/Chatbot/GetProfilePictureById/" +
                            (data.chatroomAgents[n].agentPictureId || '') +
                            "' class='rounded-circle user_img' > " +
                            "   <div class = 'user_name'>" +
                            htmlStr(data.chatroomAgents[n].agentName || '') +
                            "   </div>" +
                            "</div>";
                    }
                }

                //agentHtml = agentHtml;
            }

            var clientInfo = data.clientName ? "<div class='text-wrap chatroom_font12'>" + data.clientName + "</div>" : (data.clientId ? "<div class='text-wrap text-break chatroom_font10'>" + data.clientId + "</div>" : "");

            var html =
                "<li class='chatbot-client-li' data-clientid='" + data.clientId + "' data-chatbotid='" + data.chatbotId + "'>" +
                "   <div class='d-flex w-100'>" +
                ///////
                "       <div>" +
                "           <div class='text-center img_cont'>" +
                "	         	<img src='" + data.clientPicture + "' class='rounded-circle user_img' > " +
                "	    	    <span class='" + (data.clientConnected ? "online_icon" : "offline_icon") + "'>" +
                "                   <div class='d-flex justify-content-center align-items-center h-100 text-white'>" + unreadMessageCount +
                "                   </div > " +
                "               </span>" +
                "           </div>" +
                //"           <div class = 'user_name'>" + (data.clientName || '') + "</div>" +
                "       </div>" +
                ///////
                "	    <div class='chatroom_info w-100'>" +
                "           <div class='d-flex justify-content-between'>" +
                "               <div>" +
                /*                (data.clientId ? "<div class='text-wrap chatroom_font10'>" + data.clientId + "</div>" : "") +*/
                clientInfo +
                (data.clientChannel ? "<div class='text-nowrap chatroom_font10'>" + data.clientChannel + "</div>" : "") +
                (data.clientIP ? "<div class='text-wrap chatroom_font10'>" + data.clientIP + "</div>" : "") +
                "               </div>" +
                "               <div class='text-wrap text-end mt-auto'>" +
                (dateString != "" ? "<div class='text-center chatroom_font10'>" + htmlStr(dateString) + "</div>" : "") +
                "                   <div class='text-center chatroom_font10'>" + htmlStr(timeString) + "</div>" +
                "               </div>" +
                "   	    </div>" +
                msgHtml +
                "   	</div>" +
                "   	<div class='" + css1 + "'>" +
                agentHtml +
                "   	<div class='flex-shrink-1 ms-3'>" +
                "           <div class='text-center img_cont'>" +
                "               <img src='/Chatbot/ProfilePicture/" + (data.chatbotPictureId ? data.chatbotPictureId : "") + "' class='rounded-circle user_img' > " +

                (data.responseConfirmEnabled == false ? "" :
                    "	    	    <span class= 'eye_icon text-center align-middle'>" +
                    "                   <i class='far fa-eye text-white fa-xs'></i>" +
                    "               </span>") +

                "           </div>" +
                "           <div class = 'user_name'>" + htmlStr(data.chatbotName || '') +
                "           </div>" +
                "       </div>" +
                "       </div>" +
                "       <div class='chatroom_menu_botton'>" +
                "       </div>" +
                "   </div>" +
                "</li>";

            return html;

        }

        var setChatroomEvent = function () {
            _$nlpPage.find('.chatbot-client-li').click(function (e) {

                if (!(_$nlpPage.find('.chatbot-client-li.activeli').length > 0 && _$nlpPage.find('.chatbot-client-li.activeli')[0] == $(this)[0])) {
                    _$nlpPage.find('.chatbot-client-li').removeClass('activeli');
                    $(this).addClass('activeli');

                    _$nlpPage.find('#chatroom-chatbotId').val($(this).data('chatbotid'));
                    _$nlpPage.find('#chatroom-clientId').val($(this).data('clientid'));
                    _$nlpPage.find('#chatbot_msg_text').val('');
                    $('div.msg_card_body').empty();

                    app.chatbot.agentRequestHistoryMessages({
                        chatbotId: $(this).data('chatbotid'),
                        clientId: $(this).data('clientid'),
                    });
                }

                //if (getViewport() != "xl") {
                //    event.preventDefault();
                //    // Getting the height of the document
                //    var n = $(document).height();
                //    $('html, body').animate({ scrollTop: n }, 1000);
                //}

                showLeftListPane = false;
                showHideWindow();
            });
        }


        var getAllChatrooms = function () {
            //debugger
            //_$nlpPage.find('.contacts').empty();

            _nlpService.getAllChatrooms(
                _$nlpPage.find('#ChatbotSelectFilter').val())
                .done(function (data) {
                    //debugger

                    _$nlpPage.find('.contacts').empty();

                    for (var i = 0; i < data.length; i++) {
                        var $html = $(getChatroomHtml(data[i], ""));

                        var count = $html.find('.div-agent').length;

                        for (var j = 1; j < count; j++) {
                            $html.find('.div-agent').eq(1).remove();
                        }

                        if (isNew(data[i].latestMessageTime))
                            _$nlpPage.find('.contacts').prepend($html[0]);
                        else
                            _$nlpPage.find('.contacts').append($html[0]);
                    }

                    //_$nlpPage.find('.contacts').find('.chatroom_menu_botton').empty();
                    setChatroomEvent();
                });
        };

        var setRightChatroomHeader = function ($headerData) {
            //debugger
            $headerData.find('.div-2msg').empty();
            $headerData.find('li').wrap('<div>').contents().unwrap();

            if (_permissions.send) {
                $headerData.find('.chatroom_menu_botton').append(
                    "<span id='action_menu_btn'><i class='fas fa-ellipsis-v'></i></span>" +
                    "<div class='action_menu'>" +
                    "	<ul>" +
                    "		<li><i class='fas fa-play'></i> Start to Reply</li>" +
                    "	</ul>" +
                    "</div>	"
                ).addClass("ms-8");
            }

            _$nlpPage.find('#chatroom-right-header').html($headerData[0].innerHTML).addClass('p-2');

            _$nlpPage.find('#action_menu_btn').click(function () {
                if (_$nlpPage.find('#chatroom-right-header').find('span.eye_icon').length > 0) {
                    _$nlpPage.find('#chatroom-right-header').find('ul').html(
                        "<li id='NlpCbDisableResponseConfirm'><i class='fas fa-eye-slash'></i>" + app.localize("NlpCbDisableResponseConfirm") + "</li>" +
                        "<li id ='BackToList' class='d-md-none'><i class='fa fa-arrow-left d-md-none'></i>" + app.localize("BackToList") + "</li>");

                    _$nlpPage.find('#chatroom-right-header').find('#NlpCbDisableResponseConfirm').click(function () {
                        app.chatbot.enableResponseConfirm({
                            chatbotId: _$nlpPage.find('#chatroom-chatbotId').val(),
                            clientId: _$nlpPage.find('#chatroom-clientId').val(),
                            enableResponseConfirm: false
                        });
                    });

                } else {
                    _$nlpPage.find('#chatroom-right-header').find('ul').html(
                        "<li id='NlpCbDisableResponseConfirm'><i class='far fa-eye'></i>" + app.localize("NlpCbEnableResponseConfirm") + "</li>" +
                        "<li id ='BackToList' class='d-md-none'><i class='fa fa-arrow-left'></i>" + app.localize("BackToList") + "</li>");
                    _$nlpPage.find('#chatroom-right-header').find('#NlpCbDisableResponseConfirm').click(function () {
                        app.chatbot.enableResponseConfirm({
                            chatbotId: _$nlpPage.find('#chatroom-chatbotId').val(),
                            clientId: _$nlpPage.find('#chatroom-clientId').val(),
                            enableResponseConfirm: true
                        });
                    });
                }

                _$nlpPage.find('#chatroom-right-header').find('#BackToList').click(function () {
                    $("body").attr('data-backtolist', 'off');
                    showLeftListPane = true;
                    showHideWindow();
                });

                _$nlpPage.find('.action_menu').toggle();
            });
        };


        _$nlpPage.find('#ChatbotSelectFilter').change(function () {
            _$nlpPage.find('div.msg_card_body').empty();
            _$nlpPage.find('#chatroom-right-header').empty();

            _$nlpPage.find('#chatroom-chatbotId').val('');
            _$nlpPage.find('#chatroom-clientId').val('');

            getAllChatrooms();
        });



        _$nlpPage.find('#nlp_agent_chatroom').on('chatroom.updateChatroomStatus', function (event, status) {
            try {
                if ($('#ChatbotSelectFilter').val() != "a796a32a-cf36-4d00-b90d-c915a2b86043" &&
                    $('#ChatbotSelectFilter').val() != status.chatbotId)
                    return;

                var $html = $(getChatroomHtml(status, ""));
                var $header = $(getChatroomHtml(status, "d-flex"));

                var count = $html.find('.div-agent').length;
                for (var j = 1; j < count; j++) {
                    $html.find('.div-agent').eq(1).remove();
                }

                var $li = _$nlpPage.find(".chatbot-client-li[data-chatbotId='" + status.chatbotId + "'][data-clientid='" + status.clientId + "']");

                if ($li.length > 0) {
                    var active = $li.hasClass('activeli');
                    //debugger

                    $li.html($html[0].innerHTML);

                    if (active) {
                        $li.addClass('activeli');
                        setRightChatroomHeader($header);
                    }

                    if (isNew(status.latestMessageTime)) {
                        if (_$nlpPage.find('.chatbot-client-li')[0] != $li[0]) {
                            $li.remove();
                            _$nlpPage.find('.contacts').prepend($li[0]);

                            _$nlpPage.find('.contacts').find('.chatroom_menu_botton').empty();
                            setChatroomEvent();
                        }
                    }
                }
                else {
                    if (isNew(status.latestMessageTime))
                        _$nlpPage.find('.contacts').prepend($html[0]);
                    else
                        _$nlpPage.find('.contacts').append($html[0]);

                    _$nlpPage.find('.contacts').find('.chatroom_menu_botton').empty();
                    setChatroomEvent();
                }
            } catch (e) {
                console.error(e, e.stack);
            }
        });


        var messageReceived = function (message) {
            var senderTime = new Date(message.senderTime);

            var timeString = null;
            if (isToday(senderTime))
                timeString = senderTime.toLocaleTimeString();
            else
                timeString = senderTime.toLocaleTimeString() + " | " + senderTime.toLocaleDateString();

            if (typeof String.prototype.replaceAll == "undefined") {
                String.prototype.replaceAll = function (match, replace) {
                    return this.replace(new RegExp(match, 'g'), () => replace);
                }
            }

            var html =
                "   <div class='d-flex " + (message.senderRole == "client" ? "justify-content-start" : "justify-content-end") + " mb-8' data-id='" + message.id + "'>" +

                (message.senderRole == "client" ?
                    "       <div>" +
                    "           <div class='text-center img_cont AAA'>" +
                    "	         	<img src='" +
                    ((message.senderRole == "client" ? message.senderImage : message.receiverImage) || '') +
                    "' class='rounded-circle user_img_msg' > " +
                    "           </div>" +
                    "       </div>" 
                    : "") +

                "	    <div class='chatroom_info w-100 " +
                (message.senderRole == "client" ? "" : "text-end") +
                "'>" +
                "   	    <div class='d-inline-block " +
                (message.senderRole == "client" ? "msg_cotainer" : "msg_cotainer_send text-start") +
                "'>" +
                (message.message || '').replaceAll("\\\"", '\"') +
                "               <div class='" + (message.senderRole == "client" ? "msg_time" : "msg_time_send") + " text-nowrap'>" + htmlStr(timeString) +
                "               </div>" +
                "     	    </div>" +
                "   	</div>" +

                (message.senderRole == "client" ? "" :
                    "   	<div class='flex-shrink-1 ms-3'>" +
                    "           <div class='img_cont'>" +
                    "               <img class='rounded-circle user_img_msg' src='" + ((message.senderRole == "client" ? message.receiverImage : message.senderImage) || '') + "'> " +
                    "           </div>" +
                    "       </div>")
                 +
                "   </div>";

            $('div.msg_card_body').append(html);
            $('div.msg_card_body').scrollTop($('div.msg_card_body')[0].scrollHeight);
        };


        _$nlpPage.find('#nlp_agent_chatroom').on('chatroom.agentMessagesReceived', function (event, messages) {
            try {
                if (messages.messages.length == 0)
                    return;

                var _chatbotId = _$nlpPage.find('#chatroom-chatbotId').val();
                var _clientId = _$nlpPage.find('#chatroom-clientId').val();

                clearSuggestedAnswerDiv();

                if (messages.messages[0].chatbotId != _chatbotId || messages.messages[0].clientId != _clientId)
                    return;

                for (var i = 0; i < messages.messages.length; i++) {
                    messageReceived(messages.messages[i]);
                }

                app.chatbot.agentSendReceipt({
                    chatbotId: _chatbotId,
                    clientId: _clientId
                });
            } catch (e) {
                console.error(e, e.stack);
            }
        });

        _$nlpPage.find('#nlp_agent_chatroom').on('chatroom.agentMessageReceived', function (event, message) {
            try {
                var _chatbotId = _$nlpPage.find('#chatroom-chatbotId').val();
                var _clientId = _$nlpPage.find('#chatroom-clientId').val();

                if (message.chatbotId != _chatbotId || message.clientId != _clientId)
                    return;

                messageReceived(message);

                app.chatbot.agentSendReceipt({
                    chatbotId: _chatbotId,
                    clientId: _clientId
                });

            } catch (e) {
                console.error(e, e.stack);
            }
        });


        _$nlpPage.find('#nlp_agent_chatroom').on('chatroom.reconnect', function (event) {
            try {
                var _chatbotId = _$nlpPage.find('#chatroom-chatbotId').val();
                var _clientId = _$nlpPage.find('#chatroom-clientId').val();

                app.chatbot.agentReconnect({
                    chatbotId: _chatbotId,
                    clientId: _clientId
                });
            } catch (e) {
                console.error(e, e.stack);
            }
        });

        _$nlpPage.find('#nlp_agent_chatroom').on('chatroom.suggestedAnswers', function (event, message) {
            try {
                var _chatbotId = _$nlpPage.find('#chatroom-chatbotId').val();
                var _clientId = _$nlpPage.find('#chatroom-clientId').val();

                clearSuggestedAnswerDiv();

                var senderTime = new Date(message.senderTime);

                var timeString = null;
                if (isToday(senderTime))
                    timeString = senderTime.toLocaleTimeString();
                else
                    timeString = senderTime.toLocaleTimeString() + " | " + senderTime.toLocaleDateString();


                var html1 = "";

                if (message.suggestedAnswers) {
                    var answers = JSON.parse(message.suggestedAnswers)


                    for (var i = 0; i < answers.length; i++) {
                        html1 +=
                            "<div class='btn-group mt-2 mx-1 suggested-button' role='group'>" +
                            "   <button type='button' class='btn btn-light-primary btn_wrap_left btn_to_client text-start'>" + htmlStr(answers[i]) +
                            "   </button>" +
                            "   <button type='button' class='btn btn-light-primary btn_wrap_left p-2 btn_to_chatroom'><i class='fas fa-location-arrow p-0'></i></button>" +
                            "</div>";
                    }

                    html1 =
                        "<div class='mt-5 d-flex justify-content-end fw-bold'>" + "   <div>" +
                        html1 +
                        "	</div>" + "</div>";
                }

                var html2 =
                    "   <div class='suggested-answer-div d-flex justify-content-end mb-8'>" +
                    "       <div>" +
                    "           <div class='text-center img_cont'>" +
                    "	         	<img src='" +
                    ((message.senderRole == "chatbot" ? message.receiverImage : message.senderImage) || '') +
                    "' class='rounded-circle user_img_msg' > " +
                    "           </div>" +
                    "       </div>" +
                    "	    <div class='chatroom_info w-100 text-end'>" +
                    "   	    <div class='d-inline-block msg_cotainer_send text-start'>" +
                    app.localize("YouCanSendFollowingQuestionsToUser") +
                    "               <div class='msg_time_send text-nowrap'>" + htmlStr(timeString) +
                    "               </div>" +
                    "     	    </div>" +
                    "   	    <div class='d-block text-end'>" +
                    html1 +
                    "     	    </div>" +
                    "   	</div>" +
                    "   	<div class='flex-shrink-1 ms-3'>" +
                    "           <div class='img_cont'>" +
                    "               <img class='rounded-circle user_img_msg' src='" + ((message.senderRole == "chatbot" ? message.senderImage : message.receiverImage) || '') + "'> " +
                    "           </div>" +
                    "       </div>" +
                    "   </div>";

                _$nlpPage.find('div.msg_card_body').append(html2);
                _$nlpPage.find('div.msg_card_body').scrollTop($('div.msg_card_body')[0].scrollHeight);

                debugger

                _$nlpPage.find('div.msg_card_body button.btn_to_client').click(function (e) {
                    var text = $(this).parent().find('.btn_to_client')[0].innerText;
                    _$nlpPage.find('#chatbot_msg_text').val(text);
                });

                _$nlpPage.find('div.msg_card_body button.btn_to_chatroom').click(function (e) {
                    var text = $(this).parent().find('.btn_to_client')[0].innerText;
                    _$nlpPage.find('#chatbot_msg_text').val(text);
                    sendMessage();
                });

            } catch (e) {
                console.error(e, e.stack);
            }
        });



        sendMessage = function () {
            clearSuggestedAnswerDiv();
            var message = _$nlpPage.find('#chatbot_msg_text').val().trim();

            if (message.length == 0) {
                return;
            }

            _$nlpPage.find('#chatbot_msg_text').val('');

            app.chatbot.agentSendMessage({
                message: message,
                messageType: "text",
                senderRole: "agent",
                receiverRole: "client",
                chatbotId: _$nlpPage.find('#chatroom-chatbotId').val(),
                clientId: _$nlpPage.find('#chatroom-clientId').val(),
            });
        };


        sendMessageToChatbot = function () {
            clearSuggestedAnswerDiv();
            var message = _$nlpPage.find('#chatbot_msg_text').val().trim();

            if (message.length == 0) {
                return;
            }

            _$nlpPage.find('#chatbot_msg_text').val('');

            //app.chatbot.agentSendMessageToChatbot({
            app.chatbot.agentSendMessage({
                message: message,
                messageType: "text",
                senderRole: "agent",
                receiverRole: "chatbot",
                chatbotId: _$nlpPage.find('#chatroom-chatbotId').val(),
                clientId: _$nlpPage.find('#chatroom-clientId').val(),
            });
        };


        clearSuggestedAnswerDiv = function () {
            //_$nlpPage.find('div.suggested-answer-div').remove();
        }


        $(document).ready(function () {
            console.log("document ready!");

            $('#kt_wrapper').addClass("h-100");
            $('#kt_footer').remove();

            _$nlpPage.find('#nlp_agent_chatroom').removeClass('d-none');

            _$nlpPage.find('abp-page-subheader').addClass("d-none d-xl-block");
            $('#kt_wrapper').find('.d-lg-none').removeClass('d-lg-none').addClass('d-xl-none');

            showHideWindow();

            var top = _$nlpPage.find("#chatroom_h100").position().top;
            var height = window.innerHeight - top - 20;

            if (getViewport() != "md") {
                //_$nlpPage.find(".card").height(height + top / 3);
                _$nlpPage.find(".card").height(height);
            }
            else {
                _$nlpPage.find(".card").height(height);
            }

            _$nlpPage.find(".chat").removeClass("d-none");

            getAllChatrooms();

            _$nlpPage.find('#chatbot_msg_text').keypress(function (e) {
                if (e.which === 13) {
                    e.preventDefault();

                    if (e.shiftKey == true)
                        sendMessageToChatbot();
                    else
                        sendMessage();
                }
            });

            _$nlpPage.find('.send_btn').click(function (e) {
                sendMessage();
            });

            _$nlpPage.find('.send_agent_btn').click(function (e) {
                sendMessageToChatbot();
            });

            //setTimeout(function () {
            //    var top = ($(window).scrollTop() || $("body").scrollTop());
            //}, 1000);

            //_$nlpPage.find('#kt_subheader').addClass("d-none d-xl-block");

            showHideWindow();
        });

        function getViewport() {
            // https://stackoverflow.com/a/8876069
            //const width = Math.max(
            //    document.documentElement.clientWidth,
            //    window.innerWidth || 0
            //)

            const width = _$nlpPage.find('#nlp_agent_chatroom').width();

            if (width <= 576) return 'xs'
            if (width <= 768) return 'sm'
            return 'md';
            //if (width <= 992) return 'md'
            //if (width <= 1200) return 'lg'
            //return 'xl'
        }

        $(window).resize(function () {
            //_$nlpPage.find(".chat").addClass("d-none");

            //var height = _$nlpPage.find("#chatroom_h100").height();
            var top = _$nlpPage.find("#chatroom_h100").position().top;
            var height = window.innerHeight - top - 20;

            if (getViewport() != "md") {
                //_$nlpPage.find(".card").height(height + top / 3);
                _$nlpPage.find(".card").height(height);
            }
            else {
                _$nlpPage.find(".card").height(height);
            }

            _$nlpPage.find(".chat").removeClass("d-none");
            _$nlpPage.find('#kt_wrapper').find('div.d-lg-none').removeClass("d-lg-none");
            showHideWindow();
        });

        function htmlStr(str) {
            return $('<div>').text(str).html();
        }


        var showHideWindow = function () {
            if (getViewport() == "md") {
                _$nlpPage.find(".card").removeClass("d-none");

                //var body = KTUtil.getBody();
                $("body").attr('data-backtolist', 'off');

                _$nlpPage.find("#LeftListPane").addClass("col-6").removeClass('col-12').removeClass('d-none');
                _$nlpPage.find("#RighChatPane").addClass("col-6").removeClass('col-12').removeClass('d-none');
            }
            else {
                if (showLeftListPane == true) {
                    _$nlpPage.find("#LeftListPane").removeClass("d-none");
                    _$nlpPage.find("#RighChatPane").addClass("d-none");

                    //var body = KTUtil.getBody();
                    $("body").attr('data-backtolist', 'off');
                }
                else {
                    _$nlpPage.find("#LeftListPane").addClass("d-none");
                    _$nlpPage.find("#RighChatPane").removeClass("d-none");

                    //var body = KTUtil.getBody();
                    $("body").attr('data-backtolist', 'on');

                    $('#back_to_list').prop("onclick", null).off("click");
                    $('#back_to_list').html("<i class='fa fa-arrow-left'></i>");

                    $('#back_to_list').click(function (e) {
                        //var body = KTUtil.getBody();
                        $("body").attr('data-backtolist', 'off');

                        e.preventDefault();
                        showLeftListPane = true;
                        showHideWindow();
                    });
                }

                _$nlpPage.find("#LeftListPane").removeClass("col-6").addClass('col-12');
                _$nlpPage.find("#RighChatPane").removeClass("col-6").addClass('col-12');
            }
        };
    });
})();
