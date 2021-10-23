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
        if (p[1].indexOf('GUARDIANID') !== -1) {
            var gId = p[1].substr(10);
            if ($('#GUARDIANID').val() !== gId)
                getGuardian(gId);
        }
    }
});
// hash change
$(window).on('hashchange', function () {
    var p = window.location.hash.split('#');
    if (p.length === 2) {
        if (p[1].indexOf('GUARDIANID') !== -1) {
            var gId = p[1].substr(10);
            if ($('#GUARDIANID').val() !== gId)
                getGuardian(gId);
        }
    }
});

// expand client window view
$(document).on("click", ".btnClientExpand", function () {
    if ($(this).hasClass('fa-plus-square-o')) {
        $('.clientView').hide();
        $(this).parent().parent().parent().removeClass('col-lg-6').addClass('col-lg-12');
        $(this).removeClass('fa-plus-square-o').addClass('fa-minus-square-o');
        $(this).next().addClass('expanded');
        $(this).parent().parent().find('.boxContainer').addClass('expanded');
        $(this).parent().parent().show(500);
    }
    else
        initialClientView();
});

// initialize client window view
function initialClientView() {
    $('.clientView').hide();
    $('.clientView').parent().removeClass('col-lg-12').addClass('col-lg-6');
    $('.btnClientExpand').removeClass('fa-plus-minus-o').addClass('fa-plus-square-o');
    $('.clientView').find('.boxContainer').removeClass('expanded');
    $('.clientView').scrollTop(0);
    $('.clientView').show(500);
}

function getGuardian(id) {
    $("#guardianPage").load(srvcUrl + '/Guardian?id=' + id);
}
function clearSearch() {
    $('#nm').value = '';
    search();
}
function search() {
    var str = $.trim($('#nm').val()).split("`").join("").toLowerCase();

    $('.nameListItem').each(function () {
        switch ($('#guardianSearchOption').val()) {
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


            case '5':
                if ($(this).text().toLowerCase().indexOf(str) === -1 || $(this).find('.fa-check').length !== 1 || $(this).find('.clientCount').length === 1)
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
function openGuardianActivateDeactivateModal() {
    waitOn();
    $.ajax({
        type: 'GET',
        url: srvcUrl + '/OpenGuardianActivateDeactivateModal?id=' + $('#GUARDIANID').val(),
        contentType: 'application/json; charset=utf-8',
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#modals').html(data);
        },
        error: ajaxError,
        timeout: 10000
    });
}

function toggleGuardianActive(deleted) {
    var Data = {
        'guardianUId': $('#GUARDIANID').val(),
        'deleted': deleted
    };
    $('#actionModal').modal('hide');
    waitOn();
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/ToggleGuardian',
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'json',
        success: function (r) {
            waitOff();
            if (r.er.code !== 0)
                Alert(r.er.msg);
            else {
                $('.nameList').html(r.nameList);
                $('#guardianPage').html(r.guardianPage);
            }
        },
        error: ajaxError,
        timeout: 10000
    });
}

function openNewGuardianModal() {
    waitOn();
    $.ajax({
        type: 'GET',
        url: srvcUrl + '/OpenNewGuardianModal',
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#actionModal').modal('hide');

            $('#modals').html(data);
        },
        error: ajaxError,
        timeout: 10000
    })
}

function addGuardian() {
    $('#firstName').val($.trim($('#firstName').val()))
    $('#lastName').val($.trim($('#lastName').val()))


    if ($('#lastName').val().length < 2)
        Alert('Please enter a last name');
    else if ($('#firstName').val().length < 2)
        Alert('Please enter a first name');
    else if (!emailCheck($('#email').val()))
        Alert('Email - Please enter a valid email!');
    else if (!checkPhone($('#phone')))
        Alert('Cell - Please enter a valid cell phone number!');
    else {
        var Data = {
            'firstName': $('#firstName').val(),
            'lastName': $('#lastName').val(),
            'email': $('#email').val(),
            'phone': $('#phone').val(),
            'addressLine1': $('#addressLine1').val(),
            'addressLine2': $('#addressLine2').val(),
            'city': $('#city').val(),
            'state': $('#_StateSelector').val(),
            'postalCode': $('#postalCode').val()
        };
        $('#actionModal').modal('hide');
        waitOn();
        $.ajax({
            type: 'POST',
            url: srvcUrl + '/AddGuardian',
            data: JSON.stringify(Data),
            contentType: 'application/json; charset=utf-8',
            headers: headers,
            dataType: 'json',
            success: function (r) {
                waitOff();
                if (r.er.code !== 0)
                    Alert(r.er.msg);
                else {
                    clearSearch();
                    $('.nameList').html(r.nameList);
                    $('#guardianPage').html(r.guardianPage);
                }
             
            },
            error: ajaxError,
            timeout: 10000
        })
    }
}

