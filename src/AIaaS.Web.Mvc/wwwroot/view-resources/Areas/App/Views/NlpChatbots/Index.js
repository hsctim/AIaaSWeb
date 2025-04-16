(function () {
    $(function () {

        const TrainingStatus_RequireRetraining = 10;
        const TrainingStatus_Queueing = 100;
        const TrainingStatus_Training = 200;
        const TrainingStatus_Trained = 1000;
        const TrainingStatus_Cancelled = 2000;
        const TrainingStatus_Failed = 2001;

        var needToRefreshTrainingStatus = false;
        setInterval(refreshTrainingStatus, 5000);

        var _$nlpChatbotsTable = $('#NlpChatbotsTable');
        var _nlpChatbotsService = abp.services.app.nlpChatbots;

        var _permissions = {
            create: abp.auth.hasPermission('Pages.NlpChatbot.NlpChatbots.Create'),
            edit: abp.auth.hasPermission('Pages.NlpChatbot.NlpChatbots.Edit'),
            'delete': abp.auth.hasPermission('Pages.NlpChatbot.NlpChatbots.Delete'),
            train: abp.auth.hasPermission('Pages.NlpChatbot.NlpChatbots.Train')
        };

        var _permissionsNlpQAs = abp.auth.hasPermission('Pages.NlpChatbot.NlpQAs');

        var _createOrEditModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpChatbots/CreateOrEditModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpChatbots/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpChatbotModal'
        });

        var _viewNlpChatbotModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpChatbots/ViewNlpChatbotModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpChatbots/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpChatbotModal',
        });

        var _deleteModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpChatbots/DeleteModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpChatbots/_DeleteModal.js',
            modalClass: 'DeleteNlpChatbotModal'
        });

        var _ImportModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpChatbots/ImportModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpChatbots/_ImportModal.js',
            modalClass: 'ImportNlpChatbotModal'
        });

        var _ExportModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpChatbots/ExportModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpChatbots/_ExportModal.js',
            modalClass: 'ExportNlpChatbotModal'
        });

        var dataTable = _$nlpChatbotsTable.DataTable({
            paging: true,
            serverSide: true,
            processing: true,
            //lengthMenu: [10, 50, 100],
            listAction: {
                ajaxFunction: _nlpChatbotsService.getAll,
                inputFilter: function () {
                    return {
                        filter: $('#NlpChatbotsTableFilter').val()
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
                    //width: 120,
                    targets: 1,
                    data: null,
                    orderable: false,
                    //autoWidth: false,
                    defaultContent: '',
                    rowAction: {
                        cssClass: 'btn btn-brand dropdown-toggle',
                        text: '<i class="fa fa-cog"></i> <span class="d-none d-lg-inline-block">' + app.localize('Actions') + '</span> <span class="caret"></span>',
                        items: [
                            {
                                text: app.localize('View'),
                                iconStyle: 'far fa-eye me-2',
                                action: function (data) {
                                    _viewNlpChatbotModal.open({ id: data.record.nlpChatbot.id });
                                }
                            },
                            {
                                text: app.localize('Edit'),
                                iconStyle: 'far fa-edit me-2',
                                visible: function () {
                                    return _permissions.edit;
                                },
                                action: function (data) {
                                    _createOrEditModal.open({ id: data.record.nlpChatbot.id });
                                }
                            },
                            {
                                text: app.localize('Delete'),
                                iconStyle: 'far fa-trash-alt me-2',
                                visible: function () {
                                    return _permissions.delete;
                                },
                                action: function (data) {
                                    _deleteModal.open({ id: data.record.nlpChatbot.id });
                                }
                            },
                            {
                                iconStyle: 'nlp-action-separator separator my-2',
                                visible: function (data) {
                                    return data.record.nlpChatbot.enableWebChat;
                                },
                            },
                            {
                                text: app.localize('OpenWebChatPage'),
                                iconStyle: 'flaticon-browser me-2',
                                visible: function (data) {
                                    return data.record.nlpChatbot.enableWebChat;
                                },
                                action: function (data) {
                                    window.open("/webchat/" + data.record.nlpChatbot.id, "_chatPalWebChat");
                                    //webchat/index.html?chatbotId=" + data.record.nlpChatbot.id
                                }
                            },
                        ]
                    }
                },
                {
                    targets: 2,
                    data: "nlpChatbot.name",
                    name: "name",
                    render: function (chatbotName, type, row, meta) {

                        var $container = $("<div/>").addClass("text-center text-wrap");

                        var profilePictur;

                        if (row.nlpChatbot.chatbotPictureId == null || row.nlpChatbot.chatbotPictureId == undefined) {
                            profilePicture = "/Chatbot/ProfilePicture";
                        }
                        else {
                            profilePicture = "/Chatbot/ProfilePicture/" + row.nlpChatbot.chatbotPictureId;
                        }

                        if (profilePicture) {
                            var $img = $("<img/>")
                                .addClass("img-circle")
                                .attr("src", profilePicture);

                            $container.append($img);
                        }

                        $container.append($("<div/>").addClass('mt-2').text(chatbotName));
                        return $container[0].outerHTML;
                    }
                },
                //{
                //    targets: 3,
                //    data: "nlpChatbot.language",
                //    name: "language"
                //},
                {
                    targets: 3,
                    data: "nlpChatbot.greetingMsg",
                    name: "greetingMsg",
                    render: function (greetingMsg, type, row, meta) {
                        var $container = $("<div/>").addClass("text-wrap min-w-50px");
                        $container.append(greetingMsg);
                        return $container[0].outerHTML;
                    }
                },
                {
                    targets: 4,
                    data: "nlpChatbot.failedMsg",
                    name: "failedMsg",
                    render: function (failedMsg, type, row, meta) {
                        var $container = $("<div/>").addClass("text-wrap min-w-50px");
                        $container.append(failedMsg);
                        return $container[0].outerHTML;
                    }

                },
                {
                    targets: 5,
                    data: "nlpChatbot.alternativeQuestion",
                    name: "alternativeQuestion",
                    render: function (alternativeQuestion, type, row, meta) {
                        var $container = $("<div/>").addClass("text-wrap min-w-50px");
                        $container.append(alternativeQuestion);
                        return $container[0].outerHTML;
                    }
                },
                {
                    targets: 6,
                    data: "trainingStatus",
                    orderable: false,
                    name: "trainingStatus",
                    render: function (trainingStatus, type, row, meta) {
                        return $("<div/>")
                            .attr("data-id", row.nlpChatbot.id)
                            .addClass("dropdown align-middle text-center trainstatus")
                            .append($("<button/>")
                                .attr('type', 'button')
                                .attr('data-bs-toggle', 'dropdown')
                                .attr('aria-expanded', 'false')
                                .addClass('btn btn-sm btn-icon')
                                .addClass(function () {
                                    if (trainingStatus.trainingStatus >= TrainingStatus_Queueing && trainingStatus.trainingStatus < TrainingStatus_Trained)
                                        return "btn-info";
                                    else if (trainingStatus.trainingStatus == TrainingStatus_Trained)
                                        return "btn-success";
                                    else
                                        return "btn-danger";
                                })
                                .attr("title", app.localize('NlpTraining') + ' - ' + getTrainingStatus(trainingStatus).replaceAll("<br/>", ", "))
                                .append($("<i/>").addClass("fas")
                                    .addClass(function () {
                                        if (trainingStatus.trainingStatus >= TrainingStatus_Queueing && trainingStatus.trainingStatus < TrainingStatus_Training)
                                            return "fa-spinner fa-spin";
                                        if (trainingStatus.trainingStatus >= TrainingStatus_Training && trainingStatus.trainingStatus < TrainingStatus_Trained)
                                            return "fa-cog fa-spin";
                                        else
                                            return "fa-flask";
                                    })
                                )
                            )
                            .append($("<div/>")
                                .addClass('mt-2')
                                .html(function () {
                                    return getTrainingStatus(trainingStatus);
                                })
                            )
                            .append(
                                function () {
                                    if (_permissions.train == false || row.nlpChatbot.disabled == true) {
                                        return $("<div/>").addClass("dropdown-menu")
                                            .append($("<span/>").addClass("dropdown-item-text text-muted pl-3 text-nowrap")
                                                .html(getTrainingStatus(trainingStatus)));
                                    }
                                    else {
                                        return $("<ul/>").addClass("dropdown-menu")
                                            .append($("<li/>").addClass("dropdown-item-text text-muted pl-3 text-nowrap")
                                                .html(getTrainingStatus(trainingStatus)))
                                            .append($("<li/>").append($("<hr/>").addClass("dropdown-divider")))
                                            .append($("<li/>").append($("<a/>").addClass("dropdown-item")
                                                .attr("href", "#").append(getTrainingAction(trainingStatus))
                                                .attr("data-op", "training")
                                                .attr("data-id", row.nlpChatbot.id)
                                                .attr("data-status", trainingStatus.trainingStatus)
                                            ));
                                    }
                                }
                            )[0].outerHTML;
                    },
                },
                {
                    targets: 7,
                    data: "nlpChatbot.disabled",
                    name: "disabled",
                    render: function (disabled) {
                        if (disabled) {
                            return '<div class="text-center"><i class="fa fa-ban text-danger" title=' + app.localize("Disabled") + '></i></div>';
                        }
                        return "";
                    },
                },
            ]
        });


        _$nlpChatbotsTable.on('click', '.dropdown-item[data-op="training"]', function (e) {
            var id = $(this).data('id');
            var status = $(this).data('status');

            if (status < TrainingStatus_Queueing || status == TrainingStatus_Cancelled || status == TrainingStatus_Trained || status == TrainingStatus_Failed) {

                abp.message.confirm(
                    '', app.localize('NlpChatbotTrainingConfirm'),
                    function (isConfirmed) {
                        if (isConfirmed) {
                            var rebuild = false;
                            if ($('#rebuildCheck').prop("checked"))
                                rebuild = true;

                            _nlpChatbotsService.trainChatbot(id, rebuild).done(function () {
                                //abp.notify.info(app.localize('NlpCbMInfoQueueing'));
                                dataTable.ajax.reload();
                            });
                        }
                    },
                    {
                        isHtml: true,
                    }
                );
            }
            else if (status >= TrainingStatus_Queueing && status < TrainingStatus_Trained) {
                abp.message.confirm('', app.localize('NlpChatbotTrainingCancelConfirm'),
                    function (isConfirmed) {
                        if (isConfirmed) {
                            _nlpChatbotsService.stopTrainingChatbot(id).done(function () {
                                //abp.notify.warn(app.localize('NlpChatbotStatusCancel'));
                                dataTable.ajax.reload();
                            });;
                        }
                    }
                );
            }
        });


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
            if (status.trainingStatus >= TrainingStatus_Queueing && status.trainingStatus <= TrainingStatus_Training)
                needToRefreshTrainingStatus = true;

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
                if (status.trainingProgress>0 && status.trainingRemaining == 0)
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

        function refreshTrainingStatus() {
            if (needToRefreshTrainingStatus == true) {
                _nlpChatbotsService.getAllTrainingStatus().done(function (statusList) {
                    for (var i = 0; i < statusList.length; i++) {
                        var status = statusList[i];

                        var div = $("<div/>")
                            .append($("<button/>")
                                .attr('type', 'button')
                                .attr('data-bs-toggle', 'dropdown')
                                .attr('aria-expanded', 'false')
                                .addClass('btn btn-sm btn-icon')
                                .addClass(function () {
                                    if (status.trainingStatus >= TrainingStatus_Queueing && status.trainingStatus < TrainingStatus_Trained)
                                        return "btn-info";
                                    else if (status.trainingStatus == TrainingStatus_Trained)
                                        return "btn-success";
                                    else
                                        return "btn-danger";
                                })
                                .attr("title", app.localize('NlpTraining') + ' - ' + getTrainingStatus(status).replaceAll("<br/>", ", "))
                                .append($("<i/>").addClass("fas")
                                    .addClass(function () {
                                        if (status.trainingStatus >= TrainingStatus_Queueing && status.trainingStatus < TrainingStatus_Training)
                                            return "fa-spinner fa-spin";
                                        if (status.trainingStatus >= TrainingStatus_Training && status.trainingStatus < TrainingStatus_Trained)
                                            return "fa-cog fa-spin";
                                        else
                                            return "fa-flask";
                                    })
                                )
                            )
                            .append($("<div/>")
                                .addClass('mt-2')
                                .html(function () {
                                    return getTrainingStatus(status);
                                })
                            )
                            .append(
                                function () {
                                    if (_permissions.train == false) {
                                        return $("<div/>").addClass("dropdown-menu")
                                            .append($("<span/>").addClass("dropdown-item-text text-muted pl-3 text-nowrap")
                                                .html(getTrainingStatus(status)));
                                    }
                                    else {
                                        return $("<ul/>").addClass("dropdown-menu")
                                            .append($("<li/>").addClass("dropdown-item-text text-muted pl-3 text-nowrap")
                                                .html(getTrainingStatus(status)))
                                            .append($("<li/>").append($("<hr/>").addClass("dropdown-divider")))
                                            .append($("<li/>").append($("<a/>").addClass("dropdown-item")
                                                .attr("href", "#").append(getTrainingAction(status))
                                                .attr("data-op", "training")
                                                .attr("data-id", status.id)
                                                .attr("data-status", status.trainingStatus)
                                            ));
                                    }
                                }
                        )[0].innerHTML;

                        $('div.trainstatus[data-id="' + status.id+ '"]').html(div);
                    }
                });
            }

            needToRefreshTrainingStatus = false;
        }

        function getTrainingAction(status) {
            if (status.trainingStatus == TrainingStatus_Trained)
                return app.localize("NlpTraining_Restart");
            else if (status.trainingStatus >= TrainingStatus_Queueing && status.trainingStatus < TrainingStatus_Trained)
                return app.localize("NlpTraining_Cancel");

            return app.localize("NlpTraining_Start");
        }

        function getNlpChatbots() {
            dataTable.ajax.reload();
            dataTable.columns.adjust().draw();
        }



        $('#ImportChatbotFromFileButton').click(function () {
            _ImportModal.open({
                chatbotId: $('#ChatbotSelect').val()
            });
        });

        $('#ExportChatbotToFileButton').click(function () {
            _ExportModal.open({
                chatbotId: $('#ChatbotSelect').val()
            });
        });

        $('#CreateNewNlpChatbotButton').click(function () {
            _createOrEditModal.open();
        });


        abp.event.on('app.createOrEditNlpChatbotModalSaved', function () {
            getNlpChatbots();
        });


        $('#GetNlpChatbotsButton').click(function (e) {
            e.preventDefault();
            getNlpChatbots();
        });


        $(document).keypress(function (e) {
            if (e.which === 13) {
                getNlpChatbots();
            }
        });
    });
})();
