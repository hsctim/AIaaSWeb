(function ($) {
    app.modals.CreateOrEditNlpCbModelModal = function () {
        var _nlpCbModelsService = abp.services.app.nlpCbModels;

        var _modalManager;
        var _$nlpCbModelInformationForm = null;

        var _NlpCbModelnlpChatbotLookupTableModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpCbModels/NlpChatbotLookupTableModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpCbModels/_NlpCbModelNlpChatbotLookupTableModal.js',
            modalClass: 'NlpChatbotLookupTableModal'
        }); var _NlpCbModeluserLookupTableModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpCbModels/UserLookupTableModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpCbModels/_NlpCbModelUserLookupTableModal.js',
            modalClass: 'UserLookupTableModal'
        });

        this.init = function (modalManager) {
            _modalManager = modalManager;

            var modal = _modalManager.getModal();
            //modal.find('.date-picker').datetimepicker({
            //    locale: abp.localization.currentLanguage.name,
            //    format: 'L'
            //});

            _$nlpCbModelInformationForm = _modalManager.getModal().find('form[name=NlpCbModelInformationsForm]');
            _$nlpCbModelInformationForm.validate();

            $("button.save-button").addClass("d-none");
        };

        $('#OpenNlpChatbotLookupTableButton').click(function () {
            var nlpCbModel = _$nlpCbModelInformationForm.serializeFormToObject();

            _NlpCbModelnlpChatbotLookupTableModal.open({ id: nlpCbModel.nlpChatbotId, displayName: nlpCbModel.nlpChatbotName }, function (data) {
                _$nlpCbModelInformationForm.find('input[name=nlpChatbotName]').val(data.displayName);
                _$nlpCbModelInformationForm.find('input[name=nlpChatbotId]').val(data.id);
            });
        });

        $('#ClearNlpChatbotNameButton').click(function () {
            _$nlpCbModelInformationForm.find('input[name=nlpChatbotName]').val('');
            _$nlpCbModelInformationForm.find('input[name=nlpChatbotId]').val('');
        });

        $('#OpenUserLookupTableButton').click(function () {
            var nlpCbModel = _$nlpCbModelInformationForm.serializeFormToObject();

            _NlpCbModeluserLookupTableModal.open({ id: nlpCbModel.nlpCbMTrainingCancellationUserId, displayName: nlpCbModel.userName }, function (data) {
                _$nlpCbModelInformationForm.find('input[name=userName]').val(data.displayName);
                _$nlpCbModelInformationForm.find('input[name=nlpCbMTrainingCancellationUserId]').val(data.id);
            });
        });

        $('#ClearUserNameButton').click(function () {
            _$nlpCbModelInformationForm.find('input[name=userName]').val('');
            _$nlpCbModelInformationForm.find('input[name=nlpCbMTrainingCancellationUserId]').val('');
        });

        $('#OpenUser2LookupTableButton').click(function () {
            var nlpCbModel = _$nlpCbModelInformationForm.serializeFormToObject();

            _NlpCbModeluserLookupTableModal.open({ id: nlpCbModel.nlpCbMDeletionUserId, displayName: nlpCbModel.userName2 }, function (data) {
                _$nlpCbModelInformationForm.find('input[name=userName2]').val(data.displayName);
                _$nlpCbModelInformationForm.find('input[name=nlpCbMDeletionUserId]').val(data.id);
            });
        });

        $('#ClearUserName2Button').click(function () {
            _$nlpCbModelInformationForm.find('input[name=userName2]').val('');
            _$nlpCbModelInformationForm.find('input[name=nlpCbMDeletionUserId]').val('');
        });

        this.save = function () {
            if (!_$nlpCbModelInformationForm.valid()) {
                return;
            }

            if ($('#NlpCbModel_NlpChatbotId').prop('required') && $('#NlpCbModel_NlpChatbotId').val() == '') {
                abp.message.error(app.localize('{0}IsRequired', app.localize('NlpChatbot')));
                return;
            }
            if ($('#NlpCbModel_NlpCbMTrainingCancellationUserId').prop('required') && $('#NlpCbModel_NlpCbMTrainingCancellationUserId').val() == '') {
                abp.message.error(app.localize('{0}IsRequired', app.localize('User')));
                return;
            }

            var nlpCbModel = _$nlpCbModelInformationForm.serializeFormToObject();

            _modalManager.setBusy(true);
            _nlpCbModelsService.updateName(
                nlpCbModel
            ).done(function () {
                abp.notify.info(app.localize('SavedSuccessfully'));
                _modalManager.close();
                abp.event.trigger('app.createOrEditNlpCbModelModalSaved');
            }).always(function () {
                _modalManager.setBusy(false);
            });
        };
    };
})(jQuery);