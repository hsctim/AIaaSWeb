/**
 * WebChat Module
 * Handles the initialization, message sending, and event registration for the chatbot web interface.
 */
(function () {
    let chatbotId, chatbotIcon, RID, alternativeQuestion;

    /**
     * Initializes the webchat interface by setting up the chatbot ID, icon, and session.
     */
    const initialize = function () {
        chatbotId = getUrlParameter("chatbotId");
        chatbotIcon = getUrlParameter("chatbotIcon");

        // Retrieve or generate a unique session ID (RID)
        RID = getCookie("RID") || uuidv4();
        setCookie("RID", RID, 365);

        // Set up the chatbot interface
        $('div.img_cont').empty().append(
            `<img src='${chatbotIcon}' class='rounded-circle user_img'>`
        );

        // Fetch chatbot session info
        $.getJSON(`../Chatbot/SessionInfo/${chatbotId}`, function (data) {
            alternativeQuestion = data.AlternativeQuestion;

            $('div.user_info').empty().append(
                `<span>${data.Name}</span><p>${data.GreetingMsg}</p>`
            );

            $('#chatbot_msg_text').attr("placeholder", data.TypeMessage);
        });

        registerEvents();
        welcomeMsg();
    };

    /**
     * Sends a message to the chatbot.
     */
    const sendMessage = function () {
        const message = $('#chatbot_msg_text').val().trim();

        if (message.length === 0) {
            $("#webchat_close_btn").trigger("click");
            return;
        }

        $('#chatbot_msg_text').val('');

        app.chatbot.sendMessage({
            message,
            clientId: RID,
            senderRole: "client",
            receiverRole: "chatbot",
            chatbotId
        });
    };

    /**
     * Binds UI events such as keypress and button clicks.
     */
    const bindUiEvents = function () {
        $('#chatbot_msg_text').keypress(function (e) {
            if (e.which === 13) {
                e.preventDefault();
                sendMessage();
            }
        });

        $('div.input-group-append').click(function () {
            sendMessage();
        });

        // Close chatbot window
        $('#webchat_close_btn').click(function () {
            const event = new CustomEvent('closeWebChatEvent');
            window.parent.document.dispatchEvent(event);
            app.chatbot.closeConnection();
            window.close();
        });
    };

    /**
     * Registers events for handling incoming messages and reconnection logic.
     */
    const registerEvents = function () {
        const messageReceived = function (message) {
            const senderTime = new Date(message.senderTime);
            let html = "";

            $('div.suggested-question').empty();

            // Avoid duplicate messages
            if ($(".msg_card_body").find(`div[data-id='${message.id || ''}']`).length > 0) {
                return;
            }

            // Add replaceAll polyfill if not available
            if (typeof String.prototype.replaceAll === "undefined") {
                String.prototype.replaceAll = function (match, replace) {
                    return this.replace(new RegExp(match, 'g'), () => replace);
                };
            }

            // Handle messages from the chatbot or agent
            if (message.senderRole === "agent" || message.senderRole === "chatbot") {
                html = `
                    <div class='d-flex justify-content-start mb-4' data-id='${message.id}'>
                        <div class='img_cont_msg'>
                            <img src='${message.senderImage}' class='rounded-circle user_img_msg' alt='${message.senderName}'>
                        </div>
                        <div class='msg_cotainer word-wrap mw-100'>
                            ${message.message.replaceAll("\\\"", '\"')}
                            <span class='msg_time text-nowrap'>
                                ${senderTime.toLocaleTimeString()} | ${senderTime.toLocaleDateString()}
                            </span>
                        </div>
                    </div>`;

                // Add alternative questions if available
                if (message.alternativeQuestion) {
                    const questions = JSON.parse(message.alternativeQuestion);
                    html += `
                        <div class='d-flex justify-content-end suggested-question text-end'>
                            <div>
                                ${questions.length > 0 ? `<div class='align-middle mt-2'>${alternativeQuestion}</div>` : ""}
                                ${questions.map(q => `<button type='button' class='btn btn-outline-primary btn_wrap_left mt-2 mx-1 suggested-button'>${q}</button>`).join('')}
                            </div>
                        </div>`;
                }

                $('div.card-body').append(html);

                // Handle suggested question button clicks
                if (message.alternativeQuestion) {
                    $('button.suggested-button').click(function (e) {
                        $('#chatbot_msg_text').val(e.target.innerText);
                        $('div.suggested-question').empty();
                        sendMessage();
                    });
                }
            } else {
                // Handle messages from the client
                html = `
                    <div class='d-flex justify-content-end mb-4' data-id='${message.id}'>
                        <div class='msg_cotainer_send word-wrap mw-100'>
                            ${message.message.replaceAll("\\\"", '\"')}
                            <span class='msg_time_send text-nowrap'>
                                ${senderTime.toLocaleTimeString()} | ${senderTime.toLocaleDateString()}
                            </span>
                        </div>
                    </div>`;

                $('div.card-body').append(html);
            }

            // Scroll to the latest message
            $('div.card-body').scrollTop($('div.card-body')[0].scrollHeight);
        };

        // Event handlers for incoming messages
        $('#webchat-container').on('chatbot.messagesReceived', function (event, messages) {
            if (messages && messages.messages) {
                messages.messages.forEach(message => messageReceived(message));
                if (messages.messages.length > 0) {
                    app.chatbot.sendReceipt(messages.messages[0]);
                }
            }
        });

        $('#webchat-container').on('chatbot.messageReceived', function (event, message) {
            if (message) {
                messageReceived(message);
                app.chatbot.sendReceipt(message);
            }
        });

        // Handle reconnection
        $('#webchat-container').on('chatroom.reconnect', function () {
            app.chatbot.clientReconnect({
                chatbotId,
                senderRole: "client",
                clientId: RID
            });
        });

        bindUiEvents();
    };

    /**
     * Sends a welcome message and requests history and greeting messages.
     */
    const welcomeMsg = function () {
        if (!app.chatbot.isConnected()) {
            setTimeout(welcomeMsg, 1000); // Retry after 1 second
            return;
        }

        app.chatbot.requestHistoryMessages({
            clientId: RID,
            senderRole: "client",
            chatbotId
        });

        app.chatbot.requestGreetingMessage({
            clientId: RID,
            senderRole: "client",
            chatbotId
        });
    };

    /**
     * Retrieves a URL parameter by name.
     * @param {string} sParam - The parameter name.
     * @returns {string|null} - The parameter value or null if not found.
     */
    const getUrlParameter = function (sParam) {
        const sPageURL = window.location.search.substring(1);
        const sURLVariables = sPageURL.split('&');

        for (let i = 0; i < sURLVariables.length; i++) {
            const sParameterName = sURLVariables[i].split('=');
            if (sParameterName[0] === sParam) {
                return sParameterName[1] ? decodeURIComponent(sParameterName[1]) : null;
            }
        }
        return null;
    };

    /**
     * Sets a cookie with the specified name, value, and expiration days.
     * @param {string} name - The cookie name.
     * @param {string} value - The cookie value.
     * @param {number} days - The number of days until the cookie expires.
     */
    const setCookie = function (name, value, days) {
        const date = new Date();
        date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
        document.cookie = `${name}=${value || ""}; expires=${date.toUTCString()}; path=/; SameSite=Lax`;
    };

    /**
     * Retrieves a cookie value by name.
     * @param {string} name - The cookie name.
     * @returns {string|null} - The cookie value or null if not found.
     */
    const getCookie = function (name) {
        const nameEQ = `${name}=`;
        const ca = document.cookie.split(';');
        for (let i = 0; i < ca.length; i++) {
            let c = ca[i].trim();
            if (c.indexOf(nameEQ) === 0) return c.substring(nameEQ.length);
        }
        return null;
    };

    /**
     * Generates a UUID (v4).
     * @returns {string} - A randomly generated UUID.
     */
    const uuidv4 = function () {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            const r = Math.random() * 16 | 0;
            const v = c === 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    };

    // Initialize the webchat on document ready
    $(document).ready(function () {
        initialize();
        $('div.container-fluid').removeClass('d-none');
    });
})();

