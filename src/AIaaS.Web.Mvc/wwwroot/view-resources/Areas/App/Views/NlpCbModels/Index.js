(function () {
    $(function () {

        const TrainingStatus_RequireRetraining = 10;
        const TrainingStatus_Queueing = 100;
        const TrainingStatus_Training = 200;
        const TrainingStatus_Trained = 1000;
        const TrainingStatus_Cancelled = 2000;
        const TrainingStatus_Failed = 2001;

        var _$nlpCbModelsTable = $('#NlpCbModelsTable');
        var _nlpCbModelsService = abp.services.app.nlpCbModels;
        var _nlpChatbotsService = abp.services.app.nlpChatbots;

        var currentTrainingStatus = 0;
        setInterval(checkTrainingStatus, 3000);

        //$('.date-picker').datetimepicker({
        //    locale: abp.localization.currentLanguage.name,
        //    format: 'L'
        //});

        var _permissions = {
            'train': abp.auth.hasPermission('Pages.NlpChatbot.NlpChatbots.Train')
        };

        var _createOrEditModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpCbModels/CreateOrEditModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpCbModels/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpCbModelModal'
        });

        //var getDateFilter = function (element) {
        //    if (element.data("DateTimePicker").date() == null) {
        //        return null;
        //    }
        //    return element.data("DateTimePicker").date().format("YYYY-MM-DDT00:00:00Z");
        //}

        var dataTable = _$nlpCbModelsTable.DataTable({
            paging: true,
            serverSide: true,
            processing: true,
            //lengthMenu: [10, 50, 100],
            listAction: {
                ajaxFunction: _nlpCbModelsService.getAll,
                inputFilter: function () {
                    var chatbotIdVal = $('#ChatbotIdId').val();

                    $('#NlpChatbot_Disabled').prop("checked")

                    if (chatbotIdVal == "" || chatbotIdVal == null || chatbotIdVal == undefined) {
                        if ($('#ChatbotSelect').val())
                            $('#ChatbotIdId').val($('#ChatbotSelect').val());
                    }

                    setTrainingChatbotButton();

                    return {
                        nlpChatbotId: $('#ChatbotIdId').val()
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
                    orderable: false,
                    render: function (name, type, row, meta) {
                        return $("<div/>")
                            .addClass("text-left text-nowrap")
                            .append($("<button/>")
                                .addClass("btn btn-sm btn-danger")
                                .addClass(_permissions.train && row.nlpCbModel.nlpCbMStatus >= TrainingStatus_Queueing && row.nlpCbModel.nlpCbMStatus < TrainingStatus_Trained ? "" : "d-none")
                                .attr("data-op", _permissions.train && row.nlpCbModel.nlpCbMStatus >= TrainingStatus_Queueing && row.nlpCbModel.nlpCbMStatus < TrainingStatus_Trained ? "train" : undefined)

                                .prop('disabled', _permissions.train && row.nlpCbModel.nlpCbMStatus >= TrainingStatus_Queueing && row.nlpCbModel.nlpCbMStatus < TrainingStatus_Trained ? false : true)
                                .append($("<i/>")
                                    .addClass("bi bi-stop-circle")).append(app.localize("Cancel"))
                            )[0].outerHTML;
                    },
                },
                {
                    targets: 2,
                    data: "nlpCbModel.nlpCbMStatus",
                    name: "nlpCbMStatus",
                    class: "text-center",
                    render: function (nlpCbMStatus) {
                        if (nlpCbMStatus) {
                            return getTrainingStatusForTable(nlpCbMStatus);
                        }
                        return "";
                    }
                },
                {
                    targets: 3,
                    data: "nlpCbMCreatorUser",
                    name: "nlpCbMCreatorUserFk.name"
                },
                {
                    targets: 4,
                    data: "nlpCbMCreationTime",
                    name: "nlpCbMCreationTime",
                    class: "text-center",
                    render: function (nlpCbMCreationTime) {
                        if (nlpCbMCreationTime) {
                            return $("<div/>")
                                .addClass("text-wrap min-w-50px").html(getDateTime(nlpCbMCreationTime))[0].outerHTML;
                        }
                        return "";
                    }
                },
                {
                    targets: 5,
                    data: "nlpCbModel.nlpCbMTrainingStartTime",
                    name: "nlpCbMTrainingStartTime",
                    class: "text-center",
                    render: function (nlpCbMTrainingStartTime) {
                        if (nlpCbMTrainingStartTime) {
                            return $("<div/>")
                                .addClass("text-wrap min-w-50px").html(getDateTime(nlpCbMTrainingStartTime))[0].outerHTML;
                        }
                        return "";
                    }
                },
                {
                    targets: 6,
                    data: "nlpCbModel.nlpCbMTrainingCompleteTime",
                    name: "nlpCbMTrainingCompleteTime",
                    class: "text-center",
                    render: function (nlpCbMTrainingCompleteTime) {
                        if (nlpCbMTrainingCompleteTime) {
                            //return getDateTime(nlpCbMTrainingCompleteTime);
                            return $("<div/>")
                                .addClass("text-wrap min-w-50px").html(getDateTime(nlpCbMTrainingCompleteTime))[0].outerHTML;
                        }
                        return "";
                    }
                },

                {
                    targets: 7,
                    data: "nlpCbModel.nlpCbAccuracy",
                    name: "nlpCbAccuracy",
                    class: "text-center",
                    render: function (nlpCbAccuracy) {
                        if (nlpCbAccuracy) {
                            return nlpCbAccuracy.toFixed(2);
                        }
                        return "";
                    }
                },
                {
                    targets: 8,
                    data: "nlpCbMTrainingCancellationUser",
                    name: "nlpCbMTrainingCancellationUserFk.name"
                },
                {
                    targets: 9,
                    data: "nlpCbModel.nlpCbMTrainingCancellationTime",
                    name: "nlpCbMTrainingCancellationTime",
                    class: "text-center",
                    render: function (nlpCbMTrainingCancellationTime) {
                        if (nlpCbMTrainingCancellationTime) {
                            //return getDateTime(nlpCbMTrainingCancellationTime);
                            return $("<div/>")
                                .addClass("text-wrap min-w-50px").html(getDateTime(nlpCbMTrainingCancellationTime))[0].outerHTML;
                        }
                        return "";
                    }
                },
            ]
        });


        function setTrainingChatbotButton() {
            var chatbotId = $('#ChatbotIdId').val();

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

                getNlpCbModels();
            }
        }

        function getNlpCbModels() {
            try {
                dataTable.ajax.reload();
            } catch (e) {
                debugger
            }
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

        function getTrainingStatusForTable(code) {
            if (code >= TrainingStatus_Queueing && code < TrainingStatus_Training)
                return app.localize("NlpTrainingStatus_Queueing").replace("\\n", "<br/>");
            else if (code >= TrainingStatus_Training && code < TrainingStatus_Trained)
                return app.localize("NlpTrainingStatus_Training").replace("\\n", "<br/>");
            else if (code == TrainingStatus_Trained)
                return app.localize("NlpTrainingStatus_Trained").replace("\\n", "<br/>");
            else if (code == TrainingStatus_Cancelled)
                return app.localize("NlpTrainingStatus_Cancelled").replace("\\n", "<br/>");
            else if (code == TrainingStatus_Failed)
                return app.localize("NlpTrainingStatus_Failed").replace("\\n", "<br/>")
            else if (code == TrainingStatus_RequireRetraining)
                return app.localize("NlpTrainingStatus_RequireRetraining").replace("\\n", "<br/>");


            return app.localize("NlpTrainingStatus_NotTraining").replace("\\n", "<br/>");
        }


        function stopNlpCbModel(chatbotId) {
            if (_permissions.train == false)
                trturn;

            abp.message.confirm(
                app.localize('NlpChatbotTrainingCancelConfirm'),
                app.localize('AreYouSure'),
                function (isConfirmed) {
                    if (isConfirmed) {
                        _nlpChatbotsService.stopTrainingChatbot(chatbotId).done(function () {
                            //abp.notify.warn(app.localize('NlpChatbotStatusCancel'));
                            getNlpCbModels();
                        });
                    }
                }
            );
        }


        $('#TrainingChatbotButton').click(function () {
            if (_permissions.train == false)
                trturn;

            var chatbotId = $('#ChatbotIdId').val();
            _nlpChatbotsService.getChatbotTrainingStatus(chatbotId).done(function (status) {
                currentTrainingStatus = status.trainingStatus;

                if (status.trainingStatus < TrainingStatus_Queueing || status.trainingStatus == TrainingStatus_Cancelled || status.trainingStatus == TrainingStatus_Trained || status.trainingStatus == TrainingStatus_Failed) {

                    //var msgDiv = $('<div/>').addClass("form-check form-check-inline")
                    //    .append("<input class='form-check-input mt-0' type='checkbox' value='' id='rebuildCheck'>")
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
                                    getNlpCbModels();
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
                                    getNlpCbModels();
                                });
                            }
                        }
                    );
                }
            });
        });

        _$nlpCbModelsTable.on('click', '.btn[data-op="view"]', function (e) {
            _createOrEditModal.open({ id: $(this).data('id') });
        });

        _$nlpCbModelsTable.on('click', '.btn[data-op="train"]', function (e) {
            if (_permissions.train)
                stopNlpCbModel($('#ChatbotIdId').val());
        });


        function getDateTime(dateTime) {
            //if ($(document).width() < 1200)
            //    return moment.utc(dateTime).local().format('L') + '<br/> ' + moment.utc(dateTime).local().format('LTS');
            //else
            /*            return moment.utc(dateTime).local().format('L') + '&nbsp;' + moment.utc(dateTime).local().format('LTS');*/

            return moment.utc(dateTime).local().format('lll');
        }


        $('#ChatbotSelect').change(function () {
            $('#ChatbotIdId').val($('#ChatbotSelect').val());

            getNlpCbModels();
        });

        $('#NlpCbMShowAllCheck').change(function () {
            getNlpCbModels();
        });


        abp.event.on('app.createOrEditNlpCbModelModalSaved', function () {
            getNlpCbModels();
        });

        $('#GetNlpCbModelsButton').click(function (e) {
            e.preventDefault();
            getNlpCbModels();
        });

        $(document).keypress(function (e) {
            if (e.which === 13) {
                getNlpCbModels();
            }
        });
    });
})();