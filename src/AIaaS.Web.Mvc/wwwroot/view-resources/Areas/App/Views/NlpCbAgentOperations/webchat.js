(function () {

    var chatbotId,
        chatbotIcon,
        RID,
        alternativeQuestion;

    var initialize = function () {
        //debugger;

        chatbotId = getUrlParameter("chatbotId");
        chatbotIcon = getUrlParameter("chatbotIcon");

        RID = getCookie("RID");
        if (!RID) {
            RID = uuidv4();
        }

        setCookie("RID", RID, 365);

        //initial webchat interface
        $('div.img_cont').empty().append(
            "<img src='" + chatbotIcon + "' class='rounded-circle user_img'>"
        );


        $.getJSON("../Chatbot/SessionInfo/" + chatbotId, function (data) {
            alternativeQuestion = data.AlternativeQuestion;

            $('div.user_info').empty().append(
                "<span>" + data.Name + "</span>" +
                "<p>" + data.GreetingMsg + "</p>"
            );

            $('#chatbot_msg_text').attr("placeholder", data.TypeMessage);
        });

        registerEvents();
        welcomeMsg();
    };

    //Messages
    sendMessage = function () {
        var message = $('#chatbot_msg_text').val().trim();

        if (message.length == 0) {
            $("#webchat_close_btn").trigger("click");
            return;
        }

        $('#chatbot_msg_text').val('');

        app.chatbot.sendMessage({
            message: message,
            clientId: RID,
            senderRole: "client",
            receiverRole: "chatbot",
            chatbotId: chatbotId,
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
            //$('div.input-group').addClass("d-none");
            //$('.fa-window-close').addClass("d-none");
            window.close();
        });
    }

    var registerEvents = function () {

        var messageReceived = function (message) {
            var senderTime = new Date(message.senderTime);
            var html = "";

            $('div.suggested-question').empty();

            $('.msg_card_body').find('')

            if ($(".msg_card_body").find("div[data-id='" + (message.id || '') + "']").length > 0) {
                return;
            }

            if (typeof String.prototype.replaceAll == "undefined") {
                String.prototype.replaceAll = function (match, replace) {
                    return this.replace(new RegExp(match, 'g'), () => replace);
                }
            }

            if (message.senderRole == "agent" || message.senderRole == "chatbot") {
                html =
                    "<div class='d-flex justify-content-start mb-4' data-id='" + message.id + "'>" +
                    "   <div class='img_cont_msg'>" +
                    "       <img src='" + message.senderImage + "' class='rounded-circle user_img_msg'" +
                    "       alt='" + message.senderName + "'>" +
                    "	</div>" +
                    "   <div class='msg_cotainer word-wrap mw-100'>" +
                    message.message.replaceAll("\\\"", '\"') +
                    "       <span class='msg_time text-nowrap'>" +
                    senderTime.toLocaleTimeString() + " | " + senderTime.toLocaleDateString() +
                    "       </span>" +
                    "	</div>" +
                    "</div>";

                if (message.alternativeQuestion) {
                    var questions = JSON.parse(message.alternativeQuestion)
                    html +=
                        "<div class='d-flex justify-content-end suggested-question text-end'>" +
                        "   <div>";

                    if (questions.length > 0) {
                        html += "<div class='align-middle mt-2'>" + alternativeQuestion + "</div>";
                    }

                    for (var i = 0; i < questions.length; i++) {
                        html += "<button type='button' class='btn btn-outline-primary btn_wrap_left mt-2 mx-1 suggested-button'>" +
                            questions[i] + "</button>";
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
                    "   <div class='msg_cotainer_send word-wrap mw-100'>" +
                    message.message.replaceAll("\\\"", '\"') +
                    "       <span class='msg_time_send text-nowrap'>" +
                    senderTime.toLocaleTimeString() + " | " + senderTime.toLocaleDateString() +
                    "       </span>" +
                    "   </div>" +
                    "</div>";

                $('div.card-body').append(html);
            }


            $('div.card-body').scrollTop($('div.card-body')[0].scrollHeight);
        };

        $('#webchat-container').on('chatbot.messagesReceived', function (event, messages) {
            if (messages && messages.messages) {
                for (i = 0; i < messages.messages.length; i++) {
                    messageReceived(messages.messages[i]);
                }

                if (messages.messages.length > 0)
                    app.chatbot.sendReceipt(messages.messages[0]);
            }
        });

        $('#webchat-container').on('chatbot.messageReceived', function (event, message) {
            if (message) {
                messageReceived(message);
                app.chatbot.sendReceipt(message);
            }
        });

        debugger
        $('#webchat-container').on('chatroom.reconnect', function (event) {
            debugger
            console.log("$('#webchat-container').on('chatroom.reconnect', function (event)");
            app.chatbot.clientReconnect({
                chatbotId: chatbotId,
                senderRole: "client",
                clientId: RID
            });
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
            clientId: RID,
            senderRole: "client",
            chatbotId: chatbotId,
        });

        app.chatbot.requestGreetingMessage({
            clientId: RID,
            senderRole: "client",
            chatbotId: chatbotId,
        });
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

    $('document').ready(function () {
        initialize();
        $('div.container-fluid').removeClass('d-none');
    });
})();
