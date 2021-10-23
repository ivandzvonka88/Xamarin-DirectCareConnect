$(document).ready(function () {
    token = $('input[name="__RequestVerificationToken"]').val();
    headers = {};
    headers['__RequestVerificationToken'] = token;
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

    getDenialReasons();
    setAutocomplete();
});
//});
function resetFilter() {
    if ($("#tags").val().length == 0) {
        $('.divClient').each(function (index, elem) {
            $(this).show();
        });
    }
}
function clearFilter() {
    $("#tags").val('');
    $('.divClient').each(function (index, elem) {
        $(this).show();
    });
}
function c() {
    console.log($("#tags").val());
    if ($("#tags").val() == "") {
        $('.divClient').each(function (index, elem) {
            $(this).show();
        });
    }
}
// Custom
function reset() {
    $('.clickable').each(function (index, tr) {
        var rowid = $(this).attr('id').replace('dtitle-', '');

        $('#ddata-' + rowid + ' .ddate-data').html('');
        $(this).find('.cbox').removeClass('disp').addClass('disp-none');
        if ($(this).find('.cbox input').is(':checked'))
            $(this).find('.cbox input').prop('checked', false);


        if ($('#ddata-' + rowid).is(':visible')) {
            $('#ddate-' + rowid).find('i').removeClass('fa-plus-square').addClass('fa-minus-square');
        }
        else {
            $('#ddate-' + rowid).find('i').removeClass('fa-minus-square').addClass('fa-plus-square');
        }
    });

}

function showBulkStatusChange() {
    const selClaims = $('input[type=checkbox].claimSelect:checked');

    if (selClaims.length <= 0) {
        swal("Therapy Corner 2.0", "Select at least one claim", "error");
        return;
    }

    const currentStatus = parseInt($('#claimStatus').val());

    $.ajax({
        url: srvcUrl + '/GetValidClaimStatuses',
        method: 'POST',
        headers,
        data: JSON.stringify({ currentClaimStatusId: currentStatus }),
        contentType: 'application/json; charset=utf-8',
        success: function (resp) {
            $('#bulkStatusCd').empty();
            $('#bulkStatusCd').append('<option value="">Choose</option>');

            resp.forEach(status => {
                if (status.claimstatusid != currentStatus) {
                    $('#bulkStatusCd').append('<option value="' + status.claimstatusid + '">' + status.name + '</option>');
                }
            });

            $('.modal.bulkStatusChangeModal').modal();
        }
    });

    const arrClaimIDs = selClaims.map((idx, x) => {
        return $(x).data('claimId').toString();
    }).toArray();
    $('#bulkStatusChgClaimIds').val(arrClaimIDs.join(','));
}

function saveBulkStatusChange() {
    const newStatusCd = $('#bulkStatusCd').val();

    if (!newStatusCd || newStatusCd == '') {
        swal("Therapy Corner 2.0", "Choose a new status", "error");
        return;
    }

    waitOn();

    data = {
        status: $('#bulkStatusCd').val(),
        claimIds: $('#bulkStatusChgClaimIds').val(),
        isHCFA: true
    };

    $.ajax({
        type: 'POST',
        headers: headers,
        url: srvcUrl + '/UpdateBatchStatus',
        data: JSON.stringify(data),
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        success: function (data) {
            $('.modal.bulkStatusChangeModal').modal('hide');
            waitOff();
            filterChange();
        }
    });
}

