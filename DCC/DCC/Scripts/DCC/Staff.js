var token;
var headers;
$(document).ready(function () {
    token = $('input[name="__RequestVerificationToken"]').val();
    headers = {};
    headers['__RequestVerificationToken'] = token;
    // For Client/Staff list in left panel
    if ($('.searchNames').length !== 0) {
        $(window).resize(function () {
            $('#width').text($(window).width());
            checksize();
        });
        document.addEventListener('scroll', function (event) {
            checksize();
        });
        checksize();
    }

    //hash check for reload
    var p = window.location.hash.split('#');
    if (p.length === 2) {
        if (p[1].indexOf('PRID') !== -1) {
            var prid = p[1].substr(4);
            if ($('#PRID').val() !== prid)
                getStaffMember(prid);
        }
       
    }
});
// hash change
$(window).on('hashchange', function () {
    var p = window.location.hash.split('#');
    if (p.length === 2) {
        if (p[1].indexOf('PRID') !== -1) {
            var prid = p[1].substr(4);
            if ($('#PRID').val() !== prid)
                getStaffMember(prid);
        }     
    }
});

$(document).on("click", ".btnStaffExpand", function () {
    if ($(this).hasClass('fa-plus-square-o')) {
        $('.staffView').hide();
        $(this).parent().parent().parent().removeClass('col-lg-6').addClass('col-lg-12');
        $(this).removeClass('fa-plus-square-o').addClass('fa-minus-square-o');
        $(this).next().addClass('expanded');
        $(this).parent().parent().find('.boxContainer').addClass('expanded');
        $(this).parent().parent().show(500);
    }
    else
        initialStaffView();
});

function initialStaffView() {
    $('.staffView').hide();
    $('.staffView').parent().removeClass('col-lg-12').addClass('col-lg-6');
    $('.btnStaffExpand').removeClass('fa-plus-minus-o').addClass('fa-plus-square-o');
    $('.staffView').find('.boxContainer').removeClass('expanded');
    $('.staffView').scrollTop(0);
    $('.staffView').show(500);
}

function getStaffMember(id) {
    $("#staffPage").load(srvcUrl+ '/StaffMember?id=' + id);
}
function clearSearch() {
    $('#nm').value = '';
    search();
}
function search() {
    var str = $.trim($('#nm').val()).split("`").join("").toLowerCase();

    $('.nameListItem').each(function () {
        switch ($('#staffSearchOption').val()) {
            case '1':
                if ($(this).text().toLowerCase().indexOf(str) === -1 || $(this).hasClass('inactive'))
                    $(this).hide();
                else
                    $(this).show();

                break;
            case '2':
                if ($(this).text().toLowerCase().indexOf(str) === -1 || !$(this).hasClass('inactive'))
                    $(this).hide();
                else
                    $(this).show();
                break;

            case '3':
                if ($(this).text().toLowerCase().indexOf(str) === -1 || $(this).find('.fa-check').length !== 1)
                    $(this).hide();
                else
                    $(this).show();
                break;

            case '4':
                if ($(this).text().toLowerCase().indexOf(str) === -1 || $(this).find('.fa-check').length === 1)
                    $(this).hide();
                else
                    $(this).show();
                break;
            default:
                if ($(this).text().toLowerCase().indexOf(str) === -1)
                    $(this).hide();
                else
                    $(this).show();
        }
    });

    return false;
}


function openAddStaffCommentModal() {
    waitOn();
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/OpenAddCommentModal',
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#actionModals').html(data);
        },
        error: ajaxError,
        timeout: 10000
    });
}

function addStaffComment() {
    $('#actionModal').modal('hide');

    waitOn();
    var Data = {
        'prId': $('#PRID').val(),
        'comment': $('#staffComment').val()
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/AddComment',
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#staffComments').html(data);
        },
        error: ajaxError,
        timeout: 10000
    });
}

function openEditCredentialModal(i) {
    waitOn();
    var Data = {
        'prId': $('#PRID').val(),
        'credId': i
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/openEditCredentialModal',
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#actionModals').html(data);
        },
        error: ajaxError,
        timeout: 10000
    });
}

function openAddCredentialModal(i) {
    waitOn();
    var Data = {
        'prId': $('#PRID').val(),
        'credTypeId': i
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/openAddCredentialModal',
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#actionModals').html(data);
        },
        error: ajaxError,
        timeout: 10000
    });
}

