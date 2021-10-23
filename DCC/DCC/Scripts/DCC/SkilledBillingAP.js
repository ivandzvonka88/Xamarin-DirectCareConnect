$(document).ready(function () {
    var dedObj;
    var denialObj;
    var claimIds;
    //console.log($('.dateControl').val());
    token = $('input[name="__RequestVerificationToken"]').val();
    headers = {};
    headers['__RequestVerificationToken'] = token;
    /*step 2*/
    var s2 = $(".clickable");
    s2.click(function (e) {
        //step 2-a
        //console.log('test');
        var rowid = $(this).attr('id').replace('dtitle-', '');
        $('#selrowid').val(rowid);
        var clt = rowid.split('_')[3];
        $('#selectedClientIndex').val($(this).find('#clientInfoIndex_' + clt).val());
        //selectClientOrClaimStatus('clicked');
        getClaimStatuses();
    });
    //var s3 = $("#iFilter");
    //s3.change();


    /*step 5 - checkbox approve all - stop triggering other events*/
    var s5 = $(".cbox input");
    s5.click(function (e) {

        e.cancelBubble = true;
        if (e.stopPropagation) e.stopPropagation();
        /*write your click event here*/
    });

    /*step 6 - edit button - stop triggering other events*/
    var s6 = $(".edtbtn");
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

    /*step 7 - edit button - stop triggering other events*/
    var s7 = $("#btnclear-filter-payor");
    s7.click(function (e) {
        $("#txt-filter-payor").val('').focus();

        /*write your click event here*/
    });

    /*step 9 - edit button - stop triggering other events*/
    var s9 = $('.cbox .dropdown').find('.alink');
    s9.dropdown();
    s9.on('click', function (e) {
        e.cancelBubble = true;
        if (e.stopPropagation) e.stopPropagation();
        /*      write your click event here
    alert('.sidemenu .alink');*/
    });

    $('.client-profile-link').on('click', function (e) {
        e.cancelBubble = true;
        if (e.stopPropagation) e.stopPropagation();
    })


    function reset() {


        $('.clickable').each(function (index, tr) {
            var rowid = $(this).attr('id').replace('dtitle-', '');

            //console.log(rowid);
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
    //function isEmpty(el) {
    //    return !$.trim(el.html())
    //}

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
            //event.preventDefault();
            $('#hdnReasonDenialValId').val(ui.item.value);
            $("#txtReasons").val(ui.item.label);
            return false;
        }
    });
});

var SkilledBillingAP = (function(){
    var selectedClientClaims = [];
    var excludedClientClaims = [];
    
    return {
        selectedClientClaims: function(){
            return selectedClientClaims;
        },
        formattedSelectedClientClaims: function(getExcluded = false){ //this will basically return a formatted list of claims in json array form
            let clientClaims = []
            let tempSelectedClientClaims = !getExcluded ? selectedClientClaims.concat() : excludedClientClaims.concat()
            
            for (var clientClaim of tempSelectedClientClaims) {
                var clientNameIndex = clientClaims.findIndex(({ clientName }) => clientName == clientClaim.name);

                if (clientNameIndex < 0) {
                    let claimObject = {
                        clientName: clientClaim.name,
                        claimIds: [clientClaim.claimId],
                    }
                    if (getExcluded) claimObject.reason = [clientClaim.reason]
                    clientClaims.push(claimObject);
                } else {
                    var currentClientClaims = clientClaims[clientNameIndex]
                    if (getExcluded) {
                        //check if there are claims that has the same issue.
                        var claimIssueIndex = currentClientClaims.reason.indexOf(String(clientClaim.reason))
                        if (claimIssueIndex >= 0) {
                            currentClientClaims.claimIds[claimIssueIndex] += `, ${clientClaim.claimId}`;
                        } else {
                            currentClientClaims.claimIds.push(clientClaim.claimId)
                            currentClientClaims.reason.push(clientClaim.reason)
                        }
                    } else {
                        currentClientClaims.claimIds.push(clientClaim.claimId)
                    }
                }
            }
            
            return clientClaims
        },
        insertClientClaim: function(clientClaim){
            //check first if the claim is already in the array
            if(selectedClientClaims.findIndex( ({ claimId }) => claimId == clientClaim.claimId) < 0 ){
                selectedClientClaims.push(clientClaim);
            }
        },
        removeClientClaim: function(clientClaim){
            //check if the claim is in the array
            var clientClaimIndex = selectedClientClaims.findIndex( ({ claimId }) => claimId == clientClaim.claimId );
            if ( clientClaimIndex >= 0){
                selectedClientClaims.splice(clientClaimIndex, 1);
            }
        },
        excludedClientClaims: function (clientClaim = null, toInsert = false) {
            if (clientClaim != null) {
                if (toInsert) {
                    if (excludedClientClaims.findIndex(({ claimId }) => claimId == clientClaim.claimId) < 0) {
                        excludedClientClaims.push(clientClaim);
                    }
                } else if (!toInsert) {
                    var clientClaimIndex = excludedClientClaims.findIndex(({ claimId }) => claimId == clientClaim.claimId);
                    if (clientClaimIndex >= 0) {
                        excludedClientClaims.splice(clientClaimIndex, 1);
                    }
                }
            }

            return excludedClientClaims
        },
    }
})()

