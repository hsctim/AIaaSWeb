/**
 * NlpTokens Module
 * Handles the management, filtering, and CRUD operations for NLP tokens.
 */
(function () {
    $(function () {
        const _$nlpTokensTable = $('#NlpTokensTable');
        const _nlpTokensService = abp.services.app.nlpTokens;

        // Permissions for CRUD operations
        const _permissions = {
            create: abp.auth.hasPermission('Pages.NlpTokens.Create'),
            edit: abp.auth.hasPermission('Pages.NlpTokens.Edit'),
            delete: abp.auth.hasPermission('Pages.NlpTokens.Delete')
        };

        // Modal manager for creating or editing NLP tokens
        const _createOrEditModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpTokens/CreateOrEditModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpTokens/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpTokenModal'
        });

        /**
         * Initializes the date pickers for filtering.
         */
        $('.date-picker').datetimepicker({
            locale: abp.localization.currentLanguage.name,
            format: 'L'
        });

        /**
         * Retrieves the minimum date filter value.
         * @param {Object} element - The date picker element.
         * @returns {string|null} - The formatted start date or null if not set.
         */
        const getDateFilter = (element) => {
            const date = element.data("DateTimePicker").date();
            return date ? date.format("YYYY-MM-DDT00:00:00Z") : null;
        };

        /**
         * Retrieves the maximum date filter value.
         * @param {Object} element - The date picker element.
         * @returns {string|null} - The formatted end date or null if not set.
         */
        const getMaxDateFilter = (element) => {
            const date = element.data("DateTimePicker").date();
            return date ? date.format("YYYY-MM-DDT23:59:59Z") : null;
        };

        /**
         * Initializes the DataTable for displaying NLP tokens.
         */
        const dataTable = _$nlpTokensTable.DataTable({
            paging: true,
            serverSide: true,
            processing: true,
            listAction: {
                ajaxFunction: _nlpTokensService.getAll,
                inputFilter: () => ({
                    filter: $('#NlpTokensTableFilter').val()
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
                        text: `<i class="fa fa-cog"></i> ${app.localize('Actions')} <span class="caret"></span>`,
                        items: [
                            {
                                text: app.localize('Edit'),
                                iconStyle: 'far fa-edit me-2',
                                visible: () => _permissions.edit,
                                action: (data) => _createOrEditModal.open({ id: data.record.nlpToken.id })
                            },
                            {
                                text: app.localize('Delete'),
                                iconStyle: 'far fa-trash-alt me-2',
                                visible: () => _permissions.delete,
                                action: (data) => deleteNlpToken(data.record.nlpToken)
                            }
                        ]
                    }
                },
                {
                    targets: 2,
                    data: "nlpToken.nlpTokenType",
                    name: "nlpTokenType"
                },
                {
                    targets: 3,
                    data: "nlpToken.nlpTokenValue",
                    name: "nlpTokenValue"
                }
            ]
        });

        /**
         * Reloads the DataTable with updated data.
         */
        const getNlpTokens = () => {
            dataTable.ajax.reload();
        };

        /**
         * Deletes an NLP token after confirmation.
         * @param {Object} nlpToken - The token to delete.
         */
        const deleteNlpToken = (nlpToken) => {
            abp.message.confirm(
                '',
                app.localize('AreYouSure'),
                (isConfirmed) => {
                    if (isConfirmed) {
                        _nlpTokensService.delete({ id: nlpToken.id }).done(() => {
                            getNlpTokens();
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

        $('#CreateNewNlpTokenButton').click(() => {
            _createOrEditModal.open();
        });

        abp.event.on('app.createOrEditNlpTokenModalSaved', () => {
            getNlpTokens();
        });

        $('#GetNlpTokensButton').click((e) => {
            e.preventDefault();
            getNlpTokens();
        });

        $(document).keypress((e) => {
            if (e.which === 13) {
                getNlpTokens();
            }
        });
    });
})();

