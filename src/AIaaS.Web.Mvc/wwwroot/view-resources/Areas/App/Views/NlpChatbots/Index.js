/**
 * NlpChatbots Module
 * Handles the management, training, and status tracking of NLP chatbots.
 */
(function () {
    $(function () {
        // Training status constants
        const TrainingStatus = {
            RequireRetraining: 10,
            Queueing: 100,
            Training: 200,
            Trained: 1000,
            Cancelled: 2000,
            Failed: 2001
        };

        let needToRefreshTrainingStatus = false;

        // Refresh training status every 5 seconds
        setInterval(refreshTrainingStatus, 5000);

        const _$nlpChatbotsTable = $('#NlpChatbotsTable');
        const _nlpChatbotsService = abp.services.app.nlpChatbots;

        const _permissions = {
            create: abp.auth.hasPermission('Pages.NlpChatbot.NlpChatbots.Create'),
            edit: abp.auth.hasPermission('Pages.NlpChatbot.NlpChatbots.Edit'),
            delete: abp.auth.hasPermission('Pages.NlpChatbot.NlpChatbots.Delete'),
            train: abp.auth.hasPermission('Pages.NlpChatbot.NlpChatbots.Train')
        };

        const _createOrEditModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpChatbots/CreateOrEditModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpChatbots/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpChatbotModal'
        });

        const _viewNlpChatbotModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpChatbots/ViewNlpChatbotModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpChatbots/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpChatbotModal'
        });

        const _deleteModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpChatbots/DeleteModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpChatbots/_DeleteModal.js',
            modalClass: 'DeleteNlpChatbotModal'
        });

        const _importModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpChatbots/ImportModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpChatbots/_ImportModal.js',
            modalClass: 'ImportNlpChatbotModal'
        });

        const _exportModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpChatbots/ExportModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpChatbots/_ExportModal.js',
            modalClass: 'ExportNlpChatbotModal'
        });

        /**
         * Initializes the DataTable for displaying NLP chatbots.
         */
        const dataTable = _$nlpChatbotsTable.DataTable({
            paging: true,
            serverSide: true,
            processing: true,
            listAction: {
                ajaxFunction: _nlpChatbotsService.getAll,
                inputFilter: () => ({
                    filter: $('#NlpChatbotsTableFilter').val()
                })
            },
            columnDefs: [
                {
                    className: 'control responsive',
                    orderable: false,
                    render: () => '',
                    targets: 0
                },
                {
                    targets: 1,
                    data: null,
                    orderable: false,
                    defaultContent: '',
                    rowAction: {
                        cssClass: 'btn btn-brand dropdown-toggle',
                        text: `<i class="fa fa-cog"></i> <span class="d-none d-lg-inline-block">${app.localize('Actions')}</span> <span class="caret"></span>`,
                        items: [
                            {
                                text: app.localize('View'),
                                iconStyle: 'far fa-eye me-2',
                                action: (data) => _viewNlpChatbotModal.open({ id: data.record.nlpChatbot.id })
                            },
                            {
                                text: app.localize('Edit'),
                                iconStyle: 'far fa-edit me-2',
                                visible: () => _permissions.edit,
                                action: (data) => _createOrEditModal.open({ id: data.record.nlpChatbot.id })
                            },
                            {
                                text: app.localize('Delete'),
                                iconStyle: 'far fa-trash-alt me-2',
                                visible: () => _permissions.delete,
                                action: (data) => _deleteModal.open({ id: data.record.nlpChatbot.id })
                            },
                            {
                                text: app.localize('OpenWebChatPage'),
                                iconStyle: 'flaticon-browser me-2',
                                visible: (data) => data.record.nlpChatbot.enableWebChat,
                                action: (data) => window.open(`/webchat/${data.record.nlpChatbot.id}`, "_chatPalWebChat")
                            }
                        ]
                    }
                },
                {
                    targets: 2,
                    data: "nlpChatbot.name",
                    name: "name",
                    render: (chatbotName, type, row) => {
                        const profilePicture = row.nlpChatbot.chatbotPictureId
                            ? `/Chatbot/ProfilePicture/${row.nlpChatbot.chatbotPictureId}`
                            : "/Chatbot/ProfilePicture";

                        return $("<div/>")
                            .addClass("text-center text-wrap")
                            .append($("<img/>").addClass("img-circle").attr("src", profilePicture))
                            .append($("<div/>").addClass('mt-2').text(chatbotName))[0].outerHTML;
                    }
                },
                {
                    targets: 3,
                    data: "nlpChatbot.greetingMsg",
                    name: "greetingMsg",
                    render: (greetingMsg) => $("<div/>").addClass("text-wrap min-w-50px").append(greetingMsg)[0].outerHTML
                },
                {
                    targets: 4,
                    data: "nlpChatbot.failedMsg",
                    name: "failedMsg",
                    render: (failedMsg) => $("<div/>").addClass("text-wrap min-w-50px").append(failedMsg)[0].outerHTML
                },
                {
                    targets: 5,
                    data: "nlpChatbot.alternativeQuestion",
                    name: "alternativeQuestion",
                    render: (alternativeQuestion) => $("<div/>").addClass("text-wrap min-w-50px").append(alternativeQuestion)[0].outerHTML
                },
                {
                    targets: 6,
                    data: "trainingStatus",
                    orderable: false,
                    name: "trainingStatus",
                    render: (trainingStatus, type, row) => renderTrainingStatus(trainingStatus, row)
                },
                {
                    targets: 7,
                    data: "nlpChatbot.disabled",
                    name: "disabled",
                    render: (disabled) => disabled
                        ? '<div class="text-center"><i class="fa fa-ban text-danger" title=' + app.localize("Disabled") + '></i></div>'
                        : ""
                }
            ]
        });

        /**
         * Renders the training status column with dropdown actions.
         * @param {Object} trainingStatus - The training status object.
         * @param {Object} row - The row data.
         * @returns {string} - The rendered HTML for the training status column.
         */
        function renderTrainingStatus(trainingStatus, row) {
            const dropdownButton = $("<button/>")
                .attr('type', 'button')
                .attr('data-bs-toggle', 'dropdown')
                .attr('aria-expanded', 'false')
                .addClass('btn btn-sm btn-icon')
                .addClass(getTrainingStatusClass(trainingStatus))
                .attr("title", app.localize('NlpTraining') + ' - ' + getTrainingStatus(trainingStatus).replaceAll("<br/>", ", "))
                .append($("<i/>").addClass("fas").addClass(getTrainingStatusIcon(trainingStatus)));

            const dropdownMenu = $("<ul/>").addClass("dropdown-menu")
                .append($("<li/>").addClass("dropdown-item-text text-muted pl-3 text-nowrap").html(getTrainingStatus(trainingStatus)))
                .append($("<li/>").append($("<hr/>").addClass("dropdown-divider")))
                .append($("<li/>").append($("<a/>").addClass("dropdown-item")
                    .attr("href", "#")
                    .append(getTrainingAction(trainingStatus))
                    .attr("data-op", "training")
                    .attr("data-id", row.nlpChatbot.id)
                    .attr("data-status", trainingStatus.trainingStatus)));

            return $("<div/>")
                .attr("data-id", row.nlpChatbot.id)
                .addClass("dropdown align-middle text-center trainstatus")
                .append(dropdownButton)
                .append($("<div/>").addClass('mt-2').html(getTrainingStatus(trainingStatus)))
                .append(dropdownMenu)[0].outerHTML;
        }

        /**
         * Returns the appropriate CSS class for the training status button.
         * @param {Object} trainingStatus - The training status object.
         * @returns {string} - The CSS class.
         */
        function getTrainingStatusClass(trainingStatus) {
            if (trainingStatus.trainingStatus >= TrainingStatus.Queueing && trainingStatus.trainingStatus < TrainingStatus.Trained) {
                return "btn-info";
            } else if (trainingStatus.trainingStatus === TrainingStatus.Trained) {
                return "btn-success";
            } else {
                return "btn-danger";
            }
        }

        /**
         * Returns the appropriate icon class for the training status button.
         * @param {Object} trainingStatus - The training status object.
         * @returns {string} - The icon class.
         */
        function getTrainingStatusIcon(trainingStatus) {
            if (trainingStatus.trainingStatus >= TrainingStatus.Queueing && trainingStatus.trainingStatus < TrainingStatus.Training) {
                return "fa-spinner fa-spin";
            } else if (trainingStatus.trainingStatus >= TrainingStatus.Training && trainingStatus.trainingStatus < TrainingStatus.Trained) {
                return "fa-cog fa-spin";
            } else {
                return "fa-flask";
            }
        }

        /**
         * Refreshes the training status for all chatbots.
         */
        function refreshTrainingStatus() {
            if (!needToRefreshTrainingStatus) return;

            _nlpChatbotsService.getAllTrainingStatus().done((statusList) => {
                statusList.forEach((status) => {
                    const $statusDiv = $('div.trainstatus[data-id="' + status.id + '"]');
                    $statusDiv.html(renderTrainingStatus(status, { nlpChatbot: { id: status.id } }));
                });
            });

            needToRefreshTrainingStatus = false;
        }

        /**
         * Reloads the DataTable with updated data.
         */
        function getNlpChatbots() {
            dataTable.ajax.reload();
            dataTable.columns.adjust().draw();
        }

        // Event Handlers
        $('#ImportChatbotFromFileButton').click(() => _importModal.open({ chatbotId: $('#ChatbotSelect').val() }));
        $('#ExportChatbotToFileButton').click(() => _exportModal.open({ chatbotId: $('#ChatbotSelect').val() }));
        $('#CreateNewNlpChatbotButton').click(() => _createOrEditModal.open());
        $('#GetNlpChatbotsButton').click((e) => {
            e.preventDefault();
            getNlpChatbots();
        });

        $(document).keypress((e) => {
            if (e.which === 13) {
                getNlpChatbots();
            }
        });

        abp.event.on('app.createOrEditNlpChatbotModalSaved', () => {
            getNlpChatbots();
        });
    });
})();