(function ($) {
    app.modals.CreateOrEditNlpChatbotModal = function () {
        var _nlpChatbotsService = abp.services.app.nlpChatbots;

        var _modalManager;
        var _$nlpChatbotInformationForm = null;

        this.init = function (modalManager) {
            _modalManager = modalManager;

            var modal = _modalManager.getModal();
            //modal.find('.date-picker').datetimepicker({
            //    locale: abp.localization.currentLanguage.name,
            //    format: 'L',
            //});

            _$nlpChatbotInformationForm = _modalManager.getModal().find('form[name=NlpChatbotInformationsForm]');
            _$nlpChatbotInformationForm.validate();

            if (_$nlpChatbotInformationForm.find('#IsViewMode').val() == "1") {
                _$nlpChatbotInformationForm.find('input').attr('readonly', true).addClass('readonly');
                _$nlpChatbotInformationForm.find('input:checkbox').attr('disabled', true).attr('readonly', true).addClass('readonly');
                _$nlpChatbotInformationForm.find('select').attr('disabled', true).addClass('readonly');
                _$nlpChatbotInformationForm.find('textarea').attr('readonly', true).addClass('readonly').addClass('bg-light');
            }
        };

        this.save = function () {

            _$nlpChatbotInformationForm.find('input:invalid').each(function () {
                // Find the tab-pane that this element is inside, and get the id
                var $closest = $(this).closest('.tab-pane');
                var id = $closest.attr('id');
                // Find the link that corresponds to the pane and have it show
                $('.nav a[href="#' + id + '"]').tab('show');
                // Only want to do it once
                return false;
            });


            $.validator.setDefaults({
                ignore: [],
            });

            if (!_$nlpChatbotInformationForm.valid()) {
                return;
            }

            createOrEdit();
        };

        $('.nlpchatbotimg').click(function (event) {
            //debugger
            $('#NlpChatbot_ImageFileObj').val("");
            $('#NlpChatbot_ImageFile').val($(event.target).data("filename"));
            $('#ChatbotImage').attr('src', event.target.src);
        });

        $('#NlpChatbot_ImageFileObj').change(function (input) {
            var file = $('#NlpChatbot_ImageFileObj')[0].files[0];
            var fileInput = $('#NlpChatbot_ImageFileObj');

            if (file) {
                var type = '|' + file.type.slice(file.type.lastIndexOf('/') + 1) + '|';
                if ('|jpg|jpeg|png|gif|'.indexOf(type) === -1) {
                    abp.message.warn(app.localize('BotPicture_Warn_SizeLimit'));
                    fileInput.val("");
                    return false;
                }

                //debugger
                //File size check
                if (file.size > 102400) //100KB
                {
                    abp.message.warn(app.localize('BotPicture_Warn_SizeLimit'));
                    fileInput.val("");
                    return false;
                }

                var reader = new FileReader();
                reader.onload = function (e) {
                    $('#ChatbotImage').attr('src', e.target.result);
                }
            };
            reader.readAsDataURL(input.target.files[0]);
        });

        function createOrEdit() {
            var file = $('#NlpChatbot_ImageFileObj')[0].files[0];
            if (file) {
                //File type check
                var type = '|' + file.type.slice(file.type.lastIndexOf('/') + 1) + '|';
                if ('|jpg|jpeg|png|gif|'.indexOf(type) === -1) {
                    abp.message.warn(app.localize('BotPicture_Warn_SizeLimit'));
                    //inputImg.files = null;
                    $('#NlpChatbot_ImageFileObj').val("");
                    return false;
                }

                if (file.size > 102400) //100KB
                {
                    abp.message.warn(app.localize('BotPicture_Warn_SizeLimit'));
                    $('#NlpChatbot_ImageFileObj').val("");
                    return false;
                }
            }

            //debugger;

            var dto = _$nlpChatbotInformationForm.serializeFormToObject();

            dto["disabled"] = $('#NlpChatbot_Disabled').prop("checked")
            dto["enableFacebook"] = $('#NlpChatbot_EnableFacebook').prop("checked")
            dto["enableWebAPI"] = $('#NlpChatbot_EnableWebAPI').prop("checked")
            dto["enableLine"] = $('#NlpChatbot_EnableLine').prop("checked")
            dto["enableWebChat"] = $('#NlpChatbot_EnableWebChat').prop("checked")
            dto["enableWebHook"] = $('#ChatPal_EnableWebHook').prop("checked")

            var formData = new FormData();
            for (var k in dto) {
                formData.append(k, dto[k]);
            }

            if (file)
                formData.append("file", file);

            _modalManager.setBusy(true);

            return abp.ajax({
                url: abp.appPath + 'App/NlpChatbots/CreateOrEdit',
                type: 'POST',
                data: formData,
                async: false,
                cache: false,
                contentType: false,
                processData: false,
            }).done(function () {
                abp.notify.info(app.localize('SavedSuccessfully'));
                _modalManager.close();
                abp.event.trigger('app.createOrEditNlpChatbotModalSaved');
            }).always(function () {
                _modalManager.setBusy(false);
            });
        }


        $('#ResetThreshold').click(function (event) {
            $('#PredThreshold').val($('input#defaultPredThreshold').val());
            $('#WSPredThreshold').val($('input#defaultWSPredThreshold').val());
            $('#SuggestionThreshold').val($('input#defaultSuggestionThreshold').val());
        });



        function updateGPTOptions() {
            if ($('#GPTOptionsList').val() == "2") {
                $('#GPTOptionsArea').show();
                $('#OPENAI_APIKEY').prop('required', true);
            } else {
                $('#GPTOptionsArea').hide();
                $('#OPENAI_APIKEY').removeAttr('required');
            }
        }

        $('#GPTOptionsList').change(function () {
            updateGPTOptions();
        });

        updateGPTOptions();



        function updateWebAPI() {
            if ($('#NlpChatbot_EnableWebAPI').is(':checked')) {
                $('#ChatPal_EnableWebHook').attr('disabled', false).attr('readonly', false);
            } else {
                $('#ChatPal_EnableWebHook').attr('disabled', true).attr('readonly', true).prop("checked", false);
            }
        }

        $("#NlpChatbot_EnableWebAPI").on("click", function () {
            updateWebAPI();
        });

        updateWebAPI();

    };
})(jQuery);