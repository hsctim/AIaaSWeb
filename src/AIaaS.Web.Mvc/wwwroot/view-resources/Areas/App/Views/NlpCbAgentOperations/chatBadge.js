(chatBadge = function () {

    var rootURL = "https://localhost:44302/",
        chatbotId = "",
        chatbotIcon = rootURL + "Chatbot/ProfilePicture/",
        badgeIcon = rootURL + "Chatbot/ProfilePicture/",
        wss = "wss://localhost:44302/signalr-chatbot?chatbotId=",
        webchatURL = rootURL + "webchat/index.html",

        elementKey = "GRwyb85K",
        //badgeTop = (window.innerHeight - badgeHeight) / 2,
        paneWidth = Math.min(477, window.innerWidth),
        paneHeigh = Math.min(500, window.innerHeight),

        //panelHeight = Math.min(),

        bindEvent = function (ele, type, handle) {
            if (ele.addEventListener) {
                ele.addEventListener(type, handle);
            } else if (ele.attachEvent) {
                ele.attachEvent('on' + type, handle);
            }
        },

        initialize = function () {
            var scriptElement = document.getElementById("chatBadgeScript");
            chatbotId = scriptElement.getAttribute("chatbotId");
            if (!chatbotId)
                return;

            //set default value if badgeStyle is null
            var badgeStyle = scriptElement.getAttribute("badgeStyle");
            if (!badgeStyle) {
                badgeStyle = "top:150px; width:60px; height:60px; right:0px; overflow:hidden; border-radius:50%; position:fixed";
            }

            var _chatbotIcon = scriptElement.getAttribute("chatbotIcon");
            if (_chatbotIcon)
                chatbotIcon = _chatbotIcon;
            else
                chatbotIcon = chatbotIcon + chatbotId;

            var _badgeIcon = scriptElement.getAttribute("badgeIcon");
            if (_badgeIcon)
                badgeIcon = _badgeIcon;
            else
                badgeIcon = badgeIcon + chatbotId;

            wss = wss + chatbotId;

            addChatbotBadgePane(badgeStyle);
            addChatbotBadgeClickEvent();
        },

        addChatbotBadgePane = function (badgeStyle) {
            var badgeId = 'chatbotBadge' + elementKey;
            var paneId = 'chatbotPanel' + elementKey;

            var div = document.getElementById(badgeId);
            if (div == null) {
                var html =
                    "   <div id='" + badgeId + "' style='" + badgeStyle + "'>" +
                    "      <img src='" + badgeIcon + "' style='width:100%;height:100%;' >" +
                    "       </img>" +
                    "   </div>" +
                    "   <div id='" + paneId + "'> " +
                    "   </div>"
                    ;

                var div = document.createElement("div");
                div.innerHTML = html;
                document.body.appendChild(div);
            }
        },


        addChatbotBadgeClickEvent = function () {
            var badgeId = 'chatbotBadge' + elementKey;
            var paneId = 'chatbotPanel' + elementKey;

            document.getElementById(badgeId)
                .addEventListener('click', function (event) {

                    var scriptElement = document.getElementById("chatBadgeScript");
                    var badge = document.getElementById(badgeId);
                    var pane = document.getElementById(paneId);

                    badge.style.display = "none";

                    //set default value if paneStyle is null
                    var paneStyle = scriptElement.getAttribute("paneStyle");
                    if (!paneStyle) {
                        paneStyle = "top:150px; width:60px; height:60px; right:0px; overflow:hidden; border-radius:50%; position:fixed";
                    }
                    pane.setAttribute("style", paneStyle);


                    var iframe = "<iframe src='" + webchatURL + "?chatbotId=" + chatbotId +
                        "&chatbotIcon=" + encodeURIComponent(chatbotIcon) + "' style='width: 100%; height: 100%; border: 0;'>" +
                        "</iframe>";

                    pane.innerHTML = iframe;

                    window.document.addEventListener('closeWebChatEvent',
                        function (e) {
                            badge.style.display = "block";
                            pane.style.display = "none";
                        }, { once: true });
                });
        };

    initialize();
})();


