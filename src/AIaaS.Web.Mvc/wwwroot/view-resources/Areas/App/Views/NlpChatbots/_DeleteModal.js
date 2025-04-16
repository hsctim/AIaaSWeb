(function ($) {

    app.modals.DeleteNlpChatbotModal = function () {
        var _nlpChatbotsService = abp.services.app.nlpChatbots;

        var _modalManager;
        var _$nlpChatbotForm = null;

        this.init = function (modalManager) {
            _modalManager = modalManager;
            var $modal = _modalManager.getModal();

            _$nlpChatbotForm = _modalManager.getModal().find('form[name=NlpChatbotInformationsForm]');

            $modal.find('div.modal-lg').removeClass('modal-lg');
            //$modal.find('.modal-title').addClass('text-danger');

            if (typeof String.prototype.replaceAll == "undefined") {
                String.prototype.replaceAll = function (match, replace) {
                    return this.replace(new RegExp(match, 'g'), () => replace);
                }
            }

            var html = app.localize("DeleteNlpChatbotConfirmText");
            html = html.replaceAll("[", "<b class='bg-light'>").replaceAll("]", "</b>")
                .replaceAll("{0}", _$nlpChatbotForm.find('[name="chatbotName"]').val())
                .replaceAll("{1}", _$nlpChatbotForm.find('#userEmailAddress').val());

            $modal.find('#formWarningMessage').append(html);

            $modal.find('.delete-button').click(function () {
                deleteChatbot();
            });
        };

        deleteChatbot = function () {
            debugger;
            if (!_$nlpChatbotForm.valid()) {
                return;
            }

            if (_$nlpChatbotForm.find("#DeleteChatbotModal_EMail").val().trim() != _$nlpChatbotForm.find("#userEmailAddress").val().trim()) {

                var $html = $("<div>").addClass("invalid-feedback").append(app.localize("InvalidEmailAddress"));

                var $email = _$nlpChatbotForm.find('#DeleteChatbotModal_EMail');
                $email.addClass('is-invalid');
                $email.after($html[0]);
                return;
            }

            _modalManager.setBusy(true);
            _nlpChatbotsService.delete({
                id: _$nlpChatbotForm.find('[name="id"]').val()
            }).done(function () {
                abp.notify.info(app.localize('SuccessfullyDeleted'));
                _modalManager.close();
                abp.event.trigger('app.createOrEditNlpChatbotModalSaved');
            }).always(function () {
                _modalManager.setBusy(false);
            });

            //function deleteNlpChatbot(nlpChatbotId, nlpChatbotName) {
            //    abp.message.confirm(
            //        app.localize('NlpChatbotDeleteWarningMessage', nlpChatbotName),
            //        app.localize('AreYouSure'),
            //        function (isConfirmed) {
            //            if (isConfirmed) {
            //                _nlpChatbotsService.delete({
            //                    id: nlpChatbotId
            //                }).done(function () {
            //                    getNlpChatbots(true);
            //                    abp.notify.success(app.localize('SuccessfullyDeleted'));
            //                });
            //            }
            //        }
            //    );
            //}

        };

        //var nlpToken = _$nlpTokenInformationForm.serializeFormToObject();

        //_modalManager.setBusy(true);
        //_nlpTokensService.createOrEdit(
        //    nlpToken
        //).done(function () {
        //    abp.notify.info(app.localize('SavedSuccessfully'));
        //    _modalManager.close();
        //    abp.event.trigger('app.createOrEditNlpTokenModalSaved');
        //}).always(function () {
        //    _modalManager.setBusy(false);
        //});

    };
})(jQuery);