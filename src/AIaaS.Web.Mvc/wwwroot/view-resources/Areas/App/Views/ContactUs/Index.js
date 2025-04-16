var CurrentPage = function () {

    var handleContactUs = function () {
        var $ContactUsForm = $('#contactus-form');
        var $submitButton = $('#ContactUsSubmit');


        $submitButton.click(function (e) {
            if (!$ContactUsForm.valid()) {
                return false;
            }

            abp.ui.setBusy(
                null,
                abp.ajax({
                    contentType: app.consts.contentTypes.formUrlencoded,
                    url: $ContactUsForm.attr('action'),
                    data: $ContactUsForm.serialize()
                }).done(function () {
                    abp.message.success(app.localize('MessageSentUsSuccess'), app.localize('MessageSent'))
                        .done(function () {
                            $ContactUsForm.trigger("reset");
                        });
                })
            );
        });

    }

    handleContactUs();

}();
