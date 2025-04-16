(function () {
    $(function () {

        var _$nlpCbMessagesTable = $('#NlpCbMessagesTable');
        var _nlpCbMessagesService = abp.services.app.nlpCbMessages;

        //debugger
        //$('.date-picker').datetimepicker({
        //    locale: abp.localization.currentLanguage.name,
        //    format: 'L',
        //    timeZone: moment.tz.guess(),
        //});

        //$('#MinNlpSentTimeFilterId').data("DateTimePicker").date(moment().subtract(7, 'days'));
        //$('#MaxNlpSentTimeFilterId').data("DateTimePicker").date(moment());

        var $selectedDate = {
            startDate: moment().subtract(7, 'days') ,
            endDate: moment(),
        };


        $('.date-picker').on('apply.daterangepicker', function (ev, picker) {
            $(this).val(picker.startDate.format('MM/DD/YYYY'));
        });

        $('#MinNlpSentTimeFilterId')
            .daterangepicker({
                autoUpdateInput: false,
                singleDatePicker: true,
                locale: abp.localization.currentLanguage.name,
                format: 'L',
            })
            .val($selectedDate.startDate.format('MM/DD/YYYY'))
            .on('apply.daterangepicker', (ev, picker) => {
                $selectedDate.startDate = picker.startDate;
                getNlpCbMessages();
            });

        $('#MaxNlpSentTimeFilterId')
            .daterangepicker({
                autoUpdateInput: false,
                singleDatePicker: true,
                locale: abp.localization.currentLanguage.name,
                format: 'L',
            })
            .val($selectedDate.endDate.format('MM/DD/YYYY'))
            .on('apply.daterangepicker', (ev, picker) => {
                $selectedDate.endDate = picker.startDate;
                getNlpCbMessages();
            });


        var _permissions = {
            create: abp.auth.hasPermission('Pages.Administration.NlpCbMessages.Create'),
            edit: abp.auth.hasPermission('Pages.Administration.NlpCbMessages.Edit'),
            'delete': abp.auth.hasPermission('Pages.Administration.NlpCbMessages.Delete')
        };

        var _createOrEditModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpCbMessages/CreateOrEditModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpCbMessages/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpCbMessageModal'
        });

        function pad(num, size) {
            num = num.toString();
            while (num.length < size) num = "0" + num;
            return num;
        }

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


        var dataTable = _$nlpCbMessagesTable.DataTable({
            paging: true,
            serverSide: true,
            //lengthMenu: [10, 50, 100],
            processing: true,
            listAction: {
                ajaxFunction: _nlpCbMessagesService.getAll,
                inputFilter: function () {
                    return {
                        filter: $('#NlpCbMessagesTableFilter').val(),
                        minNlpSentTimeFilter: getDateFilter($('#MinNlpSentTimeFilterId')),
                        maxNlpSentTimeFilter: getMaxDateFilter($('#MaxNlpSentTimeFilterId')),
                        nlpChatbotId: $('#ChatbotSelect').val()
                    };
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
                    targets: 1,
                    data: "nlpCreationTime",
                    name: "NlpCreationTime",
                    class: "text-center",
                    render: function (nlpCreationTime) {
                        if (nlpCreationTime) {
                            return getDateTime(nlpCreationTime);
                            //return $("<div/>")
                            //    .addClass("text-wrap").html(getDateTime(nlpCreationTime))[0].outerHTML;
                        }

                        return "";
                    }
                },
                {
                    targets: 2,
                    data: "nlpCbSentType",
                    name: "NlpCbSentType",
                    render: function (sentType, type, row, meta) {
                        var $container = $("<div/>")
                        if (sentType == "send") {
                            var html = app.localize("NlpCbRole:" + row.nlpCbReceiverRoleName) +
                                " <i class='fas fa-angle-double-left text-primary'></i> " +
                                app.localize("NlpCbRole:" + row.nlpCbSenderRoleName);

                            $container.addClass("text-primary").append(html);
                        }
                        else {
                            var html = app.localize("NlpCbRole:" + row.nlpCbSenderRoleName) +
                                " <i class='fas fa-angle-double-right text-danger'></i> " +
                                app.localize("NlpCbRole:" + row.nlpCbReceiverRoleName);

                            $container.addClass("text-danger").append(html);
                        }

                        return $container[0].outerHTML;
                    }
                },
                {
                    targets: 3,
                    data: "nlpMessage",
                    name: "nlpMessage",
                    //width: "35%",
                    render: function (question, type, row, meta) {
                        var $container = $("<div/>");

                        $container.append($("<div/>").addClass("text-with-truncation min-w-100px").html(question));

                        return $container.html();
                    }
                },
                {
                    targets: 4,
                    data: "priorWfS",
                    name: "priorWfS",
                    //width: "35%",
                    render: function (question, type, row, meta) {
                        var $container = $("<span/>");

                        try {
                            var priorWfS = row.priorWfS;
                            var currentWfS = row.currentWfS;

                            if (!priorWfS && !currentWfS)
                                return '';

                            var status = '';

                            if (priorWfS)
                                status = '<span class="text-wrap">' + $('<div/>').append(priorWfS).html() + '</span>';
                            else
                                status = '<i class="bi bi-dot"></i>';

                            if (currentWfS)
                                status += '<i class="bi bi-arrow-right"></i><span class="text-wrap">' + $('<div/>').append(currentWfS).html() + '</span>';
                            else
                                status += '<i class="bi bi-arrow-right"></i><i class="bi bi-dot"></i>';

                            $container.append(status);
                        } catch (e) {
                        }
                        return $container.html();
                    }
                },
                {
                    targets: 5,
                    data: "nlpChatbotName",
                    name: "nlpChatbotName"
                },
                {
                    targets: 6,
                    data: "nlpCbAgentName",
                    name: "nlpCbAgentName"
                },
                //{
                //    targets: 6,
                //    data: "nlpCbUserName",
                //    name: "NlpUserFk.name",
                //    visible: abp.session.tenantId == 1
                //},
                {
                    targets: 7,
                    data: "channelName",
                    name: "channelName"
                },
                {
                    targets: 8,
                    data: "nlpClientName",
                    name: "clientId",
                    //orderable: false,
                    render: function (clientName, type, row, meta) {
                        if (clientName) {
                            var $container = $("<div/>").addClass("text-wrap min-w-50px").append(clientName);
                            return $container[0].outerHTML;
                            //return clientName;
                        }
                        else {
                            var $container = $("<div/>").addClass("text-wrap min-w-50px").append(row.clientId);
                            return $container[0].outerHTML;
                            //return row.clientId;
                        }
                    }
                }
            ]
        });


        function getNlpCbMessages() {
            try {
                dataTable.ajax.reload();
            } catch (e) {
                debugger
            }
        }

        function getDateTime(dateTime) {
            if ($(document).width() < 1200)
                return moment.utc(dateTime).local().format('L') + '<br/> ' + moment.utc(dateTime).local().format('LTS');
            else
                return moment.utc(dateTime).local().format('L') + '&nbsp;' + moment.utc(dateTime).local().format('LTS');
            //return moment.utc(dateTime).local().format('lll');
        }


        function deleteNlpCbMessage(nlpCbMessage) {
            abp.message.confirm(
                '',
                app.localize('AreYouSure'),
                function (isConfirmed) {
                    if (isConfirmed) {
                        _nlpCbMessagesService.delete({
                            id: nlpCbMessage.id
                        }).done(function () {
                            getNlpCbMessages(true);
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

        $('#CreateNewNlpCbMessageButton').click(function () {
            _createOrEditModal.open();
        });

        $('#ChatbotSelect').change(function () {
            getNlpCbMessages();
        });

        $('#GetNlpCbMessagesButton').click(function (e) {
            e.preventDefault();
            getNlpCbMessages();
        });

        $(document).keypress(function (e) {
            if (e.which === 13) {
                getNlpCbMessages();
            }
        });
    });
})();
