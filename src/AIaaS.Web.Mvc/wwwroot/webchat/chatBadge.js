(chatBadge = function () {

    var rootURL = "/",
        chatbotId = "",
        chatbotIcon = rootURL + "Chatbot/ProfilePicture/",
        badgeIcon = rootURL + "Chatbot/ProfilePicture/",
        wss = "wss://" + location.host + "/signalr-chatbot?chatbotId=",
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

            //檢查badgeStyle
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
            var frameId = 'chatbotFrame' + elementKey;

            document.getElementById(badgeId)
                .addEventListener('click', function (event) {

                    var scriptElement = document.getElementById("chatBadgeScript");
                    var badge = document.getElementById(badgeId);
                    var pane = document.getElementById(paneId);

                    badge.style.display = "none";

                    //檢查badgeStyle
                    var paneStyle = scriptElement.getAttribute("paneStyle");
                    if (!paneStyle) {
                        paneStyle = "bottom:0px; width:450px; height:80%; right:10px; overflow:hidden; position:fixed; z-index:99990;";
                    }
                    pane.setAttribute("style", paneStyle);

                    var iframe = "<iframe id='" + frameId + "' src='" + webchatURL + "?chatbotId=" + chatbotId +
                        "&chatbotIcon=" + encodeURIComponent(chatbotIcon) + "' style='width: 100%; height: 100%; border: 0;'  scrolling='no'>" +
                        "</iframe>";

                    pane.innerHTML = iframe;

                    if (window.document.getElementById("chatbotPanelGRwyb85K").offsetWidth > window.innerWidth) {
                        window.document.getElementById("chatbotPanelGRwyb85K").style.width = (window.innerWidth - 20) + "px";
                    }

                    if (window.document.getElementById("chatbotPanelGRwyb85K").offsetHeight > window.innerHeight) {
                        window.document.getElementById("chatbotPanelGRwyb85K").style.height = (window.innerHeight - 20) + "px";
                    }

                    window.document.addEventListener('closeWebChatEvent',
                        function (e) {
                            badge.style.display = "block";
                            pane.style.display = "none";
                        }, { once: true });
                });
        };

    initialize();
})();