function getClients(payerId, elem, selfPay, clid = null, isGovtPgrm, isDirect = false, flag = 0, Tid = 0) {
    $('#selAllClient').prop('checked', false);
    $('.chkSelAll').prop('checked', false);
    selectAllClients();
    var unselect = false;
    if (isDirect) {
        if (isGovtPgrm) {
            $('#iFilter>option:eq(3)').prop('selected', true);
            $('#iFilter').val("3");
            $('#lnkDownloadHCFA').html("Download DDD File");
            $('#iFilter option[data-status-type="G"]').show();
            $('#iFilter option[data-status-type="I"]').hide();
        }
        else if (selfPay) {
            $('#iFilter>option:eq(9)').prop('selected', true);
            $('#iFilter').val("9");
            $('#lnkDownloadHCFA').css('display', 'none');
            $('#iFilter option[data-status-type="G"]').hide();
            $('#iFilter option[data-status-type="I"]').hide();
        }
        else {
            $('#iFilter>option:eq(1)').prop('selected', true);
            $('#iFilter').val("1");
            $('#lnkDownloadHCFA').html("Download All HCFA");
            $('#iFilter option[data-status-type="G"]').hide();
            $('#iFilter option[data-status-type="I"]').show();
        }
        $('#lnkDownloadHCFA').removeClass('disp-none').addClass('disp');
    }
    $('.nameListItem').removeClass('active-tab');
    if (!unselect) $(elem).addClass('active-tab');
    if ((payerId == undefined || payerId == '') && !selfPay) {

        swal("Therapy Corner 2.0", "Select an insurance", "warning");
        return;
    }
    $("#hdnPyrId").val(payerId);
    $("#hdnTierId").val(Tid);
    if (flag == "0") {
        if (isGovtPgrm) flag = 2;
        else flag = 0;
        //flag = $("#hdnInsFlag").val();
    }
    $("#hdnInsFlag").val(flag);
    $("#hdnPayerClid").val(clid);

    if (selfPay)
        $("#hdnSelfPay").val('1');
    else {
        $("#hdnSelfPay").val('0');
    }
    $('#apploader').removeClass('disp-none').addClass('disp');
    var rowid = $(elem).attr('id');
    var tierId = -1;
    if (isGovtPgrm) {
        tierId = Tid;
    }
    var Data = {
        'payerId': payerId,
        'status': $('#iFilter').val(),
        'selfpay': selfPay,
        'tierId': tierId,
        'insuranceFlag': flag,
        'isGovtIns': isGovtPgrm,
        'dddClientFlag': $('#ddd_non_ddd').val()
    };
    console.log(Data);
    console.log(moment())
    $.ajax({
        type: 'POST',
        headers: headers,
        url: srvcUrl + '/ClientList',
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        dataType: 'html',
        success: function (data) {
            console.log(moment())
            //console.log(data);
            waitOff();
            $("#hdnGovtPrgmSelected").val(isGovtPgrm);
            $("#hdnTierId").val(tierId);
            $('.clientDet').html(data);
            var availableTags = [];
            $('.clientName').each(function (index, elem) {
                var obj = {};
                obj["id"] = $(this).find('#clientId').val();
                obj["name"] = $(this).text();
                //remove duplicates from string[] claimIds. (ES6)
                obj["claims"] = [...new Set($(this).find('#claimIds').val().split(','))].join();
                availableTags.push(obj);
            });
            $("#tags").autocomplete({
                source: function (request, response) {
                    //data :: JSON list defined
                    var array = ($.map(availableTags, function (value, key) {
                        return {
                            label: value.name + " Claim Ids: " + value.claims,
                            value: value.id
                        }
                    }));
                    response($.ui.autocomplete.filter(array, request.term));
                },
                minlength: 0,
                //select
                select: function (e, ui) {
                    event.preventDefault();
                    $("#tags").val(ui.item.label);
                    filterClients(ui.item.value);
                    return false;
                }
            });
            $('#apploader').removeClass('disp').addClass('disp-none');
        },
        error: ajaxError,
        timeout: 130000
    });
}

function generateInvoice() {

}

function getSelfPayClients(elem) {
    $('#iFilter>option:eq(9)').prop('selected', true);
    $('#iFilter').val("9");
    $('#iFilter option[data-status-type="G"]').hide();
    $('#iFilter option[data-status-type="I"]').hide();
    $('#lnkDownloadHCFA').removeClass('disp').addClass('disp-none');
    $('.nameListItem').removeClass('active-tab');
    $(elem).addClass('active-tab');
    $("#hdnPyrId").val('');
    $("#hdnSelfPay").val('1');
    $("#hdnGovtPrgmSelected").val(false);
    $("#hdnTierId").val('');
    $('#apploader').removeClass('disp-none').addClass('disp');
    $('.dropdown-menu-right .edtbtn.genInv').removeClass('disp-none');

    //var rowid = $(elem).attr('id');
    var Data = {
        'status': $('#iFilter').val(),
        'selfpay': true
    };
    $.ajax({
        type: 'POST',
        headers: headers,
        url: srvcUrl + '/ClientList',
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        dataType: 'html',
        success: function (data) {
            // console.log(data);
            waitOff();

            $('.clientDet').html(data);
            var availableTags = [];
            $('.clientName').each(function (index, elem) {
                var obj = {};
                obj["id"] = $(this).find('#clientId').val();
                obj["name"] = $(this).text();
                availableTags.push(obj);
            });
            $("#tags").autocomplete({
                source: function (request, response) {
                    //data :: JSON list defined
                    var array = ($.map(availableTags, function (value, key) {
                        return {
                            label: value.name,
                            value: value.id
                        }
                    }));
                    response($.ui.autocomplete.filter(array, request.term));
                },
                minlength: 0,
                //select
                select: function (e, ui) {
                    event.preventDefault();
                    $("#tags").val(ui.item.label);
                    filterClients(ui.item.value);
                    return false;
                }
            });
            $('#apploader').removeClass('disp').addClass('disp-none');
        },
        error: ajaxError,
        timeout: 130000
    });
}

