(function () {

    var chatbotId,
        chatbotIcon,
        RID,                    //Random ID
        RTOKEN,                 //Random Token
        alternativeQuestion;

    var _messageReceived;
    


    var initialize = function () {
        //debugger;

        chatbotId = getUrlParameter("chatbotId");
        chatbotIcon = getUrlParameter("chatbotIcon");
        questionParam = getUrlParameter("question");

        RID = getCookie("RID");
        if (!RID) {
            RID = uuidv4();
        }

        RTOKEN = getCookie("RTOKEN")
        if (!RTOKEN) {
            RTOKEN = randomPwd();
        }

        setCookie("RID", RID, 365);
        setCookie("RTOKEN", RTOKEN, 365);

        if (!chatbotIcon) {
            chatbotIcon = "/Chatbot/ProfilePicture/" + chatbotId;
        }

        //initial webchat interface
        $('div.img_cont').empty().append(
            "<img src='" + chatbotIcon + "' class='rounded-circle user_img'>"
        );

        //nitial height and width
        //var vw = Math.max(document.documentElement.clientWidth || 0, window.innerWidth || 0);

        //$('#webchat-container').height()

        $(document).ready(function () {
            $(window).resize(function () {
                ResizeChatWindow();
            }).on("orientationchange", function () {
                ResizeChatWindow();;

                setTimeout(function () {
                    ResizeChatWindow();
                }, 1000);
            });

            ResizeChatWindow();
        });

        $.getJSON("../Chatbot/SessionInfo/" + chatbotId, function (data) {

            if (!data) {
                console.log("The WebChat function is disabled.");

                $('div#webchat-container').empty();
                return;
            }

            registerEvents();
            welcomeMsg();

            alternativeQuestion = data.AlternativeQuestion;

            $('div.user_info').empty().append(
                "<span>" + htmlStr(data.Name) + "</span>" +
                "<p>" + htmlStr(data.GreetingMsg) + "</p>"
            );

            $('#chatbot_msg_text').attr("placeholder", data.TypeMessage);
        });
    };

    var isToday = function (someDate) {
        var today = new Date()
        return someDate.getDate() == today.getDate() &&
            someDate.getMonth() == today.getMonth() &&
            someDate.getFullYear() == today.getFullYear()
    }

    //Messages
    sendMessage = function (question) {
        $('div.suggested-question').empty();

        var message = $('#chatbot_msg_text').val().trim();

        if (question)
            message = message+question;

        if (message.length == 0) {
            $("#webchat_close_btn").trigger("click");
            return;
        }

        $('#chatbot_msg_text').val('');

        //立即顯示傳送至Chatbot的Message
        _messageReceived({
            "id": "00000000-0000-0000-0000-000000000000",
            "message": message,
            "messageType": "text",
            "senderRole": "client",
            "senderTime": new Date(),
        });

        app.chatbot.sendMessage({
            message: message,
            chatbotId: chatbotId,
            clientId: RID,
            clientToken: RTOKEN
        });
    };

    //Events & UI   

    bindUiEvents = function () {
        $('#chatbot_msg_text').keypress(function (e) {
            if (e.which === 13) {
                e.preventDefault();
                sendMessage();
            }
        });

        $('div.input-group-append').click(function (e) {
            sendMessage();
        });

        //close chatbot Window
        $('#webchat_close_btn').click(function () {
            var event = new CustomEvent('closeWebChatEvent');
            window.parent.document.dispatchEvent(event);
            app.chatbot.closeConnection();
            $('div.input-group').empty();
            $('.fa-window-close').empty();
            $('.suggested-question').empty();
            window.close();
        });
    }

    var registerEvents = function () {

        var messageReceived = function (message) {
            var senderTime = new Date(message.senderTime);
            var html = "";

            $('div.gpt-inference').empty();

            if (message.id != '00000000-0000-0000-0000-000000000000' && $(".msg_card_body").find("div[data-id='" + (message.id || '') + "']").length > 0) {
                return;
            }

            var timeString = null;
            if (isToday(senderTime))
                timeString = senderTime.toLocaleTimeString();
            else
                timeString = senderTime.toLocaleTimeString() + " | " + senderTime.toLocaleDateString();

            if (message.senderRole == "chatbot" && chatbotIcon) {
                message.senderImage = chatbotIcon;
            }

            if (typeof String.prototype.replaceAll == "undefined") {
                String.prototype.replaceAll = function (match, replace) {
                    return this.replace(new RegExp(match, 'g'), () => replace);
                }
            }

            if (message.senderRole == "agent" || message.senderRole == "chatbot") {

                message.message = message.message.replaceAll("\n", "<br>");

                html =
                    "<div class='d-flex justify-content-start mb-4' data-id='" + message.id + "'>" +
                    "   <div class='img_cont_msg'>" +
                    "       <img src='" + message.senderImage + "' class='rounded-circle user_img_msg'" +
                    "       alt='" + htmlStr(message.senderName) + "'>" +
                    "	</div>" +
                    "   <div class='msg_cotainer text-wrap mw-100'>" +
                    (message.message || '').replaceAll("\\\"", '\"') +
                    "       <span class='msg_time text-nowrap'>" + htmlStr(timeString) + "</span>" +
                    "	</div>" +
                    "</div>";

                if (message.alternativeQuestion) {
                    var questions = JSON.parse(message.alternativeQuestion)
                    html +=
                        "<div class='d-flex justify-content-end suggested-question text-end'>" +
                        "   <div>";

                    if (questions.length > 0) {
                        html += "<div class='align-middle mt-2'>" + htmlStr(alternativeQuestion) + "</div>";
                    }

                    for (var i = 0; i < questions.length; i++) {
                        html += "<button type='button' class='btn btn-outline-primary btn_wrap_left mt-2 mx-1 suggested-button'>" +
                            htmlStr(questions[i]) + "</button>";
                    }
                    html += "	</div>";
                }

                $('div.card-body').append(html);
                if (message.alternativeQuestion) {
                    $('button.suggested-button').click(function (e) {
                        //debugger;
                        $('#chatbot_msg_text').val(e.target.innerText);
                        $('div.suggested-question').empty();
                        sendMessage();
                    });
                }
            }
            else {
                html =
                    "<div class='d-flex justify-content-end mb-4' data-id='" + message.id + "'>" +
                    "   <div class='msg_cotainer_send text-wrap mw-100'>" +
                    message.message.replaceAll("\\\"", '\"') +
                    "       <span class='msg_time_send text-nowrap'>" + htmlStr(timeString) + "</span>" +
                    "   </div>" +
                    "</div>" +

                "<div class='d-flex justify-content-start mb-4 gpt-inference d-none'>" +
                    "   <div class='img_cont_msg'>" +
                    "       <img src='" + chatbotIcon + "' class='rounded-circle user_img_msg'>" +

                    "	</div>" +
                    "   <div class='msg_cotainer word-wrap mw-100 gpt-inference-text d-none'>" +                    
                    "	</div>" +
                    "</div>";

                $('div.card-body').append(html);
            }


            $('div.card-body').scrollTop($('div.card-body')[0].scrollHeight);
        };

        _messageReceived = messageReceived;

        $('#webchat-container').on('chatbot.messagesReceived', function (event, messages) {
            try {
                $('div.suggested-question').empty();

                $(".msg_card_body").find("div[data-id='00000000-0000-0000-0000-000000000000']").remove();

                if (messages && messages.messages) {
                    for (i = 0; i < messages.messages.length; i++) {
                        messageReceived(messages.messages[i]);
                    }

                    if (messages.messages.length > 0)
                        app.chatbot.sendReceipt(messages.messages[0]);
                }
            } catch (e) {
                console.error(e, e.stack);
            }
        });


        $('#webchat-container').on('chatroom.reconnect', function (event) {
            try {
                console.log("$('#webchat-container').on('chatroom.reconnect', function (event)");
                app.chatbot.clientReconnect({
                    chatbotId: chatbotId,
                    clientId: RID
                });
            } catch (e) {
                console.error(e, e.stack);
            }
        });

        bindUiEvents();
        //_chatbotBar.bindUiEvents();
    };


    welcomeMsg = function () {
        if (app.chatbot.isConnected() == false) {
            setTimeout(welcomeMsg, 1000); // check again in a second
            return;
        }

        app.chatbot.requestHistoryMessages({
            chatbotId: chatbotId,
            clientId: RID,
            clientToken: RTOKEN
        });

        app.chatbot.requestGreetingMessage({
            clientId: RID,
            chatbotId: chatbotId,
        });

        ResizeChatWindow();
    };


    var getUrlParameter = function getUrlParameter(sParam) {
        var sPageURL = window.location.search.substring(1),
            sURLVariables = sPageURL.split('&'),
            sParameterName,
            i;

        for (i = 0; i < sURLVariables.length; i++) {
            sParameterName = sURLVariables[i].split('=');

            if (sParameterName[0] === sParam) {
                return typeof sParameterName[1] === undefined ? true : decodeURIComponent(sParameterName[1]);
            }
        }
        return null;
    };


    function setCookie(name, value, days) {
        var expires = "";
        if (days) {
            var date = new Date();
            date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
            expires = "; expires=" + date.toUTCString();
        }
        document.cookie = name + "=" + (value || "") + expires + "; path=/; SameSite=Lax";
    }

    function getCookie(name) {
        var nameEQ = name + "=";
        var ca = document.cookie.split(';');
        for (var i = 0; i < ca.length; i++) {
            var c = ca[i];
            while (c.charAt(0) == ' ') c = c.substring(1, c.length);
            if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length, c.length);
        }
        return null;
    }

    function uuidv4() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }

    function randomPwd() {
        var chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        var string_length = 32;
        var randomstring = '';
        for (var i = 0; i < string_length; i++) {
            var rnum = Math.floor(Math.random() * chars.length);
            randomstring += chars.substring(rnum, rnum + 1);
        }
        return randomstring;
    }

    function htmlStr(str) {
        return $('<div>').text(str).html();
    }

    function ResizeChatWindow() {
        var height = $('#webchat-container').height();
        var width = $('#webchat-container').width();
        $('div#webchat-body').height(height-1);
    }

    SendParamMessage = function () {
        if (questionParam) {
            if ($('.msg_cotainer').length == 0) {
                setTimeout(SendParamMessage, 1000); // check again in a second
                return;
            }

            sendMessage(questionParam);
        }        
    }


    $(function () {
        initialize();
        $('div.container-fluid').removeClass('d-none');

        if (window.document.getElementById("webchat-body").offsetHeight > window.innerHeight) {
            window.document.getElementById("webchat-body").style.height = (window.innerHeight - 20) + "px";
        }

        setInterval(function () {            
            var gptInferenceText = $('div.gpt-inference-text').last().text();
            $('div.gpt-inference-text').text(gptInferenceText + '. ');

            $('div.gpt-inference').removeClass('d-none');
            $('div.gpt-inference-text').removeClass('d-none');

        }, 2000);

        SendParamMessage();
    });

})();
