var CurrentPage = (function () {
    var handleLogin = function () {
        var $loginForm = $('form.login-form');
        var $submitButton = $('#kt_login_signin_submit');


        if (abp.session.tenantId) {
            abp.multiTenancy.setTenantIdCookie(null);
            location.reload();
            return;
        }

        $submitButton.click(function () {
            trySubmitForm();
        });

        $loginForm.validate({
            rules: {
                username: {
                    required: true,
                },
                password: {
                    required: true,
                },
            },
        });

        $loginForm.find('input').keypress(function (e) {
            if (e.which === 13) {
                trySubmitForm();
            }
        });

        $('a.social-login-icon').click(function () {
            var $a = $(this);
            var $form = $a.closest('form');
            $form.find('input[name=provider]').val($a.attr('data-provider'));
            $form.submit();
        });

        $loginForm.find('input[name=returnUrlHash]').val(location.hash);

        $('input[type=text]').first().focus();

        function trySubmitForm() {
            if (!$('form.login-form').valid()) {
                return;
            }

            function setCaptchaToken(callback) {
                callback = callback || function () { };
                if (!abp.setting.getBoolean('App.UserManagement.UseCaptchaOnLogin')) {
                    callback();
                } else {
                    grecaptcha.reExecute(function (token) {
                        $('#recaptchaResponse').val(token);
                        callback();
                    });
                }
            }

            setCaptchaToken(function () {
                abp.ui.setBusy(
                    null,
                    abp
                        .ajax({
                            contentType: app.consts.contentTypes.formUrlencoded,
                            url: $loginForm.attr('action'),
                            data: $loginForm.serialize(),
                            abpHandleError: false
                        }).fail(function (error) {
                            $('input[name ="tenantId"]').val("");
                            $('#accountsModalCenter').modal('hide');
                            $('#multipleCredentials').empty();
                            setCaptchaToken();
                            abp.ajax.showError(error);
                        }).done(function (info) {
                            if (info && info.Users) {
                                $('#multipleCredentials').empty();

                                for (i = 0; i < info.Users.length; i++) {
                                    $('#multipleCredentials').append(
                                        $("<a/>").addClass("list-group-item list-group-item-action text-center text-primary")
                                            .attr("href", "#")
                                            .attr('data-name', info.Users[i].tenantName).attr('data-id', info.Users[i].tenantId).attr('data-op', 'login')
                                            .prop('title', app.localize("Login"))
                                            .append(app.localize("LoginAs{0}User", info.Users[i].tenantName + "\\" + info.Users[i].userName))
                                    );
                                }

                                $('#accountsModalCenter').modal('show');

                                $('a[data-op="login"]').on("click", function () {
                                    var tenantId = $(this).data('id');
                                    $('input[name ="isHost"]').val(false);
                                    $('input[name ="tenantId"]').val("");



                                    if (tenantId) {
                                        $('input[name ="tenantId"]').val(tenantId);
                                        $('#accountsModalCenter').modal('hide');
                                        $('#multipleCredentials').empty();
                                        trySubmitForm();
                                    }
                                    else {
                                        $('input[name ="isHost"]').val(true);
                                        $('#accountsModalCenter').modal('hide');
                                        $('#multipleCredentials').empty();
                                        trySubmitForm();
                                    }
                                });
                            }
                        })
                );
            })
        }
    }

    return {
        init: function () {
            handleLogin();
        }
    };
})();
