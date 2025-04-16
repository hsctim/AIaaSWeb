/**
 * CreateOrEditNlpTokenModal
 * Handles the creation and editing of NLP tokens within a modal interface.
 */
(function ($) {
    app.modals.CreateOrEditNlpTokenModal = function () {
        const _nlpTokensService = abp.services.app.nlpTokens;

        let _modalManager;
        let _$nlpTokenInformationForm = null;

        /**
         * Initializes the modal and sets up form validation.
         * @param {Object} modalManager - The modal manager instance.
         */
        this.init = function (modalManager) {
            _modalManager = modalManager;

            // Get the modal and initialize the form
            const $modal = _modalManager.getModal();
            _$nlpTokenInformationForm = $modal.find('form[name=NlpTokenInformationsForm]');
            _$nlpTokenInformationForm.validate();
        };

        /**
         * Saves the NLP token by validating the form and submitting the data.
         */
        this.save = function () {
            // Validate the form before submission
            if (!_$nlpTokenInformationForm.valid()) {
                return;
            }

            // Serialize form data into an object
            const nlpToken = _$nlpTokenInformationForm.serializeFormToObject();

            // Set the modal to busy state and call the service to save the token
            _modalManager.setBusy(true);
            _nlpTokensService.createOrEdit(nlpToken)
                .done(function () {
                    abp.notify.info(app.localize('SavedSuccessfully'));
                    _modalManager.close();
                    abp.event.trigger('app.createOrEditNlpTokenModalSaved');
                })
                .always(function () {
                    // Reset the modal busy state
                    _modalManager.setBusy(false);
                });
        };
    };
})(jQuery);





