/**
 * NlpCbQAAccuracies Module
 * Handles the management, filtering, and training status tracking of NLP chatbot QA accuracies.
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

        const _$nlpCbQAAccuraciesTable = $('#NlpCbQAAccuraciesTable');
        const _nlpCbQAAccuraciesService = abp.services.app.nlpCbQAAccuracies;
        const _nlpChatbotsService = abp.services.app.nlpChatbots;
        const _nlpQAsService = abp.services.app.nlpQAs;

        let currentTrainingStatus = 0;
        let oldTrainingStatus = 0;

        // Check training status every 3 seconds
        setInterval(checkTrainingStatus, 3000);

        const _permissions = {
            create: abp.auth.hasPermission('Pages.NlpCbQAAccuracies.Create'),
            edit: abp.auth.hasPermission('Pages.NlpCbQAAccuracies.Edit'),
            delete: abp.auth.hasPermission('Pages.NlpCbQAAccuracies.Delete'),
            editQA: abp.auth.hasPermission('Pages.NlpChatbot.NlpQAs.Edit'),
            train: abp.auth.hasPermission('Pages.NlpChatbot.NlpChatbots.Train')
        };

        const _createOrEditModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpCbQAAccuracies/CreateOrEditModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpCbQAAccuracies/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpCbQAAccuracyModal'
        });

        const _createOrEditQAModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpQAs/CreateOrEditModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpQAs/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpQAModal'
        });

        const _discardModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpQAs/DiscardNlpQAModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpQAs/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpQAModal'
        });

        // Default date range for filters
        const $selectedDate = {
            startDate: moment().subtract(7, 'days'),
            endDate: moment()
        };

        /**
         * Initializes date pickers for filtering QA accuracies by date.
         */
        const initializeDatePickers = function () {
            $('#MinNlpCreationTimeFilterId')
                .daterangepicker({
                    autoUpdateInput: false,
                    singleDatePicker: true,
                    locale: abp.localization.currentLanguage.name,
                    format: 'L'
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
                    format: 'L'
                })
                .val($selectedDate.endDate.format('MM/DD/YYYY'))
                .on('apply.daterangepicker', (ev, picker) => {
                    $selectedDate.endDate = picker.startDate;
                    getNlpCbQAAccuracies();
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
         * Initializes the DataTable for displaying NLP chatbot QA accuracies.
         */
        const dataTable = _$nlpCbQAAccuraciesTable.DataTable({
            paging: true,
            serverSide: true,
            processing: true,
            listAction: {
                ajaxFunction: _nlpCbQAAccuraciesService.getAll,
                inputFilter: function () {
                    setTrainingChatbotButton();
                    return {
                        filter: $('#NlpCbQAAccuraciesTableFilter').val(),
                        minNlpCreationTimeFilter: getDateFilter(),
                        maxNlpCreationTimeFilter: getMaxDateFilter(),
                        nlpChatbotId: $('#ChatbotSelect').val()
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
                    data: "nlpCbQAAccuracy.creationTime",
                    name: "creationTime",
                    class: "text-center",
                    render: (creationTime) => creationTime
                        ? $("<div/>").addClass("text-wrap min-w-50px").html(getDateTime(creationTime))[0].outerHTML
                        : ""
                },
                {
                    targets: 2,
                    name: "question",
                    render: (data, type, row) => $("<div/>")
                        .addClass("text-wrap min-w-50px")
                        .append($("<p/>").addClass("text-with-truncation").append(row.nlpCbQAAccuracy.question))[0].outerHTML
                },
                {
                    targets: 3,
                    data: null,
                    name: "answer",
                    orderable: false,
                    render: (data, type, row, meta) => renderAnswerColumn(data, meta)
                }
            ]
        });

        /**
         * Renders the answer column with predictions and actions.
         * @param {Object} data - The row data.
         * @param {Object} meta - The row metadata.
         * @returns {string} - The rendered HTML for the answer column.
         */
        const renderAnswerColumn = function (data, meta) {
            const $container = $("<span/>");
            const $table = $('<table/>').addClass('display table dt-responsive');
            const metaRow = meta.row;
            let subRow = 0;

            data.nlpCbQAAccuracy.answerPredict.forEach((qa) => {
                const even = (metaRow + subRow) % 2 === 0;
                const same = qa.question.some(q => data.nlpCbQAAccuracy.question.trim().toUpperCase() === q.trim().toUpperCase());

                const accuracy = $('<span/>')
                    .addClass(qa.answerAcc >= 0.5 ? "text-success" : "text-danger")
                    .append(`${(qa.answerAcc * 100).toFixed(2)}%`)
                    .prop('title', app.localize('NlpCbPredictability'));

                const questionList = $('<ul/>').addClass("my-0").attr("title", app.localize("NlpCbQuestion"));
                qa.question.forEach(q => {
                    questionList.append($("<li/>").addClass("text-wrap min-w-50px").append($("<p/>").addClass("text-with-truncation").append(q)));
                });

                const answerList = $('<ul/>').addClass("my-0").attr("title", app.localize("NlpCbAnswer"));
                qa.answer.forEach(a => {
                    answerList.append($("<li/>").addClass("text-wrap min-w-50px").append($("<p/>").addClass("text-with-truncation").append(a.answer)));
                });

                const addButton = $("<button/>")
                    .addClass("btn btn-success btn-sm btn-icon shadow-none")
                    .addClass(same || !_permissions.editQA ? "d-none" : "")
                    .prop('disabled', same)
                    .attr("title", app.localize("NlpCbAddQA"))
                    .attr("data-question", data.nlpCbQAAccuracy.question)
                    .attr("data-qaid", qa.answerId)
                    .attr("data-op", 'addQuestion')
                    .append($("<i/>").addClass("fas fa-plus"));

                const editButton = $("<button/>")
                    .addClass("btn btn-primary btn-sm btn-icon shadow-none")
                    .addClass(!_permissions.editQA ? "d-none" : "")
                    .attr("title", app.localize("EditNlpQA"))
                    .attr("data-qaid", qa.answerId)
                    .attr("data-op", 'editQA')
                    .append($("<i/>").addClass("la la-edit"));

                $table.append($('<tr/>').addClass(even ? "odd" : "even")
                    .append($('<td/>').addClass("w-30px text-center").append(accuracy).append(addButton).append(editButton))
                    .append($('<td/>').append(questionList))
                    .append($('<td/>').append(answerList)));

                subRow++;
            });

            $container.append($table);
            return $container[0].innerHTML;
        };

        /**
         * Updates the training chatbot button based on the current training status.
         */
        function setTrainingChatbotButton() {
            const chatbotId = $('#ChatbotSelect').val();

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
                }
            }
        }

        /**
         * Reloads the DataTable with updated data.
         */
        function getNlpCbQAAccuracies() {
            dataTable.ajax.reload();
        }

        /**
         * Formats a date and time for display.
         * @param {string} dateTime - The date and time string.
         * @returns {string} - The formatted date and time.
         */
        function getDateTime(dateTime) {
            return moment.utc(dateTime).local().format('L') + '<br/> ' + moment.utc(dateTime).local().format('LTS');
        }

        // Event Handlers
        $('#ChatbotSelect').change(() => {
            getNlpCbQAAccuracies();
        });

        $('#GetNlpCbQAAccuraciesButton').click((e) => {
            e.preventDefault();
            getNlpCbQAAccuracies();
        });

        $(document).keypress((e) => {
            if (e.which === 13) {
                getNlpCbQAAccuracies();
            }
        });

        initializeDatePickers();
    });
})();