function showClaimComments(domRef, claimId) {
    if ($('div.modal.claimComments')) {
        $.ajax({
            url: srvcUrl + '/ClaimCommentList?claimId=' + claimId,
            headers: headers,
            type: 'GET',
            success: function (resp) {
                if (resp && $('div.modal.claimComments')) {
                    $('div.modal.claimComments .modal-body').html(resp);
                }
            }
        })

        $('div.claimComments').modal('show');
    }
}
/// function to load claim status accordion
function getClaimStatuses() {
    waitOn();
    var rowid = $('#selrowid').val();
    var clId = rowid.split('_')[3];
    var tierId = $("#hdnGovtPrgmSelected").val() == "true" ? $("#hdnTierId").val() : -1;
    var flag = $("#hdnInsFlag").val();
    //var Data = {
    //    'clientId': clId,
    //    'policyId': $("#hdnPyrId").val()
    //};

    $('#act_btn_' + rowid.split('_')[1]).show();
    //if the client's name is already expanded, collapse the claims back down
    if($('#ddata-' + rowid + '-1').length > 0) {
        $('.claimStatusAcc_' + clId).html('');
        $('#act_btn_' + rowid.split('_')[1]).hide();
        waitOff();
        return;
    }
    
    $.ajax({
        type: 'GET',
        url: srvcUrl + '/GetClientDetailStatuses?clientId=' + clId + '&policyId=' + $("#hdnPyrId").val() + '&filterStatus=' + $('#iFilter').val() + '&tierId=' + tierId + '&insuranceFlag=' + flag + '&isGovtPgm=' + $("#hdnGovtPrgmSelected").val() + '&isSelfPay=' + $("#hdnSelfPay").val(),
        headers: headers,
        //data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        success: function (data) {
            waitOff();
            var cllclass = '.claimStatusAcc';
            var id = '.claimStatusAcc_' + clId;
            $(cllclass).html('');
            $.each(data, function (index, item) {
                $(id).append("<div class='col-sm-12'><div class='row statusAccord' style='margin-top:3px;'><div class='col-sm-10 clickableStatus'><span class='dtitle-link'>"
                    + item.name + "</span><input id='hdnClientClaimStatus_" + clId + "' type='hidden' value='" + item.value
                    + "' /></div><div class='col-sm-2 selAllCont'><div class='selAll disp-none'>Select All<input type='checkbox' class='chkSelAll' onclick='selectAllCliam(this);'/></div></div><div id='ddata-Payor_"
                    + $("#hdnPyrId").val() + "_Client_" + clId + "_Status_" + item.value + "' class='col-sm-12 ddata-Payor claimDet disp-none' style='overflow-y: scroll;'></div></div></div>")
            });
            //$('#ddata-' + rowid).html(data);

            var s10 = $(".clickableStatus");
            s10.click(function (e) {
                //step 2-a
                // console.log('test');
                var rowid = $('#selrowid').val();
                //$(this).attr('id').replace('dtitle-', '');
                //
                var clt = rowid.split('_')[3];
                var stctid = '#hdnClientClaimStatus_' + clt;
                $('#selStatus2id').val($(this).find(stctid).val());
                $('#selectedClientIndex').val($('#clientInfoIndex_' + clt).val());
                selectClientOrClaimStatus('clicked');
                var cbox = $(this).siblings()[0].children[0];
                if ($(cbox).hasClass('disp-none')) { $(cbox).removeClass('disp-none').addClass('disp'); }
                else if ($(cbox).hasClass('disp')) { $(cbox).removeClass('disp').addClass('disp-none'); }
                //getClaimStatuses();
            });
            waitOff();
            
            //only auto expand the claim details when there is only 1 status 
            if($('.clickableStatus').length == 1) s10.click();
            console.log('(document).ready');
        },
        error: ajaxError,
        timeout: 130000
    });
}
function selectClient() {
    $('.claimSelect').prop('checked', false);
    claimsId = [];
}
function selectAllCliam(ctl) {
    //waitOn();
    $('.claimSelect').each(function () {
        if (!$(ctl).prop("checked")) {
            $(this).prop('checked', true);
            //$(this).attr('checked', 'checked');
        } else {
            $(this).prop('checked', false);
            //$(this).removeAttr('checked');
        }
        $(this).click();
    });//.then(function () { waitOff(); });
}
function filterChange() {
    //console.log('test');
    var selfPay = ($("#hdnSelfPay").val() == '1');
    var elem = $('.nameListItem.active-tab');
    if (selfPay) elem = $('#clSelfPay');
    var isGovtPgrm = $("#hdnGovtPrgmSelected").val() == "true";
    getClients($("#hdnPyrId").val(), elem, selfPay, null, isGovtPgrm, 0, 0, $("#hdnTierId").val());
    //clear the #tags input
    $('#tags').val('');
    // selectClientOrClaimStatus('');
    //if ($('#iFilter').val() == '2') {
    //    $('#chkAllClient').removeClass('disp-none').addClass('disp');
    //    $('#allClientAction').removeClass('disp-none').addClass('disp');
    //}
    //else {
    //    $('#chkAllClient').removeClass('disp').addClass('disp-none');
    //    $('#allClientAction').removeClass('disp').addClass('disp-none');
    //}
}
var claimsId = [];
function selectAllClients() {
    if ($('#selAllClient').prop('checked')) {
        $('.chkClientSelect').prop('checked', true);
    } else {
        $('.chkClientSelect').prop('checked', false);
    }
    claimsId = [];
}
function selectClientOrClaimStatus() {
    var rowid = $('#selrowid').val();
    if (rowid == undefined) {
        if ($("#hdnPyrId").val() == '')
            //getClients();
            return;
        else {
            //getClients($("#hdnPyrId").val(),null)
        }
    }
    else if (rowid == "" && $("#hdnPyrId").val() != '') {
        getClients($("#hdnPyrId").val(), null, ($("#hdnSelfPay").val() == '1'), null, 0, $("#hdnTierId").val());
        //getClients($("#hdnPyrId").val(), null, ($("#hdnSelfPay").val() == '1'), null, $("#hdnGovtPrgmSelected").val() == "true", $("#hdnTierId").val());
    }

    var clt = rowid.split('_')[3];
    var dta = $('#ddata-Payor_' + $("#hdnPyrId").val() + '_Client_' + clId);// $('#ddata-' + rowid);
    var clId = clt;//$("#cl_clientId").val();  code change

    //step 2-3
    reset();
    $(".date-expand i").removeClass('fa-plus-square').addClass('fa-minus-square');
    ///*checkbox*/
    var id = '#dtitle-' + rowid;
    var cbox = $('#act-' + rowid);

    if (cbox.hasClass('disp-none')) { cbox.removeClass('disp-none').addClass('disp'); }
    else if (cbox.hasClass('disp')) { cbox.removeClass('disp').addClass('disp-none'); }
    //if ($('#selStatus2id').val() == '2') {
    //    $('.lnkAppAll').removeClass('disp-none').addClass('disp');
    //}
    //else {
    //    $('.lnkAppAll').removeClass('disp').addClass('disp-none');
    //}


    if (dta.hasClass('disp-none')) {
        $('.ddata-Payor').removeClass('disp').addClass('disp-none');
        dta.removeClass('disp-none').addClass('disp');
        cbox.removeClass('disp-none').addClass('disp');

        /*load all clients data here using ajax- loads on each click*/
        $('#ddata-' + rowid + ' .ddate-data').html($('.data-wrap').html());
        //multiple data loading
        $('#ddata-PayorA-ClientA-1').append('<hr/>');
        $('#ddata-PayorA-ClientA-1').append($('.data-wrap').html());



        $('#ddata-' + rowid + '.ddate-data').show();
    }
    else if (dta.hasClass('disp')) {
        dta.removeClass('disp').addClass('disp-none');
        cbox.removeClass('disp').addClass('disp-none');
    }
    getClientInfo(clId);

}

