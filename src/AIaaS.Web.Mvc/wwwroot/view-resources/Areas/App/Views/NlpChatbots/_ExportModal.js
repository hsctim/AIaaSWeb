/**
 * ExportNlpChatbotModal
 * Handles the export functionality for NLP chatbots within a modal interface.
 */
(function ($) {
    app.modals.ExportNlpChatbotModal = function () {
        const _nlpChatbotsService = abp.services.app.nlpChatbots;

        let _modalManager;
        let _$nlpChatbotInformationForm = null;

        /**
         * Initializes the modal and sets up form validation.
         * @param {Object} modalManager - The modal manager instance.
         */
        this.init = function (modalManager) {
            _modalManager = modalManager;
            const $modal = _modalManager.getModal();

            // Adjust modal size
            $modal.find('div.modal-lg').removeClass('modal-lg');

            // Initialize the form and enable validation
            _$nlpChatbotInformationForm = $modal.find('form[name=NlpChatbotInformationsForm]');
            _$nlpChatbotInformationForm.validate();
        };

        /**
         * Handles the export button click event to export the selected chatbot to a file.
         */
        $('#ExportChatbotToFile').click(function (e) {
            e.preventDefault(); // Prevent default form submission behavior

            // Set the modal to busy state
            _modalManager.setBusy(true);

            // Call the service to export the chatbot
            _nlpChatbotsService.getNlpChatbotsToFile($('#ChatbotSelect').val())
                .done(function (result) {
                    // Download the exported file
                    app.downloadTempFile(result);
                    _modalManager.close();
                })
                .fail(function () {
                    // Close the modal on failure
                    _modalManager.close();
                })
                .always(function () {
                    // Reset the modal busy state
                    _modalManager.setBusy(false);
                });
        });
    };
})(jQuery);