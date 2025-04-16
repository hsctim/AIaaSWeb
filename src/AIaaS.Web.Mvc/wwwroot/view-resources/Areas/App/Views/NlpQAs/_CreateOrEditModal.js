(function ($) {
    app.modals.CreateOrEditNlpQAModal = function () {
        var _nlpQAsService = abp.services.app.nlpQAs;

        var _modalManager;
        var _$nlpQAInformationForm = null;

        this.init = function (modalManager) {
            _modalManager = modalManager;

            var modal = _modalManager.getModal();

            _$nlpQAInformationForm = _modalManager.getModal().find('form[name=NlpQAInformationsForm]');
            _$nlpQAInformationForm.validate();

            var chatbtotId = _$nlpQAInformationForm.find("#NlpChatbotId").val();

            _nlpQAsService.getCaterogies(chatbtotId).done(function (json) {
                $("#ModalCategoryList").empty();

                $.each(json, function (i, item) {
                    if (item)
                        $("#ModalCategoryList").append($("<option>").attr('value', item).text(item));
                });
            });


            var editPermission = abp.auth.hasPermission('Pages.NlpChatbot.NlpQAs.Edit');

            /*            updateWorkflowSelect();*/

            if ($('#IsViewMode').val() == "1") {
                $('#NlpQA_QuestionCategory').attr('readonly', true);
                $('.NlpApproximateQuestion').attr('readonly', true);
                $('.NlpApproximateAnswer').attr('readonly', true);
                _$nlpQAInformationForm.find('select').prop('disabled', 'disabled');
                _$nlpQAInformationForm.find('check').prop('disabled', 'disabled');
            }
        };

        $('.nlpdeletebutton').click(function (e) {
            $(this).closest("div.questonDiv").remove();

            if ($(".NlpApproximateQuestion").length == 0)
                addNewQuestion("");

            if ($(".NlpApproximateAnswer").length == 0)
                addNewAnswer("");
        });

        $('#NlpQA_AddQuestion').click(function (e) {
            addNewQuestion("");
        });

        $('#NlpQA_AddAnswer').click(function (e) {
            addNewAnswer("");
        });

        function addNewQuestion(value) {
            var questions = $(".NlpApproximateQuestion");

            $.each(questions, function (index, item) {
                if (item.value === "") {
                    $(item).closest("div.d-flex").remove();
                }
            });

            return $('#NlpQuestionDiv').before(function () {
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
                                .prepend($("<i/>")
                                    .addClass("fa fa-minus mx-0"))
                                .append($("<span/>")
                                    .addClass("d-none d-lg-inline-block")
                                    .text(app.localize("Delete"))
                                )
                                .click(function (e) {
                                    e.target.closest("div.d-flex").remove();
                                    if ($(".NlpApproximateQuestion").length == 0)
                                        addNewQuestion("");

                                })
                            )
                        )
                    );
            });
        }

        function addNewAnswer(value) {
            var answers = $(".NlpApproximateAnswer");

            $.each(answers, function (index, item) {
                if (item.value === "") {
                    $(item).closest("div.d-flex").remove();
                }
            });

            return $('#NlpAnswerDiv').before(function () {
                return $("<div/>")
                    .addClass("d-flex justify-content-end mb-2")

                    .append(
                        $("<div/>")
                        .addClass("input-group flex-grow-1")

                        .append($("<input/>")
                            .addClass("form-control NlpApproximateAnswer")
                            .attr("placeholder", app.localize("InputNewNlpAnswer"))
                            .attr("type", "text")
                            .attr("name", "answers[]")
                            .val(value)
                            .prop('required', true)
                        )

                        .append(
                            $("<div class='my-auto ms-3'><label class='form-check form-check-custom form-check-solid gpt-check'><input type='checkbox' name='gpts[]' class='form-check-input gpt-check-input NlpGptCheck'></label></div>")
                        )


                    )
                    .append($("<div/>").addClass("ms-5 my-auto")
                        .append($("<button/>")
                            .addClass("nlpdeletebutton btn btn-danger btn-sm text-nowrap")
                            .attr("type", "button")
                            .prop('title', app.localize("Delete"))
                            .prepend($("<i/>")
                                .addClass("fa fa-minus mx-0")
                            )
                            .append($("<span/>")
                                .addClass("d-none d-lg-inline-block")
                                .text(app.localize("Delete"))
                            )
                            .click(function (e) {
                                e.target.closest("div.d-flex").remove();

                                if ($(".NlpApproximateAnswer").length == 0)
                                    addNewAnswer("");
                            })
                        )
                    );
            });
        }

        this.save = function () {

            if (_$nlpQAInformationForm.find("input[name='qaType']").val() == "1") {
                _$nlpQAInformationForm.find(".NlpApproximateQuestion").prop('required', false);
            }



            $.each($(".NlpApproximateQuestion"), function (index, item) {
                $(item).val($(item).val().trim());
            });

            $.each($(".NlpApproximateAnswer"), function (index, item) {
                $(item).val($(item).val().trim());
            });

            if ($(".NlpApproximateQuestion").length > 1) {
                $.each($(".NlpApproximateQuestion"), function (index, item) {
                    if (item.value.trim() === "") {
                        $(item).closest("div.d-flex").remove();
                    }
                });
            }

            if ($(".NlpApproximateAnswer").length > 1) {
                $.each($(".NlpApproximateAnswer"), function (index, item) {
                    if (item.value.trim() === "") {
                        $(item).closest("div.d-flex").remove();
                    }
                });
            }

            _$nlpQAInformationForm.find('input:invalid').each(function () {
                // Find the tab-pane that this element is inside, and get the id
                var $closest = $(this).closest('.tab-pane');
                var id = $closest.attr('id');
                // Find the link that corresponds to the pane and have it show
                $('.nav a[href="#' + id + '"]').tab('show');
                // Only want to do it once
                return false;
            });


            if (!_$nlpQAInformationForm.valid()) {
                $.each($(".NlpApproximateQuestion"), function (index, item) {
                    if (item.value.trim() === "") {
                        $(item).addClass("is-invalid");
                    }
                });
                return;
            }

            //get all checked gpt ids   
            var gpts = $(".NlpGptCheck").map(function () {
                return $(this).is(":checked") ? true : false;
            }).get();
            
            var nlpQA = _$nlpQAInformationForm.serializeFormToObject();

            let answerSets = []
            for (let i = 0; i < nlpQA.answers.length; i++) {
                let answerSet = {
                    answer: nlpQA.answers[i],
                    gpt: gpts[i]
                }
                answerSets.push(answerSet)          
            }
            nlpQA.answerSets = answerSets;

            //delete answers from nlpQA
            delete nlpQA.answers;

            _modalManager.setBusy(true);

            _nlpQAsService.createOrEdit(
                nlpQA
            ).done(function () {
                abp.notify.info(app.localize('SavedSuccessfully'));
                _modalManager.close();
                abp.event.trigger('app.createOrEditNlpQAModalSaved');
            }).fail(function () {
                _modalManager.close();
            }).always(function () {
                _modalManager.setBusy(false);
            });
        };


        //function updateWorkflowSelect() {
        //    if ($('#enabledWorkflow').is(':checked')) {
        //        $('#currentWfState').prop('disabled', false);
        //        $('#nextWfState').prop('disabled', false);
        //    } else {
        //        $('#currentWfState').prop('disabled', 'disabled');
        //        $('#nextWfState').prop('disabled', 'disabled');
        //    }
        //}

        //$("#enabledWorkflow").on("click", function () {
        //    updateWorkflowSelect();
        //});

    };
})(jQuery);
