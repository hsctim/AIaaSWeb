/**
 * ImportNlpChatbotModal
 * Handles the import functionality for NLP chatbots within a modal interface.
 */
(function ($) {
    app.modals.ImportNlpChatbotModal = function () {
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
         * Handles the file input change event to trigger the import process.
         */
        $('#ImportChatbotFromFile').change(function () {
            handleFileUpload();
        });

        /**
         * Handles the file upload and submission process.
         */
        function handleFileUpload() {
            const file = $('#ImportChatbotFromFile')[0].files[0];

            // Validate file size (limit: 100MB)
            if (file && file.size > 104857600) {
                abp.message.warn(app.localize('JsonFile_Warn_SizeLimit', 100));
                $('#ImportChatbotFromFile').val(""); // Reset the file input
                return;
            }

            // Serialize form data
            const dto = _$nlpChatbotInformationForm.serializeFormToObject();
            const formData = new FormData();

            // Append form data and file to FormData
            Object.keys(dto).forEach((key) => formData.append(key, dto[key]));
            if (file) {
                formData.append("file", file);
            }

            // Disable the file input and set the modal to busy state
            $('#ImportChatbotFromFile').attr("disabled", true);
            _modalManager.setBusy(true);

            // Submit the form data via AJAX
            abp.ajax({
                url: abp.appPath + 'App/NlpChatbots/ImportJsonFile',
                type: 'POST',
                data: formData,
                contentType: false,
                processData: false
            }).done(function () {
                abp.notify.info(app.localize('ImportSuccessfully'));
                _modalManager.close();
                abp.event.trigger('app.createOrEditNlpChatbotModalSaved');
            }).fail(function () {
                abp.message.error(app.localize('ImportFailed'));
            }).always(function () {
                // Re-enable the file input and reset the modal busy state
                $('#ImportChatbotFromFile').attr("disabled", false);
                _modalManager.setBusy(false);
            });
        }
    };
})(jQuery);



