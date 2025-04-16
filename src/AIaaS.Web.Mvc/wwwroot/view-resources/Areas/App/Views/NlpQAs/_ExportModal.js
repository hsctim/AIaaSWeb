/**
 * ExportNlpQAModal
 * Handles the export functionality for NLP QA (Question-Answer) pairs within a modal interface.
 */
(function ($) {
    app.modals.ExportNlpQAModal = function () {
        const _nlpQAsService = abp.services.app.nlpQAs;

        let _modalManager;
        let _$nlpQAInformationForm = null;

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
            _$nlpQAInformationForm = $modal.find('form[name=NlpQAInformationsForm]');
            _$nlpQAInformationForm.validate();
        };

        /**
         * Handles the export button click event to export NLP QA data to a file.
         */
        $('#ExportToExcelFile').click(function (e) {
            e.preventDefault(); // Prevent default form submission behavior

            // Set the modal to busy state
            _modalManager.setBusy(true);

            // Call the service to export the QA data
            _nlpQAsService.getNlpQAsToFile($('#ChatbotSelect').val())
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




