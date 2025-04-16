/**
 * CreateOrEditNlpCbModelModal
 * Handles the creation and editing of NLP chatbot models within a modal interface.
 */
(function ($) {
    app.modals.CreateOrEditNlpCbModelModal = function () {
        const _nlpCbModelsService = abp.services.app.nlpCbModels;

        let _modalManager;
        let _$nlpCbModelInformationForm = null;

        // Lookup table modals for NLP chatbot and user selection
        const _NlpCbModelnlpChatbotLookupTableModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpCbModels/NlpChatbotLookupTableModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpCbModels/_NlpCbModelNlpChatbotLookupTableModal.js',
            modalClass: 'NlpChatbotLookupTableModal'
        });

        const _NlpCbModeluserLookupTableModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpCbModels/UserLookupTableModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpCbModels/_NlpCbModelUserLookupTableModal.js',
            modalClass: 'UserLookupTableModal'
        });

        /**
         * Initializes the modal and sets up form validation.
         * @param {Object} modalManager - The modal manager instance.
         */
        this.init = function (modalManager) {
            _modalManager = modalManager;
            _$nlpCbModelInformationForm = _modalManager.getModal().find('form[name=NlpCbModelInformationsForm]');
            _$nlpCbModelInformationForm.validate();

            // Hide the save button initially
            $("button.save-button").addClass("d-none");
        };

        /**
         * Opens the NLP chatbot lookup table modal and sets the selected chatbot.
         */
        $('#OpenNlpChatbotLookupTableButton').click(function () {
            const nlpCbModel = _$nlpCbModelInformationForm.serializeFormToObject();

            _NlpCbModelnlpChatbotLookupTableModal.open(
                { id: nlpCbModel.nlpChatbotId, displayName: nlpCbModel.nlpChatbotName },
                function (data) {
                    _$nlpCbModelInformationForm.find('input[name=nlpChatbotName]').val(data.displayName);
                    _$nlpCbModelInformationForm.find('input[name=nlpChatbotId]').val(data.id);
                }
            );
        });

        /**
         * Clears the selected NLP chatbot.
         */
        $('#ClearNlpChatbotNameButton').click(function () {
            _$nlpCbModelInformationForm.find('input[name=nlpChatbotName]').val('');
            _$nlpCbModelInformationForm.find('input[name=nlpChatbotId]').val('');
        });

        /**
         * Opens the user lookup table modal and sets the selected user for training cancellation.
         */
        $('#OpenUserLookupTableButton').click(function () {
            const nlpCbModel = _$nlpCbModelInformationForm.serializeFormToObject();

            _NlpCbModeluserLookupTableModal.open(
                { id: nlpCbModel.nlpCbMTrainingCancellationUserId, displayName: nlpCbModel.userName },
                function (data) {
                    _$nlpCbModelInformationForm.find('input[name=userName]').val(data.displayName);
                    _$nlpCbModelInformationForm.find('input[name=nlpCbMTrainingCancellationUserId]').val(data.id);
                }
            );
        });

        /**
         * Clears the selected user for training cancellation.
         */
        $('#ClearUserNameButton').click(function () {
            _$nlpCbModelInformationForm.find('input[name=userName]').val('');
            _$nlpCbModelInformationForm.find('input[name=nlpCbMTrainingCancellationUserId]').val('');
        });

        /**
         * Opens the user lookup table modal and sets the selected user for deletion.
         */
        $('#OpenUser2LookupTableButton').click(function () {
            const nlpCbModel = _$nlpCbModelInformationForm.serializeFormToObject();

            _NlpCbModeluserLookupTableModal.open(
                { id: nlpCbModel.nlpCbMDeletionUserId, displayName: nlpCbModel.userName2 },
                function (data) {
                    _$nlpCbModelInformationForm.find('input[name=userName2]').val(data.displayName);
                    _$nlpCbModelInformationForm.find('input[name=nlpCbMDeletionUserId]').val(data.id);
                }
            );
        });

        /**
         * Clears the selected user for deletion.
         */
        $('#ClearUserName2Button').click(function () {
            _$nlpCbModelInformationForm.find('input[name=userName2]').val('');
            _$nlpCbModelInformationForm.find('input[name=nlpCbMDeletionUserId]').val('');
        });

        /**
         * Saves the NLP chatbot model by submitting the form data.
         */
        this.save = function () {
            if (!_$nlpCbModelInformationForm.valid()) {
                return;
            }

            // Validate required fields
            if ($('#NlpCbModel_NlpChatbotId').prop('required') && $('#NlpCbModel_NlpChatbotId').val() === '') {
                abp.message.error(app.localize('{0}IsRequired', app.localize('NlpChatbot')));
                return;
            }
            if ($('#NlpCbModel_NlpCbMTrainingCancellationUserId').prop('required') && $('#NlpCbModel_NlpCbMTrainingCancellationUserId').val() === '') {
                abp.message.error(app.localize('{0}IsRequired', app.localize('User')));
                return;
            }

            const nlpCbModel = _$nlpCbModelInformationForm.serializeFormToObject();

            _modalManager.setBusy(true);
            _nlpCbModelsService.updateName(nlpCbModel)
                .done(function () {
                    abp.notify.info(app.localize('SavedSuccessfully'));
                    _modalManager.close();
                    abp.event.trigger('app.createOrEditNlpCbModelModalSaved');
                })
                .always(function () {
                    _modalManager.setBusy(false);
                });
        };
    };
})(jQuery);