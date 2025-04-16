/**
 * NlpCbAgentOperations Page Script
 * Handles chatroom operations, message handling, and UI interactions for NLP chatbot agents.
 */
(function () {
    $(function () {
        const _nlpService = abp.services.app.nlpCbAgentOperations;
        const _$nlpPage = $('div[name="NlpCbAgentOperationsPage"]');
        const _permissions = {
            send: abp.auth.hasPermission('Pages.NlpChatbot.NlpCbAgentOperations.SendMessage')
        };

        let showLeftListPane = true;

        /**
         * Checks if a given date is today.
         * @param {Date} someDate - The date to check.
         * @returns {boolean} - True if the date is today, false otherwise.
         */
        const isToday = (someDate) => {
            const today = new Date();
            return someDate.getDate() === today.getDate() &&
                someDate.getMonth() === today.getMonth() &&
                someDate.getFullYear() === today.getFullYear();
        };

        /**
         * Checks if a given time is newer than the latest time in the chatroom.
         * @param {string} inputTimeString - The input time as a string.
         * @returns {boolean} - True if the input time is newer, false otherwise.
         */
        const isNew = (inputTimeString) => {
            const latestTime = new Date(_$nlpPage.find('#chatroom-latestTime').val());
            const inputTime = new Date(inputTimeString);

            if (latestTime > inputTime) {
                return false;
            } else {
                _$nlpPage.find('#chatroom-latestTime').val(inputTimeString);
                return true;
            }
        };

        /**
         * Generates the HTML for a chatroom list item.
         * @param {Object} data - The chatroom data.
         * @param {string} css1 - Additional CSS classes.
         * @returns {string} - The generated HTML string.
         */
        const getChatroomHtml = (data, css1) => {
            const unreadMessageCount = data.unreadMessageCount > 0 ? `${data.unreadMessageCount}` : "";
            const latestTime = new Date(data.latestMessageTime);
            const timeString = isToday(latestTime) ? latestTime.toLocaleTimeString() : latestTime.toLocaleTimeString();
            const dateString = isToday(latestTime) ? "" : latestTime.toLocaleDateString();

            let msgHtml = "";
            if (data.latestMessages) {
                data.latestMessages.forEach((message) => {
                    if (!message.isClientSent) {
                        msgHtml += `
                            <div class='pt-1 text-end'>
                                <div class='d-inline-block text-wrap text-start msg_cotainer_send'>
                                    ${htmlStr(message.message)}
                                </div>
                            </div>`;
                    } else {
                        msgHtml += `
                            <div class='pt-1'>
                                <div class='d-inline-block text-wrap text-start msg_cotainer'>
                                    ${htmlStr(message.message)}
                                </div>
                            </div>`;
                    }
                });
            }

            msgHtml = `<div class='div-2msg'>${msgHtml}</div>`;

            let agentHtml = "";
            if (data.chatroomAgents) {
                data.chatroomAgents.forEach((agent) => {
                    const isCurrentUser = agent.agentId === abp.session.userId;
                    agentHtml += `
                        <div class='text-center img_cont div-agent ms-2'>
                            <img src='/Chatbot/GetProfilePictureById/${agent.agentPictureId || ''}' class='rounded-circle user_img'>
                            <div class='user_name'>${htmlStr(agent.agentName || '')}</div>
                        </div>`;
                });
            }

            const clientInfo = data.clientName
                ? `<div class='text-wrap chatroom_font12'>${data.clientName}</div>`
                : data.clientId
                    ? `<div class='text-wrap text-break chatroom_font10'>${data.clientId}</div>`
                    : "";

            return `
                <li class='chatbot-client-li' data-clientid='${data.clientId}' data-chatbotid='${data.chatbotId}'>
                    <div class='d-flex w-100'>
                        <div>
                            <div class='text-center img_cont'>
                                <img src='${data.clientPicture}' class='rounded-circle user_img'>
                                <span class='${data.clientConnected ? "online_icon" : "offline_icon"}'>
                                    <div class='d-flex justify-content-center align-items-center h-100 text-white'>${unreadMessageCount}</div>
                                </span>
                            </div>
                        </div>
                        <div class='chatroom_info w-100'>
                            <div class='d-flex justify-content-between'>
                                <div>
                                    ${clientInfo}
                                    ${data.clientChannel ? `<div class='text-nowrap chatroom_font10'>${data.clientChannel}</div>` : ""}
                                    ${data.clientIP ? `<div class='text-wrap chatroom_font10'>${data.clientIP}</div>` : ""}
                                </div>
                                <div class='text-wrap text-end mt-auto'>
                                    ${dateString ? `<div class='text-center chatroom_font10'>${htmlStr(dateString)}</div>` : ""}
                                    <div class='text-center chatroom_font10'>${htmlStr(timeString)}</div>
                                </div>
                            </div>
                            ${msgHtml}
                        </div>
                        <div class='${css1}'>
                            ${agentHtml}
                            <div class='flex-shrink-1 ms-3'>
                                <div class='text-center img_cont'>
                                    <img src='/Chatbot/ProfilePicture/${data.chatbotPictureId || ""}' class='rounded-circle user_img'>
                                    ${data.responseConfirmEnabled === false ? "" : `
                                        <span class='eye_icon text-center align-middle'>
                                            <i class='far fa-eye text-white fa-xs'></i>
                                        </span>`}
                                </div>
                                <div class='user_name'>${htmlStr(data.chatbotName || '')}</div>
                            </div>
                        </div>
                        <div class='chatroom_menu_botton'></div>
                    </div>
                </li>`;
        };

        /**
         * Sets up click events for chatroom list items.
         */
        const setChatroomEvent = () => {
            _$nlpPage.find('.chatbot-client-li').click(function () {
                if (!(_$nlpPage.find('.chatbot-client-li.activeli').length > 0 &&
                    _$nlpPage.find('.chatbot-client-li.activeli')[0] === $(this)[0])) {
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

                showLeftListPane = false;
                showHideWindow();
            });
        };

        /**
         * Fetches all chatrooms and updates the UI.
         */
        const getAllChatrooms = () => {
            _nlpService.getAllChatrooms(_$nlpPage.find('#ChatbotSelectFilter').val())
                .done((data) => {
                    _$nlpPage.find('.contacts').empty();

                    data.forEach((chatroom) => {
                        const $html = $(getChatroomHtml(chatroom, ""));
                        const count = $html.find('.div-agent').length;

                        for (let j = 1; j < count; j++) {
                            $html.find('.div-agent').eq(1).remove();
                        }

                        if (isNew(chatroom.latestMessageTime)) {
                            _$nlpPage.find('.contacts').prepend($html[0]);
                        } else {
                            _$nlpPage.find('.contacts').append($html[0]);
                        }
                    });

                    setChatroomEvent();
                });
        };

        /**
         * Escapes HTML special characters in a string.
         * @param {string} str - The input string.
         * @returns {string} - The escaped string.
         */
        const htmlStr = (str) => $('<div>').text(str).html();

        /**
         * Toggles the visibility of the left and right panes based on the viewport size.
         */
        const showHideWindow = () => {
            const viewport = getViewport();

            if (viewport === "md") {
                _$nlpPage.find("#LeftListPane").addClass("col-6").removeClass('col-12 d-none');
                _$nlpPage.find("#RighChatPane").addClass("col-6").removeClass('col-12 d-none');
            } else if (showLeftListPane) {
                _$nlpPage.find("#LeftListPane").removeClass("d-none");
                _$nlpPage.find("#RighChatPane").addClass("d-none");
            } else {
                _$nlpPage.find("#LeftListPane").addClass("d-none");
                _$nlpPage.find("#RighChatPane").removeClass("d-none");
            }
        };

        /**
         * Determines the current viewport size.
         * @returns {string} - The viewport size ('xs', 'sm', or 'md').
         */
        const getViewport = () => {
            const width = _$nlpPage.find('#nlp_agent_chatroom').width();
            if (width <= 576) return 'xs';
            if (width <= 768) return 'sm';
            return 'md';
        };

        // Initialize the page
        $(document).ready(() => {
            console.log("Document ready!");
            getAllChatrooms();
        });
    });
})();