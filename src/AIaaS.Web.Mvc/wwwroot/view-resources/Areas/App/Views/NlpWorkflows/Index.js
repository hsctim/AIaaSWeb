(function () {
    $(function () {

        var _$nlpWorkflowsTable = $('#NlpWorkflowsTable');
        var _nlpWorkflowsService = abp.services.app.nlpWorkflows;

        var _permissions = {
            create: abp.auth.hasPermission('Pages.Administration.NlpWorkflows.Create'),
            edit: abp.auth.hasPermission('Pages.Administration.NlpWorkflows.Edit'),
            'delete': abp.auth.hasPermission('Pages.Administration.NlpWorkflows.Delete')
        };

        var _createOrEditModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpWorkflows/CreateOrEditModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpWorkflows/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpWorkflowModal',
            modalSize: 'modal-md',
        });

        var _viewNlpWorkflowModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpWorkflows/ViewNlpWorkflowModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpWorkflows/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpWorkflowModal',
            modalSize: 'modal-md',

        });


        var dataTable = _$nlpWorkflowsTable.DataTable({
            paging: true,
            serverSide: true,
            processing: true,
            "initComplete": function (settings, json) {
                $('.nlp-action-separator').closest('li').addClass('separator my-2').empty();
            },
            listAction: {
                ajaxFunction: _nlpWorkflowsService.getAll,
                inputFilter: function () {
                    return {
                        nlpChatbotId: $('#ChatbotSelect').val()
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
                        element: $('<div/>')
                            .addClass('text-start')
                            .append(
                                $('<button/>')
                                    .addClass('btn btn-icon btn-bg-light btn-active-color-primary btn-sm')
                                    .attr('title', app.localize('AuditLogDetail'))
                                    .append($('<i/>').addClass('la la-search'))
                            )
                            .click(function () {
                                showAuditLogDetails($(this).data());
                            }),
                        cssClass: 'btn btn-brand dropdown-toggle',
                        text: '<i class="fa fa-cog"></i><span class="d-none d-lg-inline-block">' + app.localize('Actions') + '</span><span class="caret"></span>',
                        items: [
                            {
                                text: app.localize('View'),
                                iconStyle: 'far fa-eye me-2',
                                action: function (data) {
                                    _viewNlpWorkflowModal.open({ id: data.record.nlpWorkflow.id });
                                }
                            },
                            {
                                text: app.localize('Edit'),
                                iconStyle: 'far fa-edit me-2',
                                visible: function () {
                                    return _permissions.edit;
                                },
                                action: function (data) {
                                    _createOrEditModal.open({ id: data.record.nlpWorkflow.id });
                                }
                            },
                            {
                                text: app.localize('Delete'),
                                iconStyle: 'far fa-trash-alt me-2',
                                visible: function () {
                                    return _permissions.delete;
                                },
                                action: function (data) {
                                    deleteNlpWorkflow(data.record.nlpWorkflow);
                                }
                            },
                            {
                                iconStyle: 'nlp-action-separator separator my-2',
                            },
                            {
                                text: app.localize('NlpWorkflowState'),
                                iconStyle: 'bi bi-bar-chart-steps me-2',
                                action: function (data) {
                                    window.location.href = '/App/NlpWorkflows/NlpWorkflowStates/' + data.record.nlpWorkflow.id;
                                }
                            },
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
                                    .addClass('btn btn-primary btn-sm btn-icon shadow-none ')
                                    .attr('title', app.localize('NlpWorkflowState'))
                                    .append($('<i/>').addClass('bi bi-bar-chart-steps'))
                            )
                            .click(function () {
                                window.location.href = '/App/NlpWorkflows/NlpWorkflowStates/' + $(this).data().nlpWorkflow.id;
                            }),
                    },
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
                    render: function (disabled) {
                        if (disabled) {
                            return '<div class="text-center"><i class="fa fa-ban text-danger" title=' + app.localize("Disabled") + '></i></div>';
                        }

                        return "";
                    }

                },
            ]
        });

        function getNlpWorkflows() {
            dataTable.ajax.reload();
        }

        function deleteNlpWorkflow(nlpWorkflow) {
            abp.message.confirm(
                app.localize('NlpWorkflowDeleteWarningMessage', nlpWorkflow.name),
                app.localize('AreYouSure'),
                function (isConfirmed) {
                    if (isConfirmed) {
                        _nlpWorkflowsService.delete({
                            id: nlpWorkflow.id
                        }).done(function () {
                            getNlpWorkflows(true);
                            abp.notify.success(app.localize('SuccessfullyDeleted'));
                        });
                    }
                }
            );
        }

        $('#CreateNewNlpWorkflowButton').click(function () {
            _createOrEditModal.open({
                chatbotId: $('#ChatbotSelect').val()
            });
        });


        abp.event.on('app.createOrEditNlpWorkflowModalSaved', function () {
            getNlpWorkflows();
        });


        $('#ChatbotSelect').change(function () {
            getNlpWorkflows();
        });

        $(document).keypress(function (e) {
            if (e.which === 13) {
                getNlpWorkflows();
            }
        });
    });
})();