function getClientInfo(clId) {
    var rowid = $('#selrowid').val();
    if (rowid != "") {
        if (clId) {
        }
        else
            clId = rowid.split('_')[3];
    }
    else {
        if ($('.clickable')[0]) {
            clId = ($('.clickable')[0].id).replace('dtitle-', '').split('_')[3];
            $('#selectedClientIndex').val(1);
        }
    }
    if (!clId)
        return;
    $('#apploader').removeClass('disp-none').addClass('disp');
    $('#CLSVID').val(clId);
    var tierId = -1;
    if ($("#hdnGovtPrgmSelected").val() == "true")
        tierId = $("#hdnTierId").val();
    var flag = $("#hdnInsFlag").val();
    // $('.ddata-Payor').removeClass('disp');
    var Data = {
        'clientId': clId,
        'selrowid': $('#selrowid').val(),
        'claimStatusId': $('#iFilter').val(),
        'selectedClientIndex': $('#selectedClientIndex').val(),
        'policyId': $("#hdnPyrId").val(),
        'claimStatusId2': $('#selStatus2id').val(),
        'tierId': tierId,
        'insuranceFlag': flag
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/ClientDetails',
        headers: headers,
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        dataType: 'html',
        success: function (data) {
            waitOff();
            //console.log($('.claimDet'));
            //$('.claimDet').empty();
            //$('#ddata-' + rowid).html(data);
            $('.claimDet').html('');
            $('#ddata-Payor_' + $("#hdnPyrId").val() + '_Client_' + clId + '_Status_' + $('#selStatus2id').val()).removeClass('disp-none');
            $('#ddata-Payor_' + $("#hdnPyrId").val() + '_Client_' + clId + '_Status_' + $('#selStatus2id').val()).html(data);
            if ($('#iFilter').val() == '7') {
                //$('.claimDet').find('.chkPending>>input[type=checkbox]').each(function () {
                //    $(this).prop("checked", true);
                //});
                $('.chkPending').find('input:checkbox').prop("checked", true);
            }
            $('.claimDet').find('.btn-download').each(function () {
                $(this).prop("checked", true);
            });

            var btn = $(".btn-download");
            btn.click(function (e) {
                downloadReport(this);
            });
            $('#apploader').removeClass('disp').addClass('disp-none');
            //var payorId = '#tr-Payor_'+rowid.split('_')[1];
            //var claims = $('#ddata-' + rowid).find('.ddate').length;
            //if (claims == 0) {
            //    $(payorId).removeClass('disp').addClass('disp-none');
            //} else {
            //    $(payorId).removeClass('disp-none').addClass('disp');
            //}
            $("table[class*=tblSkilledBilling]").find('tr.collapsedrow').addClass('disp-none');

            if ($("#hdnSelfPay").val() == '1') {
                $('.dropdown-menu-right .edtbtn.genInv').removeClass('disp-none');
            } else {
                $('.dropdown-menu-right .edtbtn.genInv').addClass('disp-none');
            }
            //if there are multiple status for claims
            $('.claimDet').each(function(){
                let claimsCount = $(this).find('.data-wrap').length;
                if(claimsCount > 0){
                    
                    //get the window height
                    let windowHeight = $(window).height();
                    
                    //set the maxheight to 80% of the browser height.
                    $(this).css('max-height', `${windowHeight * .8}px`);
                }
            });
            document.getElementById('dtitle-' + rowid).scrollIntoView();

            //check if there are marked claims
            if(claimsId.length > 0){
                //get the parent div
                let parent_div_id = '#ddata-Payor_' + $("#hdnPyrId").val() + '_Client_' + clId + '_Status_' + $('#selStatus2id').val()
                //iterate through all checkbox 
                $(parent_div_id + ' .claimSelect').each(function () {
                    //if data-claim-id is found inside claimsId, check the checkbox
                    if(claimsId.indexOf($(this).data('claim-id').toString()) >= 0){
                        $(this).prop('checked', true)
                    }
                });
            }
        },
        error: ajaxError,
        timeout: 130000
    });
}

