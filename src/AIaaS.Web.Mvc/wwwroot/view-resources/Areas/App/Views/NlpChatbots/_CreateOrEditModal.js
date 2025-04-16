/**
 * CreateOrEditNlpChatbotModal
 * Handles the creation and editing of NLP chatbots within a modal interface.
 */
(function ($) {
    app.modals.CreateOrEditNlpChatbotModal = function () {
        const _nlpChatbotsService = abp.services.app.nlpChatbots;

        let _modalManager;
        let _$nlpChatbotInformationForm = null;

        /**
         * Initializes the modal and sets up form validation.
         * @param {Object} modalManager - The modal manager instance.
         */
        this.init = function (modalManager) {
            _modalManager = modalManager;
            _$nlpChatbotInformationForm = _modalManager.getModal().find('form[name=NlpChatbotInformationsForm]');
            _$nlpChatbotInformationForm.validate();

            // Set form fields to readonly if in view mode
            if (_$nlpChatbotInformationForm.find('#IsViewMode').val() === "1") {
                _$nlpChatbotInformationForm.find('input, select, textarea').attr('readonly', true).addClass('readonly');
                _$nlpChatbotInformationForm.find('input:checkbox').attr('disabled', true);
                _$nlpChatbotInformationForm.find('textarea').addClass('bg-light');
            }
        };

        /**
         * Saves the chatbot by validating the form and submitting the data.
         */
        this.save = function () {
            // Highlight the first invalid tab if validation fails
            _$nlpChatbotInformationForm.find('input:invalid').each(function () {
                const $closest = $(this).closest('.tab-pane');
                const id = $closest.attr('id');
                $('.nav a[href="#' + id + '"]').tab('show');
                return false; // Stop after the first invalid input
            });

            $.validator.setDefaults({ ignore: [] });

            if (!_$nlpChatbotInformationForm.valid()) {
                return;
            }

            createOrEdit();
        };

        /**
         * Handles chatbot image selection and preview.
         */
        $('.nlpchatbotimg').click(function (event) {
            $('#NlpChatbot_ImageFileObj').val("");
            $('#NlpChatbot_ImageFile').val($(event.target).data("filename"));
            $('#ChatbotImage').attr('src', event.target.src);
        });

        /**
         * Validates and previews the uploaded chatbot image.
         */
        $('#NlpChatbot_ImageFileObj').change(function (input) {
            const file = input.target.files[0];
            const fileInput = $('#NlpChatbot_ImageFileObj');

            if (file) {
                const type = '|' + file.type.slice(file.type.lastIndexOf('/') + 1) + '|';
                if ('|jpg|jpeg|png|gif|'.indexOf(type) === -1 || file.size > 102400) { // 100KB limit
                    abp.message.warn(app.localize('BotPicture_Warn_SizeLimit'));
                    fileInput.val("");
                    return false;
                }

                const reader = new FileReader();
                reader.onload = (e) => $('#ChatbotImage').attr('src', e.target.result);
                reader.readAsDataURL(file);
            }
        });

        /**
         * Submits the form data to create or edit the chatbot.
         */
        function createOrEdit() {
            const file = $('#NlpChatbot_ImageFileObj')[0].files[0];
            if (file) {
                const type = '|' + file.type.slice(file.type.lastIndexOf('/') + 1) + '|';
                if ('|jpg|jpeg|png|gif|'.indexOf(type) === -1 || file.size > 102400) { // 100KB limit
                    abp.message.warn(app.localize('BotPicture_Warn_SizeLimit'));
                    $('#NlpChatbot_ImageFileObj').val("");
                    return false;
                }
            }

            const dto = _$nlpChatbotInformationForm.serializeFormToObject();
            dto["disabled"] = $('#NlpChatbot_Disabled').prop("checked");
            dto["enableFacebook"] = $('#NlpChatbot_EnableFacebook').prop("checked");
            dto["enableWebAPI"] = $('#NlpChatbot_EnableWebAPI').prop("checked");
            dto["enableLine"] = $('#NlpChatbot_EnableLine').prop("checked");
            dto["enableWebChat"] = $('#NlpChatbot_EnableWebChat').prop("checked");
            dto["enableWebHook"] = $('#ChatPal_EnableWebHook').prop("checked");

            const formData = new FormData();
            Object.keys(dto).forEach((key) => formData.append(key, dto[key]));
            if (file) formData.append("file", file);

            _modalManager.setBusy(true);

            abp.ajax({
                url: abp.appPath + 'App/NlpChatbots/CreateOrEdit',
                type: 'POST',
                data: formData,
                async: false,
                cache: false,
                contentType: false,
                processData: false
            }).done(() => {
                abp.notify.info(app.localize('SavedSuccessfully'));
                _modalManager.close();
                abp.event.trigger('app.createOrEditNlpChatbotModalSaved');
            }).always(() => {
                _modalManager.setBusy(false);
            });
        }

        /**
         * Resets threshold values to their defaults.
         */
        $('#ResetThreshold').click(() => {
            $('#PredThreshold').val($('input#defaultPredThreshold').val());
            $('#WSPredThreshold').val($('input#defaultWSPredThreshold').val());
            $('#SuggestionThreshold').val($('input#defaultSuggestionThreshold').val());
        });

        /**
         * Updates GPT options visibility based on the selected value.
         */
        function updateGPTOptions() {
            if ($('#GPTOptionsList').val() === "2") {
                $('#GPTOptionsArea').show();
                $('#OPENAI_APIKEY').prop('required', true);
            } else {
                $('#GPTOptionsArea').hide();
                $('#OPENAI_APIKEY').removeAttr('required');
            }
        }

        $('#GPTOptionsList').change(updateGPTOptions);
        updateGPTOptions();

        /**
         * Updates WebAPI-related options based on the WebAPI checkbox state.
         */
        function updateWebAPI() {
            const isEnabled = $('#NlpChatbot_EnableWebAPI').is(':checked');
            $('#ChatPal_EnableWebHook').attr('disabled', !isEnabled).attr('readonly', !isEnabled).prop("checked", isEnabled);
        }

        $('#NlpChatbot_EnableWebAPI').click(updateWebAPI);
        updateWebAPI();
    };
})(jQuery);