function getCredentialFileName() {
    $('#credFileName').val($('#credFileNameX').val().split('\\').pop());
}

function updateCredential() {
    var x = $('#credFileName').val().toLowerCase();
    if (x === '' && $('#credId').val() === '0') Alert('No file has been selected!');
    else if ($('#docId').val().length === 0)
        Alert('Please enter a document Id');
    else if ($('#credTypeId').val() === null || $('#credTypeId').val() === 0)
        Alert('Please select a credential type');
    else if ($('#validFrom').val() === '')
        Alert('A start date is required');
    else if ($('#validTo').val() === '')
        Alert('An end date is required');
    else if (new Date($('#validTo').val()) < new Date($('#validFrom').val()))
        Alert('Valid to date needs to be greater than valid from date')
    else {
        files = $('#credFileNameX').get(0).files;
        var data = new FormData();
        //    data.append('files', files[0]);
        data.append('files', $('#credFileNameX')[0].files[0]);
        data.append('prId', $('#PRID').val());
        data.append('credId', $('#credId').val());
        data.append('credTypeId', $('#credTypeId').val());
        data.append('docId', $('#docId').val());
        data.append('validFrom', $('#validFrom').val());
        data.append('validTo', $('#validTo').val());
        waitOn();
        $('#actionModal').modal('hide');
        $.ajax({
            type: 'POST',
            url: srvcUrl + '/UpdateCredential',
            headers: headers,
            contentType: false,
            processData: false,
            data: data,
            dataType: 'html',
            success: function (data) {
                waitOff();
                $('#credentials').html(data);
            },
            error: ajaxError,
            timeout: 100000
        });
    }
}

function openCredentialVerifyModal(i) {
    waitOn();
    var Data = {
        'prId': $('#PRID').val(),
        'credId': i
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/OpenVerifyCredentialModal',
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#actionModals').html(data);
        },
        error: ajaxError,
        timeout: 10000
    });
}

function verifyCredential() {
    var Data = {
        'prId': $('#PRID').val(),
        'credId': $('#credId').val(),
        'verified': true
    };
    $('#actionModal').modal('hide');
    waitOn();
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/VerifyCredential',
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#credentials').html(data);
        },
        error: ajaxError,
        timeout: 10000
    });
}
function openCredentialDeleteModal(i) {
    waitOn();
    var Data = {
        'prId': $('#PRID').val(),
        'credId': i
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/OpenDeleteCredentialModal',
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#actionModals').html(data);
        },
        error: ajaxError,
        timeout: 10000
    });
}
function deleteCredential() {
    var Data = {
        'prId': $('#PRID').val(),
        'credId': $('#credId').val(),
        'docName': $('#docName').val()
    };
    $('#actionModal').modal('hide');
    waitOn();
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/DeleteCredential',
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#credentials').html(data);
        },
        error: ajaxError,
        timeout: 10000
    });
}



function getCredential(id) {
    location = srvcUrl + '/GetCredential?id=' + id;
}

function openDeleteStaffServiceModal(id, clientName, svc) {
    waitOn();
    var Data = {
        'prId': $('#PRID').val(),
        'relId': id,
        'clientName': clientName,
        'svcLong': svc
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/OpenDeleteStaffServiceModal',
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#actionModals').html(data);
        },
        error: ajaxError,
        timeout: 10000
    });
}

function deleteStaffService(id) {
    var Data = {
        'prId': $('#PRID').val(),
        'relId': id
    };
    $('#actionModal').modal('hide');
    waitOn();
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/DeleteStaffService',
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#staffServices').html(data);
        },
        error: ajaxError,
        timeout: 10000
    });
}

function openAddStaffServiceModal() {
    waitOn();
    var Data = {
        'prId': $('#PRID').val()
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/OpenAddStaffServiceModal',
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#actionModals').html(data);
        },
        error: ajaxError,
        timeout: 10000
    });

}

function checkServiceRequirement() {
    var s = $('#service').val().split('-');

    $('#prIdr').val('');
    if (s[2] === '1')
        $('#relationshipX').slideDown();
    else
        $('#relationshipX').slideUp();
}

