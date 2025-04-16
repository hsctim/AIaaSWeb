/**
 * CreateOrEditNlpWorkflowModal
 * Handles the creation and editing of NLP workflows within a modal interface.
 */
(function ($) {
    app.modals.CreateOrEditNlpWorkflowModal = function () {
        const _nlpWorkflowsService = abp.services.app.nlpWorkflows;

        let _modalManager;
        let _$nlpWorkflowInformationForm = null;

        // Modal manager for chatbot lookup
        const _NlpWorkflownlpChatbotLookupTableModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpWorkflows/NlpChatbotLookupTableModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpWorkflows/_NlpWorkflowNlpChatbotLookupTableModal.js',
            modalClass: 'NlpChatbotLookupTableModal'
        });

        /**
         * Initializes the modal and sets up form validation.
         * @param {Object} modalManager - The modal manager instance.
         */
        this.init = function (modalManager) {
            _modalManager = modalManager;

            // Get the modal and initialize the form
            const $modal = _modalManager.getModal();
            _$nlpWorkflowInformationForm = $modal.find('form[name=NlpWorkflowInformationsForm]');
            _$nlpWorkflowInformationForm.validate();
        };

        /**
         * Opens the chatbot lookup modal and sets the selected chatbot.
         */
        $('#OpenNlpChatbotLookupTableButton').click(function () {
            const nlpWorkflow = _$nlpWorkflowInformationForm.serializeFormToObject();

            _NlpWorkflownlpChatbotLookupTableModal.open(
                { id: nlpWorkflow.nlpChatbotId, displayName: nlpWorkflow.nlpChatbotName },
                function (data) {
                    _$nlpWorkflowInformationForm.find('input[name=nlpChatbotName]').val(data.displayName);
                    _$nlpWorkflowInformationForm.find('input[name=nlpChatbotId]').val(data.id);
                }
            );
        });

        /**
         * Clears the selected chatbot from the form.
         */
        $('#ClearNlpChatbotNameButton').click(function () {
            _$nlpWorkflowInformationForm.find('input[name=nlpChatbotName]').val('');
            _$nlpWorkflowInformationForm.find('input[name=nlpChatbotId]').val('');
        });

        /**
         * Saves the NLP workflow by validating the form and submitting the data.
         */
        this.save = function () {
            // Validate the form before submission
            if (!_$nlpWorkflowInformationForm.valid()) {
                return;
            }

            // Ensure the chatbot ID is provided if required
            if ($('#NlpWorkflow_NlpChatbotId').prop('required') && $('#NlpWorkflow_NlpChatbotId').val() === '') {
                abp.message.error(app.localize('{0}IsRequired', app.localize('NlpChatbot')));
                return;
            }

            // Serialize form data into an object
            const nlpWorkflow = _$nlpWorkflowInformationForm.serializeFormToObject();

            // Set the modal to busy state and call the service to save the workflow
            _modalManager.setBusy(true);
            _nlpWorkflowsService.createOrEdit(nlpWorkflow)
                .done(function () {
                    abp.notify.info(app.localize('SavedSuccessfully'));
                    _modalManager.close();
                    abp.event.trigger('app.createOrEditNlpWorkflowModalSaved');
                })
                .always(function () {
                    // Reset the modal busy state
                    _modalManager.setBusy(false);
                });
        };
    };
})(jQuery);