function downloadReport(btn) {
    var reportType = $(btn).parent().prev().val();
    var claimIdDownLoad = $(btn).parent().find('.claimId').val();
    //alert(reportType);
    //if () {
    if (reportType == "HCFA") {
        var clientId = $('#selrowid').val().split('_')[3];
        downloadMultipleHCFA(claimIdDownLoad, clientId);
        //var Data = {
        //    'id': reportType
        //    //'reportType': reportType
        //};
        //var clientId = $('#selrowid').val().split('_')[3];
        //var selectedClientIndex = $('#selectedClientIndex').val();
        //var selrowid = $('#selrowid').val();
        //var policyId = $('#hdnPyrId').val();
        ////window.location.href = srvcUrl1 + '/clients/GetChart?id=' + reportType;string clientId, string selrowid, int selectedClientIndex, string policyId, int claimId
        //var companyId = $("#hdnPyrId").val();
        //window.location.href = srvcUrl + '/GetHCFA?clientId=' + clientId + '&selrowid=' + selrowid + '&selectedClientIndex=' + selectedClientIndex + '&policyId=' + policyId + '&companyId=' + companyId;
    }
    else if (reportType == "Prescription") {
        var chartId = $(btn).parent().find('.chartfileid').val();
        window.location.href = srvcUrl1 + '/clients/GetChart?id=' + chartId;//+ 40;
    }
    else if (reportType == "ProgressReport") {
        var prgsId = $(btn).parent().find('.progressfileid').val();
        window.location.href = srvcUrl + '/GetProgressReport?id=' + prgsId;
    }
    else if (reportType == "ClientNotes") {
        var noteId = $(btn).parent().find('.notefileid').val();
        window.location.href = srvcUrl + '/GetClientNote?id=' + noteId;
    }
    else if (reportType == "99") {
        //var Data = { ids: [46, 47] };
        //var p = $.param(Data);
        var clientId = $('#selrowid').val().split('_')[3];
        var selectedClientIndex = $('#selectedClientIndex').val();
        var selrowid = $('#selrowid').val();
        var policyId = $('#hdnPyrId').val();
        //window.location.href = srvcUrl1 + '/clients/GetChart?id=' + reportType;string clientId, string selrowid, int selectedClientIndex, string policyId, int claimId
        var companyId = $("#hdnPyrId").val();
        var chartId = $(btn).parent().find('.chartfileid').val();
        var prgs = $(btn).parent().find('.progressfile').val();
        var note = $(btn).parent().find('.notefile').val();
        var chartex = $(btn).parent().find('.chartfileex').val();
        var prgsex = $(btn).parent().find('.progressfileex').val();
        var noteex = $(btn).parent().find('.notefileex').val();
        window.location.href = srvcUrl + '/GetChartZip?chart=' + chartId + '&chartextension=' + chartex + '&progressreport=' + prgs + '&prextension=' + prgsex + '&clientnote=' + note + '&cnextension=' + noteex + '&clientId=' + clientId + '&selrowid=' + selrowid + '&selectedClientIndex=' + selectedClientIndex + '&policyId=' + policyId + '&companyId=' + companyId;
    }
    else {

    }
    //}
    //zipFiles();
}
function zipFiles() {
    //var zip = new JSZip();
    //var count = 0;
    //var zipFilename = "zipFilename.zip";
    //var urls = [
    //    '../../Images/loading.gif',
    //    '../../Images/ajaxloader.gif',
    //];

    //urls.forEach(function (url) {
    //    var filename = "filename";
    //    // loading a file and add it in a zip file
    //    JSZipUtils.getBinaryContent(url, function (err, data) {
    //        if (err) {
    //            throw err; // or handle the error
    //        }
    //        zip.file(filename, data, { binary: true });
    //        count++;
    //        if (count == urls.length) {
    //            var zipFile = zip.generate({ type: "blob" });
    //            saveAs(zipFile, zipFilename);
    //        }
    //    });
    //});
    //var zip = new JSZip();
    //zip.file("Hello.txt", "Hello World\n");
    //var img = zip.folder("images");
    ////img.file("smile.gif", imgData, { base64: true });
    //zip.generateAsync({ type: "blob" })
    //    .then(function (content) {
    //        // see FileSaver.js
    //        saveAs(content, "example.zip");
    //    });
}
function checkClaimApproval(val) {
    var rowid = $('#selrowid').val();
    var panel = $('#ddata-' + rowid + '_Status_' + $('#selStatus2id').val());
    if ($('.claimSelect:checked').length > 0) {
        $('#claimAppoveMsg').html('Approve claims for:<br/>'); /*+ $('#clFn').text()*/
        // $('#lblClientName').text(val);
        var clientText = val + '<br/>';
        clientText += "Claim IDs: " + getSpecificClientMarkedClaims().claimIds.join(', ');
        $('#lblClientName').html(clientText);
        $('.approveClaimItemModal').modal('show');
        $('#hdnClientApproval').val('2'); //changed value to 2, which means marked claims for a specific client only
    }
    else {
        swal("Therapy Corner 2.0", "Select at least one claim", "warning");
    }
}
function clientClaimApproval(markedClaimsOnly = false) {
    //var rowid = $('#selrowid').val();
    //var panel = $('#ddata-' + rowid + '_Status_' + $('#selStatus2id').val());
    // if ($('.chkClientSelect:checked').length > 0) {
    //     $('#hdnClientApproval').val('1');
    //     $('#claimAppoveMsg').html('Approve Claims<br/>'); /*+ $('#clFn').text()*/
    //     $('#lblClientName').text('For all selected clients');
    //     $('.approveClaimItemModal').modal('show');
    //     $('#hdnClientApproval').val('1');
    // }
    // else {
    //     swal("Therapy Corner 2.0", "Select at least one client", "warning");
    // }

    let windowHeight = $(window).height();

    //set the maxheight to 80% of the browser height.
    $('.approveClaimItemModal').find('.modal-body').css({'max-height': `${windowHeight * .8}px`, 'overflow': `auto`});
    
    if(!markedClaimsOnly) {
        if ($('.chkClientSelect:checked').length > 0) {
            $('#hdnClientApproval').val('1');
            $('#claimAppoveMsg').html('Approve Claims<br/>'); /*+ $('#clFn').text()*/
            $('#lblClientName').text('For all selected clients');
            $('.approveClaimItemModal').modal('show');
            $('#hdnClientApproval').val('1');
        }
        else {
            swal("Therapy Corner 2.0", "Select at least one client", "warning");
        }
    } else {
        if(claimsId.length > 0){
            $('#claimAppoveMsg').html('Approve the following claims:<br/>');
            var clientNames = "";
            var clientClaims = SkilledBillingAP.formattedSelectedClientClaims();
            
            for (var clientClaim of clientClaims) {
                clientNames += clientClaim.clientName + ': ' + clientClaim.claimIds + '<br/>';
            }
            
            $('#lblClientName').html(clientNames);
            $('.approveClaimItemModal').modal('show');
            $('#hdnClientApproval').val('0');
        } else {
            swal("Therapy Corner 2.0", "Select at least one claim", "warning");
        }
    }
}

function approveClaims() {
    $('.approveClaimItemModal').modal('hide');
    waitOn();
    if ($('#hdnClientApproval').val() == '1') {
        var clnts = "";
        $.each($(".chkClientSelect"), function (index, elem) {
            if ($(elem).prop("checked")) {
                clnts += $(elem).val() + ",";
            }
        });
        $('.claimSelect').prop('checked', false);
        claimsId = [];
        var selfPay = ($("#hdnSelfPay").val() == '1');
        var Data = {
            'payerId': $("#hdnPyrId").val(),
            'selfpay': selfPay,
            'tierId': $("#hdnGovtPrgmSelected").val() == "true" ? $("#hdnTierId").val() : -1,
            'clientIds': clnts,
            'claimStatus': $('#iFilter').val()
        };
        $.ajax({
            type: 'POST',
            url: srvcUrl + '/UpdateBatchApproveAllClients',
            headers: headers,
            data: JSON.stringify(Data),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (data) {
                waitOff();
                swal("Therapy Corner 2.0", 'Approved Claims ' + $('#lblClientName').text(), "success");

                $('.claimSelect').each(function () {
                    $(this).prop('checked', false);
                });
                $('#selAllClient').prop('checked', false);
                filterChange();
            },
            error: ajaxError,
            timeout: 10000
        });
    }
    else {
        var clms = "";
        var final_excluded_claims = [];
        var exclude_claim_table = '';

        //get the excluded claims all clients
        let excluded_claims = SkilledBillingAP.formattedSelectedClientClaims(true)

        if($('#hdnClientApproval').val() == '0'){ // clicked "Approve all marked claims" from purple status bar
            $.each(claimsId, function (index, value) {
                if (claimsId.length - 1 == index) {
                    clms += value
                }
                else
                    clms += value + ",";
            });
            for (let claim of excluded_claims) {
                for (let claimId of claim.claimIds) {
                    if (claimId.split(',').length > 0) {
                        claimId.split(',').forEach(function (item) { final_excluded_claims.push(item.trim()) })
                    } else {
                        final_excluded_claims.push(claimId)
                    }
                }
            }
            exclude_claim_table = createClaimErrorTable(excluded_claims)
        } else { // clicked "Approve all" from 3 dots beside client name
            //get the marked claims for the specific client
            let specificClientClaims = getSpecificClientMarkedClaims()
            clms += specificClientClaims.claimIds.join(', ');
            let client_index = excluded_claims.findIndex(({ clientName }) => clientName == specificClientClaims.clientName)
            if (client_index >= 0) {
                for (let claimId of excluded_claims[client_index].claimIds) {
                    if (claimId.split(',').length > 0) {
                        claimId.split(',').forEach(function (item) { final_excluded_claims.push(item.trim()) })
                    } else {
                        final_excluded_claims.push(claimId)
                    }
                }
                //then create the table to show the excluded claims with their reason.
                exclude_claim_table = createClaimErrorTable([excluded_claims[client_index]])
            }
        }

        var Data = {
            'claimIds': clms,
            'status': $('#selStatus2id').val()
        };
        //check if there are excluded claims and if the claimstatus filter is pending insurance payment
        if (SkilledBillingAP.excludedClientClaims().length > 0 && $('#iFilter').val() == 1) {
            swal({
                title: "Therapy Corner 2.0",
                content: {
                    element: 'div',
                    attributes: {
                        innerHTML: `Are you sure you want to approve the following claims?<br>${exclude_claim_table}`,
                    },
                },
                icon: "warning",
                buttons: {
                    confirm: true,
                    exclude: { text: 'No, exclude claims', value: false },
                },
                dangerMode: true,
            }).then((approve) => {
                let final_claim_for_approval = Data.claimIds.split(',').map(item => item.trim());
                if (!approve) {//if user does not want to approve the claims with issue
                    for (let claimId of final_excluded_claims) {
                        let index = final_claim_for_approval.indexOf(claimId)
                        if (index >= 0) final_claim_for_approval.splice(index, 1)
                    }
                }

                Data.claimIds = final_claim_for_approval.join(',')

                if (Data.claimIds.length <= 0) {
                    swal("Therapy Corner 2.0", "No claims selected for approval.", "error");
                    waitOff();
                    return;
                }


                $.ajax({
                    type: 'POST',
                    url: srvcUrl + '/UpdateBatchApproveAll',
                    headers: headers,
                    data: JSON.stringify(Data),
                    contentType: 'application/json; charset=utf-8',
                    dataType: 'json',
                    success: function (data) {
                        waitOff();
                        swal("Therapy Corner 2.0", 'Approved Claims ' + $('#lblClientName').text(), "success");

                        $('.claimSelect').each(function () {
                            $(this).prop('checked', false);
                        });
                        $('.chkSelAll').prop('checked', false);
                        filterChange();
                    },
                    error: ajaxError,
                    timeout: 10000
                });
            })
        } else {

            $.ajax({
                type: 'POST',
                url: srvcUrl + '/UpdateBatchApproveAll',
                headers: headers,
                data: JSON.stringify(Data),
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                success: function (data) {
                    waitOff();
                    swal("Therapy Corner 2.0", 'Approved Claims ' + $('#lblClientName').text(), "success");

                    $('.claimSelect').each(function () {
                        $(this).prop('checked', false);
                    });
                    $('.chkSelAll').prop('checked', false);
                    filterChange();
                },
                error: ajaxError,
                timeout: 10000
            });

        }
    }
    //approveClaimsSuccess();
}