function addService() {
    var s = $('#service').val().split('-');
    if (s[2] !== '1')
        $('#prIdr').val(1);

    if (s[2] === '1' && $('#prIdr').val() === '')
        Alert('This service requires you to select the staff/client relationship');
    else {
        var Data = {
            'prId': $('#PRID').val(),
            'prIdr': $('#prIdr').val(),
            'clsvId': s[0],
            'clsvidId': s[1]
        };
        $('#actionModal').modal('hide');
        waitOn();
        $.ajax({
            type: 'POST',
            url: srvcUrl + '/AddStaffService',
            data: JSON.stringify(Data),
            contentType: 'application/json; charset=utf-8',
            headers: headers,
            dataType: 'html',
            success: function (data) {
                waitOff();
                $('#staffServices').html(data);
            },
            error: ajaxError,
            timeout: 10000
        });
    }
}

function getPayrollInfo(r) {

    if ($('#selPayrollPeriod').val() !== '0') {

        var Data = {
            'prId': $('#PRID').val(),
            'startDate': $('#selPayrollPeriod').val()
        };
        waitOn();
        $.ajax({
            type: 'POST',
            url: srvcUrl +'/GetpayrollInfo',
            data: JSON.stringify(Data),
            contentType: 'application/json',
            dataType: 'html',
            success: function (data) {
                waitOff();
                $('#staffHours').html(data);
            },
            error: ajaxError,
            timeout: 10000
        });


    }
}

function openNewStaffModal() {
    waitOn();
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/OpenAddStaffMemberModal',
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#actionModals').html(data);
        },
        error: ajaxError,
        timeout: 10000
    });
}

function addStaff() {
    $('#fnz').val($.trim($('#fnz').val()))
    $('#lnz').val($.trim($('#lnz').val()))

    if ($('#lnz').val().length < 2)
        Alert('Please enter a last name');
    else if ($('#fnz').val().length < 2)
        Alert('Please enter a first name');
    else if (!emailCheck($('#emz').val()))
        Alert('Please enter a valid email!');
    else if (!checkPhone($('#clz')) || $('#clz').val().length === 0)
        Alert('Please enter a valid cell phone number!');
    else if ($('#rolez').val() === '0')
        Alert('Role - Please select a staff role!');
    else if ($('#qualifiedTherapistz').prop('checked') && $('#assistantTherapistz').prop('checked'))
        Alert('Cannot be both a master therapist and assistant');
    else if ($('#qualifiedTherapistz').prop('checked') && !$('#occupationalTherapyz').prop('checked') && !$('#speechTherapyz').prop('checked') && !$('#physicalTherapyz').prop('checked'))
        Alert('Please select a discipline for the therapist');
    else if ($('#assistantTherapistz').prop('checked') && !$('#occupationalTherapyz').prop('checked') && !$('#speechTherapyz').prop('checked') && !$('#physicalTherapyz').prop('checked'))
        Alert('Please select a discipline for the assistant therapist');
    else if (($('#occupationalTherapyz').prop('checked') || $('#speechTherapyz').prop('checked') || $('#physicalTherapyz').prop('checked')) &&
        !$('#assistantTherapistz').prop('checked') && !$('#qualifiedTherapistz').prop('checked'))
        Alert('Please check either the Master Therapist or Assistant Therapist Box if selecting a discipline');
    else if ($('#bcbaz').prop('checked') && $('#rbtz').prop('checked'))
        Alert('Cannot be both a board certified behavioral analyst and a behavioral tech');
    else {
        $('#actionModal').modal('hide');
        waitOn();
        var Data = {
            'prid': 0,
            'ln': $('#lnz').val(),
            'fn': $('#fnz').val(),
            'em': $('#emz').val(),
            'cl': $('#clz').val(),
            'qualifiedTherapist': $('#qualifiedTherapistz').prop('checked'),
            'assistantTherapist': $('#assistantTherapistz').prop('checked'),
            'basicProvider': $('#basicProviderz').prop('checked'),
            'occupationalTherapy': $('#occupationalTherapyz').prop('checked'),
            'speechTherapy': $('#speechTherapyz').prop('checked'),
            'physicalTherapy': $('#physicalTherapyz').prop('checked'),
            'BCBA': $('#bcbaz').prop('checked'),
            'RBT': $('#rbtz').prop('checked'),
            'role': $('#rolez').val()
        };
        $.ajax({
            type: 'POST',
            url: srvcUrl + '/AddStaff',
            data: JSON.stringify(Data),
            contentType: 'application/json; charset=utf-8',
            headers: headers,
            dataType: 'json',
            success: getNewStaffMemberSuccess,
            error: ajaxError,
            timeout: 10000
        });
    }
    return false;
}