function filterClients(clientId) {
    $('.divClient').each(function (index, elem) {
        if ($(this).find('#clientId').val() == clientId) {
            $(this).show();
            //force click the client name after the filter
            $(this).find('.clickable').click();
        } else {
            $(this).hide();
        }
    });
}
var defaultReason;
var reasonList;
// Loading Denial reasons //
function getDenialReasons() {
    $.ajax({
        type: 'POST',
        headers: headers,
        url: srvcUrl + '/DenialReasonList',
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        success: function (data) {
            waitOff();
            reasonList = data;
            //defaultReason = $.grep(reasonList, function (v) {
            //    return $.trim(v.id) == "1";
            //});
        },

        error: ajaxError,
        timeout: 10000
    });
}

function setAutocomplete() {
    $("#txtDedReasons").autocomplete({
        source: function (request, response) {
            //data :: JSON list defined
            console.log(request.term.length);
            var array = ($.map(reasonList, function (value, key) {
                return {
                    label: value.id + ': ' + value.name,
                    value: $.trim(value.id)
                }
            }));
            response($.ui.autocomplete.filter(array, request.term));
        },
        open: function () {
            $("ul.ui-menu").width($(this).innerWidth());
        },
        minlength: 0,
        //select
        select: function (e, ui) {
            event.preventDefault();
            $('#hdnReasonValId').val(ui.item.value);
            $("#txtDedReasons").val(ui.item.label);
            return false;
        }
    }).val($("#hdbDefaultReason").val());//.val('Deductible Amount');//.data('autocomplete')._trigger('select');
    //$("#txtDedReasons").val(defaultReason[0].name);
    $("#txtReasons").autocomplete({
        source: function (request, response) {
            //data :: JSON list defined
            var array = ($.map(reasonList, function (value, key) {
                return {
                    label: value.id + ': ' + value.name,
                    value: value.id
                }
            }));
            response($.ui.autocomplete.filter(array, request.term));
        },
        open: function () {
            $("ul.ui-menu").width($(this).innerWidth());
        },
        minlength: 0,
        //select
        select: function (e, ui) {
            event.preventDefault();
            $('#hdnReasonDenialValId').val(ui.item.value);
            $("#txtReasons").val(ui.item.label);
            return false;
        }
    });
    $("#txtbatchReasons").autocomplete({
        source: function (request, response) {
            //data :: JSON list defined
            var array = ($.map(reasonList, function (value, key) {
                return {
                    label: value.id + ': ' + value.name,
                    value: value.id
                }
            }));
            response($.ui.autocomplete.filter(array, request.term));
        },
        open: function () {
            $("ul.ui-menu").width($(this).innerWidth());
        },
        minlength: 0,
        //select
        select: function (e, ui) {
            event.preventDefault();
            $('#hdnReasonBatchDenialValId').val(ui.item.value);
            $("#txtbatchReasons").val(ui.item.label);
            return false;
        }
    });

}

function ClaimDenial() {
    waitOff();
    displayClaimsDenied();

}

function displayClaimsDenied() {
    $('#claimDeniedMsg').html('Denied Claims<br/>For: ' + $('#clFn').text());
    $('.deniedClaimsModal').modal('show');
}

function deductAmt() {
    $('.deductibleModal').modal('hide');
    waitOn();
    DeductAmount();
}

function DeductAmount() {
    waitOff();
    displayDeductAmount();

}

function displayDeductAmount() {
    $('#deductibleMsg').html('Amount Deducted <br/>For: ' + $('#clFn').text());
    $('.deductibleDisplayModal').modal('show');
}


function showCommentBox() {

    $('.commentModal').modal('show');

}

function editInsurancePolicy() {
    $('.insuarancePolicyModal').modal('show');
}

