var CurrentPage = function () {
    function init() {
        $('#kt_page').removeClass('d-flex').removeClass('flex-row').removeClass('flex-column-fluid');

        $('#kt_content_container').removeClass('container-xxl');
    }

    return {
        init: init
    };
}();
