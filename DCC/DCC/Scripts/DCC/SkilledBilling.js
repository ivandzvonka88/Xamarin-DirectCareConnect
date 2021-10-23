$(document).ready(function () {
    /*step 1*/
    var s1 = $("#menu-Payor a");
    s1.click(function (e) {
        $('.row-payor').hide();
        $('.ddata-Payor').removeClass('disp').removeClass('disp-none').addClass('disp-none');
        reset();
        var rowid = $(this).attr('id');
        $('#tr-' + rowid).toggle();
    });
    /*step 2*/
    var s2 = $(".clickable");
    s2.click(function (e) {

        reset();
        
        var rowid = $(this).attr('id').replace('dtitle-', '');
        var dta = $('#ddata-' + rowid);
        // dta.toggle();
        console.log('#ddata-' + rowid);

        /*checkbox*/
        var cbox = $(this).find('.cbox');
        //if (cbox.hasClass('disp-none')) { cbox.removeClass('disp-none').addClass('disp'); }
        //else if (cbox.hasClass('disp')) { cbox.removeClass('disp').addClass('disp-none'); }

        if (dta.hasClass('disp-none')) {
            $('.ddata-Payor').removeClass('disp').addClass('disp-none');
            dta.removeClass('disp-none').addClass('disp');
            cbox.removeClass('disp-none').addClass('disp');

            /*load all clients data here using ajax- loads on each click*/
            $('#ddata-' + rowid + ' .ddate-data').html($('.dummydata').html());
            $('#ddata-' + rowid + ' .ddate-data').show();
        }
        else if (dta.hasClass('disp')) {
            dta.removeClass('disp').addClass('disp-none');
            cbox.removeClass('disp').addClass('disp-none');
        }
    });
    /*step 3*/
    var s3 = $(".ddate-btn");
    s3.click(function (e) {
        var rowid = $(this).attr('id').replace('ddate-', '');
        var parentRow = '#ddata-' + rowid.substr(0, rowid.lastIndexOf("-"));
        $('#ddata-' + rowid).toggle();
        //if (isEmpty($('#ddata-' + rowid))) {
        //    $(parentRow + ' .ddate-data').html('');
        //    $('#ddata-' + rowid).html($('.dummydata').html());
        //} else { $('#ddata-' + rowid).html(''); }
        /*if using ajax to reload the content*/
    });
    /*step 4 - link title- clients*/
    //var s4 = $(".dtitle-link");
    //s4.click(function (e) {
    //    e.cancelBubble = true;
    //    if (e.stopPropagation) e.stopPropagation();
    //    /*write your click event here*/
    //});

    /*step 5 - checkbox approve all - stop triggering other events*/
    var s5 = $(".cbox input");
    s5.click(function (e) {
        e.cancelBubble = true;
        if (e.stopPropagation) e.stopPropagation();
        /*write your click event here*/
    });

    /*step 6 - edit button - stop triggering other events*/
    var s6 = $(".edt");
    s6.click(function (e) {
        e.cancelBubble = true;
        if (e.stopPropagation) e.stopPropagation();
        /*write your click event here*/
    });

    /*step 6 - checkbox approve all */
    //s5.change(function (e) {
    //    var rowid = $(this).attr('id').replace('cb-', '');
    //    if (this.checked) {
    //        var returnVal = confirm("Are you sure?");
    //        $(this).prop("checked", returnVal);
    //        if (returnVal) {
    //            //$('#ddata-' + rowid).toggle();
    //            //$('#dtitle-' + rowid + ' .cbox').removeClass('disp').addClass('disp-none');
    //            $('#dtitle-' + rowid).trigger("click");
    //        }
    //    }
    //    /*write your click event here*/
    //});


    function reset() {


        $('.clickable').each(function (index, tr) {
            var rowid = $(this).attr('id').replace('dtitle-', '');
            //console.log(rowid);
            $('#ddata-' + rowid + ' .ddate-data').html('');
            $(this).find('.cbox').removeClass('disp').addClass('disp-none');
            if ($(this).find('.cbox input').is(':checked'))
                $(this).find('.cbox input').prop('checked', false);
        });

    }
    function isEmpty(el) {
        return !$.trim(el.html())
    }

});

    
////function checkClaimApproval() {
////    $('#claimAppoveMsg').html('Approve Claims<br/>For: Client A'); /*+ $('#clFn').text()*/
////    $('.approveClaimItemModal').modal('show');
   
