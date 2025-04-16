(function () {
    app.widgets.Widgets_Tenant_SubscriptionSummary = function () {
        var _tenantDashboardService = abp.services.app.tenantDashboard;
        var _widget;

        this.init = function (widgetManager) {
            _widget = widgetManager.getWidget();
            getSubscriptionSummary();
            setInterval(getSubscriptionSummary, 5 * 60 * 1000);
        };

        var initDashboardSubscriptionSummary = function (data) {
            _widget.find("#tenantName").text(data.tenantCodeName);
            _widget.find("#subscriptionEdition").text(data.editionName);

            if (data.subscriptionEndDateUtc) {
                var date = new Date();
                date.setDate(date.getDate() - 7);
                if (moment.utc(data.subscriptionEndDateUtc).local().toDate() > date)
                    _widget.find("#subscriptionEndDate").addClass("text-warning");

                _widget.find("#subscriptionEndDate").text(moment.utc(data.subscriptionEndDateUtc).local().format('L'));
            }
            else
                _widget.find("#subscriptionEndDate").text(app.localize("Forever"));

            var permission = abp.auth.hasPermission("Pages.Administration.Tenant.SubscriptionManagement");
            if (permission) {
                _widget.find("#SubscriptionSummary").attr('href', "/App/SubscriptionManagement");
            }

            var users = data.currentUserCount.toString() + " / ";
            if (data.maxUserCount == 0)
                users = users + app.localize("Unlimited");
            else {
                users = users + data.maxUserCount.toString();

                if (data.currentUserCount > data.maxUserCount)
                    _widget.find("#users").addClass("text-danger");
            }
            _widget.find("#users").text(users);

            var chatbots = data.currentChatbotCount.toString() + " / ";
            if (data.maxChatbotCount == 0)
                chatbots = chatbots + app.localize("Unlimited");
            else {
                chatbots = chatbots + data.maxChatbotCount.toString();

                if (data.currentChatbotCount > data.maxChatbotCount)
                    _widget.find("#nlpChatbots").addClass("text-danger");
            }
            _widget.find("#nlpChatbots").text(chatbots);

            var nlpQAs = "";
            if (data.maxQACount == 0)
                nlpQAs = app.localize("Unlimited");
            else {
                nlpQAs = data.maxQACount.toString();
            }
            _widget.find("#nlpQAs").text(nlpQAs);

            var nlpPUs = "";
            if (data.maxPUCount == 0)
                nlpPUs = app.localize("Unlimited");
            else {
                nlpPUs = data.maxPUCount.toString();
            }
            _widget.find("#nlpPUs").text(nlpPUs);
        };

        var getSubscriptionSummary = function () {
            _tenantDashboardService.getSubscriptionSummary().done(function (result) {
                initDashboardSubscriptionSummary(result);
            });
        };
    };


})();