function approveClaimsSuccess(r) {
    waitOff();
    //if (r.er.code === 1) Alert('System Error - ' + r.er.msg);
    //else if (r.er.code === 2) Alert('Error - ' + r.er.msg);
    //else displayClaimsApproved();
    displayClaimsApproved();
}

function displayClaimsApproved() {
    $('#claimAppovedMsg').html('Approved Claims<br/>For: ' + $('#clFn').text());
    $('.approvedClaimsModal').modal('show');
}
function objectifyForm(formArray) {//serialize data function
    var returnArray = {};
    for (var i = 0; i < formArray.length; i++) {
        returnArray[formArray[i]['name']] = formArray[i]['value'];
    }
    return returnArray;
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

function editInsurancePolicy(InsurancePolicyId) {
    var Data = {
        'InsurancePolicyId': InsurancePolicyId
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/GetInsurancePolicy',
        headers: headers,
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('.insuarancePolicyModal').html(data);
            //$('#apploader').removeClass('disp').addClass('disp-none');
        },
        error: ajaxError,
        timeout: 10000
    });
    $('.insuarancePolicyModal').modal('show');
    //$('.insuarancePolicyModal').hide();
    //$('.modal-backdrop').hide();
}

function editClient() {
    //var rowid = $('#selrowid').val();
    //var panel = $('#ddata-' + rowid);
    if ($('.claimSelect:checked').length > 0) {
        $('.clientInvoiceModal').modal('show');
        var clms = "";
        $.each(claimsId, function (index, value) {
            if (claimsId.length - 1 == index) {
                clms += value
            }
            else
                clms += value + ",";
        });
        var Data = {
            'claimIds': clms
        };
        //claimsId = [];
        $.ajax({
            type: 'GET',
            url: srvcUrl + '/GetClaimPayments?claimIds=' + clms,
            headers: headers,
            //data: JSON.stringify(Data),
            contentType: 'application/json; charset=utf-8',
            dataType: 'html',
            success: function (data) {
                waitOff();
                $('#dvAllPaymentList').html(data);
            },
            error: ajaxError,
            timeout: 10000
        });
    }
    else {
        swal("Therapy Corner 2.0", "Select at least one claim", "warning");
    }
}
function expandDetails(elm) {
    var rowid = $(elm).attr('id').replace('ddate-', '');

    if ($('#ddata-' + rowid).is(':visible')) {
        $('#ddata-' + rowid).hide();
        $(elm).find('i').removeClass('fa-minus-square').addClass('fa-plus-square');
        $('#ddata-' + rowid).find('hr').hide();

    }

    else {
        $('#ddata-' + rowid).show();
        $(elm).find('i').removeClass('fa-plus-square').addClass('fa-minus-square');
        $('#ddata-' + rowid).find('hr').show();
    }


}
function callUpdate(e, objName, claimId, ServiceId, insurancePolicyID, pymtAllowedAmt) {
    clientId = $('#selrowid').val().split('_')[3];
    $('#apploader').removeClass('disp-none').addClass('disp');
    var valObj = $(e).val();
    //var insurancePolicyID= $('#insurancePolicyID').val();
    var amtBilled = null, paidAmt = null, allowedAmt = null, coInsAmt = null, dddStdt = null, dddEddt = null, dddAu = null, comments = null, claimStatusId = -1, selfPayStatus = null, paymentDate = null;
    if (objName == 'BilledAmount')
        amtBilled = valObj.replace("$", "");
    if (objName == 'PaidAmount')
        paidAmt = valObj.replace("$", "");
    if (objName == 'DDDStart')
        dddStdt = valObj;
    if (objName == 'DDDEnd')
        dddEddt = valObj;
    if (objName == 'DDDAu')
        dddAu = valObj;
    if (objName == 'Comments')
        comments = valObj;
    if (objName == 'Status')
        claimStatusId = valObj;
    if (objName == 'SelfPayStatus')
        selfPayStatus = valObj;
    if (objName == 'CoInsuranceAmount')
        coInsAmt = valObj.replace("$", "");
    if (objName == 'AllowedAmount') {
        allowedAmt = valObj.replace("$", "");
    } else {
        allowedAmt = pymtAllowedAmt;
    }
    if (objName == 'PaymentDate')
        paymentDate = valObj;

    //if()
    var Data = {
        'ClaimId': claimId,
        'ClientID': clientId,
        'InsurancePolicyId': insurancePolicyID,
        'AmountBilled': amtBilled,
        'Amount': paidAmt,
        'CoInsuranceAmount': coInsAmt,
        'AllowedAmount': allowedAmt,
        'DDDstdt': dddStdt,
        'DDDeddt': dddEddt,
        'DDDau': dddAu,
        'Comments': comments,
        'ServiceId': ServiceId,
        'ClaimStatusId': claimStatusId,
        'SelfPayStatus': selfPayStatus,
        'PaymentDate': paymentDate
    };
    console.log(Data);
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/UpdateClaim',
        headers: headers,
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        dataType: 'html',
        success: function (data) {
            waitOff();
            if (objName == 'Status') {
                //getClients($("#hdnPyrId").val(), null);
                selectClientOrClaimStatus();
                swal("Therapy Corner 2.0", 'Claim status changed', "success");
                filterChange();
                //$('.claimStatusAcc').html('');
                //$('.selAll').removeClass('disp').addClass('disp-none') 
            } else if (objName == 'Comments'){
                //clear comment box after succesful comment save
                $(e).val('');
                //show "View Comments" link
                $('#claimComments_' + claimId).css('display', 'inline');
            }
            $('#apploader').removeClass('disp').addClass('disp-none');
            if (objName == 'PaidAmount' || objName == 'PaymentDate') {
                //update the element to reflect the recorded payment
                $('.claimSelect').each(function () {
                    if ($(this).data('claim-id') == claimId) {
                        $(this).attr('data-payment-id', 1)
                        $(this).data('payment-id', 1)
                        SkilledBillingAP.excludedClientClaims({
                            claimId: claimId,
                        }, false)
                    } 
                })
            }
        },
        error: ajaxError,
        timeout: 10000
    });
}

