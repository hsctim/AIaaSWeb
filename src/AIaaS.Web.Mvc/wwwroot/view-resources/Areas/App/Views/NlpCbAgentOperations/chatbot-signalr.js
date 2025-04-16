/**
 * Chatbot SignalR Module
 * Handles SignalR connections and interactions for chatbot operations.
 */
var app = app || {};
app.chatbot = app.chatbot || {};

(function () {
    // Ensure SignalR is available before proceeding
    if (!signalR) {
        console.error("SignalR is not defined. Exiting chatbot initialization.");
        return;
    }

    // Initialize namespaces
    app.signalr = app.signalr || {};
    app.signalr.hubs = app.signalr.hubs || {};

    let chatbotHub = null;

    /**
     * Configures the SignalR connection, including reconnection logic and event registration.
     * @param {HubConnection} connection - The SignalR connection instance.
     */
    function configureConnection(connection) {
        app.signalr.hubs.chatbot = connection;
        chatbotHub = connection;

        let reconnectTime = 5000;
        let tries = 1;
        const maxTries = 8;

        /**
         * Attempts to reconnect to the SignalR server with exponential backoff.
         */
        function tryReconnect() {
            if (tries > maxTries) {
                console.warn("Max reconnection attempts reached. Stopping reconnection.");
                return;
            }

            connection.start()
                .then(() => {
                    reconnectTime = 5000;
                    tries = 1;
                    console.log("Reconnected to SignalR chatbot server!");
                    $('div[name="NlpCbAgentOperationsPage"]').find('#nlp_agent_chatroom').trigger('chatroom.reconnect');
                })
                .catch(() => {
                    tries++;
                    reconnectTime *= 2;
                    setTimeout(tryReconnect, reconnectTime);
                });
        }

        // Handle disconnection and attempt reconnection if autoConnect is enabled
        connection.onclose((e) => {
            console[e ? "error" : "log"](`Chatbot connection closed: ${e || "No error"}`);
            if (app.signalr.autoConnect) {
                tryReconnect();
            }
        });

        // Register event handlers for chatbot notifications
        registerChatbotEvents(connection);
    }

    /**
     * Establishes a SignalR connection with transport fallback.
     * @param {string} url - The SignalR hub URL.
     * @param {Function} configureConnection - Callback to configure the connection.
     * @returns {Promise<HubConnection>} - A promise resolving to the SignalR connection.
     */
    function startConnection(url, configureConnection) {
        if (app.signalr.remoteServiceBaseUrl) {
            url = app.signalr.remoteServiceBaseUrl + url;
        }

        if (app.signalr.qs) {
            url += `?${app.signalr.qs}`;
        }

        /**
         * Attempts to start the connection using the specified transport.
         * @param {number} transport - The SignalR transport type.
         * @returns {Promise<HubConnection>} - A promise resolving to the SignalR connection.
         */
        function start(transport) {
            console.log(`Starting connection using ${signalR.HttpTransportType[transport]} transport`);

            const connection = new signalR.HubConnectionBuilder()
                .withUrl(url, transport)
                .build();

            if (configureConnection) {
                configureConnection(connection);
            }

            return connection.start()
                .then(() => connection)
                .catch((error) => {
                    console.error(`Failed to start connection with ${signalR.HttpTransportType[transport]} transport: ${error.message}`);
                    if (transport !== signalR.HttpTransportType.LongPolling) {
                        return start(transport + 1);
                    }
                    return Promise.reject(error);
                });
        }

        return start(signalR.HttpTransportType.WebSockets);
    }

    /**
     * Registers SignalR event handlers for chatbot-related events.
     * @param {HubConnection} connection - The SignalR connection instance.
     */
    function registerChatbotEvents(connection) {
        const pageSelector = 'div[name="NlpCbAgentOperationsPage"]';
        const chatroomSelector = '#nlp_agent_chatroom';

        connection.on('agentGetChatbotMessage', (message) => {
            $(pageSelector).find(chatroomSelector).trigger('chatroom.agentMessageReceived', message);
        });

        connection.on('agentGetChatbotMessages', (messages) => {
            $(pageSelector).find(chatroomSelector).trigger('chatroom.agentMessagesReceived', { messages });
        });

        connection.on('updateChatroomStatus', (status) => {
            try {
                $(pageSelector).find(chatroomSelector).trigger('chatroom.updateChatroomStatus', status);
            } catch (e) {
                console.error("Error handling updateChatroomStatus event:", e);
            }
        });

        connection.on('suggestedAnswers', (status) => {
            try {
                $(pageSelector).find(chatroomSelector).trigger('chatroom.suggestedAnswers', status);
            } catch (e) {
                console.error("Error handling suggestedAnswers event:", e);
            }
        });
    }

    /**
     * Sends a message from the agent to the chatbot.
     * @param {Object} messageData - The message data to send.
     * @param {Function} callback - Callback to execute after the operation.
     */
    app.chatbot.agentSendMessage = function (messageData, callback) {
        if (!isConnected()) {
            callback?.();
            return;
        }

        chatbotHub.invoke('agentSendMessage', messageData).then(callback);
    };

    /**
     * Checks if the SignalR connection is active.
     * @returns {boolean} - True if connected, false otherwise.
     */
    function isConnected() {
        return chatbotHub?.connection?.connectionState === signalR.HubConnectionState.Connected;
    }

    // Expose connection methods
    app.signalr.connect = function () {
        startConnection('/signalr-chatbot', configureConnection)
            .then((connection) => {
                console.log("Connected to SignalR chatbot server!");
                connection.invoke('register').then(() => {
                    console.log("Registered to the SignalR chatbot server!");
                });
            })
            .catch((error) => {
                console.error("Error connecting to SignalR chatbot server:", error.message);
            });
    };

    app.signalr.startConnection = startConnection;

    // Auto-connect if enabled
    if (app.signalr.autoConnect === undefined) {
        app.signalr.autoConnect = true;
    }

    if (app.signalr.autoConnect) {
        app.signalr.connect();
    }
})();
