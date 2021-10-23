var token;
var headers;

$(document).ready(function () {

    $("#AlertRoleSelector").change(function () {
        $(this).find("option:selected").each(function () {
            var optionValue = $(this).attr("value");
            optionValue = 'alertbox-' + optionValue
            if (optionValue) {
                $(".alertbox").not("." + optionValue).hide();
                $("." + optionValue).show();
            } else {
                $(".alertbox").hide();
            }
        });
    }).change();


    token = $('input[name="__RequestVerificationToken"]').val();
    headers = {};
    headers['__RequestVerificationToken'] = token;
    headers['__CompanyId'] = $('#CurrentCompanyId').val();
});

function AlertItem(){
    var roleId;
    var alertTypeId;
    var redEnabled;
    var redvalue;
    var amberEnabled;
    var amberValue;
}
function saveAlertSettings() {
    var cmd = [];

    $('.alerts-listing').find('.alertbox').each(function () {
        var roleId = $(this).find('.roleId').val();
        $(this).find('tr').each(function () {
            $(this).find('.alertType').each(function () {
                var alertItem = new AlertItem();
                alertItem.roleId = roleId;
                alertItem.alertTypeId = $(this).val();
                var id = alertItem.roleId + '-' + alertItem.alertTypeId;
                alertItem.redEnabled = $('#RC' + id).prop('checked');
                alertItem.redValue = $('#RV' + id).val();
                alertItem.amberEnabled = $('#AC' + id).prop('checked');
                alertItem.amberValue = $('#AV' + id).val();
                cmd.push(alertItem);
            });
        });
    });
    waitOn();
    var Data = {
        'alertRows': cmd
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/SetAlerts',
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'html',
        success: function (data) {
            waitOff();
            if (r.code !== 0)
                Alert(r.msg);
            else
                SuccessAlert('Successfuly Updated');
        },
        error: ajaxError,
        timeout: 10000
    });
}

