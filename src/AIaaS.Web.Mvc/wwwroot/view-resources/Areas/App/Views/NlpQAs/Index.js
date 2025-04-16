/**
 * NlpQAs Module
 * Handles the management, filtering, and workflow of NLP QA (Question-Answer) pairs.
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

        // Workflow state constants
        const WorkflowStates = {
            Null: "00000000-0000-0000-0000-000000000000",
            NoChange: "00000000-0000-0000-0000-000000000010"
        };

        let currentTrainingStatus = 0;

        // Refresh training status every 3 seconds
        setInterval(checkTrainingStatus, 3000);

        const _$nlpQAsTable = $('#NlpQAsTable');
        const _nlpQAsService = abp.services.app.nlpQAs;
        const _nlpChatbotsService = abp.services.app.nlpChatbots;

        const _permissions = {
            create: abp.auth.hasPermission('Pages.NlpChatbot.NlpQAs.Create'),
            edit: abp.auth.hasPermission('Pages.NlpChatbot.NlpQAs.Edit'),
            delete: abp.auth.hasPermission('Pages.NlpChatbot.NlpQAs.Delete')
        };

        // Modal managers
        const _createOrEditModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpQAs/CreateOrEditModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpQAs/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpQAModal'
        });

        const _viewNlpQAModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpQAs/ViewNlpWorkflowModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpQAs/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpQAModal'
        });

        const _discardModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpQAs/DiscardNlpQAModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpQAs/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpQAModal',
            modalSize: 'modal-md'
        });

        const _importModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpQAs/ImportModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpQAs/_ImportModal.js',
            modalClass: 'ImportNlpQAModal'
        });

        const _exportModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpQAs/ExportModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpQAs/_ExportModal.js',
            modalClass: 'ExportNlpQAModal'
        });

        /**
         * Initializes the DataTable for displaying NLP QA pairs.
         */
        const dataTable = _$nlpQAsTable.DataTable({
            paging: true,
            serverSide: true,
            processing: true,
            listAction: {
                ajaxFunction: _nlpQAsService.getAll,
                inputFilter: () => {
                    setTrainingChatbotButton();
                    if ($('#NlpQACategoryUpdated').val() === '1') {
                        return {
                            filter: $('#NlpQAsTableFilter').val(),
                            categoryFilter: $('#CategoryFilterId').val(),
                            nlpChatbotGuidFilter: $('#ChatbotSelect').val()
                        };
                    }
                    $('#NlpQACategoryUpdated').val('1');
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
                    data: null,
                    orderable: false,
                    defaultContent: '',
                    rowAction: {
                        cssClass: 'btn btn-brand dropdown-toggle',
                        text: `<i class="fa fa-cog"></i><span class="d-none d-lg-inline-block">${app.localize('Actions')}</span><span class="caret"></span>`,
                        items: [
                            {
                                text: app.localize('View'),
                                iconStyle: 'far fa-eye me-2',
                                action: (data) => {
                                    const chatbotId = $('#ChatbotSelect').val();
                                    _viewNlpQAModal.open({ id: data.record.nlpQA.id, chatbotId });
                                }
                            },
                            {
                                text: app.localize('Edit'),
                                iconStyle: 'far fa-edit me-2',
                                visible: () => _permissions.edit,
                                action: (data) => {
                                    const chatbotId = $('#ChatbotSelect').val();
                                    _createOrEditModal.open({ id: data.record.nlpQA.id, chatbotId });
                                }
                            },
                            {
                                text: app.localize('Delete'),
                                iconStyle: 'far fa-trash-alt me-2',
                                visible: () => _permissions.delete,
                                action: (data) => deleteNlpQA(data.record.nlpQA)
                            }
                        ]
                    }
                },
                {
                    targets: 2,
                    data: "nlpQA.question",
                    name: "question",
                    render: (question) => renderList(question)
                },
                {
                    targets: 3,
                    data: "nlpQA.answer",
                    name: "answer",
                    width: "35%",
                    render: (answer) => renderAnswerList(answer)
                },
                {
                    targets: 4,
                    data: "nlpQA.questionCategory",
                    name: "questionCategory",
                    render: (category) => `<div class="min-w-50px text-with-truncation">${category}</div>`
                },
                {
                    targets: 5,
                    data: "currentWfState",
                    name: "workflow",
                    render: (data, type, row) => renderWorkflowState(row.nlpQA)
                },
                {
                    targets: 6,
                    orderable: false,
                    render: (name, type, row) => renderCheckbox(row.nlpQA.id)
                }
            ],
            drawCallback: () => selectionAllEvent()
        });

        /**
         * Renders a list of items (e.g., questions or answers).
         * @param {string} data - The JSON string containing the list.
         * @returns {string} - The rendered HTML.
         */
        function renderList(data) {
            const $container = $("<span/>");
            try {
                const items = JSON.parse(data);
                const ul = $('<ul/>').addClass("list-group list-group-flush");
                items.forEach((item) => ul.append($("<li/>").addClass("ms-2 text-with-truncation").text(item)));
                $container.append(ul);
            } catch {
                $container.text(data);
            }
            return $container[0].innerHTML;
        }

        /**
         * Renders a list of answers with GPT icons if applicable.
         * @param {string} data - The JSON string containing the answers.
         * @returns {string} - The rendered HTML.
         */
        function renderAnswerList(data) {
            const $container = $("<span/>");
            try {
                const answers = JSON.parse(data);
                const ul = $('<ul/>').addClass("list-group list-group-flush");
                answers.forEach((item) => {
                    const li = $("<li/>").addClass("ms-2 text-with-truncation");
                    if (item.GPT) {
                        li.append($("<img/>").attr("src", "/Common/Images/chatgpt-icon.png").addClass("chatgpt-icon me-2"));
                    }
                    li.append(item.Answer || item);
                    ul.append(li);
                });
                $container.append(ul);
            } catch {
                $container.text(data);
            }
            return $container[0].innerHTML;
        }

        /**
         * Renders the workflow state for a QA pair.
         * @param {Object} nlpQA - The QA object containing workflow data.
         * @returns {string} - The rendered HTML.
         */
        function renderWorkflowState(nlpQA) {
            const $container = $("<span/>");
            let state = "";

            if (!nlpQA.currentWfState && !nlpQA.currentWf && (nlpQA.nextWfStateId === WorkflowStates.NoChange || nlpQA.nextWfStateId === WorkflowStates.Null)) {
                state = "";
            } else {
                if (nlpQA.currentWfState && nlpQA.currentWf) {
                    state = `<span class="text-wrap">${nlpQA.currentWf} : ${nlpQA.currentWfState}</span>`;
                } else if (nlpQA.currentWf) {
                    state = `<span class="text-wrap">${nlpQA.currentWf} : *</span>`;
                } else {
                    state = '<i class="bi bi-dot"></i>';
                }

                if (nlpQA.nextWfState) {
                    state += `<br/><i class="bi bi-arrow-right"></i><br/><span class="text-wrap">${nlpQA.nextWf} : ${nlpQA.nextWfState}</span>`;
                } else if (nlpQA.nextWfStateId === WorkflowStates.Null) {
                    state += '<br/><i class="bi bi-arrow-right"></i><br/><i class="bi bi-dot"></i>';
                } else if (nlpQA.nextWfStateId === WorkflowStates.NoChange) {
                    state += '<br/><i class="bi bi-arrow-right"></i><br/><i class="bi bi-arrow-repeat"></i>';
                }
            }

            $container.html(state);
            return $container[0].outerHTML;
        }

        /**
         * Renders a checkbox for selecting QA pairs.
         * @param {string} id - The QA ID.
         * @returns {string} - The rendered HTML.
         */
        function renderCheckbox(id) {
            return $("<div/>")
                .addClass("text-center text-nowrap")
                .append($("<input/>")
                    .addClass("form-check-input qa-checkbox mt-3")
                    .attr("type", "checkbox")
                    .attr("data-op", "qaSelect")
                    .attr("data-id", id)
                )[0].outerHTML;
        }

        /**
         * Deletes a QA pair after confirmation.
         * @param {Object} nlpQA - The QA object to delete.
         */
        function deleteNlpQA(nlpQA) {
            abp.message.confirm(
                app.localize('NlpQADeleteWarningMessage', nlpQA.question),
                app.localize('AreYouSure'),
                (isConfirmed) => {
                    if (isConfirmed) {
                        _nlpQAsService.delete({ id: nlpQA.id }).done(() => {
                            getNlpQAs();
                            abp.notify.success(app.localize('SuccessfullyDeleted'));
                        });
                    }
                }
            );
        }

        /**
         * Updates the category list based on the selected chatbot.
         */
        function updateCategories() {
            const chatbotId = $('#ChatbotSelect').val();
            if (chatbotId) {
                _nlpQAsService.getCaterogies(chatbotId).done((json) => {
                    const $categoryList = $("#CategoryList").empty();
                    json.caterogies.forEach((item) => {
                        if (item) {
                            $categoryList.append($("<option>").attr('value', item).text(item));
                        }
                    });
                    $('#CategoryFilterId').val(json.selectItem);
                    getNlpQAs();
                });
            }
        }

        /**
         * Refreshes the training status for the selected chatbot.
         */
        function checkTrainingStatus() {
            if ([TrainingStatus.Queueing, TrainingStatus.Training].includes(currentTrainingStatus)) {
                setTrainingChatbotButton();
            }
        }

        /**
         * Reloads the DataTable with updated data.
         */
        function getNlpQAs() {
            const chatbotId = $('#ChatbotSelect').val();
            _nlpQAsService.getQaCount(chatbotId).done((count) => {
                $('#NlpQaCount').text(app.localize("QaUsageCount0", count));
                dataTable.ajax.reload();
                selectionAllEvent();
            });
        }

        /**
         * Handles the selection and deletion of multiple QA pairs.
         */
        function selectionAllEvent() {
            _$nlpQAsTable.find('.QaTableSelectAll').click(() => {
                $('input[data-op="qaSelect"]').prop('checked', true);
            });

            _$nlpQAsTable.find('.QaTableUnselectAll').click(() => {
                $('input[data-op="qaSelect"]').prop('checked', false);
            });

            _$nlpQAsTable.find('.QaTableDelete').click(() => {
                const ids = $("input:checked[data-op='qaSelect']").map((_, el) => $(el).data("id")).get();
                if (ids.length === 0) {
                    abp.notify.info(app.localize('NoData'));
                    return;
                }

                abp.message.confirm(
                    app.localize('NlpQADeleteSelectionWarningMessage'),
                    app.localize('AreYouSure'),
                    (isConfirmed) => {
                        if (isConfirmed) {
                            _nlpQAsService.deleteSelections({ ids }).done(() => {
                                getNlpQAs();
                                abp.notify.success(app.localize('SuccessfullyDeleted'));
                            });
                        }
                    }
                );
            });
        }

        // Event Handlers
        $('#ChatbotSelect').change(() => {
            $('#CategoryFilterId').val('');
            updateCategories();
        });

        $('#CategoryFilterId').change(() => getNlpQAs());
        $('#CreateNewNlpQAButton').click(() => _createOrEditModal.open({ chatbotId: $('#ChatbotSelect').val() }));
        $('#ImportQAsFromExcelButton').click(() => _importModal.open({ chatbotId: $('#ChatbotSelect').val() }));
        $('#ExportQAsToExcelButton').click(() => _exportModal.open({ chatbotId: $('#ChatbotSelect').val() }));
        $('#GetNlpQAsButton').click((e) => {
            e.preventDefault();
            getNlpQAs();
        });

        $(document).keypress((e) => {
            if (e.which === 13) {
                getNlpQAs();
            }
        });

        // Initial setup
        updateCategories();
        selectionAllEvent();
    });
})();




