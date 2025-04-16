/**
 * NlpCbMessages Module
 * Handles the initialization, filtering, and management of NLP chatbot messages.
 */
(function () {
    $(function () {
        const _$nlpCbMessagesTable = $('#NlpCbMessagesTable');
        const _nlpCbMessagesService = abp.services.app.nlpCbMessages;

        // Default date range for filters
        const $selectedDate = {
            startDate: moment().subtract(7, 'days'),
            endDate: moment(),
        };

        /**
         * Initializes date pickers for filtering messages by date.
         */
        const initializeDatePickers = function () {
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
        };

        /**
         * Retrieves the minimum date filter value.
         * @returns {string|null} - The formatted start date or null if not set.
         */
        const getDateFilter = function () {
            return $selectedDate.startDate ? $selectedDate.startDate.format('YYYY-MM-DDT00:00:00Z') : null;
        };

        /**
         * Retrieves the maximum date filter value.
         * @returns {string|null} - The formatted end date or null if not set.
         */
        const getMaxDateFilter = function () {
            return $selectedDate.endDate ? $selectedDate.endDate.format('YYYY-MM-DDT23:59:59Z') : null;
        };

        /**
         * Initializes the DataTable for displaying NLP chatbot messages.
         */
        const initializeDataTable = function () {
            _$nlpCbMessagesTable.DataTable({
                paging: true,
                serverSide: true,
                processing: true,
                listAction: {
                    ajaxFunction: _nlpCbMessagesService.getAll,
                    inputFilter: function () {
                        return {
                            filter: $('#NlpCbMessagesTableFilter').val(),
                            minNlpSentTimeFilter: getDateFilter(),
                            maxNlpSentTimeFilter: getMaxDateFilter(),
                            nlpChatbotId: $('#ChatbotSelect').val(),
                        };
                    },
                },
                columnDefs: [
                    {
                        className: 'control responsive',
                        orderable: false,
                        render: () => '',
                        targets: 0,
                    },
                    {
                        targets: 1,
                        data: "nlpCreationTime",
                        name: "NlpCreationTime",
                        class: "text-center",
                        render: (nlpCreationTime) => nlpCreationTime ? getDateTime(nlpCreationTime) : "",
                    },
                    {
                        targets: 2,
                        data: "nlpCbSentType",
                        name: "NlpCbSentType",
                        render: (sentType, type, row) => {
                            const $container = $("<div/>");
                            const html = sentType === "send"
                                ? `${app.localize("NlpCbRole:" + row.nlpCbReceiverRoleName)} <i class='fas fa-angle-double-left text-primary'></i> ${app.localize("NlpCbRole:" + row.nlpCbSenderRoleName)}`
                                : `${app.localize("NlpCbRole:" + row.nlpCbSenderRoleName)} <i class='fas fa-angle-double-right text-danger'></i> ${app.localize("NlpCbRole:" + row.nlpCbReceiverRoleName)}`;
                            $container.addClass(sentType === "send" ? "text-primary" : "text-danger").append(html);
                            return $container[0].outerHTML;
                        },
                    },
                    {
                        targets: 3,
                        data: "nlpMessage",
                        name: "nlpMessage",
                        render: (question) => $("<div/>").addClass("text-with-truncation min-w-100px").html(question).html(),
                    },
                    {
                        targets: 4,
                        data: "priorWfS",
                        name: "priorWfS",
                        render: (question, type, row) => {
                            const $container = $("<span/>");
                            try {
                                const priorWfS = row.priorWfS;
                                const currentWfS = row.currentWfS;

                                if (!priorWfS && !currentWfS) return '';

                                let status = priorWfS
                                    ? `<span class="text-wrap">${$('<div/>').append(priorWfS).html()}</span>`
                                    : '<i class="bi bi-dot"></i>';

                                status += currentWfS
                                    ? `<i class="bi bi-arrow-right"></i><span class="text-wrap">${$('<div/>').append(currentWfS).html()}</span>`
                                    : '<i class="bi bi-arrow-right"></i><i class="bi bi-dot"></i>';

                                $container.append(status);
                            } catch (e) {
                                console.error(e);
                            }
                            return $container.html();
                        },
                    },
                    { targets: 5, data: "nlpChatbotName", name: "nlpChatbotName" },
                    { targets: 6, data: "nlpCbAgentName", name: "nlpCbAgentName" },
                    {
                        targets: 7,
                        data: "channelName",
                        name: "channelName",
                    },
                    {
                        targets: 8,
                        data: "nlpClientName",
                        name: "clientId",
                        render: (clientName, type, row) => {
                            const $container = $("<div/>").addClass("text-wrap min-w-50px").append(clientName || row.clientId);
                            return $container[0].outerHTML;
                        },
                    },
                ],
            });
        };

        /**
         * Reloads the DataTable with updated filters.
         */
        const getNlpCbMessages = function () {
            try {
                _$nlpCbMessagesTable.DataTable().ajax.reload();
            } catch (e) {
                console.error("Error reloading messages:", e);
            }
        };

        /**
         * Formats a date and time for display.
         * @param {string} dateTime - The date and time string.
         * @returns {string} - The formatted date and time.
         */
        const getDateTime = function (dateTime) {
            const format = moment.utc(dateTime).local();
            return $(document).width() < 1200
                ? `${format.format('L')}<br/>${format.format('LTS')}`
                : `${format.format('L')}&nbsp;${format.format('LTS')}`;
        };

        /**
         * Deletes an NLP chatbot message after confirmation.
         * @param {Object} nlpCbMessage - The message to delete.
         */
        const deleteNlpCbMessage = function (nlpCbMessage) {
            abp.message.confirm(
                '',
                app.localize('AreYouSure'),
                function (isConfirmed) {
                    if (isConfirmed) {
                        _nlpCbMessagesService.delete({ id: nlpCbMessage.id }).done(() => {
                            getNlpCbMessages();
                            abp.notify.success(app.localize('SuccessfullyDeleted'));
                        });
                    }
                }
            );
        };

        // Event Handlers
        $('#ShowAdvancedFiltersSpan').click(() => {
            $('#ShowAdvancedFiltersSpan').hide();
            $('#HideAdvancedFiltersSpan').show();
            $('#AdvacedAuditFiltersArea').slideDown();
        });

        $('#HideAdvancedFiltersSpan').click(() => {
            $('#HideAdvancedFiltersSpan').hide();
            $('#ShowAdvancedFiltersSpan').show();
            $('#AdvacedAuditFiltersArea').slideUp();
        });

        $('#CreateNewNlpCbMessageButton').click(() => {
            _createOrEditModal.open();
        });

        $('#ChatbotSelect').change(() => {
            getNlpCbMessages();
        });

        $('#GetNlpCbMessagesButton').click((e) => {
            e.preventDefault();
            getNlpCbMessages();
        });

        $(document).keypress((e) => {
            if (e.which === 13) {
                getNlpCbMessages();
            }
        });

        // Initialize components
        initializeDatePickers();
        initializeDataTable();
    });
})();

