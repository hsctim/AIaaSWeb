/**
 * DeleteNlpChatbotModal
 * Handles the deletion of NLP chatbots within a modal interface.
 */
(function ($) {
    app.modals.DeleteNlpChatbotModal = function () {
        const _nlpChatbotsService = abp.services.app.nlpChatbots;

        let _modalManager;
        let _$nlpChatbotForm = null;

        /**
         * Initializes the modal and sets up the confirmation message.
         * @param {Object} modalManager - The modal manager instance.
         */
        this.init = function (modalManager) {
            _modalManager = modalManager;
            const $modal = _modalManager.getModal();

            _$nlpChatbotForm = $modal.find('form[name=NlpChatbotInformationsForm]');

            // Adjust modal size
            $modal.find('div.modal-lg').removeClass('modal-lg');

            // Add polyfill for String.prototype.replaceAll if not available
            if (typeof String.prototype.replaceAll === "undefined") {
                String.prototype.replaceAll = function (match, replace) {
                    return this.replace(new RegExp(match, 'g'), () => replace);
                };
            }

            // Generate and display the confirmation message
            const chatbotName = _$nlpChatbotForm.find('[name="chatbotName"]').val();
            const userEmail = _$nlpChatbotForm.find('#userEmailAddress').val();
            const confirmationMessage = app.localize("DeleteNlpChatbotConfirmText")
                .replaceAll("[", "<b class='bg-light'>")
                .replaceAll("]", "</b>")
                .replaceAll("{0}", chatbotName)
                .replaceAll("{1}", userEmail);

            $modal.find('#formWarningMessage').append(confirmationMessage);

            // Bind the delete button click event
            $modal.find('.delete-button').click(deleteChatbot);
        };

        /**
         * Deletes the chatbot after validating the form and email confirmation.
         */
        const deleteChatbot = function () {
            // Validate the form
            if (!_$nlpChatbotForm.valid()) {
                return;
            }

            // Validate the email confirmation
            const enteredEmail = _$nlpChatbotForm.find("#DeleteChatbotModal_EMail").val().trim();
            const expectedEmail = _$nlpChatbotForm.find("#userEmailAddress").val().trim();

            if (enteredEmail !== expectedEmail) {
                const $emailField = _$nlpChatbotForm.find('#DeleteChatbotModal_EMail');
                $emailField.addClass('is-invalid');
                $emailField.after($("<div>").addClass("invalid-feedback").text(app.localize("InvalidEmailAddress")));
                return;
            }

            // Set the modal to busy state and call the delete service
            _modalManager.setBusy(true);
            _nlpChatbotsService.delete({
                id: _$nlpChatbotForm.find('[name="id"]').val()
            }).done(() => {
                abp.notify.info(app.localize('SuccessfullyDeleted'));
                _modalManager.close();
                abp.event.trigger('app.createOrEditNlpChatbotModalSaved');
            }).always(() => {
                _modalManager.setBusy(false);
            });
        };
    };
})(jQuery);