/**
 * NlpWorkflowStates Module
 * Handles the management, filtering, and CRUD operations for NLP workflow states.
 */
(function () {
    $(function () {
        const _$nlpWorkflowStatesTable = $('#NlpWorkflowStatesTable');
        const _nlpWorkflowStatesService = abp.services.app.nlpWorkflowStates;

        // Permissions for CRUD operations
        const _permissions = {
            create: abp.auth.hasPermission('Pages.Administration.NlpWorkflows.Create'),
            edit: abp.auth.hasPermission('Pages.Administration.NlpWorkflows.Edit'),
            delete: abp.auth.hasPermission('Pages.Administration.NlpWorkflows.Delete')
        };

        // Modal managers for creating, editing, and viewing workflow states
        const _createOrEditModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpWorkflowStates/CreateOrEditModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpWorkflowStates/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpWorkflowStateModal'
        });

        const _viewNlpWorkflowStateModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpWorkflowStates/ViewnlpWorkflowStateModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpWorkflowStates/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpWorkflowStateModal'
        });

        /**
         * Initializes the DataTable for displaying NLP workflow states.
         */
        const dataTable = _$nlpWorkflowStatesTable.DataTable({
            paging: true,
            serverSide: true,
            processing: true,
            listAction: {
                ajaxFunction: _nlpWorkflowStatesService.getAll,
                inputFilter: () => ({
                    nlpWorkflowId: $('#WorkflowId').val()
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
                                action: (data) => _viewNlpWorkflowStateModal.open({ id: data.record.nlpWorkflowState.id })
                            },
                            {
                                text: app.localize('Edit'),
                                iconStyle: 'far fa-edit me-2',
                                visible: () => _permissions.edit,
                                action: (data) => _createOrEditModal.open({ id: data.record.nlpWorkflowState.id })
                            },
                            {
                                text: app.localize('Delete'),
                                iconStyle: 'far fa-trash-alt me-2',
                                visible: () => _permissions.delete,
                                action: (data) => deleteNlpWorkflowState(data.record.nlpWorkflowState)
                            }
                        ]
                    }
                },
                {
                    targets: 2,
                    data: "nlpChatbotName",
                    name: "nlpWorkflowFk.nlpChatbotFk.name"
                },
                {
                    targets: 3,
                    data: "nlpWorkflowName",
                    name: "nlpWorkflowFk.name"
                },
                {
                    targets: 4,
                    data: "nlpWorkflowState.stateName",
                    name: "stateName"
                },
                {
                    targets: 5,
                    data: "nlpWorkflowState.stateInstruction",
                    name: "stateInstruction"
                },
                {
                    targets: 6,
                    data: "nlpWorkflowState.responseNonWorkflowAnswer",
                    name: "responseNonWorkflowAnswer",
                    render: (responseNonWorkflowAnswer) => responseNonWorkflowAnswer
                        ? '<div class="text-center"><i class="fa fa-check text-success" title=' + app.localize("True") + '></i></div>'
                        : ""
                },
                {
                    targets: 7,
                    data: "nlpWorkflowState.dontResponseNonWorkflowErrorAnswer",
                    name: "dontResponseNonWorkflowErrorAnswer",
                    render: (dontResponseNonWorkflowErrorAnswer) => dontResponseNonWorkflowErrorAnswer
                        ? '<div class="text-center"><i class="fa fa-check text-success" title=' + app.localize("True") + '></i></div>'
                        : ""
                }
            ]
        });

        /**
         * Reloads the DataTable with updated data.
         */
        const getNlpWorkflowStates = () => {
            dataTable.ajax.reload();
        };

        /**
         * Deletes an NLP workflow state after confirmation.
         * @param {Object} nlpWorkflowState - The workflow state to delete.
         */
        const deleteNlpWorkflowState = (nlpWorkflowState) => {
            abp.message.confirm(
                app.localize('NlpWorkflowStateDeleteWarningMessage', nlpWorkflowState.stateName),
                app.localize('AreYouSure'),
                (isConfirmed) => {
                    if (isConfirmed) {
                        _nlpWorkflowStatesService.delete({ id: nlpWorkflowState.id }).done(() => {
                            getNlpWorkflowStates();
                            abp.notify.success(app.localize('SuccessfullyDeleted'));
                        });
                    }
                }
            );
        };

        // Event Handlers
        $('#CreateNewNlpWorkflowStateButton').click(() => {
            _createOrEditModal.open({ workflowId: $('#WorkflowId').val() });
        });

        abp.event.on('app.createOrEditNlpWorkflowStateModalSaved', () => {
            getNlpWorkflowStates();
        });

        $(document).keypress((e) => {
            if (e.which === 13) {
                getNlpWorkflowStates();
            }
        });
    });
})();






