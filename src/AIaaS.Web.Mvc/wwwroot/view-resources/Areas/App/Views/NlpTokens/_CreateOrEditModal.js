(function ($) {
  app.modals.CreateOrEditNlpTokenModal = function () {
    var _nlpTokensService = abp.services.app.nlpTokens;

    var _modalManager;
    var _$nlpTokenInformationForm = null;

    this.init = function (modalManager) {
      _modalManager = modalManager;

      var modal = _modalManager.getModal();
      //modal.find('.date-picker').datetimepicker({
      //  locale: abp.localization.currentLanguage.name,
      //  format: 'L',
      //});

      _$nlpTokenInformationForm = _modalManager.getModal().find('form[name=NlpTokenInformationsForm]');
      _$nlpTokenInformationForm.validate();
    };

    this.save = function () {
      if (!_$nlpTokenInformationForm.valid()) {
        return;
      }

      var nlpToken = _$nlpTokenInformationForm.serializeFormToObject();

      _modalManager.setBusy(true);
      _nlpTokensService
        .createOrEdit(nlpToken)
        .done(function () {
          abp.notify.info(app.localize('SavedSuccessfully'));
          _modalManager.close();
          abp.event.trigger('app.createOrEditNlpTokenModalSaved');
        })
        .always(function () {
          _modalManager.setBusy(false);
        });
    };
  };
})(jQuery);
