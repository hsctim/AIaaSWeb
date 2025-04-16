/**
 * ImportNlpQAModal
 * Handles the import functionality for NLP QA (Question-Answer) pairs within a modal interface.
 */
(function ($) {
    app.modals.ImportNlpQAModal = function () {
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

            // Initialize date picker
            $modal.find('.date-picker').datetimepicker({
                locale: abp.localization.currentLanguage.name,
                format: 'L'
            });

            // Initialize the form and enable validation
            _$nlpQAInformationForm = $modal.find('form[name=NlpQAInformationsForm]');
            _$nlpQAInformationForm.validate();
        };

        /**
         * Handles the file input change event to trigger the import process.
         */
        $('#ImportQAsFromExcelFile').change(function () {
            handleFileUpload();
        });

        /**
         * Handles the file upload and submission process.
         */
        function handleFileUpload() {
            const file = $('#ImportQAsFromExcelFile')[0].files[0];

            // Validate file size (limit: 100MB)
            if (file && file.size > 104857600) {
                abp.message.warn(app.localize('ExcelFile_Warn_SizeLimit', 100));
                $('#ImportQAsFromExcelFile').val(""); // Reset the file input
                return;
            }

            // Serialize form data
            const dto = _$nlpQAInformationForm.serializeFormToObject();
            const formData = new FormData();

            // Append form data and file to FormData
            Object.keys(dto).forEach((key) => formData.append(key, dto[key]));
            if (file) {
                formData.append("file", file);
            }

            // Set the modal to busy state
            _modalManager.setBusy(true);

            // Submit the form data via AJAX
            abp.ajax({
                url: abp.appPath + 'App/NlpQAs/ImportExcelFile',
                type: 'POST',
                data: formData,
                contentType: false,
                processData: false
            }).done(function () {
                abp.notify.info(app.localize('ImportSuccessfully'));
                _modalManager.close();
                abp.event.trigger('app.createOrEditNlpQAModalSaved');
            }).fail(function () {
                abp.message.error(app.localize('ImportFailed'));
                _modalManager.close();
            }).always(function () {
                // Reset the modal busy state
                _modalManager.setBusy(false);
            });
        }
    };
})(jQuery);




