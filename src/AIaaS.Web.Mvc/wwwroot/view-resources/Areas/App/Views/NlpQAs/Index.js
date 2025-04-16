(function () {
    $(function () {

        const TrainingStatus_RequireRetraining = 10;
        const TrainingStatus_Queueing = 100;
        const TrainingStatus_Training = 200;
        const TrainingStatus_Trained = 1000;
        const TrainingStatus_Cancelled = 2000;
        const TrainingStatus_Failed = 2001;

        const WfsNull = "00000000-0000-0000-0000-000000000000";
        const WfsNoChange = "00000000-0000-0000-0000-000000000010";

        var currentTrainingStatus = 0;
        setInterval(checkTrainingStatus, 3000);

        var _$nlpQAsTable = $('#NlpQAsTable');
        var _nlpQAsService = abp.services.app.nlpQAs;
        var _nlpChatbotsService = abp.services.app.nlpChatbots;


        var _permissions = {
            create: abp.auth.hasPermission('Pages.NlpChatbot.NlpQAs.Create'),
            edit: abp.auth.hasPermission('Pages.NlpChatbot.NlpQAs.Edit'),
            'delete': abp.auth.hasPermission('Pages.NlpChatbot.NlpQAs.Delete'),
            //train: abp.auth.hasPermission('Pages.NlpChatbot.NlpChatbots.Train')
        };

        var _createOrEditModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpQAs/CreateOrEditModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpQAs/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpQAModal'
        });

        var _viewNlpQAModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpQAs/ViewNlpWorkflowModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpQAs/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpQAModal',
        });

        var _DiscardModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpQAs/DiscardNlpQAModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpQAs/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpQAModal',
            modalSize: 'modal-md'
        });

        var _ImportModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpQAs/ImportModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpQAs/_ImportModal.js',
            modalClass: 'ImportNlpQAModal'
        });

        var _ExportModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpQAs/ExportModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpQAs/_ExportModal.js',
            modalClass: 'ExportNlpQAModal'
        });

        var dataTable = _$nlpQAsTable.DataTable({
            paging: true,
            serverSide: true,
            processing: true,
            //lengthMenu: [10, 50, 100],
            listAction: {
                ajaxFunction: _nlpQAsService.getAll,
                inputFilter: function () {
                    setTrainingChatbotButton();

                    if ($('#NlpQACategoryUpdated').val() == '1')
                        return {
                            filter: $('#NlpQAsTableFilter').val(),
                            categoryFilter: $('#CategoryFilterId').val(),
                            nlpChatbotGuidFilter: $('#ChatbotSelect').val(),
                        };

                    $('#NlpQACategoryUpdated').val('1');
                },
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
                        text: '<i class="fa fa-cog"></i><span class="d-none d-lg-inline-block">' + app.localize('Actions') + '</span><span class="caret"></span>',
                        items: [
                            {
                                text: app.localize('View'),
                                iconStyle: 'far fa-eye me-2',
                                action: function (data) {
                                    var chatbotSelVal = $('#ChatbotSelect').val();
                                    _viewNlpQAModal.open({ id: data.record.nlpQA.id, chatbotId: chatbotSelVal });
                                }
                            },
                            {
                                text: app.localize('Edit'),
                                iconStyle: 'far fa-edit me-2',
                                visible: function () {
                                    return _permissions.edit;
                                },
                                action: function (data) {
                                    var chatbotSelVal = $('#ChatbotSelect').val();
                                    _createOrEditModal.open({ id: data.record.nlpQA.id, chatbotId: chatbotSelVal });
                                }
                            },
                            {
                                text: app.localize('Delete'),
                                iconStyle: 'far fa-trash-alt me-2',
                                visible: function () {
                                    return _permissions.delete;
                                },
                                action: function (data) {
                                    deleteNlpQA(data.record.nlpQA);
                                }
                            }]
                    }
                },
                {
                    targets: 2,
                    data: "nlpQA.question",
                    name: "question",
                    render: function (question, type, row, meta) {
                        //debugger;
                        var $container = $("<span/>");

                        try {
                            var arrQuestion = JSON.parse(question);

                            var ul = $('<ul/>').addClass("list-group list-group-flush");

                            $.each(arrQuestion, function (index, item) {
                                ul.append($("<li/>").addClass("ms-2 text-with-truncation").append(item));
                            });

                            $container.append(ul);
                        } catch (e) {
                            $container.append(question);
                        }
                        return $container[0].innerHTML;
                    }
                },
                {
                    targets: 3,
                    data: "nlpQA.answer",
                    name: "answer",
                    width: "35%",
                    render: function (answer, type, row, meta) {
                        //debugger;
                        var $container = $("<span/>");

                        try {
                            var arrAnswer = JSON.parse(answer);

                            var ul = $('<ul/>').addClass("list-group list-group-flush");

                            $.each(arrAnswer, function (index, item) {
                                // check attribute of object is defined
                                if (item.Answer == undefined) {
                                    ul.append($("<li/>").addClass("ms-2 text-with-truncation").append(item));
                                }
                                else {
                                    ul.append($("<li/>").addClass("ms-2 text-with-truncation")
                                        .append(function () {
                                            if (item.GPT == true)
                                                return $("<img src='/Common/Images/chatgpt-icon.png' class= 'chatgpt-icon me-2' ></span > ");
                                            else
                                                return $("");
                                        })

                                        .append(item.Answer));
                                }
                            });
                            //<img src="path/to/image.png" class="removeImage" alt="Remove this image" />
                            //<span class='icon'></span>
                            $container.append(ul);
                        } catch (e) {
                            $container.append(answer);
                        }
                        return $container.html();
                    }
                },
                {
                    targets: 4,
                    data: "nlpQA.questionCategory",
                    name: "questionCategory",
                    render: function (questionCategory, type, row, meta) {
                        var $container = $("<div/>").addClass("min-w-50px text-with-truncation");
                        $container.append(questionCategory);
                        return $container[0].outerHTML;
                    }

                },
                {
                    targets: 5,
                    data: "currentWfState",
                    name: "workflow",
                    render: function (data, type, row, meta) {
                        var $container = $("<span/>");

                        try {
                            var nlpQA = row.nlpQA;

                            //var current = row.nlpQA.currentWfState;
                            //var next = row.nlpQA.nextWfState;

                            //if (!current && !next)
                            //    return '';

                            var state = '';

                            if (!nlpQA.currentWfState && !nlpQA.currentWf && (nlpQA.nextWfStateId == WfsNoChange || nlpQA.nextWfStateId == WfsNull))
                                status = "";
                            else {
                                if (nlpQA.currentWfState && nlpQA.currentWf)
                                    state = '<span class="text-wrap">' + $('<div/>').append(nlpQA.currentWf + " : " + nlpQA.currentWfState).html() + '</span>';
                                else if (nlpQA.currentWf)
                                    state = '<span class="text-wrap">' + $('<div/>').append(nlpQA.currentWf + " : *").html() + '</span>';
                                else
                                    state = '<i class="bi bi-dot"></i>';

                                if (nlpQA.nextWfState)
                                    state += '<br/><i class="bi bi-arrow-right"></i><br/><span class="text-wrap">' + $('<div/>').append(nlpQA.nextWf + " : " + nlpQA.nextWfState).html() + '</span>';
                                else if (nlpQA.nextWfStateId == WfsNull)
                                    state += '<br/><i class="bi bi-arrow-right"></i><br/><i class="bi bi-dot"></i>';
                                else if (nlpQA.nextWfStateId == WfsNoChange)
                                    state += '<br/><i class="bi bi-arrow-right"></i><br/><i class="bi bi-arrow-repeat"></i>';
                            }


                            $container.append(state);
                        } catch (e) {
                        }
                        return $container.html();
                    }

                },

                {
                    targets: 6,
                    orderable: false,
                    render: function (name, type, row, meta) {
                        var rowData = row;
                        return $("<div/>")
                            .addClass("text-center text-nowrap")
                            .append(function () {
                                return $("<input/>")
                                    .addClass("form-check-input qa-checkbox mt-3").attr('type', 'checkbox')
                                    .prop('disabled', _permissions.Import == false)
                                    .attr("data-op", "qaSelect")
                                    .attr("data-id", row.nlpQA.id);
                            }).click(function (event) {
                                if (event.target.getAttribute("data-op") == "edit")
                                    _createOrEditModal.open({ id: $(this).data().nlpQALibrary.id });
                                if (event.target.getAttribute("data-op") == "delete" && _permissions.delete)
                                    deleteNlpQALibrary($(this).data().nlpQALibrary);
                            })[0].outerHTML;
                    }
                },

            ],

            "drawCallback": function (settings) {
                if (_$nlpQAsTable.find('td.control.responsive').length > 0)
                    _$nlpQAsTable.find('td.control.responsive').click(function (e) {
                        selectionAllEvent();
                    });
            }
        });

        function selectionAllEvent() {
            _$nlpQAsTable.find('.QaTableSelectAll').click(function (e) {
                var importChecks = $('input[data-op="qaSelect"]');

                $.each(importChecks, function () {
                    $(this).prop('checked', true);
                });
            });

            _$nlpQAsTable.find('.QaTableUnselectAll').click(function (e) {
                var importChecks = $('input[data-op="qaSelect"]');

                $.each(importChecks, function () {
                    $(this).prop('checked', false);
                });
            });

            _$nlpQAsTable.find('.QaTableDelete').click(function (e) {
                var ids = $("input:checked[data-op='qaSelect']").toArray().map(function (e) {
                    return $(e).data("id");
                });

                if (ids.length == 0) {
                    abp.notify.info(app.localize('NoData'));
                    return;
                }

                //_nlpQALibrariesService.duplicateQAs(chatbotId, ids).done(function () {
                //    getNlpQALibraries(true);
                //    abp.notify.success(app.localize('ImportSuccessfully'));
                //});


                abp.message.confirm(
                    app.localize('NlpQADeleteSelectionWarningMessage'),
                    app.localize('AreYouSure'),
                    function (isConfirmed) {
                        if (isConfirmed) {
                            _nlpQAsService.deleteSelections({ ids: ids }).done(function () {
                                getNlpQAs(true);
                                abp.notify.success(app.localize('SuccessfullyDeleted'));
                            });
                        }
                    }
                );
            });
        }


        _$nlpQAsTable.on('click', '.btn[data-op="edit"]', function (e) {
            _createOrEditModal.open({ id: $(this).data('id') });
        });

        _$nlpQAsTable.on('click', '.btn[data-op="view"]', function (e) {
            _createOrEditModal.open({ id: $(this).data('id') });
        });

        _$nlpQAsTable.on('click', '.btn[data-op="delete"]', function (e) {
            if (_permissions.delete)
                deleteNlpQA($(this).data());
        });

        _$nlpQAsTable.find('input[id=CheckAllFilter]').click(function () {
            var importChecks = $('input[data-op="qaSelect"]');

            if ($(this).prop('checked')) {
                $.each(importChecks, function () {
                    if ($(this).is(":disabled") == false)
                        $(this).prop('checked', true);
                });
            }
            else {
                $.each(importChecks, function () {
                    if ($(this).is(":disabled") == false)
                        $(this).prop('checked', false);
                });
            }
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
                    //.prop('disabled', _permissions.train == false)
                    .attr("title", getTrainingStatus(status).replaceAll("<br/>", " "))
                    .addClass(function () {
                        if (status.trainingStatus >= TrainingStatus_Queueing && status.trainingStatus < TrainingStatus_Trained)
                            return "btn-light-info";
                        else if (status.trainingStatus == TrainingStatus_Trained)
                            return 'd-none';
                        //return "btn-light-success";
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

                //switch (currentTrainingStatus) {
                //    case TrainingStatus_Training:
                //        abp.notify.info(app.localize('NlpCbMInfoTraining'));
                //        break;
                //    case TrainingStatus_Trained:
                //        abp.notify.success(app.localize('NlpCbMInfoCompleted'));
                //        break;
                //    default:
                //}
            }
        }

        function getNlpQAs() {
            var chatbotSelVal = $('#ChatbotSelect').val();

            _nlpQAsService.getQaCount(chatbotSelVal).done(function (json) {
                $('#NlpQaCount').text(
                    app.localize("QaUsageCount0", json));
                dataTable.ajax.reload();
                selectionAllEvent();
            });
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




        function deleteNlpQA(nlpQA) {
            abp.message.confirm(
                app.localize('NlpQADeleteWarningMessage', nlpQA.question),
                app.localize('AreYouSure'),
                function (isConfirmed) {
                    if (isConfirmed) {
                        _nlpQAsService.delete({
                            id: nlpQA.id
                        }).done(function () {
                            getNlpQAs(true);
                            abp.notify.success(app.localize('SuccessfullyDeleted'));
                        });
                    }
                }
            );
        }

        function updateCaterogies() {
            var chatbotSelVal = $('#ChatbotSelect').val();

            if (chatbotSelVal) {
                _nlpQAsService.getCaterogies(chatbotSelVal).done(function (json) {
                    $("#CategoryList").empty();
                    //debugger;
                    $.each(json.caterogies, function (i, item) {
                        if (item)
                            $("#CategoryList").append($("<option>").attr('value', item).text(item));
                    });

                    $('#CategoryFilterId').val(json.selectItem);
                    getNlpQAs();
                });
            }
        }


        $('#TrainingChatbotButton').click(function () {

            var chatbotId = $('#ChatbotSelect').val();
            _nlpChatbotsService.getChatbotTrainingStatus(chatbotId).done(function (status) {
                currentTrainingStatus = status.trainingStatus;

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

            if (chatbotSelVal == "" || chatbotSelVal == null || chatbotSelVal == undefined) {
                abp.message.error(app.localize('NlpChatbotName'), app.localize("NlpChatbotName"));
            }
            else {
                _DiscardModal.open({
                    chatbotId: chatbotSelVal
                });
            }
        });


        $('#ImportQAsFromExcelButton').click(function () {
            _ImportModal.open({
                chatbotId: $('#ChatbotSelect').val()
            });
        });

        $('#ExportQAsToExcelButton').click(function () {
            _ExportModal.open({
                chatbotId: $('#ChatbotSelect').val()
            });
        });


        $('#CreateNewNlpQAButton').click(function () {
            var chatbotSelVal = $('#ChatbotSelect').val();

            if (chatbotSelVal == "" || chatbotSelVal == null || chatbotSelVal == undefined) {
                abp.message.error(app.localize('NlpChatbotName'), app.localize("NlpChatbotName"));
            }
            else {
                _createOrEditModal.open({
                    chatbotId: chatbotSelVal
                });
            }
        });

        abp.event.on('app.createOrEditNlpQAModalSaved', function () {
            getNlpQAs();
        });

        $('#GetNlpQAsButton').click(function (e) {
            e.preventDefault();
            getNlpQAs();
        });

        $(document).keypress(function (e) {
            if (e.which === 13) {
                getNlpQAs();
            }
        });


        $('#ChatbotSelect').change(function () {
            $('#CategoryFilterId').val('');
            updateCaterogies();
        });

        $('#CategoryFilterId').change(function () {
            getNlpQAs();
        });

        updateCaterogies();

        selectionAllEvent();

    });
})();