function updateClaimStatus(claimId) {
    var claimStatusId = $('#claimStatus').val()
    var Data = {
        'ClaimId': claimId,
        'ClaimStatusId': claimStatusId
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/UpdateClaimStatus',
        headers: headers,
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#apploader').removeClass('disp').addClass('disp-none');
        },
        error: ajaxError,
        timeout: 10000
    });
}
function closeModal() {
    $('.insuarancePolicyModal').modal('hide');
}
// Update Deductible //
function checkDeductible(modelVal, index, bHasDDD) {
    dedObj = modelVal;
    $('#hdnDedModel').val(modelVal);
    $('#selIndex').val(index);
    if (bHasDDD) {
        $('#dedAmt').val('0');
    }
    $('#hdnReasonValId').val(1);
    //$("#txtDedReasons").val(defaultReason.name);
    //if ($(this).prop('checked')) {
        $('.deductibleModal').modal('show');
        $(this).prop('checked', !$(this).prop('checked'));
    //}
}

function callUpdateDeductible(amount, reasonCode) {
    var item = JSON.parse(dedObj);
    if ($.isNumeric(amount)) {
        clientId = $('#selrowid').val().split('_')[3];
        var Data = {
            'ClaimId': item.claimId,
            'PaymentId': item.paymentId,
            'Amount': amount,
            'ReasonCode': reasonCode,
            'clientId': clientId
        };
        $.ajax({
            type: 'POST',
            url: srvcUrl + '/UpdateDeductible',
            headers: headers,
            data: JSON.stringify(Data),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (data) {
                waitOff();
                $('.deductibleModal').modal('hide');
                $('#dedAmt').val('');
                $('#claimComments_' + item.claimId).css('display', 'inline');
                //filterChange();

                //update the element to reflect the recorded deductible
                $('.claimSelect').each(function () {
                    if ($(this).data('claim-id') == item.claimId) {
                        $(this).attr('data-deductibleind', 1)
                        $(this).data('deductibleind', 1)
                        SkilledBillingAP.excludedClientClaims({
                            claimId: item.claimId,
                        }, false)
                    }
                })
            },
            error: ajaxError,
            timeout: 10000
        });
    }
    else {
        swal("Therapy Corner 2.0", "Please enter valid amount", "warning");
    }
}

// Update Clean Denial //
function denyClaim(amount, reasonCode) {
    if ($.isNumeric(amount)) {
        if ($('#denialComments').val()) {
            $('input[name="comments"]').val($('#denialComments').val());
            $('input[name="comments"]').trigger('blur');
        }
        var item = JSON.parse(denialObj);
        var Data = {
            'ClaimId': item.claimId,
            'PaymentId': item.paymentId,
            'Amount': amount,
            'ReasonCode': reasonCode,
            'StartDate': $('#denialStartDate').val(),
            'ClientId': $('#selrowid').val().split('_')[3]
        };
        $.ajax({
            type: 'POST',
            url: srvcUrl + '/UpdateCleanDenial',
            headers: headers,
            data: JSON.stringify(Data),
            contentType: 'application/json; charset=utf-8',
            dataType: 'html',
            success: function (data) {
                waitOff();
                filterChange();
                $('.denialClaimsModal').modal('hide');
                $('#claimComments_' + item.claimId).css('display', 'inline');
            },
            error: ajaxError,
            timeout: 10000
        });
    }
    else {
        swal("Therapy Corner 2.0", "Please enter valid amount", "warning");
    }
}

// Update Status to Pending Waiver //
function checkPendingWaiver(elem, startDate) {
    clientId = $('#selrowid').val().split('_')[3];
    var Data = {
        'clientid': clientId,
        'startDate': startDate,
        'changeToPW': $(elem).prop('checked')
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/UpdatePendingWaiver',
        headers: headers,
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        dataType: 'html',
        success: function (data) {
            waitOff();
            filterChange();
            swal("Therapy Corner 2.0", $(elem).prop('checked') ? "Claims moved to 'Pending Waiver' status" : "Claims moved out of 'Pending Waiver' status", "success");
        },
        error: ajaxError,
        timeout: 10000
    });
}
function denyBatchClaim(reasonCode) {
    var clms = "";
    $.each(claimsId, function (index, value) {
        if (claimsId.length - 1 == index) {
            clms += value
        }
        else
            clms += value + ",";
        //alert(index + ": " + value);
    });

    var Data = {
        'claimIds': clms,
        'ReasonCode': reasonCode
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/UpdateBatchDenialClaims',
        headers: headers,
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        dataType: 'html',
        success: function (data) {
            waitOff();
            filterChange();
            $('.batchdenialClaimsModal').modal('hide');
        },
        error: ajaxError,
        timeout: 10000
    });
}
function checkClaimDenial(elem, amt, startDate) {
    //if ($(elem).prop('checked')) {
    denialObj = amt;
    $('#hdnDenialModel').val(denialObj);
    var item = JSON.parse(amt);
    $('#amtBilled').val(item.billedAmount);
    $('#denialStartDate').val(startDate);
    $("#txtReasons0").autocomplete({
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
            //event.preventDefault();
            $('#hdnReasonDenialValId').val(ui.item.value);
            $("#txtReasons").val(ui.item.label);
            return false;
        }
    }).val(item.DenialReasonText);//.data('autocomplete')._trigger('select');;
    $('.denialClaimsModal').modal('show');
    /*$('.denialClaimsModal').off("hidden.bs.modal");
    $('.denialClaimsModal').on("hide.bs.modal", function (e) {
        let activeElement = $(document.activeElement);

        if (!activeElement.is('[data-toggle], [data-dismiss]')) {
            checkPendingWaiver(elem, startDate);
        }
    });*/
    $(elem).prop('checked', false);
    //}
}

