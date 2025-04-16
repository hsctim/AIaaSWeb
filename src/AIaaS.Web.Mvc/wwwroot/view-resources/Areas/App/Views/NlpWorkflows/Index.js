/**
 * NlpWorkflows Module
 * Handles the management, filtering, and CRUD operations for NLP workflows.
 */
(function () {
    $(function () {
        const _$nlpWorkflowsTable = $('#NlpWorkflowsTable');
        const _nlpWorkflowsService = abp.services.app.nlpWorkflows;

        // Permissions for CRUD operations
        const _permissions = {
            create: abp.auth.hasPermission('Pages.Administration.NlpWorkflows.Create'),
            edit: abp.auth.hasPermission('Pages.Administration.NlpWorkflows.Edit'),
            delete: abp.auth.hasPermission('Pages.Administration.NlpWorkflows.Delete')
        };

        // Modal managers for creating, editing, and viewing workflows
        const _createOrEditModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpWorkflows/CreateOrEditModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpWorkflows/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpWorkflowModal',
            modalSize: 'modal-md'
        });

        const _viewNlpWorkflowModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpWorkflows/ViewNlpWorkflowModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpWorkflows/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpWorkflowModal',
            modalSize: 'modal-md'
        });

        /**
         * Initializes the DataTable for displaying NLP workflows.
         */
        const dataTable = _$nlpWorkflowsTable.DataTable({
            paging: true,
            serverSide: true,
            processing: true,
            initComplete: function () {
                $('.nlp-action-separator').closest('li').addClass('separator my-2').empty();
            },
            listAction: {
                ajaxFunction: _nlpWorkflowsService.getAll,
                inputFilter: () => ({
                    nlpChatbotId: $('#ChatbotSelect').val()
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
                        text: `<i class="fa fa-cog"></i><span class="d-none d-lg-inline-block">${app.localize('Actions')}</span><span class="caret"></span>`,
                        items: [
                            {
                                text: app.localize('View'),
                                iconStyle: 'far fa-eye me-2',
                                action: (data) => _viewNlpWorkflowModal.open({ id: data.record.nlpWorkflow.id })
                            },
                            {
                                text: app.localize('Edit'),
                                iconStyle: 'far fa-edit me-2',
                                visible: () => _permissions.edit,
                                action: (data) => _createOrEditModal.open({ id: data.record.nlpWorkflow.id })
                            },
                            {
                                text: app.localize('Delete'),
                                iconStyle: 'far fa-trash-alt me-2',
                                visible: () => _permissions.delete,
                                action: (data) => deleteNlpWorkflow(data.record.nlpWorkflow)
                            },
                            {
                                iconStyle: 'nlp-action-separator separator my-2'
                            },
                            {
                                text: app.localize('NlpWorkflowState'),
                                iconStyle: 'bi bi-bar-chart-steps me-2',
                                action: (data) => {
                                    window.location.href = `/App/NlpWorkflows/NlpWorkflowStates/${data.record.nlpWorkflow.id}`;
                                }
                            }
                        ]
                    }
                },
                {
                    targets: 2,
                    data: null,
                    orderable: false,
                    defaultContent: '',
                    rowAction: {
                        element: $('<div/>')
                            .append(
                                $('<button/>')
                                    .addClass('btn btn-primary btn-sm btn-icon shadow-none')
                                    .attr('title', app.localize('NlpWorkflowState'))
                                    .append($('<i/>').addClass('bi bi-bar-chart-steps'))
                            )
                            .click(function () {
                                window.location.href = `/App/NlpWorkflows/NlpWorkflowStates/${$(this).data().nlpWorkflow.id}`;
                            })
                    }
                },
                {
                    targets: 3,
                    data: "nlpChatbotName",
                    name: "nlpChatbotFk.name"
                },
                {
                    targets: 4,
                    data: "nlpWorkflow.name",
                    name: "name"
                },
                {
                    targets: 5,
                    data: "nlpWorkflow.disabled",
                    name: "disabled",
                    render: (disabled) => disabled
                        ? '<div class="text-center"><i class="fa fa-ban text-danger" title=' + app.localize("Disabled") + '></i></div>'
                        : ""
                }
            ]
        });

        /**
         * Reloads the DataTable with updated data.
         */
        const getNlpWorkflows = () => {
            dataTable.ajax.reload();
        };

        /**
         * Deletes an NLP workflow after confirmation.
         * @param {Object} nlpWorkflow - The workflow to delete.
         */
        const deleteNlpWorkflow = (nlpWorkflow) => {
            abp.message.confirm(
                app.localize('NlpWorkflowDeleteWarningMessage', nlpWorkflow.name),
                app.localize('AreYouSure'),
                (isConfirmed) => {
                    if (isConfirmed) {
                        _nlpWorkflowsService.delete({ id: nlpWorkflow.id }).done(() => {
                            getNlpWorkflows();
                            abp.notify.success(app.localize('SuccessfullyDeleted'));
                        });
                    }
                }
            );
        };

        // Event Handlers
        $('#CreateNewNlpWorkflowButton').click(() => {
            _createOrEditModal.open({
                chatbotId: $('#ChatbotSelect').val()
            });
        });

        abp.event.on('app.createOrEditNlpWorkflowModalSaved', () => {
            getNlpWorkflows();
        });

        $('#ChatbotSelect').change(() => {
            getNlpWorkflows();
        });

        $(document).keypress((e) => {
            if (e.which === 13) {
                getNlpWorkflows();
            }
        });
    });
})();




