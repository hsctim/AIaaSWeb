(function () {
    $(function () {

        var _$nlpTokensTable = $('#NlpTokensTable');
        var _nlpTokensService = abp.services.app.nlpTokens;

        $('.date-picker').datetimepicker({
            locale: abp.localization.currentLanguage.name,
            format: 'L'
        });

        var _permissions = {
            create: abp.auth.hasPermission('Pages.NlpTokens.Create'),
            edit: abp.auth.hasPermission('Pages.NlpTokens.Edit'),
            'delete': abp.auth.hasPermission('Pages.NlpTokens.Delete')
        };

        var _createOrEditModal = new app.ModalManager({
            viewUrl: abp.appPath + 'App/NlpTokens/CreateOrEditModal',
            scriptUrl: abp.appPath + 'view-resources/Areas/App/Views/NlpTokens/_CreateOrEditModal.js',
            modalClass: 'CreateOrEditNlpTokenModal'
        });






        var getDateFilter = function (element) {
            if (element.data("DateTimePicker").date() == null) {
                return null;
            }
            return element.data("DateTimePicker").date().format("YYYY-MM-DDT00:00:00Z");
        }

        var getMaxDateFilter = function (element) {
            if (element.data("DateTimePicker").date() == null) {
                return null;
            }
            return element.data("DateTimePicker").date().format("YYYY-MM-DDT23:59:59Z");
        }

        var dataTable = _$nlpTokensTable.DataTable({
            paging: true,
            serverSide: true,
            processing: true,
            listAction: {
                ajaxFunction: _nlpTokensService.getAll,
                inputFilter: function () {
                    return {
                        filter: $('#NlpTokensTableFilter').val()
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
                        cssClass: 'btn btn-brand dropdown-toggle',
                        text: '<i class="fa fa-cog"></i> ' + app.localize('Actions') + ' <span class="caret"></span>',
                        items: [
                            {
                                text: app.localize('Edit'),
                                iconStyle: 'far fa-edit me-2',
                                visible: function () {
                                    return _permissions.edit;
                                },
                                action: function (data) {
                                    _createOrEditModal.open({ id: data.record.nlpToken.id });
                                }
                            },
                            {
                                text: app.localize('Delete'),
                                iconStyle: 'far fa-trash-alt me-2',
                                visible: function () {
                                    return _permissions.delete;
                                },
                                action: function (data) {
                                    deleteNlpToken(data.record.nlpToken);
                                }
                            }]
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

        function getNlpTokens() {
            dataTable.ajax.reload();
        }

        function deleteNlpToken(nlpToken) {
            abp.message.confirm(
                '',
                app.localize('AreYouSure'),
                function (isConfirmed) {
                    if (isConfirmed) {
                        _nlpTokensService.delete({
                            id: nlpToken.id
                        }).done(function () {
                            getNlpTokens(true);
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

        $('#CreateNewNlpTokenButton').click(function () {
            _createOrEditModal.open();
        });



        abp.event.on('app.createOrEditNlpTokenModalSaved', function () {
            getNlpTokens();
        });

        $('#GetNlpTokensButton').click(function (e) {
            e.preventDefault();
            getNlpTokens();
        });

        $(document).keypress(function (e) {
            if (e.which === 13) {
                getNlpTokens();
            }
        });



    });
})();