function clickBatchClaimDenial() {
    var rowid = $('#selrowid').val();
    var panel = $('#ddata-' + rowid);
    if ($('.claimSelect:checked').length > 0) {
        $('.batchdenialClaimsModal').modal('show');
    }
    else {
        swal("Therapy Corner 2.0", "Select at least one claim", "warning");
    }
}

// Prior Auth alert //
function checkPriorAuth(claimId) {
    $('#priorAuthClaimId').val(claimId);
    if ($('#chkPriorAuthA_' + claimId).prop('checked')) {
        $('.priorAuthModal').modal('show');
        $('#chkPriorAuthA_' + claimId).prop('checked', false);
    }
}
function priorAuth() {
    
    var claimId = $('#priorAuthClaimId').val()

    $.ajax({
        type: 'POST',
        url: srvcUrl + '/UpdatePriorAuth',
        headers: headers,
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        data: JSON.stringify({'claimId': claimId}),
        success: function (data) {
            waitOff();
            $('#authComments').val('');
            $('#priorAuthClaimId').val('');
            $('.priorAuthModal').modal('hide');
            $('#chkPriorAuthA_' + claimId).prop('checked', true);
        },
        error: ajaxError,
        timeout: 10000
    });
}

// Batch Pending Waiver
function addClaimsForWaiver(index, claimId, elem) {
    if ($(elem).prop('checked')) {
        if (claimsId.indexOf(claimId) < 0) {
            claimsId.push(claimId);
            SkilledBillingAP.insertClientClaim({
                claimId: claimId,
                name: $(elem).data('client-name')
            });
        }
        //check if claim has payment/deductible/denial recorded, if not add as excluded
        let payment_id = $(elem).data('payment-id')
        let deductibleind = $(elem).data('deductibleind')
        let isDenial = $(elem).data('denial')
        if (payment_id == 0 && deductibleind == 0 && isDenial == 'False') {
            let reason = (payment_id == 0 && deductibleind == 0 && isDenial == 'False') ? 'No payment/deductible/denial recorded.' : payment_id == 0 ? 'No payment recorded.' :isDenial == 'False' ? 'No denial recorded.' : 'No deductible recorded.';
            SkilledBillingAP.excludedClientClaims({
                claimId: claimId,
                name: $(elem).data('client-name'),
                reason: reason,
            }, true)
        }
    }
    else {
        var claimIdIndex = claimsId.indexOf(claimId);
        //check if the claimId actually exists in claimsId before splicing
        if(claimIdIndex >= 0){
            claimsId.splice(claimIdIndex, 1)
            SkilledBillingAP.removeClientClaim({claimId: claimId})
        }
        //also delete the claim in excluded list
        SkilledBillingAP.excludedClientClaims({
            claimId: claimId,
            name: $(elem).data('client-name')
        }, false)
    }
}

function batchPendingWaiver() {
    //var item = JSON.parse(claimIds);
    var rowid = $('#selrowid').val();
    var panel = $('#ddata-' + rowid);

    if ($('.claimSelect:checked').length > 0) {
        var clms = "";
        $.each(claimsId, function (index, value) {
            if (claimsId.length - 1 == index) {
                clms += value
            }
            else
                clms += value + ",";
            //alert(index + ": " + value);
        });
        var Data = {
            'claimIds': clms
        };
        claimsId = [];
        $.ajax({
            type: 'POST',
            url: srvcUrl + '/UpdateBatchPendingWaiver',
            headers: headers,
            data: JSON.stringify(Data),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (data) {
                waitOff();
                swal("Therapy Corner 2.0", "Claims moved to 'Pending Waiver' status", "success");
            },
            error: ajaxError,
            timeout: 10000
        });
    }
    else {
        swal("Therapy Corner 2.0", "Select at least one claim", "warning");
    }
}

function editVoidPayment(claimId, paymentId, staffid, insuranceCompanyId) {

    event.preventDefault();
    $('#voidClaimId').val(claimId);
    $('#voidPaymentId').val(paymentId);
    $('#voidStaffId').val(staffid);
    $('#voidComment').val('');
    $('#voidInsuranceCompanyId').val(insuranceCompanyId);
    $('.voidPaymentModal').modal('show');

}
function updateVoidPayment() {

    var Data = {
        'comment': $('#voidComment').val(),
        'claimid': $('#voidClaimId').val(),
        'paymentid': $('#voidPaymentId').val(),
        'staffid': $('#voidStaffId').val(),
        'insuranceCompanyId': $('#voidInsuranceCompanyId').val(),
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/VoidPayments',
        headers: headers,
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        success: function (data) {
            waitOff();
            swal("Therapy Corner 2.0", "Payment sucessfully voided", "success");
            $('.voidPaymentModal').modal('hide');
            editClient();
        },
        error: ajaxError,
        timeout: 10000
    });

}

function updateDenialError(claimid, staffid) {

    event.preventDefault();

    clientId = $('#selrowid').val().split('_')[3];
    var Data = {
        'claimid': claimid,
        'clientid': clientId,
        'staffid': staffid
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/DenialError',
        headers: headers,
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        success: function (data) {
            waitOff();
            swal("Therapy Corner 2.0", "Denial error sucessfully saved", "success");
            //alert('denial error completed!');
        },
        error: ajaxError,
        timeout: 10000
    });

}

function expandClaimDetails(item) {
    var rowid = $(item).attr('id').replace('spnSkilledBilling-', '');
    if ($(item).find('i').hasClass('fa-minus-square')) {
        $('.tblSkilledBilling-' + rowid).find('tr.collapsedrow').addClass('disp-none');
        $(item).find('i').removeClass('fa-minus-square').addClass('fa-plus-square');
    }
    else if ($(item).find('i').hasClass('fa-plus-square')) {
        $('.tblSkilledBilling-' + rowid).find('tr.collapsedrow').removeClass('disp-none');
        $(item).find('i').removeClass('fa-plus-square').addClass('fa-minus-square');
    }
}