function openGuardianEditModal() {
    waitOn();
    $.ajax({
        type: 'GET',
        url: srvcUrl + '/OpenGuardianEditModal?id=' + $('#GUARDIANID').val(),
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#modals').html(data);
        },
        error: ajaxError,
        timeout: 10000
    })
}

function updateGuardian() {
    $('#firstName').val($.trim($('#firstName').val()))
    $('#lastName').val($.trim($('#lastName').val()))
    if ($('#lastName').val().length < 2)
        Alert('Please enter a last name');
    else if ($('#firstName').val().length < 2)
        Alert('Please enter a first name');
    else if (!emailCheck($('#email').val()))
        Alert('Email - Please enter a valid email!');
    else if (!checkPhone($('#phone')))
        Alert('Cell - Please enter a valid cell phone number!');
    else
    {
        var Data = {
            'guardianUId': $('#GUARDIANID').val(),
            'firstName': $('#firstName').val(),
            'lastName': $('#lastName').val(),
            'email': $('#email').val(),
            'phone': $('#phone').val(),
            'addressLine1': $('#addressLine1').val(),
            'addressLine2': $('#addressLine2').val(),
            'city': $('#city').val(),
            'state': $('#stateSelect').val(),
            'postalCode': $('#postalCode').val()

        };
        waitOn();
        $('#actionModal').modal('hide');
        $.ajax({
            type: 'POST',
            url: srvcUrl + '/UpdateGuardian',
            data: JSON.stringify(Data),
            contentType: 'application/json; charset=utf-8',
            headers: headers,
            dataType: 'html',
            success: function (r) {
                waitOff();
                $('#guardianProfile').html(r);
            },
            error: ajaxError,
            timeout: 10000
        })


    }

   
}

function inviteGuardian() {
    waitOn();
    var Data = {
        'id': $('#GUARDIANID').val()
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/InviteGuardian',
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

function openAddClientModal() {
    waitOn();
    $.ajax({
        type: 'GET',
        url: srvcUrl + '/OpenAddClientModal',
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'html',
        success: function (r) {
            waitOff();
            $('#modals').html(r);
        },
        timeout: 10000
    })
}
function addClient() {
    if ($('#selectClient').val() === '')
        Alert('Please select a client');
    else if ($('#selectRelationship').val() === '')
        Alert('Please select relationship');
    else {

        var Data = {
            'guardianUId': $('#GUARDIANID').val(),
            'clientId': $('#selectClient').val(),
            'relationshipId': $('#selectRelationship').val()
        };
        waitOn();
        $('#actionModal').modal('hide');
        $.ajax({
            type: 'POST',
            url: srvcUrl + '/AddClient',
            data: JSON.stringify(Data),
            contentType: 'application/json; charset=utf-8',
            headers: headers,
            dataType: 'html',
            success: function (r) {
                waitOff();
                $('#guardianClients').html(r);
            },
            error: ajaxError,
            timeout: 10000
        })
    }
}

function openDeleteClientModal(i, j) {
    waitOn();
    $.ajax({
        type: 'GET',
        url: srvcUrl + '/OpenDeleteClientModal?clientId=' + i +"&clientName=" + j,
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'html',
        success: function (r) {
            waitOff();
            $('#modals').html(r);
        },
        error: ajaxError,
        timeout: 10000
    })
}

function deleteClient() {
    var Data = {
        'guardianUId': $('#GUARDIANID').val(),
        'clientId': $('#clientId').val()
    };
    waitOn();
    $('#actionModal').modal('hide');
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/DeleteClient',
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'html',
        success: function (r) {
            waitOff();
            $('#guardianClients').html(r);
        },
        error: ajaxError,
        timeout: 10000
    })
}


function genericSuccess(r) {
    waitOff();
    if (r.code === 1) Alert('System Error - ' + r.msg);
    else if (r.code === 2) Alert('Error - ' + r.msg);
    else
        SuccessAlert(r.msg);
}


function toggleGuardianMFA(i) {
    waitOn();
    $.ajax({
        type: 'GET',
        url: srvcUrl + '/ToggleGuardianMFA?guardianUserId=' + i,
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