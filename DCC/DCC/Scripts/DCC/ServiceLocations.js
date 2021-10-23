$(document).ready(function () {
    getServiceLocation();
});

var config;


function getServiceLocation() {
    waitOn();

    $.ajax({
        type: 'POST',
        url: '/ServiceLocations/GetServiceLocation',
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        success: getServiceLocationSuccess,
        error: ajaxError,
        timeout: 10000
    });
}
function getServiceLocationSuccess(r) {
    waitOff();
    if (r.er.code === 1) Alert('System Error - ' + r.er.msg);
    else if (r.er.code === 2) Alert('Error - ' + r.er.msg);
    else {
        $('.regTable').show(); 

    }
}