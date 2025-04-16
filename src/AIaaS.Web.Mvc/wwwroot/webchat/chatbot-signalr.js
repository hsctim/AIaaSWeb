/*******************************************************
 * chatbot-signalr.js
 * 
 * This script handles SignalR hub connections and event
 * handlers for chat-related functionality. It allows
 * sending and receiving chatbot messages, managing   
 * connection states, and re-establishing broken links.
 *******************************************************/
var app = app || {};
app.chatbot = app.chatbot || {};

(function () {

    // Abort if SignalR is not loaded
    if (!signalR) {
        return;
    }

    app.signalr = app.signalr || {};
    app.signalr.hubs = app.signalr.hubs || {};

    let chatbotHub = null;

    /**
     * Configures the SignalR connection, including logic
     * for reconnecting on unexpected disconnect events.
     */
    function configureConnection(connection) {
        // Expose chatbot hub globally
        app.signalr.hubs.chatbot = connection;
        chatbotHub = connection;

        let reconnectTime = 5000;
        let tries = 1;
        const maxTries = 8;

        /**
         * Attempts to reconnect with exponential backoff.
         */
        function tryReconnect() {
            if (tries > maxTries) {
                return;
            }
            connection.start()
                .then(function () {
                    reconnectTime = 5000;
                    tries = 1;
                    console.log("Reconnected to SignalR chatbot server!");
                    // Fire a custom event indicating reconnection
                    $('#webchat-container').trigger('chatroom.reconnect');
                })
                .catch(function () {
                    tries += 1;
                    reconnectTime *= 2;
                    setTimeout(tryReconnect, reconnectTime);
                });
        }

        // Ensure the hub tries to reconnect on a close event
        connection.onclose(function (error) {
            if (error) {
                console.log("Chatbot connection closed with error:", error);
            } else {
                console.log("Chatbot hub disconnected.");
            }

            if (!app.signalr.autoConnect) {
                return;
            }
            tryReconnect();
        });

        // Register event handlers for incoming chatbot data
        registerChatbotEvents(connection);
    }

    /**
     * Initiates a connection to the chatbot hub and invokes "register".
     */
    app.signalr.connect = function () {
        startConnection('/signalr-chatbot', configureConnection)
            .then(function (connection) {
                console.log("Connected to SignalR chatbot server!");
                // Invoke server method for registration
                connection.invoke("register").then(function () {
                    console.log("Registered with SignalR chatbot server!");
                });
            })
            .catch(function (error) {
                console.log(error.message);
            });
    };

    /**
     * Attempts to start a SignalR connection with fallback 
     * among potential transports (WebSockets, SSE, longPolling).
     */
    function startConnection(url, configure) {
        if (app.signalr.remoteServiceBaseUrl) {
            url = app.signalr.remoteServiceBaseUrl + url;
        }
        // Append query string if provided
        if (app.signalr.qs) {
            url += "?" + app.signalr.qs;
        }

        /**
         * Recursively tries transports until success or fallback exhaustion.
         */
        return function start(transportType) {
            console.log("Starting connection with", signalR.HttpTransportType[transportType], "transport.");

            const connection = new signalR.HubConnectionBuilder()
                .withUrl(url, transportType)
                .build();

            if (configure && typeof configure === "function") {
                configure(connection);
            }

            return connection.start()
                .then(function () {
                    return connection;
                })
                .catch(function (error) {
                    console.log("Cannot start with", signalR.HttpTransportType[transportType], "transport:", error.message);

                    // Try next transport if current failed and not at the last fallback
                    if (transportType !== signalR.HttpTransportType.LongPolling) {
                        return start(transportType + 1);
                    }
                    return Promise.reject(error);
                });
        }(signalR.HttpTransportType.WebSockets);
    }

    // Expose startConnection if needed elsewhere
    app.signalr.startConnection = startConnection;

    // Default to autoConnect if not set
    if (app.signalr.autoConnect === undefined) {
        app.signalr.autoConnect = true;
    }

    // Initiate connection on page load if autoConnect is true
    if (app.signalr.autoConnect) {
        app.signalr.connect();
    }

    /**
     * Binds events we receive from the server to local triggers.
     */
    function registerChatbotEvents(connection) {
        connection.on("receiveMessages", function (messages) {
            $("#webchat-container").trigger("chatbot.messagesReceived", { messages: messages });
        });
    }

    /**
     * Sends a chat message to the server using the hub.
     */
    app.chatbot.sendMessage = function (messageData, callback) {
        if (!isChatbotConnected()) {
            callback && callback();
            return;
        }
        chatbotHub.invoke("sendMessage", messageData).then(function (result) {
            callback && callback();
        });
    };

    /**
     * Requests the chat's historical messages from the server.
     */
    app.chatbot.requestHistoryMessages = function (messageData, callback) {
        if (!isChatbotConnected()) {
            callback && callback();
            return;
        }
        chatbotHub.invoke("requestHistoryMessages", messageData).then(function (result) {
            callback && callback();
        });
    };

    /**
     * Returns true if the hub is actively connected.
     */
    app.chatbot.isConnected = isChatbotConnected;
    function isChatbotConnected() {
        return (chatbotHub.connection.connectionState === signalR.HubConnectionState.Connected);
    }

    /**
     * Asks the server for a greeting message (initial welcome text).
     */
    app.chatbot.requestGreetingMessage = function (messageData, callback) {
        if (!isChatbotConnected()) {
            callback && callback();
            return;
        }
        chatbotHub.invoke("requestGreetingMessage", messageData).then(function (result) {
            callback && callback();
        });
    };

    /**
     * Sends an invoice or purchase receipt to the server for processing.
     */
    app.chatbot.sendReceipt = function (messageData, callback) {
        if (!isChatbotConnected()) {
            callback && callback();
            return;
        }
        chatbotHub.invoke("sendReceipt", messageData).then(function (result) {
            callback && callback();
        });
    };

    /**
     * Forces the client to reconnect to the chatbot (if supported).
     */
    app.chatbot.clientReconnect = function (messageData, callback) {
        if (!isChatbotConnected()) {
            callback && callback();
            return;
        }
        chatbotHub.invoke("clientReconnect", messageData).then(function (result) {
            callback && callback();
        });
    };

    /**
     * Closes the current hub connection and blocks auto-reconnect.
     */
    app.chatbot.closeConnection = function (callback) {
        app.signalr.autoConnect = false;

        if (chatbotHub.connection.connectionState === signalR.HubConnectionState.Disconnected) {
            console.log("Already disconnected from the SignalR chatbot server!");
            callback && callback();
            return;
        }
        chatbotHub.stop().then(function (result) {
            if (result) {
                console.log("Disconnected from the SignalR chatbot server!");
            }
            callback && callback();
        });
    };

})();
