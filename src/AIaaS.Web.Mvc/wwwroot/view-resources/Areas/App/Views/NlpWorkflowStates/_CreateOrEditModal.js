/**
 * CreateOrEditNlpWorkflowStateModal
 * Handles the creation and editing of NLP workflow states within a modal interface.
 */
(function ($) {
    app.modals.CreateOrEditNlpWorkflowStateModal = function () {
        const _nlpWorkflowStatesService = abp.services.app.nlpWorkflowStates;

        let _modalManager;
        let _$nlpWorkflowStateInformationForm = null;

        // Modal manager for workflow lookup
        const _NlpWorkflowStatenlpWorkflowLookupTableModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpWorkflowStates/NlpWorkflowLookupTableModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpWorkflowStates/_NlpWorkflowStateNlpWorkflowLookupTableModal.js',
            modalClass: 'NlpWorkflowLookupTableModal'
        });

        /**
         * Initializes the modal and sets up form validation.
         * @param {Object} modalManager - The modal manager instance.
         */
        this.init = function (modalManager) {
            _modalManager = modalManager;

            // Get the modal and initialize the form
            const $modal = _modalManager.getModal();
            _$nlpWorkflowStateInformationForm = $modal.find('form[name=NlpWorkflowStateInformationsForm]');
            _$nlpWorkflowStateInformationForm.validate();
        };

        /**
         * Opens the workflow lookup modal and sets the selected workflow.
         */
        $('#OpenNlpWorkflowLookupTableButton').click(function () {
            const nlpWorkflowState = _$nlpWorkflowStateInformationForm.serializeFormToObject();

            _NlpWorkflowStatenlpWorkflowLookupTableModal.open(
                { id: nlpWorkflowState.nlpWorkflowId, displayName: nlpWorkflowState.nlpWorkflowName },
                function (data) {
                    _$nlpWorkflowStateInformationForm.find('input[name=nlpWorkflowName]').val(data.displayName);
                    _$nlpWorkflowStateInformationForm.find('input[name=nlpWorkflowId]').val(data.id);
                }
            );
        });

        /**
         * Clears the selected workflow from the form.
         */
        $('#ClearNlpWorkflowNameButton').click(function () {
            _$nlpWorkflowStateInformationForm.find('input[name=nlpWorkflowName]').val('');
            _$nlpWorkflowStateInformationForm.find('input[name=nlpWorkflowId]').val('');
        });

        /**
         * Saves the NLP workflow state by validating the form and submitting the data.
         */
        this.save = function () {
            // Validate the form before submission
            if (!_$nlpWorkflowStateInformationForm.valid()) {
                return;
            }

            // Ensure the workflow ID is provided if required
            if ($('#NlpWorkflowState_NlpWorkflowId').prop('required') && $('#NlpWorkflowState_NlpWorkflowId').val() === '') {
                abp.message.error(app.localize('{0}IsRequired', app.localize('NlpWorkflow')));
                return;
            }

            // Serialize form data into an object
            const nlpWorkflowState = _$nlpWorkflowStateInformationForm.serializeFormToObject();

            // Set the modal to busy state and call the service to save the workflow state
            _modalManager.setBusy(true);
            _nlpWorkflowStatesService.createOrEdit(nlpWorkflowState)
                .done(function () {
                    abp.notify.info(app.localize('SavedSuccessfully'));
                    _modalManager.close();
                    abp.event.trigger('app.createOrEditNlpWorkflowStateModalSaved');
                })
                .always(function () {
                    // Reset the modal busy state
                    _modalManager.setBusy(false);
                });
        };
    };
})(jQuery);