function ManagePolicy(insPolicyId) {
    var Data = {
        'companyId': $('#companySelect' + ' :selected').val(),
        'phone': $('#phone').val(),
        'firstName': $('#firstName').val(),
        'addressLine1': $('#addressLine1').val(),
        'lastName': $('#lastName').val(),
        'addressLine2': $('#addressLine2').val(),
        'genderId': $('#genderSelect :selected').val(),
        'city': $('#city').val(),
        'dob': $('#idob').val(),
        'state': $('#stateSelect :selected').val(),
        'insuredIdNo': $('#insuredIdNo').val(),
        'postalCode': $('#postalCode').val(),
        'mcid': $('#mcid').val(),
        'policyGroupNumber': $('#policyGroupNumber').val(),
        'patientIdNo': $('#patientIdNo').val(),
        'insRelId': $('.policyModal #atcRelId' + ' :selected').val(),
        'isPrimary': $('#isPrimary').is(':checked'),
        'startDate': $('#startDate').val(),
        'endDate': $('.policyModal #endDate').val(),
        'insurancePolicyID': insPolicyId,
        'clientId': $('#CLSVID').val(),
    };
    waitOn();
    $('.policyModal').modal('hide');
    $.ajax({
        type: 'POST',

        url: srvcUrl1 + '/clients/ManagePolicy',
        //url: srvcUrl + '/ManagePolicy',

        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'html',
        success: function (data) {
            console.log("response", data);

            waitOff();
            $('#insurance').empty().html(data);
        },
        error: function (jqXHR, textStatus, errorThrown) {
            console.log('error', textStatus);
        }
    });
    console.log("Div Date", Data);
}

// add new val xxxxxx
function OpenManagePolicyView(i) {
    var clientID = $('#CLSVID').val();
    waitOn();
    $.ajax({
        type: 'POST',
        url: '/clients/OpenAddPolicyView?clientid=' + clientID + '&insurancePolicyId=' + i,
        dataType: 'html',
        headers: headers,
        success: function (data) {
            $('#modals').html(data);
            $('#actionModal').modal('show');
            waitOff();
        },
        error: ajaxError
    });
}
// end add new val xxxxx

// This should not be changed in any case since it is structure required by 
//third part plugin whichis being used for multiselect and searchable drop down for diagnosis codes.
function transformDataForMultiSelect(data) {
    var toReturn = [];
    $.each(data, function (index, item) {
        toReturn.push({ text: item.Text + " (" + $.trim(item.Value) + ")", value: $.trim(item.Value), hidden: false, disabled: false, selected: item.Selected });
    });
    return toReturn;
}
// Insuranc epolicy modal //

///xxxxxx val new
function openaddPolicyModal(i, isFirstPolicy, isAddNew) {

    var id = i;

    if (i != 0) {
        OpenManagePolicyView(id, isFirstPolicy, isAddNew);
    }
    else {
        OpenManagePolicyView(0, isFirstPolicy, isAddNew);
    }
}
//xxxxxxx end val new



// Email //
function sendEmail() {
    var email = '';
    var subject = 'This is a test mail';
    var message = 'testing mail from DCC';
    var phone = '';
    var isEmail = true;
    //var path = 'D:/testingattachment.txt';
    var path = "";
    //var path = "C:/Users/Public/Documents/testingdocument.txt";
    var data = {
        'email': email,
        'subject': subject,
        'message': message,
        'phone': phone,
        'ismail': isEmail,
        'filepath': path
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl1 + '/SkilledBilling/Communication',
        data: JSON.stringify(data),
        headers: headers,
        dataType: 'json',
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            console.log(data);
        },
        error: function (jqXHR, textStatus, errorThrown) {
            alert('error');
        }
    });
}
// functions for left side scroll search Admin/Clients/Providers
function checksize() {
    var body = document.body.getBoundingClientRect();

    var x = document.getElementById('sMarker').getBoundingClientRect();

    var relFromTop = x.top - body.top;

    if (x.top < 0) // all elements above offscreen
        posY = 5;
    else if (x.top < relFromTop) // some elements above offscreen
        posY = x.top + 5;
    else
        posY = relFromTop; // 

    if ($(window).width() < 768) {

        $('.nameList').css('overflow-y', 'scroll');
        $('.nameList').css('position', 'relative');
        $('.nameList').css('width', '90%');
        $('.searchNames').css('position', 'relative');
        $('.nameList').css('height', '200px');

    }
    else {
        $('.searchNames').css('position', 'fixed');
        $('.nameList').css('width', '230px');
        $('.nameList').css('overflow-y', 'scroll');
        $('.nameList').css('position', 'fixed');
        $('.searchNames').css('top', posY + 'px');
        $('.nameList').css('height', $(window).height() - posY - 40);
        $('.nameList').css('top', posY + 40 + 'px');
    }
}

function addScrollToMultiselect() {

    $('.dCodeDDCL .dropdown-menu ').css("overflow-y", "scroll");
    $('.dCodeDDCL .dropdown-menu ').css("height", "168");
    $('.dCodeDDCL .form-control ').css("overflow-y", "scroll");
    $('.dCodeDDCL .form-control ').css("max-height", "72");
    $('.dCodeDDCL .form-control ').css("height", "72");

}
function openWaver() {
    $('.DDDWaverModel').modal('show');
}