function getNewStaffMemberSuccess(r) {
    waitOff();
    if (r.er.code === 1) Alert('System Error - ' + r.er.msg);
    else if (r.er.code === 2) Alert('Error - ' + r.er.msg);
    else {
        $(".nameList").prepend('<div class="nameListItem " id="cl' + r.prId + '" onclick="return getStaffMember(' + r.prId + ');">' + r.ln + ' ' + r.fn + '</div>');
        window.location = srvcUrl + '#PRID' + r.prId;

    }
}

function roleChange() {
    if ($('#rolez').val() === 8) {
        $('#qualifiedTherapistz').prop('checked', true);
        $('#assistantTherapistz').prop('checked', false);
        $('#basicProviderz').prop('checked', false);
    }
    else if ($('#rolez').val() === 7) {
        $('#qualifiedTherapistz').prop('checked', false);
        $('#assistantTherapistz').prop('checked', true);
        $('#basicProviderz').prop('checked', false);
    }
    else if ($('#rolez').val() === 1) {
        $('#qualifiedTherapistz').prop('checked', false);
        $('#assistantTherapistz').prop('checked', false);
        $('#basicProviderz').prop('checked', true);
    }
}

function reInviteStaff() {
    waitOn();
    var Data = {
        'prId': $('#PRID').val()
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/ReInviteStaff',
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'json',
        success: genericSuccess,
        error: ajaxError,
        timeout: 10000
    });
    return false;
}

function displayStaffActive(deleted) {
    if (deleted) {
        $('#cl' + $('#PRID').val()).addClass('inactive');
        $('#staffActiveState').text('InActive');
        $('.boxContainer').removeClass('active').addClass('inactive');
        $('#btnStaffActivate').removeClass('fa-toggle-on').addClass('fa-toggle-off');
    }
    else {
        $('#cl' + $('#PRID').val()).removeClass('inactive');
        $('#staffActiveState').text('Active');
        $('.boxContainer').removeClass('inactive').addClass('active');
        $('#btnStaffActivate').removeClass('fa-toggle-off').addClass('fa-toggle-on');
    }
}

function openStaffPersonalModal(i) {
    waitOn();
    var Data = {
        'prId': $('#PRID').val(),
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/OpenStaffPersonalModal',
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#actionModals').html(data);
        },
        error: ajaxError,
        timeout: 10000
    });

}

function updateStaffPersonal() {
    $('#fnx').val($.trim($('#fnx').val()))
    $('#lnx').val($.trim($('#lnx').val()))

    $('#ssnfx').val($('#ssnfx').val().replace(/\D/g, ''));
    if ($('#lnx').val().length < 2)
        Alert('Please enter a last name');
    else if ($('#fnx').val().length < 2)
        Alert('Please enter a first name');
    else if ($('#clx').val().length === 0 || !checkPhone($('#clx')))
        Alert('Cell Phone Number - Please enter a 10 digit phone number!');
    else if ($('#phx').val().length !== 0 && !checkPhone($('#phx')))
        Alert('Secondary Phone Number - Please enter a 10 digit phone number or leave the field blank!');
    else if ($('#sexx').val() === '')
        Alert('Gender - Please select a gender!');
    else if ($('#ssnfx').val().length ==! 9)
        Alert('Requires a 9 digit social security number!');
    else {
        $('#actionModal').modal('hide');
        waitOn();
        var Data = {
            'prid': $('#PRID').val(),
            'em': $('#emx').val(),
            'ln': $('#lnx').val(),
            'fn': $('#fnx').val(),
            'ad1': $('#ad1x').val(),
            'ad2': $('#ad2x').val(),
            'cty': $('#ctyx').val(),
            'st': $('#stx').val(),
            'z': $('#zx').val(),
            'cl': $('#clx').val(),
            'ph': $('#phx').val(),
            'ssnf': $('#ssnfx').val(),
            'dobf': $('#dobfx').val(),
            'sex': $('#sexx').val(),
            'oldEmail': $('#oldEmail').val(),
            'userId': $('#userId').val(),
        };
        $.ajax({
            type: 'POST',
            url: srvcUrl + '/UpdateStaffPersonal',
            data: JSON.stringify(Data),
            contentType: 'application/json; charset=utf-8',
            headers: headers,
            dataType: 'html',
            success: function (data) {
                waitOff();
                $('#staffPersonal').html(data);
            },
            error: ajaxError,
            timeout: 10000
        });
        return false;
    }
}

