/**
 * NlpCbModels Module
 * Handles the management, training, and status tracking of NLP chatbot models.
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

        const _$nlpCbModelsTable = $('#NlpCbModelsTable');
        const _nlpCbModelsService = abp.services.app.nlpCbModels;
        const _nlpChatbotsService = abp.services.app.nlpChatbots;

        let currentTrainingStatus = 0;
        let oldTrainingStatus = 0;

        // Check training status every 3 seconds
        setInterval(checkTrainingStatus, 3000);

        const _permissions = {
            train: abp.auth.hasPermission('Pages.NlpChatbot.NlpChatbots.Train')
        };

        const _createOrEditModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpCbModels/CreateOrEditModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpCbModels/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpCbModelModal'
        });

        /**
         * Initializes the DataTable for displaying NLP chatbot models.
         */
        const dataTable = _$nlpCbModelsTable.DataTable({
            paging: true,
            serverSide: true,
            processing: true,
            listAction: {
                ajaxFunction: _nlpCbModelsService.getAll,
                inputFilter: function () {
                    const chatbotIdVal = $('#ChatbotIdId').val();

                    if (!chatbotIdVal && $('#ChatbotSelect').val()) {
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
                    render: () => '',
                    targets: 0
                },
                {
                    targets: 1,
                    orderable: false,
                    render: (name, type, row) => {
                        const isTraining = _permissions.train &&
                            row.nlpCbModel.nlpCbMStatus >= TrainingStatus.Queueing &&
                            row.nlpCbModel.nlpCbMStatus < TrainingStatus.Trained;

                        return $("<div/>")
                            .addClass("text-left text-nowrap")
                            .append($("<button/>")
                                .addClass(`btn btn-sm btn-danger ${isTraining ? "" : "d-none"}`)
                                .attr("data-op", isTraining ? "train" : undefined)
                                .prop('disabled', !isTraining)
                                .append($("<i/>").addClass("bi bi-stop-circle"))
                                .append(app.localize("Cancel"))
                            )[0].outerHTML;
                    }
                },
                {
                    targets: 2,
                    data: "nlpCbModel.nlpCbMStatus",
                    name: "nlpCbMStatus",
                    class: "text-center",
                    render: (nlpCbMStatus) => nlpCbMStatus ? getTrainingStatusForTable(nlpCbMStatus) : ""
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
                    render: (nlpCbMCreationTime) => nlpCbMCreationTime
                        ? $("<div/>").addClass("text-wrap min-w-50px").html(getDateTime(nlpCbMCreationTime))[0].outerHTML
                        : ""
                },
                {
                    targets: 5,
                    data: "nlpCbModel.nlpCbMTrainingStartTime",
                    name: "nlpCbMTrainingStartTime",
                    class: "text-center",
                    render: (nlpCbMTrainingStartTime) => nlpCbMTrainingStartTime
                        ? $("<div/>").addClass("text-wrap min-w-50px").html(getDateTime(nlpCbMTrainingStartTime))[0].outerHTML
                        : ""
                },
                {
                    targets: 6,
                    data: "nlpCbModel.nlpCbMTrainingCompleteTime",
                    name: "nlpCbMTrainingCompleteTime",
                    class: "text-center",
                    render: (nlpCbMTrainingCompleteTime) => nlpCbMTrainingCompleteTime
                        ? $("<div/>").addClass("text-wrap min-w-50px").html(getDateTime(nlpCbMTrainingCompleteTime))[0].outerHTML
                        : ""
                },
                {
                    targets: 7,
                    data: "nlpCbModel.nlpCbAccuracy",
                    name: "nlpCbAccuracy",
                    class: "text-center",
                    render: (nlpCbAccuracy) => nlpCbAccuracy ? nlpCbAccuracy.toFixed(2) : ""
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
                    render: (nlpCbMTrainingCancellationTime) => nlpCbMTrainingCancellationTime
                        ? $("<div/>").addClass("text-wrap min-w-50px").html(getDateTime(nlpCbMTrainingCancellationTime))[0].outerHTML
                        : ""
                }
            ]
        });

        /**
         * Updates the training chatbot button based on the current training status.
         */
        function setTrainingChatbotButton() {
            const chatbotId = $('#ChatbotIdId').val();

            if (!chatbotId) {
                $('#TrainingChatbotDropDown').addClass("d-none");
                return;
            }

            _nlpChatbotsService.getChatbotTrainingStatus(chatbotId).done((status) => {
                currentTrainingStatus = status.trainingStatus;

                $('#TrainingIcon').removeClass("fa-spinner fa-cog fa-flask fa-spin p-0").addClass("fas")
                    .addClass(() => {
                        if (status.trainingStatus >= TrainingStatus.Queueing && status.trainingStatus < TrainingStatus.Training)
                            return "fa-spinner fa-spin p-0";
                        if (status.trainingStatus >= TrainingStatus.Training && status.trainingStatus < TrainingStatus.Trained)
                            return "fa-cog fa-spin p-0";
                        return "fa-flask";
                    });

                $('#TrainingChatbotDropDown').removeClass("d-none btn-light-info btn-light-success btn-light-danger")
                    .attr("title", getTrainingStatus(status).replaceAll("<br/>", " "))
                    .addClass(() => {
                        if (status.trainingStatus >= TrainingStatus.Queueing && status.trainingStatus < TrainingStatus.Trained)
                            return "btn-light-info";
                        if (status.trainingStatus === TrainingStatus.Trained)
                            return "btn-light-success";
                        return "btn-light-danger";
                    });

                $('#TrainingCbStatus').html(getTrainingStatus(status));

                $('#TrainingChatbotButton').html(() => {
                    if (status.trainingStatus >= TrainingStatus.Queueing && status.trainingStatus < TrainingStatus.Trained)
                        return app.localize('NlpTraining_Cancel');
                    if (status.trainingStatus === TrainingStatus.Trained)
                        return app.localize('NlpTraining_Restart');
                    return app.localize('NlpTraining_Start');
                });
            });
        }

        /**
         * Periodically checks the training status and updates the UI.
         */
        function checkTrainingStatus() {
            if ([TrainingStatus.Queueing, TrainingStatus.Training].includes(currentTrainingStatus) ||
                [TrainingStatus.Queueing, TrainingStatus.Training].includes(oldTrainingStatus)) {
                setTrainingChatbotButton();
                if (oldTrainingStatus !== currentTrainingStatus) {
                    oldTrainingStatus = currentTrainingStatus;
                    getNlpCbModels();
                }
            }
        }

        /**
         * Reloads the DataTable with updated data.
         */
        function getNlpCbModels() {
            try {
                dataTable.ajax.reload();
            } catch (e) {
                console.error("Error reloading models:", e);
            }
        }

        /**
         * Formats a date and time for display.
         * @param {string} dateTime - The date and time string.
         * @returns {string} - The formatted date and time.
         */
        function getDateTime(dateTime) {
            return moment.utc(dateTime).local().format('lll');
        }

        /**
         * Retrieves the localized training status for display in the table.
         * @param {number} code - The training status code.
         * @returns {string} - The localized training status.
         */
        function getTrainingStatusForTable(code) {
            switch (code) {
                case TrainingStatus.Queueing:
                    return app.localize("NlpTrainingStatus_Queueing").replace("\\n", "<br/>");
                case TrainingStatus.Training:
                    return app.localize("NlpTrainingStatus_Training").replace("\\n", "<br/>");
                case TrainingStatus.Trained:
                    return app.localize("NlpTrainingStatus_Trained").replace("\\n", "<br/>");
                case TrainingStatus.Cancelled:
                    return app.localize("NlpTrainingStatus_Cancelled").replace("\\n", "<br/>");
                case TrainingStatus.Failed:
                    return app.localize("NlpTrainingStatus_Failed").replace("\\n", "<br/>");
                case TrainingStatus.RequireRetraining:
                    return app.localize("NlpTrainingStatus_RequireRetraining").replace("\\n", "<br/>");
                default:
                    return app.localize("NlpTrainingStatus_NotTraining").replace("\\n", "<br/>");
            }
        }

        // Event Handlers
        $('#ChatbotSelect').change(() => {
            $('#ChatbotIdId').val($('#ChatbotSelect').val());
            getNlpCbModels();
        });

        $('#GetNlpCbModelsButton').click((e) => {
            e.preventDefault();
            getNlpCbModels();
        });

        $(document).keypress((e) => {
            if (e.which === 13) {
                getNlpCbModels();
            }
        });

        abp.event.on('app.createOrEditNlpCbModelModalSaved', () => {
            getNlpCbModels();
        });
    });
})();