/**
 * CreateOrEditNlpQAModal
 * Handles the creation and editing of NLP QA (Question-Answer) pairs within a modal interface.
 */
(function ($) {
    app.modals.CreateOrEditNlpQAModal = function () {
        const _nlpQAsService = abp.services.app.nlpQAs;

        let _modalManager;
        let _$nlpQAInformationForm = null;

        /**
         * Initializes the modal and sets up form validation.
         * @param {Object} modalManager - The modal manager instance.
         */
        this.init = function (modalManager) {
            _modalManager = modalManager;
            _$nlpQAInformationForm = _modalManager.getModal().find('form[name=NlpQAInformationsForm]');
            _$nlpQAInformationForm.validate();

            const chatbotId = _$nlpQAInformationForm.find("#NlpChatbotId").val();

            // Load categories for the chatbot
            _nlpQAsService.getCaterogies(chatbotId).done((categories) => {
                const $categoryList = $("#ModalCategoryList").empty();
                categories.forEach((category) => {
                    if (category) {
                        $categoryList.append($("<option>").attr('value', category).text(category));
                    }
                });
            });

            // Handle view mode (read-only)
            if ($('#IsViewMode').val() === "1") {
                _$nlpQAInformationForm.find('input, select, textarea').attr('readonly', true).addClass('readonly');
                _$nlpQAInformationForm.find('select, .NlpApproximateQuestion, .NlpApproximateAnswer').prop('disabled', true);
            }
        };

        /**
         * Adds a new question input field.
         * @param {string} value - The initial value of the question input.
         */
        function addNewQuestion(value) {
            // Remove empty question inputs
            $(".NlpApproximateQuestion").filter((_, item) => item.value === "").closest("div.d-flex").remove();

            return $('#NlpQuestionDiv').before(() => {
                return $("<div/>").addClass("questonDiv")
                    .append($("<div/>").addClass("d-flex justify-content-end mb-2")
                        .append($("<div/>").addClass("w-100 me-auto")
                            .append($("<input/>")
                                .addClass("form-control NlpApproximateQuestion")
                                .attr("placeholder", app.localize("InputNewNlpQuestion"))
                                .attr("type", "text")
                                .attr("name", "questions[]")
                                .val(value)
                                .prop('required', true)
                            )
                        )
                        .append($("<div/>").addClass("ms-5 my-auto")
                            .append($("<button/>")
                                .addClass("form-control nlpdeletebutton btn btn-danger btn-sm text-nowrap")
                                .attr("type", "button")
                                .prop('title', app.localize("Delete"))
                                .prepend($("<i/>").addClass("fa fa-minus mx-0"))
                                .append($("<span/>").addClass("d-none d-lg-inline-block").text(app.localize("Delete")))
                                .click((e) => {
                                    e.target.closest("div.d-flex").remove();
                                    if ($(".NlpApproximateQuestion").length === 0) addNewQuestion("");
                                })
                            )
                        )
                    );
            });
        }

        /**
         * Adds a new answer input field.
         * @param {string} value - The initial value of the answer input.
         */
        function addNewAnswer(value) {
            // Remove empty answer inputs
            $(".NlpApproximateAnswer").filter((_, item) => item.value === "").closest("div.d-flex").remove();

            return $('#NlpAnswerDiv').before(() => {
                return $("<div/>").addClass("d-flex justify-content-end mb-2")
                    .append($("<div/>").addClass("input-group flex-grow-1")
                        .append($("<input/>")
                            .addClass("form-control NlpApproximateAnswer")
                            .attr("placeholder", app.localize("InputNewNlpAnswer"))
                            .attr("type", "text")
                            .attr("name", "answers[]")
                            .val(value)
                            .prop('required', true)
                        )
                        .append($("<div/>").addClass("my-auto ms-3")
                            .append($("<label/>").addClass("form-check form-check-custom form-check-solid gpt-check")
                                .append($("<input/>").attr("type", "checkbox").addClass("form-check-input gpt-check-input NlpGptCheck"))
                            )
                        )
                    )
                    .append($("<div/>").addClass("ms-5 my-auto")
                        .append($("<button/>")
                            .addClass("nlpdeletebutton btn btn-danger btn-sm text-nowrap")
                            .attr("type", "button")
                            .prop('title', app.localize("Delete"))
                            .prepend($("<i/>").addClass("fa fa-minus mx-0"))
                            .append($("<span/>").addClass("d-none d-lg-inline-block").text(app.localize("Delete")))
                            .click((e) => {
                                e.target.closest("div.d-flex").remove();
                                if ($(".NlpApproximateAnswer").length === 0) addNewAnswer("");
                            })
                        )
                    );
            });
        }

        /**
         * Saves the QA data by validating and submitting the form.
         */
        this.save = function () {
            // Handle QA type-specific validation
            if (_$nlpQAInformationForm.find("input[name='qaType']").val() === "1") {
                _$nlpQAInformationForm.find(".NlpApproximateQuestion").prop('required', false);
            }

            // Trim all question and answer inputs
            $(".NlpApproximateQuestion, .NlpApproximateAnswer").each((_, item) => {
                $(item).val($(item).val().trim());
            });

            // Remove empty questions and answers
            $(".NlpApproximateQuestion, .NlpApproximateAnswer").filter((_, item) => item.value.trim() === "").closest("div.d-flex").remove();

            // Highlight the first invalid tab if validation fails
            _$nlpQAInformationForm.find('input:invalid').each(function () {
                const $closest = $(this).closest('.tab-pane');
                const id = $closest.attr('id');
                $('.nav a[href="#' + id + '"]').tab('show');
                return false; // Stop after the first invalid input
            });

            if (!_$nlpQAInformationForm.valid()) {
                $(".NlpApproximateQuestion").filter((_, item) => item.value.trim() === "").addClass("is-invalid");
                return;
            }

            // Collect GPT checkbox values
            const gpts = $(".NlpGptCheck").map((_, checkbox) => $(checkbox).is(":checked")).get();

            // Serialize form data and construct answer sets
            const nlpQA = _$nlpQAInformationForm.serializeFormToObject();
            nlpQA.answerSets = nlpQA.answers.map((answer, index) => ({
                answer,
                gpt: gpts[index]
            }));

            // Remove the original answers array
            delete nlpQA.answers;

            _modalManager.setBusy(true);

            // Submit the QA data
            _nlpQAsService.createOrEdit(nlpQA)
                .done(() => {
                    abp.notify.info(app.localize('SavedSuccessfully'));
                    _modalManager.close();
                    abp.event.trigger('app.createOrEditNlpQAModalSaved');
                })
                .fail(() => {
                    _modalManager.close();
                })
                .always(() => {
                    _modalManager.setBusy(false);
                });
        };

        // Event Handlers
        $('#NlpQA_AddQuestion').click(() => addNewQuestion(""));
        $('#NlpQA_AddAnswer').click(() => addNewAnswer(""));
    };
})(jQuery);