function openStaffEmploymentModal() {
    waitOn();
    var Data = {
        'prId': $('#PRID').val(),
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/OpenStaffEmploymentModal',
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#actionModals').html(data);
        },
        error: ajaxError,
        timeout: 10000
    });
}

function updateStaffEmployment() {

    $('#ahcccsId').val($.trim($('#ahcccsId').val()));
    $('#CAQH').val($.trim($('#CAQH').val()));
    $('#MedicaidId').val($.trim($('#MedicaidId').val()));
    $('#npi').val($.trim($('#npi').val()));
    var credList = [];
    $(".reqCredentials").each(function () {
        var o = {};
        o.id = $(this).attr("id");
        o.isChecked = $(this).prop('checked');
        credList.push(o);
    });
    if ($('#roleId').val() === '0')
        Alert('Staff member should always have a role');
    else if (($('#roleId').val() === '8' || $('#qualifiedTherapist').prop('checked')) && $('#npi').val().length !== 10) Alert('All master level therapists require a 10 digit NPI');
    else if (($('#roleId').val() === '7' || $('#assistantTherapist').prop('checked')) && $('#speechTherapy').prop('checked') && $('#npi').val().length !== 10) Alert('All SPLAs require a 10 digit NPI');
    else if (($('#roleId').val() === '8' || $('#qualifiedTherapist').prop('checked')) && $('#ahcccsId').val().length !== 6) Alert('All master level therapists require a 6 digit AHCCCS Id');
    else if (($('#roleId').val() === '7' || $('#assistantTherapist').prop('checked')) && $('#speechTherapy').prop('checked') && $('#ahcccsId').val().length !== 6) Alert('All SPLAs require a  a 6 digit AHCCCS Id');

    else {


        $('#actionModal').modal('hide');
        waitOn();
        var Data = {
            'prid': $('#PRID').val(),
            'roleId': $('#roleId').val(),
            'hiredtf': $('#hiredtf').val(),
            'termdt': $('#termdt').val(),
            'CRverf': $('#CRverf').val(),
            'title': $('#title').val(),
            'eId': $('#eId').val(),
            'iSolvedID': $('#iSolvedID').val(),
            'supId': $('#supId').val(),
            'tempsupId': $('#tempsupId').val(),
            'npi': $('#npi').val(),
            'ahcccsId': $('#ahcccsId').val(),
            'CAQH': $('#CAQH').val(),
            'MedicaidId': $('#MedicaidId').val(),
            'qualifiedTherapist': $('#qualifiedTherapist').prop('checked'),
            'assistantTherapist': $('#assistantTherapist').prop('checked'),
            'basicProvider': $('#basicProvider').prop('checked'),
            'speechTherapy': $('#speechTherapy').prop('checked'),
            'occupationalTherapy': $('#occupationalTherapy').prop('checked'),
            'physicalTherapy': $('#physicalTherapy').prop('checked'),
            'BCBA': $('#bcba').prop('checked'),
            'RBT': $('#rbt').prop('checked'),
            'otherRequiredCredentials': credList,
            'zipCodes': $('#zipCodes').val()
        };
        $.ajax({
            type: 'POST',
            url: srvcUrl + '/UpdateStaffEmployment',
            data: JSON.stringify(Data),
            contentType: 'application/json',
            headers: headers,
            dataType: 'json',
            success: function (r) {
                waitOff();
                if (r.er.code !== 0)
                    Alert(r.er.msg);
                else {
                    $('#staffEmployment').html(r.staffEmployment);
                    $('#credentials').html(r.staffCredentials);
                }
            },
            error: ajaxError,
            timeout: 10000
        });
    }
    return false;
}

function getStaffStaffSupervisorList() {
    waitOn();
    var Data = {
        'roleId': $('#roleId').val()
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/GetSupervisorList',
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        success: updateSupervisorList,
        error: ajaxError,
        timeout: 10000
    });
}