////}

//function approveClaims() {
//    $('.approveClaimItemModal').modal('hide');
//    waitOn();
//    //var Data = {
//    //    'clsvId': $('#CLSVID').val(),
//    //    'chartId': $('#chartId').val()
//    //};
//    //$.ajax({
//    //    type: 'POST',
//    //    url: '/SkilledBilling/ApproveClaims',
//    //    data: JSON.stringify(Data),
//    //    contentType: 'application/json; charset=utf-8',
//    //    dataType: 'json',
//    //    success: approveClaimsSuccess,
//    //    error: ajaxError,
//    //    timeout: 10000
//    //});
//    approveClaimsSuccess();
//}

//function approveClaimsSuccess(r) {
//    waitOff();
//    //if (r.er.code === 1) Alert('System Error - ' + r.er.msg);
//    //else if (r.er.code === 2) Alert('Error - ' + r.er.msg);
//    //else displayClaimsApproved();
//    displayClaimsApproved();
//}

//function displayClaimsApproved() {
//    $('#claimAppovedMsg').html('Approved Claims<br/>For: ' + $('#clFn').text());
//    $('.approvedClaimsModal').modal('show');
//}

//function checkClaimDenial() {
//    if ($('#chkClaimDenialA').prop('checked')) {
//        // $('#claimDenialMsg').html('Denied Claims<br/>For: ' + $('#clFn').text());
//        $('.denialClaimsModal').modal('show');
//    }
//}



function denyClaim() {
    $('.denialClaimsModal').modal('hide');
    waitOn();
    //var Data = {
    //    'clsvId': $('#CLSVID').val(),
    //    'chartId': $('#chartId').val()
    //};
    //$.ajax({
    //    type: 'POST',
    //    url: '/SkilledBilling/ApproveClaims',
    //    data: JSON.stringify(Data),
    //    contentType: 'application/json; charset=utf-8',
    //    dataType: 'json',
    //    success: approveClaimsSuccess,
    //    error: ajaxError,
    //    timeout: 10000
    //});
    ClaimDenial();
}

function ClaimDenial() {
    waitOff();
    displayClaimsDenied();
    $('#txtReasons').val('');
    $('#amtBilled').val('');
}

function displayClaimsDenied() {
    $('#claimDeniedMsg').html('Denied Claims<br/>For: ' + $('#clFn').text());
    $('.deniedClaimsModal').modal('show');
}

function checkPriorAuth() {
    if ($('#chkPriorAuthA').prop('checked')) {
        // $('#claimDenialMsg').html('Denied Claims<br/>For: ' + $('#clFn').text());
        $('.priorAuthModal').modal('show');
    }
}

function priorAuth() {
    $('.priorAuthModal').modal('hide');
    waitOn();
    //var Data = {
    //    'clsvId': $('#CLSVID').val(),
    //    'chartId': $('#chartId').val()
    //};
    //$.ajax({
    //    type: 'POST',
    //    url: '/SkilledBilling/ApproveClaims',
    //    data: JSON.stringify(Data),
    //    contentType: 'application/json; charset=utf-8',
    //    dataType: 'json',
    //    success: approveClaimsSuccess,
    //    error: ajaxError,
    //    timeout: 10000
    //});
    PriorAuth();
}

function PriorAuth() {
    waitOff();
    displayPriorAuth();

}

function displayPriorAuth() {
    $('#priorAuthMsg').html('Prior Auth Required <br/>For: ' + $('#clFn').text());
    $('.priorAuthDisplayModal').modal('show');
}

function checkDeductible() {
    if ($('#chkDeductibleA').prop('checked')) {
        // $('#claimDenialMsg').html('Denied Claims<br/>For: ' + $('#clFn').text());
        $('.deductibleModal').modal('show');
    }
}

function deductAmt() {
    $('.deductibleModal').modal('hide');
    waitOn();
    //var Data = {
    //    'clsvId': $('#CLSVID').val(),
    //    'chartId': $('#chartId').val()
    //};
    //$.ajax({
    //    type: 'POST',
    //    url: '/SkilledBilling/ApproveClaims',
    //    data: JSON.stringify(Data),
    //    contentType: 'application/json; charset=utf-8',
    //    dataType: 'json',
    //    success: approveClaimsSuccess,
    //    error: ajaxError,
    //    timeout: 10000
    //});
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

//function editClient() {
//    $('.clientInvoiceModal').modal('show');
//}


