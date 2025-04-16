var app = app || {};
app.chatbot = app.chatbot || {};

//if (!app.chatbot.event)
//    app.chatbot.event = $('#webchat-container');

(function () {
    //debugger;

    //Check if SignalR is defined
    if (!signalR) {
        return;
    }

    //Create namespaces
    app.signalr = app.signalr || {};
    app.signalr.hubs = app.signalr.hubs || {};

    var chatbotHub = null;

    // Configure the connection
    function configureConnection(connection) {
        // Set the common hub
        app.signalr.hubs.chatbot = connection;
        chatbotHub = connection;

        let reconnectTime = 5000;
        let tries = 1;
        let maxTries = 8;

        function tryReconnect() {
            if (tries > maxTries) {
                return;
            } else {
                connection.start()
                    .then(function () {
                        reconnectTime = 5000;
                        tries = 1;
                        console.log('Reconnected to SignalR chatbot server!');

                        console.log("$('#webchat-container').trigger('chatroom.reconnect')");
                        $('#webchat-container').trigger('chatroom.reconnect');

                    }).catch(function () {
                        tries += 1;
                        reconnectTime *= 2;
                        setTimeout(function () {
                            tryReconnect();
                        }, reconnectTime);
                    });
            }
        }

        // Reconnect if hub disconnects
        connection.onclose(function (e) {
            if (e) {
                console.log('chatbot connection closed with error: ' + e);
            }
            else {
                console.log('chatbot disconnected');
            }

            if (!app.signalr.autoConnect) {
                return;
            }
            tryReconnect();
        });

        // Register to get notifications
        registerChatbotEvents(connection);
    }

    // Connect to the server
    app.signalr.connect = function () {
        // Start the connection.
        startConnection('/signalr-chatbot', configureConnection)
            .then(function (connection) {
                console.log('Connected to SignalR chatbot server!');
                //$('#webchat-container').trigger('app.chat.connected');

                // Call the Register method on the hub.
                connection.invoke('register').then(function () {
                    console.log('Registered to the SignalR chatbot server!');
                });
            })
            .catch(function (error) {
                console.log(error.message);
            });
    };

    // Starts a connection with transport fallback - if the connection cannot be started using
    // the webSockets transport the function will fallback to the serverSentEvents transport and
    // if this does not work it will try longPolling. If the connection cannot be started using
    // any of the available transports the function will return a rejected Promise.
    function startConnection(url, configureConnection) {
        if (app.signalr.remoteServiceBaseUrl) {
            url = app.signalr.remoteServiceBaseUrl + url;
        }

        // Add query string: https://github.com/aspnet/SignalR/issues/680
        if (app.signalr.qs) {
            url += '?' + app.signalr.qs;
        }

        return function start(transport) {
            console.log('Starting connection using ' + signalR.HttpTransportType[transport] + ' transport');

            var connection = new signalR.HubConnectionBuilder()
                .withUrl(url, transport)
                .build();

            if (configureConnection && typeof configureConnection === 'function') {
                configureConnection(connection);
            }

            return connection.start()
                .then(function () {
                    return connection;
                })
                .catch(function (error) {
                    console.log('Cannot start the connection using ' + signalR.HttpTransportType[transport] + ' transport. ' + error.message);
                    if (transport !== signalR.HttpTransportType.LongPolling) {
                        return start(transport + 1);
                    }

                    return Promise.reject(error);
                });
        }(signalR.HttpTransportType.WebSockets);
    }

    app.signalr.startConnection = startConnection;

    if (app.signalr.autoConnect === undefined) {
        app.signalr.autoConnect = true;
    }

    if (app.signalr.autoConnect) {
        app.signalr.connect();
    }

    function registerChatbotEvents(connection) {
        //connection.on('receiveMessage', function (message) {
        //    $('#webchat-container').trigger('chatbot.messageReceived', message);
        //});

        connection.on('receiveMessages', function (messages) {
            $('#webchat-container').trigger('chatbot.messagesReceived', { 'messages': messages });
        });
    }

    app.chatbot.sendMessage = function (messageData, callback) {

        if (chatbotHub.connection.connectionState !== signalR.HubConnectionState.Connected) {
            callback && callback();
            //app.notify.warn(app.localize('chatbotIsNotConnectedWarning'));
            return;
        }

        chatbotHub.invoke('sendMessage', messageData).then(function (result) {
            if (result) {
                //app.notify.warn(result);
            }

            callback && callback();
        });
    };

    app.chatbot.requestHistoryMessages = function (messageData, callback) {
        if (chatbotHub.connection.connectionState !== signalR.HubConnectionState.Connected) {
            callback && callback();
            //app.notify.warn(app.localize('chatbotIsNotConnectedWarning'));
            return;
        }

        chatbotHub.invoke('requestHistoryMessages', messageData).then(function (result) {
            if (result) {
                //app.notify.warn(result);
            }

            callback && callback();
        });
    };

    app.chatbot.isConnected = function () {
        if (chatbotHub.connection.connectionState !== signalR.HubConnectionState.Connected)
            return false;
        return true;
    }

    app.chatbot.requestGreetingMessage = function (messageData, callback) {
        if (chatbotHub.connection.connectionState !== signalR.HubConnectionState.Connected) {
            callback && callback();
            //app.notify.warn(app.localize('chatbotIsNotConnectedWarning'));
            return;
        }

        chatbotHub.invoke('requestGreetingMessage', messageData).then(function (result) {
            if (result) {
                //app.notify.warn(result);
            }

            callback && callback();
        });
    };



    app.chatbot.sendReceipt = function (messageData, callback) {
        if (chatbotHub.connection.connectionState !== signalR.HubConnectionState.Connected) {
            callback && callback();
            //app.notify.warn(app.localize('chatbotIsNotConnectedWarning'));
            return;
        }

        chatbotHub.invoke('sendReceipt', messageData).then(function (result) {
            if (result) {
                //app.notify.warn(result);
            }
            callback && callback();
        });
    };


    //app.chatbot.userReadMessageNotification = function (messageData, callback) {
    //    if (chatbotHub.connection.connectionState !== signalR.HubConnectionState.Connected) {
    //        callback && callback();
    //        //app.notify.warn(app.localize('chatbotIsNotConnectedWarning'));
    //        return;
    //    }

    //    chatbotHub.invoke('userReadMessageNotification', messageData).then(function (result) {
    //        if (result) {
    //            //app.notify.warn(result);
    //        }
    //        callback && callback();
    //    });
    //};


    app.chatbot.clientReconnect = function (messageData, callback) {
        if (chatbotHub.connection.connectionState !== signalR.HubConnectionState.Connected) {
            callback && callback();
            //app.notify.warn(app.localize('chatbotIsNotConnectedWarning'));
            return;
        }

        chatbotHub.invoke('clientReconnect', messageData).then(function (result) {
            if (result) {
                //app.notify.warn(result);
            }
            callback && callback();
        });
    };


    app.chatbot.closeConnection = function (callback) {
        app.signalr.autoConnect = false;

        if (chatbotHub.connection.connectionState == signalR.HubConnectionState.Disconnected) {
            callback && callback();
            //app.notify.warn(app.localize('chatbotIsNotConnectedWarning'));
            console.log('Disconnect from the SignalR chatbot server!');
            return;
        }

        chatbotHub.stop().then(function (result) {
            if (result) {
                //app.notify.warn(result);
                console.log('Disconnect from the SignalR chatbot server!');
            }
            callback && callback();
        });
    };

    //app.event.trigger = function (event, message) {
    //    webchatcontainer.trigger(event, message);
    //}
    //app.event.trigger('app.chatbot.messageReceived', message);
})();