function downloadMultipleHCFA(claimId, clientId, newFormat = false) {
    var clms = "";
    $.each(claimsId, function (index, value) {
        if (claimsId.length - 1 == index) {
            clms += value
        }
        else
            clms += value + ",";
        //alert(index + ": " + value);
    });
    if ($('.chkClientSelect:checked').length > 0 || claimId != undefined || clms.length > 0) {
        waitOn();
        var clnts = "";
        if ($('.chkClientSelect:checked').length > 0) {
            $.each($(".chkClientSelect"), function (index, elem) {
                if ($(elem).prop("checked")) {
                    clnts += $(elem).val() + ",";
                }
            });
        }
        else { clnts = clientId; }

        var selfPay = ($("#hdnSelfPay").val() == '1');
        var tierId = $("#hdnGovtPrgmSelected").val() == "true" ? $("#hdnTierId").val() : -1;
        //window.location.href = srvcUrl + '/GetMultiHCFA?clientIds=' + clnts + '&companyId=' + $("#hdnPyrId").val() + '&status=' + $('#iFilter').val() + '&selfpay=' + selfPay + '&tierId=' + tierId;
        var isGovt = ($("#hdnGovtPrgmSelected").val() == "true") ? true : false;
        var param = {
            'clientIds': clnts,
            'companyId': $("#hdnPyrId").val(),
            'status': $('#iFilter').val(),
            'selfpay': selfPay,
            'tierId': tierId,
            'isGovt': isGovt,
            'claimId': claimId,
            'claimIds': clms,
            'useNewFormat': newFormat
        };
        $.ajax({
            cache: false,
            url: srvcUrl + '/DownloadDDDFile',
            //url: srvcUrl + '/GetMultiHCFA',
            data: JSON.stringify(param),
            method: 'POST',
            contentType: 'application/json; charset=utf-8',
            beforeSend: function () {
                clearTimeout(activityTimer);
                clearTimeout(warningTimer);
                clearTimeout(countDownTimer);
            },
            success: function (data) {
                waitOff();
                startCheckActivityTimer();
                startWarningTimer();

                if (data.Data.Error != '') {
                    swal("Therapy Corner 2.0", data.Data.Error, "error");
                }

                if (data.Data.Succeed) {
                    $('#selAllClient').prop('checked', false);
                    $('.chkSelAll').prop('checked', false);
                    selectAllClients();
                    $('.claimSelect').prop('checked', false);
                    claimsId = [];
                    let str_errors = data.Data.Error

                    if (data.Data.Claims != "") {
                        swal({
                            title: "Therapy Corner 2.0",
                            text: `Would you like to place these claims in ${isGovt ? "'Pending Government Payment' status?" : "'Pending Insurance Payment' status?"}\n (${data.Data.Claims})`,
                            icon: "warning",
                            buttons: true,
                            dangerMode: true,
                        })
                            .then((change) => {
                                if (change) {

                                    var Data = {
                                        'claimIds': data.Data.Claims,
                                        'status': isGovt ? 3 : 1,
                                        'isHCFA': isGovt ? false : true
                                    };
                                    $.ajax({
                                        type: 'POST',
                                        url: srvcUrl + '/UpdateBatchStatus',
                                        headers: headers,
                                        data: JSON.stringify(Data),
                                        contentType: 'application/json; charset=utf-8',
                                        dataType: 'json',
                                        success: function (data) {
                                            waitOff();
                                            filterChange();
                                            swal(
                                                isGovt ? "Status changed to 'Pending Government Payment'" : "Status changed to 'Pending Insurance Payment'",
                                                {
                                                    icon: "success"
                                            }).then(() => {
                                                if (str_errors.length > 0) {
                                                    swal({
                                                        title: "Therapy Corner 2.0",
                                                        text: `Failed to export the following claims: \n${str_errors}`,
                                                        icon: 'warning',
                                                        dangerMode: true,
                                                    })
                                                }
                                            })
                                            ;
                                        },
                                        error: ajaxError,
                                        timeout: 10000
                                    });
                                } else {
                                    if (str_errors.length > 0) {
                                        swal({
                                            title: "Therapy Corner 2.0",
                                            text: `Failed to export the following claims: \n${str_errors}`,
                                            icon: 'warning',
                                            dangerMode: true,
                                        })
                                    }
                                }
                            });
                        
                        //window.location.href = srvcUrl + '/Download?fileKey=' + data.Data.FileKey + '&filename=' + data.Data.FileName + '&fileType=' + data.Data.FileType;
                        window.location.href = srvcUrl + '/StartDownload?fileKey=' + data.Data.FileKey + '&filename=' + data.Data.FileName + '&fileType=' + data.Data.FileType + '&coversheetKey=' + data.Data.CoverSheetKey + '&isDDD=' + data.Data.IsDDD;
                    } else {
                        if (str_errors.length > 0) {
                            swal({
                                title: "Therapy Corner 2.0",
                                text: `Failed to export the following claims: \n${str_errors}`,
                                icon: 'warning',
                                dangerMode: true,
                            })
                        }
                    }
                }
            },
            error: [startCheckActivityTimer, startWarningTimer, ajaxError]
        });
    }
    else {
        swal("Therapy Corner 2.0", "Select at least one client or claim", "warning");
    }
}

$(document).on('keyup', '.dateControl', function(e){
    if(e.key != 'Backspace') {
        var formattedDate = autoDateFormat($(this).val())
        if(formattedDate.length == 2 || formattedDate.length == 5) { formattedDate += '/' }
        $(this).val(formattedDate)
    }
})

function autoDateFormat(input){
    var key = input.substr(input.length - 1);
    if(isNaN(key)) { return input.substr(0, input.length - 1) }
    else {
        var result = input
        if(result.length == 1 && key > 1) { result = '0' + key;}
        else if (result.length == 2) {result += '/'}
        else if (result.length == 2 && result > 12) { result = '12'; }
        else if (result.length >= 8){
            var date_input = result.split('/');
            var last_day = (new Date(date_input[2], date_input[0], 0)).getDate();

            if(date_input[1] > last_day) { date_input[1] = last_day; }
            result = date_input[0] + '/' + date_input[1] + '/' + date_input[2];
        }
        
        if(result.length == 2 || result.length == 5) {result += '/'}
        return result;
    }
}

function autoDecimalFormat(input){
    return (input/100).toFixed(2);
}

function getSpecificClientMarkedClaims() { //will return an array of claimIds
    let result = {
        clientName: '',
        claimIds: [],
    }
    $.each($('.claimSelect:checked'), function(index, element){
        result.claimIds.push($(element).data('claim-id'));
        result.clientName = $(element).data('client-name')
    })
    return result;
}

function createClaimErrorTable(claims) {
    let windowHeight = $(window).height() * .7;
    var result = `<table class="table-responsive" style="max-height: ${windowHeight}px; display: revert;">`
    for (let claim of claims) {
        result += `<tr><td colspan="2" class="font-weight-bold">${claim.clientName}</td></tr>`
        for (var i = 0, ii = claim.claimIds.length; i < ii; i++) {
            result += `<tr>
                    <td>${claim.claimIds[i]}</td>
                    <td style="text-align: left;">${claim.reason[i]}</td>
                </tr>`
        }
    }
    result += '</table>'
    return result
}
