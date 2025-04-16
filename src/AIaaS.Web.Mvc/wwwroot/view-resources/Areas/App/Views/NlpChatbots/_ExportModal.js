(function ($) {

    app.modals.ExportNlpChatbotModal = function () {
        var _nlpChatbotsService = abp.services.app.nlpChatbots;

        var _modalManager;
        var _$nlpChatbotInformationForm = null;

        this.init = function (modalManager) {
            _modalManager = modalManager;
            _modalManager.getModal().find('div.modal-lg').removeClass('modal-lg');
            _$nlpChatbotInformationForm = _modalManager.getModal().find('form[name=NlpChatbotInformationsForm]');

            _$nlpChatbotInformationForm.validate();

        };

        $('#ExportChatbotToFile').click(function (e) {
            debugger;
            _modalManager.setBusy(true);
            e.preventDefault();
            debugger
            _nlpChatbotsService.getNlpChatbotsToFile($('#ChatbotSelect').val())
                .done(function (result) {
                    app.downloadTempFile(result);
                    _modalManager.close();
                }).fail(function () {
                    _modalManager.close();
                }).always(function () {
                    _modalManager.setBusy(false);
                });
        });
    };
})(jQuery);