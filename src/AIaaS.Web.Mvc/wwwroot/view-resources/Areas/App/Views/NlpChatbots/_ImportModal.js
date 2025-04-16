(function ($) {
    app.modals.ImportNlpChatbotModal = function () {
        var _nlpChatbotsService = abp.services.app.nlpChatbots;

        var _modalManager;
        var _$nlpChatbotInformationForm = null;

        this.init = function (modalManager) {
            _modalManager = modalManager;
            _modalManager.getModal().find('div.modal-lg').removeClass('modal-lg');

            _$nlpChatbotInformationForm = _modalManager.getModal().find('form[name=NlpChatbotInformationsForm]');

            _$nlpChatbotInformationForm.validate();

        };

        $('#ImportChatbotFromFile').change(function (e) {
            uploadSubmit();
        });

        function uploadSubmit() {
            debugger;

            var file = $('#ImportChatbotFromFile')[0].files[0];

            if (file) {
                if (file.size > 104857600) //1MB
                {
                    abp.message.warn(app.localize('JsonFile_Warn_SizeLimit', 100));
                    $('#ImportChatbotFromExcelFile').val("");
                    return false;
                }
            }

            var dto = _$nlpChatbotInformationForm.serializeFormToObject();
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
            //    .append($("<i/>").addClass("spinner-border spinner-border-sm me-5"))
            //    .append(app.localize("ImportingFromExcel"));

            _modalManager.setBusy(true);

            $('#ImportChatbotFromFile').attr("disabled", true);
            return abp.ajax({
                url: abp.appPath + 'App/NlpChatbots/ImportJsonFile',
                type: 'POST',
                data: formData,
                //async: false,
                //cache: false,
                contentType: false,
                processData: false,
            }).done(function () {
                abp.notify.info(app.localize('ImportSuccessfully'));
                _modalManager.close();
                abp.event.trigger('app.createOrEditNlpChatbotModalSaved');
            }).fail(function () {
                _modalManager.close();
            }).always(function () {
                $('#ImportChatbotFromFile').attr("disabled", false);
                _modalManager.setBusy(false);
            });
        }
    };
})(jQuery);
