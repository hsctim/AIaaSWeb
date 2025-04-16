/**
 * ChatBadge Module
 * Handles the display and interaction of a chatbot badge on the webpage.
 */
(function () {
    const rootURL = "https://localhost:44302/";
    let chatbotId = "";
    let chatbotIcon = `${rootURL}Chatbot/ProfilePicture/`;
    let badgeIcon = `${rootURL}Chatbot/ProfilePicture/`;
    const webchatURL = `${rootURL}webchat/index.html`;
    const elementKey = "GRwyb85K";

    /**
     * Binds an event listener to an element.
     * @param {HTMLElement} ele - The element to bind the event to.
     * @param {string} type - The type of event (e.g., 'click').
     * @param {Function} handle - The event handler function.
     */
    const bindEvent = (ele, type, handle) => {
        if (ele.addEventListener) {
            ele.addEventListener(type, handle);
        } else if (ele.attachEvent) {
            ele.attachEvent(`on${type}`, handle);
        }
    };

    /**
     * Initializes the chatbot badge by reading attributes from the script element
     * and setting up the badge and its click event.
     */
    const initialize = () => {
        const scriptElement = document.getElementById("chatBadgeScript");
        if (!scriptElement) return;

        chatbotId = scriptElement.getAttribute("chatbotId");
        if (!chatbotId) return;

        // Set default badge style if not provided
        let badgeStyle = scriptElement.getAttribute("badgeStyle") ||
            "top:150px; width:60px; height:60px; right:0px; overflow:hidden; border-radius:50%; position:fixed";

        // Update chatbot and badge icons if provided
        chatbotIcon = scriptElement.getAttribute("chatbotIcon") || `${chatbotIcon}${chatbotId}`;
        badgeIcon = scriptElement.getAttribute("badgeIcon") || `${badgeIcon}${chatbotId}`;

        // Add the chatbot badge and set up its click event
        addChatbotBadgePane(badgeStyle);
        addChatbotBadgeClickEvent();
    };

    /**
     * Adds the chatbot badge and panel to the DOM.
     * @param {string} badgeStyle - The CSS style for the badge.
     */
    const addChatbotBadgePane = (badgeStyle) => {
        const badgeId = `chatbotBadge${elementKey}`;
        const paneId = `chatbotPanel${elementKey}`;

        if (!document.getElementById(badgeId)) {
            const html = `
                <div id="${badgeId}" style="${badgeStyle}">
                    <img src="${badgeIcon}" style="width:100%;height:100%;" />
                </div>
                <div id="${paneId}"></div>
            `;

            const div = document.createElement("div");
            div.innerHTML = html;
            document.body.appendChild(div);
        }
    };

    /**
     * Sets up the click event for the chatbot badge to display the chat panel.
     */
    const addChatbotBadgeClickEvent = () => {
        const badgeId = `chatbotBadge${elementKey}`;
        const paneId = `chatbotPanel${elementKey}`;

        const badge = document.getElementById(badgeId);
        const pane = document.getElementById(paneId);

        if (!badge || !pane) return;

        bindEvent(badge, "click", () => {
            badge.style.display = "none";

            // Set default pane style if not provided
            const scriptElement = document.getElementById("chatBadgeScript");
            const paneStyle = scriptElement.getAttribute("paneStyle") ||
                "bottom:0px; width:450px; height:80%; right:10px; overflow:hidden; position:fixed; z-index:99990;";
            pane.setAttribute("style", paneStyle);

            // Add the chat iframe to the panel
            const iframe = `
                <iframe src="${webchatURL}?chatbotId=${chatbotId}&chatbotIcon=${encodeURIComponent(chatbotIcon)}" 
                        style="width: 100%; height: 100%; border: 0;">
                </iframe>`;
            pane.innerHTML = iframe;

            // Listen for the close event to hide the panel and show the badge
            document.addEventListener("closeWebChatEvent", () => {
                badge.style.display = "block";
                pane.style.display = "none";
            }, { once: true });
        });
    };

    // Initialize the chatbot badge on page load
    initialize();
})();
