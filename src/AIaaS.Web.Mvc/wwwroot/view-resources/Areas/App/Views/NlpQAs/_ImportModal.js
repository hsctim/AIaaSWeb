(function ($) {
    app.modals.ImportNlpQAModal = function () {
        var _nlpQAsService = abp.services.app.nlpQAs;

        var _modalManager;
        var _$nlpQAInformationForm = null;

        this.init = function (modalManager) {
            _modalManager = modalManager;

            var modal = _modalManager.getModal();
            modal.find('div.modal-lg').removeClass('modal-lg');
            modal.find('.date-picker').datetimepicker({
                locale: abp.localization.currentLanguage.name,
                format: 'L'
            });

            _$nlpQAInformationForm = _modalManager.getModal().find('form[name=NlpQAInformationsForm]');
            _$nlpQAInformationForm.validate();

        };

        $('#ImportQAsFromExcelFile').change(function (e) {
            uploadSubmit();
        });

        function uploadSubmit() {
            debugger;

            var file = $('#ImportQAsFromExcelFile')[0].files[0];

            if (file) {
                if (file.size > 104857600) //100MB
                {
                    abp.message.warn(app.localize('ExcelFile_Warn_SizeLimit', 100));
                    $('#ImportQAsFromExcelFile').val("");
                    return false;
                }
            }

            var dto = _$nlpQAInformationForm.serializeFormToObject();
            //dto["IgnoreDuplication"] = $('#IgnoreDuplication').prop("checked")

            var formData = new FormData();
            for (var k in dto) {
                formData.append(k, dto[k]);
            }

            if (file)
                formData.append("file", file);

            //$('#ImportQAsFromExcelButton').removeAttr('for').empty()
            //    .attr('aria-disabled', "true")
            //    .addClass("disabled")
            //    .append($("<i/>").addClass("spinner-border spinner-border-sm mr-5"))
            //    .append(app.localize("ImportingFromExcel"));

            _modalManager.setBusy(true);

            return abp.ajax({
                url: abp.appPath + 'App/NlpQAs/ImportExcelFile',
                type: 'POST',
                data: formData,
                //async: false,
                //cache: false,
                contentType: false,
                processData: false,
            }).done(function () {
                abp.notify.info(app.localize('ImportSuccessfully'));
                _modalManager.close();
                abp.event.trigger('app.createOrEditNlpQAModalSaved');
            }).fail(function () {
                _modalManager.close();
            }).always(function () {
                _modalManager.setBusy(false);
            });
        }
    };
})(jQuery);