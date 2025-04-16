(function ($) {
  app.modals.CreateOrEditNlpWorkflowModal = function () {
    var _nlpWorkflowsService = abp.services.app.nlpWorkflows;

    var _modalManager;
    var _$nlpWorkflowInformationForm = null;

    var _NlpWorkflownlpChatbotLookupTableModal = new app.ModalManager({
      viewUrl: abp.appPath + 'App/NlpWorkflows/NlpChatbotLookupTableModal',
      scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpWorkflows/_NlpWorkflowNlpChatbotLookupTableModal.js',
      modalClass: 'NlpChatbotLookupTableModal',
    });

    this.init = function (modalManager) {
      _modalManager = modalManager;

      var modal = _modalManager.getModal();
      //modal.find('.date-picker').datetimepicker({
      //  locale: abp.localization.currentLanguage.name,
      //  format: 'L',
      //});

      _$nlpWorkflowInformationForm = _modalManager.getModal().find('form[name=NlpWorkflowInformationsForm]');
      _$nlpWorkflowInformationForm.validate();
    };

    $('#OpenNlpChatbotLookupTableButton').click(function () {
      var nlpWorkflow = _$nlpWorkflowInformationForm.serializeFormToObject();

      _NlpWorkflownlpChatbotLookupTableModal.open(
        { id: nlpWorkflow.nlpChatbotId, displayName: nlpWorkflow.nlpChatbotName },
        function (data) {
          _$nlpWorkflowInformationForm.find('input[name=nlpChatbotName]').val(data.displayName);
          _$nlpWorkflowInformationForm.find('input[name=nlpChatbotId]').val(data.id);
        }
      );
    });

    $('#ClearNlpChatbotNameButton').click(function () {
      _$nlpWorkflowInformationForm.find('input[name=nlpChatbotName]').val('');
      _$nlpWorkflowInformationForm.find('input[name=nlpChatbotId]').val('');
    });

    this.save = function () {
      if (!_$nlpWorkflowInformationForm.valid()) {
        return;
      }
      if ($('#NlpWorkflow_NlpChatbotId').prop('required') && $('#NlpWorkflow_NlpChatbotId').val() == '') {
        abp.message.error(app.localize('{0}IsRequired', app.localize('NlpChatbot')));
        return;
      }

      var nlpWorkflow = _$nlpWorkflowInformationForm.serializeFormToObject();

      _modalManager.setBusy(true);
      _nlpWorkflowsService
        .createOrEdit(nlpWorkflow)
        .done(function () {
          abp.notify.info(app.localize('SavedSuccessfully'));
          _modalManager.close();
          abp.event.trigger('app.createOrEditNlpWorkflowModalSaved');
        })
        .always(function () {
          _modalManager.setBusy(false);
        });
    };
  };
})(jQuery);
