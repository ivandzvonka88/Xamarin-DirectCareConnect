﻿var token;
var headers;
$(document).ready(function () {
    token = $('input[name="__RequestVerificationToken"]').val();
    headers = {};
    headers['__RequestVerificationToken'] = token;
    headers['__CompanyId'] = $('#CurrentCompanyId').val();
    getReqCredentials();
});
var Credentials;

function getReqCredentials() {
    waitOn();
    $.ajax({
        type: 'POST',
        url: srvcUrl +'/GetReqCredentials',
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#credentials').html(data);
            $('.btns').show();

        },
        error: ajaxError,
        timeout: 10000
    });
}

function Cred() {
    var credTypeId;
    var roleId;
    var required;
    var blocking;
}

function saveCredentialSettings() {
    var cmd = [];
    var numberOfRoles = parseInt($('#rolesCount').val());
    $.each($('tr'), function (i) {
        var credId = $(this).find('.credId').val();
        if (credId !== undefined) {
            for (i = 0; i < numberOfRoles; i++) {
                var cred = new Cred();
                cred.credTypeId = credId;
                cred.roleId = $('#roleId' + i).val();
                cred.required = $(this).find('#R' + cred.credTypeId + cred.roleId).prop('checked');
                cred.blocking = $(this).find('#B' + cred.credTypeId + cred.roleId).prop('checked');
                cmd.push(cred);  
            }
        }
    });
    waitOn();
    var Data = {
        'credentialRows': cmd
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/SetReqCredentials',
        headers: headers,
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#credentials').html(data);
            $('.btns').show();
        },
        error: ajaxError,
        timeout: 10000
    });
}
function cancelChanges() {
    getReqCredentials();
}
function openAddNewCredential() {
    $('#credName').val('');
    $('input[name="credRadios"]').prop('checked', false);
    $('.addCredentialModal').modal('show');
}
function saveNewCredential() {
    if ($('#credName').val().length < 10)
        Alert('Alert must be at least 10 characters long');
    else if (!$('#empRadio').prop('checked') && !$('#roleRadio').prop('checked'))
        Alert('Role Specific or Employee Specific needs to be selected');
    else {
        waitOn();
        $('.addCredentialModal').modal('hide');
        var Data = {
            'credName' : $('#credName').val(),
            'roleSpecific': $('#roleRadio').prop('checked')
        };
        $.ajax({
            type: 'POST',
            url: srvcUrl + '/AddNewCredential',
            headers: headers,
            data: JSON.stringify(Data),
            contentType: 'application/json; charset=utf-8',
            dataType: 'html',
            success: function (data) {
                waitOff();
                $('#credentials').html(data);
                $('.btns').show();

            },
            error: ajaxError,
            timeout: 10000
        });



    }

  
}