function updateSupervisorList(r) {
    waitOff();
    if (r.er.code === 1) Alert('System Error - ' + r.er.msg);
    else if (r.er.code === 2) Alert('Error - ' + r.er.msg);
    else {
        $('#supId').empty();
        $('#tempsupId').empty();
        $('#supId').append($('<option>').val('0').text(''));
        $('#tempsupId').append($('<option>').val('0').text(''));

        if (r.hasSupervisors) {
            for (var i = 0; i < r.supervisors.length; i++) {
                $('#supId').append($('<option>').val(r.supervisors[i].id).text(r.supervisors[i].name));
                $('#tempsupId').append($('<option>').val(r.supervisors[i].id).text(r.supervisors[i].name));
            }
            if ($('#supId option[value="' + $('#supId').val() + '"]').length < 1)
                $('#supId').val('0');
            else
                $('#supId').val($('#supId').val());

            if ($('#tempsupId option[value="' + $('#tempsupId').val() + '"]').length < 1)
                $('#tempsupId').val('0');
            else
                $('#tempsupId').val($('#tempsupId').val());
            $('#supBlock').show();
            $('#tempsupBlock').show();
        }
        else {
            $('#supId').val('0');
            $('#tempsupId').val('0');
            $('#supBlock').hide();
            $('#tempsupBlock').hide();
        }
    }
}

function openSendMessageModal(i) {
    waitOn();
    var Data = {
        'prId': $('#PRID').val(),
        'fn': $('#stFn').text(),
        'ln': $('#stLn').text()
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/OpenSendStaffMessageModal',
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#actionModals').html(data);
        },
        error: ajaxError,
        timeout: 10000
    });

}


function genericSuccess(r) {
    waitOff();
    if (r.code === 1) Alert('System Error - ' + r.msg);
    else if (r.code === 2) Alert('Error - ' + r.msg);
    else
        SuccessAlert(r.msg);
}

function getPayrollInfoSuccess(r) {
    waitOff();
    if (r.er.code === 1) Alert('System Error - ' + r.er.msg);
    else if (r.er.code === 2) Alert('Error - ' + r.er.msg);
    else {
        displayPayrollInfo(r);
    }
}

function checkStaffActiveState() {
    if ($('#btnStaffActivate').hasClass('fa-toggle-off'))
        toggleStaffActive(false);
    else
        toggleStaffActive(true);
}

function toggleStaffActive(deleted) {
    var Data = {
        'prId': $('#PRID').val(),
        'deleted': deleted
    };
    $('#warningModal').modal('hide');
    waitOn();
    $.ajax({
        type: 'POST',
        url: srvcUrl +'/ToggleStaffActive',
        data: JSON.stringify(Data),
        contentType: 'application/json',
        headers: headers,
        dataType: 'json',
        success: toggleStaffActiveSuccess,
        error: ajaxError,
        timeout: 10000
    });
}

function toggleStaffActiveSuccess(r) {
    waitOff();
    if (r.er.code === 1) Alert('System Error - ' + r.er.msg);
    else if (r.er.code === 2) Alert('Error - ' + r.er.msg);
    else {
        displayStaffActive(r.deleted);
    }
}

function goToClientPage(id) {
    window.location.href = '/Clients#CLSVID' + id;
}
function selectionProviderHoursChanged() {
    if ($('#providerPeriodSelect').val() != 0) {
        var Data = {
            'providerId': $('#PRID').val(),
            'periodId': $('#providerPeriodSelect').val()
        };
        waitOn();
        $.ajax({
            type: 'POST',
            url: srvcUrl + '/GetProviderHours',
            data: JSON.stringify(Data),
            contentType: 'application/json; charset=utf-8',
            headers: headers,
            dataType: 'html',
            success: function (r) {
                waitOff();
                $('#staffHours').html(r);
            },
            error: ajaxError,
            timeout: 10000
        });
    }
}
function viewSessionLocations(SessionType, SessionId) {
    waitOn();
    $.ajax({
        type: 'GET',
        url: srvcUrl + '/ViewSessionLocations?SessionType=' + SessionType + "&SessionId=" + SessionId,
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'html',
        success: function (r) {
            waitOff();
            $('#actionModals').html(r);
        },
        error: ajaxError,
        timeout: 10000
    });
}

function toggleMFA(i) {
    waitOn();
    $.ajax({
        type: 'GET',
        url: srvcUrl + '/ToggleMFA?userId=' + i,
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'json',
        success: function (r) {
            waitOff();
            if (r) {
                $('#staffMFAText').text('MFA Enabled');
                $('#btnStaffMFA').removeClass('fa-toggle-off');
                $('#btnStaffMFA').addClass('fa-toggle-on');
            }
            else {
                $('#staffMFAText').text('MFA Disabled');
                $('#btnStaffMFA').removeClass('fa-toggle-on');
                $('#btnStaffMFA').addClass('fa-toggle-off');
            }

        },
        error: ajaxError,
        timeout: 10000
    });
}