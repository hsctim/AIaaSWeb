(function () {
    /**
     * Retrieves the script element that includes the chat badge configuration.
     * Returns immediately if not found, as there is nothing to initialize.
     */
    const scriptElement = document.getElementById("chatBadgeScript");
    if (!scriptElement) return;

    /**
     * Extracts the chatbot ID; must be provided for the badge to load.
     */
    const chatbotId = scriptElement.getAttribute("chatbotId");
    if (!chatbotId) return;

    /**
     * Determines custom or default badge/pane styling.
     */
    const badgeStyle = scriptElement.getAttribute("badgeStyle")
        || "top:150px; width:60px; height:60px; right:0px; overflow:hidden; border-radius:50%; position:fixed";
    const paneStyle = scriptElement.getAttribute("paneStyle")
        || "bottom:0px; width:450px; height:80%; right:10px; overflow:hidden; position:fixed; z-index:99990;";

    /**
     * Keys and IDs that govern how elements are identified.
     */
    const elementKey = "GRwyb85K";
    const badgeId = "chatbotBadge" + elementKey;
    const paneId = "chatbotPanel" + elementKey;
    const frameId = "chatbotFrame" + elementKey;

    /**
     * Allow optional overrides for icons; otherwise fallback to default paths.
     */
    let chatbotIcon = scriptElement.getAttribute("chatbotIcon") || ("/Chatbot/ProfilePicture/" + chatbotId);
    let badgeIcon = scriptElement.getAttribute("badgeIcon") || ("/Chatbot/ProfilePicture/" + chatbotId);

    /**
     * Constructs the chat frame URL with encoded parameters.
     */
    const webchatURL = "/webchat/index.html?chatbotId=" + chatbotId + "&chatbotIcon=" + encodeURIComponent(chatbotIcon);

    /**
     * Appends a chat badge (a clickable icon) to the DOM if it does not already exist.
     */
    function addBadge() {
        if (!document.getElementById(badgeId)) {
            const wrapper = document.createElement("div");
            wrapper.innerHTML =
                `<div id="${badgeId}" style="${badgeStyle}">
                    <img src="${badgeIcon}" style="width:100%;height:100%;">
                </div>
                <div id="${paneId}"></div>`;
            document.body.appendChild(wrapper);
        }
    }

    /**
     * Attaches the click event that toggles the chat pane visibility
     * and injects an iframe displaying the web chat interface.
     */
    function addClickEvent() {
        document.getElementById(badgeId).addEventListener("click", () => {
            const badge = document.getElementById(badgeId);
            const pane = document.getElementById(paneId);

            // Hide the badge once opened.
            badge.style.display = "none";

            // Display and style the pane, then populate it with the chat iframe.
            pane.setAttribute("style", paneStyle);
            pane.innerHTML = `<iframe
                                id="${frameId}"
                                src="${webchatURL}"
                                style="width:100%;height:100%;border:0;"
                                scrolling="no">
                              </iframe>`;

            // Handle scenarios where the pane overflows the browser view.
            if (pane.offsetWidth > window.innerWidth) {
                pane.style.width = (window.innerWidth - 20) + "px";
            }
            if (pane.offsetHeight > window.innerHeight) {
                pane.style.height = (window.innerHeight - 20) + "px";
            }

            // Listen for an event instructing the web chat to close; revert to badge display.
            document.addEventListener("closeWebChatEvent", () => {
                badge.style.display = "block";
                pane.style.display = "none";
            }, { once: true });
        });
    }

    // Initialize the badge on page load.
    addBadge();
    addClickEvent();
})();
