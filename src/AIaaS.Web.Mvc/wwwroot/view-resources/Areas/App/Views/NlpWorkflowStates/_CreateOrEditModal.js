(function ($) {
    app.modals.CreateOrEditNlpWorkflowStateModal = function () {
        var _nlpWorkflowStatesService = abp.services.app.nlpWorkflowStates;

        var _modalManager;
        var _$nlpWorkflowStateInformationForm = null;

        var _NlpWorkflowStatenlpWorkflowLookupTableModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpWorkflowStates/NlpWorkflowLookupTableModal',
            scriptUrl:
                abp.appPath +
                'view-resources/Areas/App/Views/NlpWorkflowStates/_NlpWorkflowStateNlpWorkflowLookupTableModal.js',
            modalClass: 'NlpWorkflowLookupTableModal',
        });

        this.init = function (modalManager) {
            _modalManager = modalManager;

            var modal = _modalManager.getModal();
            //modal.find('.date-picker').datetimepicker({
            //    locale: abp.localization.currentLanguage.name,
            //    format: 'L',
            //});

            _$nlpWorkflowStateInformationForm = _modalManager
                .getModal()
                .find('form[name=NlpWorkflowStateInformationsForm]');
            _$nlpWorkflowStateInformationForm.validate();
        };

        $('#OpenNlpWorkflowLookupTableButton').click(function () {
            var nlpWorkflowState = _$nlpWorkflowStateInformationForm.serializeFormToObject();

            _NlpWorkflowStatenlpWorkflowLookupTableModal.open(
                { id: nlpWorkflowState.nlpWorkflowId, displayName: nlpWorkflowState.nlpWorkflowName },
                function (data) {
                    _$nlpWorkflowStateInformationForm.find('input[name=nlpWorkflowName]').val(data.displayName);
                    _$nlpWorkflowStateInformationForm.find('input[name=nlpWorkflowId]').val(data.id);
                }
            );
        });

        $('#ClearNlpWorkflowNameButton').click(function () {
            _$nlpWorkflowStateInformationForm.find('input[name=nlpWorkflowName]').val('');
            _$nlpWorkflowStateInformationForm.find('input[name=nlpWorkflowId]').val('');
        });

        this.save = function () {
            if (!_$nlpWorkflowStateInformationForm.valid()) {
                return;
            }
            if ($('#NlpWorkflowState_NlpWorkflowId').prop('required') && $('#NlpWorkflowState_NlpWorkflowId').val() == '') {
                abp.message.error(app.localize('{0}IsRequired', app.localize('NlpWorkflow')));
                return;
            }

            var nlpWorkflowState = _$nlpWorkflowStateInformationForm.serializeFormToObject();

            _modalManager.setBusy(true);
            _nlpWorkflowStatesService
                .createOrEdit(nlpWorkflowState)
                .done(function () {
                    abp.notify.info(app.localize('SavedSuccessfully'));
                    _modalManager.close();
                    abp.event.trigger('app.createOrEditNlpWorkflowStateModalSaved');
                })
                .always(function () {
                    _modalManager.setBusy(false);
                });
        };
    };
})(jQuery);
