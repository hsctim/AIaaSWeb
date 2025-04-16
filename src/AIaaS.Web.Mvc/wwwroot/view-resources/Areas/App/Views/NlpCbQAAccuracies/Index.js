(function () {
    $(function () {

        const TrainingStatus_RequireRetraining = 10;
        const TrainingStatus_Queueing = 100;
        const TrainingStatus_Training = 200;
        const TrainingStatus_Trained = 1000;
        const TrainingStatus_Cancelled = 2000;
        const TrainingStatus_Failed = 2001;

        var _$nlpCbQAAccuraciesTable = $('#NlpCbQAAccuraciesTable');
        var _nlpCbQAAccuraciesService = abp.services.app.nlpCbQAAccuracies;
        var _nlpChatbotsService = abp.services.app.nlpChatbots;
        var _nlpQAsService = abp.services.app.nlpQAs;

        var currentTrainingStatus = 0;
        setInterval(checkTrainingStatus, 3000);

        var _permissions = {
            create: abp.auth.hasPermission('Pages.NlpCbQAAccuracies.Create'),
            edit: abp.auth.hasPermission('Pages.NlpCbQAAccuracies.Edit'),
            'delete': abp.auth.hasPermission('Pages.NlpCbQAAccuracies.Delete'),
            'editQA': abp.auth.hasPermission('Pages.NlpChatbot.NlpQAs.Edit'),
            train: abp.auth.hasPermission('Pages.NlpChatbot.NlpChatbots.Train')
        };

        var _createOrEditModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpCbQAAccuracies/CreateOrEditModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpCbQAAccuracies/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpCbQAAccuracyModal'
        });

        var _createOrEditQAModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpQAs/CreateOrEditModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpQAs/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpQAModal'
        });

        var _DiscardModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpQAs/DiscardNlpQAModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpQAs/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpQAModal'
        });


        function pad(num, size) {
            num = num.toString();
            while (num.length < size) num = "0" + num;
            return num;
        }


        //$('.date-picker').datetimepicker({
        //    locale: abp.localization.currentLanguage.name,
        //    format: 'L',
        //    timeZone: moment.tz.guess()
        //});

        //$('#MinNlpCreationTimeFilterId').data("DateTimePicker").date(moment().subtract(7, 'days'));
        //$('#MaxNlpCreationTimeFilterId').data("DateTimePicker").date(moment());

        //var getDateFilter = function (element) {
        //    if (element.data("DateTimePicker").date() == null) {
        //        return null;
        //    }

        //    var offset = element.data("DateTimePicker").date().utcOffset() / 60 * 100;

        //    if (offset >= 0)
        //        return element.data("DateTimePicker").date().format("YYYY-MM-DDT00:00:00+") + pad(offset, 4);
        //    else
        //        return element.data("DateTimePicker").date().format("YYYY-MM-DDT00:00:00-") + pad(-offset, 4);
        //}

        //var getMaxDateFilter = function (element) {
        //    if (element.data("DateTimePicker").date() == null) {
        //        return null;
        //    }

        //    var offset = element.data("DateTimePicker").date().utcOffset() / 60 * 100;

        //    if (offset >= 0)
        //        return element.data("DateTimePicker").date().format("YYYY-MM-DDT23:59:59.999+") + pad(offset, 4);
        //    else
        //        return element.data("DateTimePicker").date().format("YYYY-MM-DDT23:59:59.999-") + pad(-offset, 4);
        //}


        var $selectedDate = {
            startDate: moment().subtract(7, 'days'),
            endDate: moment(),
        };


        $('.date-picker').on('apply.daterangepicker', function (ev, picker) {
            $(this).val(picker.startDate.format('MM/DD/YYYY'));
        });

        $('#MinNlpCreationTimeFilterId')
            .daterangepicker({
                autoUpdateInput: false,
                singleDatePicker: true,
                locale: abp.localization.currentLanguage.name,
                format: 'L',
            })
            .val($selectedDate.startDate.format('MM/DD/YYYY'))
            .on('apply.daterangepicker', (ev, picker) => {
                $selectedDate.startDate = picker.startDate;
                getNlpCbQAAccuracies();
            });

        $('#MaxNlpCreationTimeFilterId')
            .daterangepicker({
                autoUpdateInput: false,
                singleDatePicker: true,
                locale: abp.localization.currentLanguage.name,
                format: 'L',
            })
            .val($selectedDate.endDate.format('MM/DD/YYYY'))
            .on('apply.daterangepicker', (ev, picker) => {
                $selectedDate.endDate = picker.startDate;
                getNlpCbQAAccuracies();
            });

        var getDateFilter = function (element) {
            if ($selectedDate.startDate == null) {
                return null;
            }
            return $selectedDate.startDate.format('YYYY-MM-DDT00:00:00Z');
        };

        var getMaxDateFilter = function (element) {
            if ($selectedDate.endDate == null) {
                return null;
            }
            return $selectedDate.endDate.format('YYYY-MM-DDT23:59:59Z');
        };


        var dataTable = _$nlpCbQAAccuraciesTable.DataTable({
            paging: true,
            serverSide: true,
            processing: true,
            //lengthMenu: [10, 50, 100],
            listAction: {
                ajaxFunction: _nlpCbQAAccuraciesService.getAll,
                inputFilter: function () {
                    setTrainingChatbotButton();
                    return {
                        filter: $('#NlpCbQAAccuraciesTableFilter').val(),
                        minNlpCreationTimeFilter: getDateFilter($('#MinNlpCreationTimeFilterId')),
                        maxNlpCreationTimeFilter: getMaxDateFilter($('#MaxNlpCreationTimeFilterId')),
                        nlpChatbotId: $('#ChatbotSelect').val()
                    };
                }
            },
            columnDefs: [
                {
                    className: 'control responsive',
                    orderable: false,
                    render: function () {
                        return '';
                    },
                    targets: 0
                },
                {
                    targets: 1,
                    data: "nlpCbQAAccuracy.creationTime",
                    name: "creationTime",
                    class: "text-center",
                    render: function (creationTime) {
                        if (creationTime) {
                            //return getDateTime(creationTime);

                            return $("<div/>")
                                .addClass("text-wrap min-w-50px").html(getDateTime(creationTime))[0].outerHTML;

                        }
                        return "";
                    }
                },
                {
                    targets: 2,
                    //data: "nlpCbQAAccuracy.question",
                    name: "question",
                    render: function (data, type, row, meta) {
                        return $("<div/>")
                            .addClass("text-wrap min-w-50px")
                            .append($("<p/>").addClass("text-with-truncation")
                                .append(row.nlpCbQAAccuracy.question))[0].outerHTML;
                    },
                },
                {
                    targets: 3,
                    data: null,
                    name: "answer",
                    orderable: false,
                    render: function (data, type, row, meta) {
                        var $container = $("<span/>");
                        var $table = $('<table/>').addClass('display table dt-responsive');
                        var metaRow = meta.row;
                        var subRow = 0;

                        for (qa of data.nlpCbQAAccuracy.answerPredict) {
                            var even = (metaRow + subRow) % 2 == 0;

                            var same = false;
                            for (q of qa.question) {
                                if (data.nlpCbQAAccuracy.question.trim().toUpperCase() == q.trim().toUpperCase())
                                    same = true;
                            }

                            var Acc = $('<span/>').addClass(qa.answerAcc >= 0.5 ? "text-success" : "").addClass(qa.answerAcc < 0.5 ? "text-danger" : "").append((qa.answerAcc * 100).toFixed(2) + "%").prop('title', app.localize('NlpCbPredictability'));

                            var ulq = $('<ul/>').addClass("my-0").attr("title", app.localize("NlpCbQuestion"));
                            for (q of qa.question) {
                                ulq.append($("<li/>").width($(document).width() / 8)
                                    .addClass("text-wrap min-w-50px")
                                    .append($("<p/>").addClass("text-with-truncation").append(q)));
                            }

                            var ula = $('<ul/>').addClass("my-0").attr("title", app.localize("NlpCbAnswer"));
                            for (a of qa.answer) {
                                ula.append($("<li/>").width($(document).width() / 6)
                                    .addClass("text-wrap min-w-50px")
                                    .append($("<p/>").addClass("text-with-truncation")
                                        .append(function () {
                                            if (a.gpt == true)
                                                return $("<img src='/Common/Images/chatgpt-icon.png' class= 'chatgpt-icon me-2' ></span > ");
                                            else
                                                return $("");
                                        }).append(a.answer)));
                            }

                            var addButton = $("<button/>")
                                .addClass("btn btn-success btn-sm btn-icon shadow-none")
                                .addClass(same || _permissions.editQA == false ? "d-none" : "")
                                .prop('disabled', same)
                                .attr("title", app.localize("NlpCbAddQA"))
                                .attr("data-question", data.nlpCbQAAccuracy.question)
                                .attr("data-qaid", qa.answerId)
                                .attr("data-op", 'addQuestion')
                                .append($("<i/>")
                                    .addClass("fas fa-plus"));

                            var addButton2 = $("<button/>")
                                .addClass("btn btn-primary btn-sm btn-icon shadow-none")
                                .addClass(_permissions.editQA == false ? "d-none" : "")
                                .attr("title", app.localize("EditNlpQA"))
                                .attr("data-qaid", data.nlpCbQAAccuracy.answerId)
                                .attr("data-qaid", qa.answerId)
                                .attr("data-op", 'editQA')
                                .append($("<i/>")
                                    .addClass("la la-edit"));

                            ulq = $('<div/>').append(ulq);
                            ula = $('<div/>').append(ula);

                            $table.append($('<tr/>').addClass(even ? "odd" : "even")
                                .append($('<td/>').addClass("w-30px text-center").append(Acc)
                                    .append($('<div/>').append(addButton).append(addButton2)))
                                .append($('<td/>').append(ulq)).append($('<td/>').append(ula)));

                            subRow++;
                        }

                        $container.append($table);
                        return $container[0].innerHTML;
                    }
                },
            ]
        });

        _$nlpCbQAAccuraciesTable.on('click', '.btn[data-op="addQuestion"]', function (e) {
            var dataQuestion = $(this).data('question');
            var dataQaId = $(this).data('qaid');

            abp.message.confirm(
                app.localize('NlpCbAddQAConfirm'),
                app.localize('AreYouSure'),
                function (isConfirmed) {
                    if (isConfirmed) {
                        _nlpQAsService.creaetQAForAccuracy({
                            nlpQuestion: dataQuestion,
                            nlpQAId: dataQaId,
                        }).done(function () {
                            getNlpCbQAAccuracies();
                            setTrainingChatbotButton();
                        });
                    }
                }
            );
        });

        _$nlpCbQAAccuraciesTable.on('click', '.btn[data-op="editQA"]', function (e) {
            _createOrEditQAModal.open({
                id: $(this).data('qaid'),
                chatbotId: $('#ChatbotSelect').val()
            })
        });

        function setTrainingChatbotButton() {
            var chatbotId = $('#ChatbotSelect').val();

            if (chatbotId == "" || chatbotId == null || chatbotId == undefined) {
                $('#TrainingChatbotDropDown').addClass("d-none");
                return;
            }

            _nlpChatbotsService.getChatbotTrainingStatus(chatbotId).done(function (status) {
                currentTrainingStatus = status.trainingStatus;

                $('#TrainingIcon').removeClass("fa-spinner fa-cog fa-flask fa-spin p-0").addClass("fas")
                    .addClass(function () {
                        if (status.trainingStatus >= TrainingStatus_Queueing && status.trainingStatus < TrainingStatus_Training)
                            return "fa-spinner fa-spin p-0";
                        if (status.trainingStatus >= TrainingStatus_Training && status.trainingStatus < TrainingStatus_Trained)
                            return "fa-cog fa-spin p-0";
                        else
                            return "fa-flask";
                    });

                $('#TrainingChatbotDropDown').removeClass("d-none btn-light-info btn-light-success btn-light-danger")
                    .attr("title", getTrainingStatus(status).replaceAll("<br/>", " "))
                    .addClass(function () {
                        if (status.trainingStatus >= TrainingStatus_Queueing && status.trainingStatus < TrainingStatus_Trained)
                            return "btn-light-info";
                        else if (status.trainingStatus == TrainingStatus_Trained)
                            return "btn-light-success";
                        else
                            return "btn-light-danger";
                    });

                $('#TrainingCbStatus').html(getTrainingStatus(status));

                $('#TrainingChatbotButton').html(function () {
                    if (status.trainingStatus >= TrainingStatus_Queueing && status.trainingStatus < TrainingStatus_Trained)
                        return app.localize('NlpTraining_Cancel');

                    if (status.trainingStatus == TrainingStatus_Trained)
                        return app.localize('NlpTraining_Restart');

                    return app.localize('NlpTraining_Start');
                });
            });
        }

        var oldTrainingStatus = 0;
        function checkTrainingStatus() {
            if (currentTrainingStatus == TrainingStatus_Queueing || currentTrainingStatus == TrainingStatus_Training || oldTrainingStatus == TrainingStatus_Queueing || oldTrainingStatus == TrainingStatus_Training) {
                setTrainingChatbotButton();
                if (oldTrainingStatus == currentTrainingStatus)
                    return;
                else
                    oldTrainingStatus = currentTrainingStatus;
            }
        }

        function getNlpCbQAAccuracies() {
            dataTable.ajax.reload();
        }

        function timeSpanString(seconds) {
            const zeroPad = (num) => String(num).padStart(2, '0')

            var days = Math.floor(seconds / (60 * 60 * 24));
            seconds -= days * (60 * 60 * 24);

            var hours = Math.floor(seconds / (60 * 60));
            seconds -= hours * (60 * 60);

            var mins = Math.floor(seconds / (60));

            seconds -= mins * (60);

            if (days == 0)
                return (zeroPad(hours) + ":" + zeroPad(mins) + ":" + zeroPad(seconds));
            else
                return (days + " " + zeroPad(hours) + ":" + zeroPad(mins) + ":" + zeroPad(seconds));
        }


        function getTrainingStatus(status) {

            if (status.trainingStatus >= TrainingStatus_Queueing && status.trainingStatus < TrainingStatus_Training) {
                if (status.trainingRemaining == 0 && status.queueRemaining == 0)
                    return app.localize("NlpTrainingStatus_Queueing").replaceAll("\\n", "<br/>");
                else if (status.trainingRemaining == 0)
                    return app.localize("NlpTrainingStatus_Queueing_QueueingProgress", timeSpanString(status.queueRemaining)).replaceAll("\\n", "<br/>");
                else if (status.queueRemaining == 0)
                    return app.localize("NlpTrainingStatus_Queueing_TrainingProgress", timeSpanString(status.trainingRemaining)).replaceAll("\\n", "<br/>");
                else
                    return app.localize("NlpTrainingStatus_Queueing_Progress", timeSpanString(status.queueRemaining), timeSpanString(status.trainingRemaining)).replaceAll("\\n", "<br/>");
            }
            else if (status.trainingStatus >= TrainingStatus_Training && status.trainingStatus < TrainingStatus_Trained) {
                if (status.trainingProgress > 0 && status.trainingRemaining == 0)
                    return app.localize("NlpTrainingStatus_Training_Progress", status.trainingProgress).replaceAll("\\n", "<br/>");
                else if (status.trainingRemaining == 0)
                    return app.localize("NlpTrainingStatus_Training").replaceAll("\\n", "<br/>");
                else
                    return app.localize("NlpTrainingStatus_Training_Remaining", status.trainingProgress, timeSpanString(status.trainingRemaining)).replaceAll("\\n", "<br/>");
            }
            else if (status.trainingStatus == TrainingStatus_Trained)
                return app.localize("NlpTrainingStatus_Trained").replace("\\n", "<br/>");
            else if (status.trainingStatus == TrainingStatus_Cancelled)
                return app.localize("NlpTrainingStatus_Cancelled").replace("\\n", "<br/>");
            else if (status.trainingStatus == TrainingStatus_Failed)
                return app.localize("NlpTrainingStatus_Failed").replace("\\n", "<br/>");
            else if (status.trainingStatus == TrainingStatus_RequireRetraining)
                return app.localize("NlpTrainingStatus_RequireRetraining").replace("\\n", "<br/>");

            return app.localize("NlpTrainingStatus_NotTraining").replace("\\n", "<br/>");
        }

        function stopNlpCbModel(chatbotId) {
            if (_permissions.train == false)
                return;

            abp.message.confirm(
                app.localize('NlpChatbotTrainingCancelConfirm'),
                app.localize('AreYouSure'),
                function (isConfirmed) {
                    if (isConfirmed) {
                        _nlpChatbotsService.stopTrainingChatbot(chatbotId).done(function () {
                            //abp.notify.warn(app.localize('NlpChatbotStatusCancel'));
                            dataTable.ajax.reload();
                        });
                    }
                }
            );
        }

        function getDateTime(dateTime) {
            return moment.utc(dateTime).local().format('L') + '<br/> ' + moment.utc(dateTime).local().format('LTS');
        }

        function deleteNlpCbQAAccuracy(nlpCbQAAccuracy) {
            abp.message.confirm(
                '',
                app.localize('AreYouSure'),
                function (isConfirmed) {
                    if (isConfirmed) {
                        _nlpCbQAAccuraciesService.delete({
                            id: nlpCbQAAccuracy.id
                        }).done(function () {
                            getNlpCbQAAccuracies(true);
                            abp.notify.success(app.localize('SuccessfullyDeleted'));
                        });
                    }
                }
            );
        }

        $('#ShowAdvancedFiltersSpan').click(function () {
            $('#ShowAdvancedFiltersSpan').hide();
            $('#HideAdvancedFiltersSpan').show();
            $('#AdvacedAuditFiltersArea').slideDown();
        });

        $('#HideAdvancedFiltersSpan').click(function () {
            $('#HideAdvancedFiltersSpan').hide();
            $('#ShowAdvancedFiltersSpan').show();
            $('#AdvacedAuditFiltersArea').slideUp();
        });

        $('#CreateNewNlpCbQAAccuracyButton').click(function () {
            _createOrEditModal.open();
        });

        abp.event.on('app.createOrEditNlpCbQAAccuracyModalSaved', function () {
            getNlpCbQAAccuracies();
        });

        abp.event.on('app.createOrEditNlpQAModalSaved', function () {
            getNlpCbQAAccuracies();
        });

        $('#GetNlpCbQAAccuraciesButton').click(function (e) {
            e.preventDefault();
            getNlpCbQAAccuracies();
        });

        $(document).keypress(function (e) {
            if (e.which === 13) {
                getNlpCbQAAccuracies();
            }
        });


        $('#TrainingChatbotButton').click(function () {
            if (_permissions.train == false)
                return;

            var chatbotId = $('#ChatbotSelect').val();
            _nlpChatbotsService.getChatbotTrainingStatus(chatbotId).done(function (status) {
                currentTrainingStatus = status.trainingStatus

                if (status.trainingStatus < TrainingStatus_Queueing || status.trainingStatus == TrainingStatus_Cancelled || status.trainingStatus == TrainingStatus_Trained || status.trainingStatus == TrainingStatus_Failed) {

                    //var msgDiv = $('<div/>').addClass("form-check form-check-inline")
                    //    .append("<input class='form-check-input-light mt-0' type='checkbox' value='' id='rebuildCheck'>")
                    //    .append($('<label/>').addClass("swal-text text-start").prop("for", "rebuildCheck")
                    //        .append(app.localize("NlpTrainingRebuildModel")))
                    //    .prop("outerHTML");

                    abp.message.confirm(
                        '', app.localize('NlpChatbotTrainingConfirm'),
                        function (isConfirmed) {
                            if (isConfirmed) {
                                //var rebuild = false;
                                //if ($('#rebuildCheck').prop("checked"))
                                //    rebuild = true;

                                _nlpChatbotsService.trainChatbot(chatbotId, true).done(function () {
                                    //abp.notify.info(app.localize('NlpCbMInfoQueueing'));
                                    setTrainingChatbotButton();
                                });
                            }
                        },
                        {
                            isHtml: true,
                        }
                    );
                }
                else if (status.trainingStatus >= TrainingStatus_Queueing && status.trainingStatus < TrainingStatus_Trained) {
                    abp.message.confirm(
                        app.localize('NlpChatbotTrainingCancelConfirm'),
                        app.localize('AreYouSure'),
                        function (isConfirmed) {
                            if (isConfirmed) {
                                _nlpChatbotsService.stopTrainingChatbot(chatbotId).done(function () {
                                    //abp.notify.warn(app.localize('NlpCbMInfoCancelled'));
                                    setTrainingChatbotButton();
                                });
                            }
                        }
                    );
                }
            });
        });

        $('#UnanswerableNlpQAButton').click(function () {
            var chatbotSelVal = $('#ChatbotSelect').val();

            if (chatbotSelVal) {
                _DiscardModal.open({
                    chatbotId: chatbotSelVal
                });
            }
            else {
                abp.message.error(app.localize('SelectOneChatbot'));
            }

        });
        $('#ChatbotSelect').change(function () {
            getNlpCbQAAccuracies();
        });

    });
})();
