$.ajaxSetup({ cache: false });
function ajaxError(r) {
    waitOff();
  //  console.log(r);
  //  Alert(r.responseText);
}

$(document).ajaxError(function (event, xhr, settings, error) {
    if (xhr.status == 0 && xhr.statusText !== undefined && xhr.statusText !== null && xhr.statusText !== '')
        Alert(xhr.statusText)
    else if (xhr.responseText === undefined || xhr.responseText === null || xhr.responseText === '')
        Alert(xhr.status + ' - Unspecified Error');

    else
        Alert(xhr.responseText);
    if (xhr.status == 403) { // 403 - Forbidden

        setTimeout(authError, 3000);
    }
});
// code for auto session end and save
function authError() {
    window.location = '/';
}

var logoutUrl = '/Account/Logoff'; // URL to logout page.

let activityTimer;
let warningTimer;
let countDownTimer;

let activityDetected = false;

function setActivityDetected() {
    activityDetected = true;
}

function activateActivityTracker() {
    window.addEventListener("mousemove", setActivityDetected);
    window.addEventListener("scroll", setActivityDetected);
    window.addEventListener("keydown", setActivityDetected);
    window.addEventListener("resize", setActivityDetected);
}

function startCheckActivityTimer() {
    activityTimer = setTimeout('checkForActivety()', checkActivityTimeout);
}

function startWarningTimer() {
    warningTimer = setTimeout('idleTimeout()', warningTimeout);
}

function startCountDownTimer() {
    countDownTimer = setTimeout('timeoutExpired()', countDownTimeout);
}


function checkForActivety() {
    if (activityDetected) {
            renewLogin();
    }
    else {
        startCheckActivityTimer();
    }
    activityDetected = false;
}

function idleTimeout() { 
    clearTimeout(activityTimer);
    clearTimeout(warningTimer);
    startCountDownTimer(); 
    if ($.isFunction(window.autoSave) && $('#actionModal').length !== 0 && $('#actionModal').hasClass('show'))
        autoSave();
    openInactivetyModal();
}

function openInactivetyModal() {
    if ($('#alertTimeoutModal').length !== 0) {
        $('#alertTimeoutModal').remove();
    }
    var t =
        '<div id="alertTimeoutModal" class="modal" tabindex="-1" role="dialog">' +
        '<div class="modal-dialog" role="document" style="width:360px">' +
        '<div class="modal-content" style="background-color:#fff0f0">' +
        '<div class="modal-header alert-modal-header"><h5><i class="fa fa-warning"></i> WARNING</h5>' +
        '<button type="button" class="close" data-dismiss="modal" aria-label="Close"><i class="fa fa-times"></i></button>' +
        '</div>' +
        '<div class="modal-body">' +
        '<p>Your session will automatically expire in 1 minute.</p>' +
        '</div > ' +
        '<div class="modal-footer">' +
        '<br><button type="button" class="btn btn-default" onclick="renewLogin();">Stay Logged In</button>' +
        '</div>' +
        '</div>' +
        '</div>' +
        '</div>';
    $(document.body).append(t);
    $('#alertTimeoutModal').modal('show');
}

function renewLogin() {
    $.ajax({
        type: 'GET',
        url:  '/Account/RenewLogin',
        contentType: 'application/json; charset=utf-8',
    //    headers: headers,
        dataType: 'html',
        success: function (r) {
            if ($('#alertTimeoutModal').length !== 0) {
                $('#alertTimeoutModal').modal('hide');
            }
            clearTimeout(activityTimer);
            clearTimeout(warningTimer);
            clearTimeout(countDownTimer);
            startCheckActivityTimer();          
            startWarningTimer();
        },
        error: ajaxError,
        timeout: 10000
    });
}

function timeoutExpired() {
   // window.location = logoutUrl;
}

// end auto session end and save



function waitOff() {
    $('#modaloverlay').hide();
    $('#overlayImage').hide();
}
function waitOn() {
    $('#modaloverlay').show();
    $('#overlayImage').show();
}
/* Utilities */
function emailCheck(emailStr) {
    var emailPat = /^(.+)@(.+)$/;
    var specialChars = '\\(\\)<>@,;:\\\\\\\"\\.\\[\\]';
    var validChars = '\[^\\s' + specialChars + '\]';
    var quotedUser = '(\"[^\"]*\")';
    var ipDomainPat = /^\[(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})\]$/;
    var atom = validChars + '+';
    var word = "(" + atom + "|" + quotedUser + ')';
    var userPat = new RegExp("^" + word + "(\\." + word + ")*$");
    var domainPat = new RegExp("^" + atom + "(\\." + atom + ")*$");
    var matchArray = emailStr.match(emailPat);
    if (matchArray === null) return false;
    var user = matchArray[1];
    var domain = matchArray[2];
    if (user.match(userPat) === null) return false;
    var IPArray = domain.match(ipDomainPat);
    if (IPArray !== null) {
        for (var i = 1; i <= 4; i++) {
            if (IPArray[i] > 255) return false;
        }
        return true;
    }
    var domainArray = domain.match(domainPat);
    if (domainArray === null) return false;
    var atomPat = new RegExp(atom, 'g');
    var domArr = domain.match(atomPat);
    var len = domArr.length;
    if (domArr[domArr.length - 1].length < 2 || domArr[domArr.length - 1].length > 3) return false;
    if (len < 2) return false;
    return true;
}
var digits = '0123456789';
var phoneNumberDelimiters = '()- ';
var validWorldPhoneChars = phoneNumberDelimiters + '+';
function isInteger(s) {
    var i;
    for (i = 0; i < s.length; i++) { var c = s.charAt(i); if (c < '0' || c > '9') return false; }
    return true;
}
function stripCharsInBag(s, bag) {
    var i;
    var r = '';
    for (i = 0; i < s.length; i++) { var c = s.charAt(i); if (bag.indexOf(c) === -1) r += c; }
    return r;
}
function checkPhone(sP) {
    if (sP.val() === null || sP.val() === '') return false;
    sP.val(stripCharsInBag(sP.val(), validWorldPhoneChars));
    if (isInteger(sP.val()) && sP.val().length === 10) {
        sP.val('(' + sP.val().substr(0, 3) + ')' + ' ' + sP.val().substr(3, 3) + '-' + sP.val().substr(6, 4));
        return true;
    }
    else return false;
}
function iconShow(el, m) {
    var pos = el.offset();
    var width = el.outerWidth();
    $('#iconInfo').html(m);
    $('#iconInfo').css({
        position: "absolute",
        top: pos.top + 30 + "px",
        left: pos.left + width - 70 + "px"
    }).show();
}
function iconHide() {
    $('#iconInfo').hide();
}
/* Utilities End */
function Alert(m, showYes, showNo, yesFor) {
    if ($('#alertModal').length != 0) {
        $('#alertModal').remove();
    }
    if ($('#alertModal').length == 0) {
        var t =
            '<div id="alertModal" class="modal" tabindex="-1" role="dialog">' +
            '<div class="modal-dialog" role="document" style="width:360px">' +
            '<div class="modal-content" style="background-color:#fff0f0">' +
            '<div class="modal-header alert-modal-header"><h5><i class="fa fa-warning"></i> WARNING</h5>' +
            '<button type="button" class="close" data-dismiss="modal" aria-label="Close"><i class="fa fa-times"></i></button>' +
            '</div>' +
            '<div class="modal-body">' +
            '<p id="alertMsg"></p>' +
            '</div > ' +
            '<div class="modal-footer">' +
            '<button type="button" id="btnClose" class="btn btn-default" data-dismiss="modal">Close</button>' +
            '<br><button type="button" id="btnYes" class="btn btn-default btnYes" data-for=' + yesFor + ' style="display:none">Yes</button>' +
            '<br><button type="button" id="btnNo" class="btn btn-default" data-dismiss="modal" data-for=' + yesFor + ' style="display:none" >No</button>' +
            '</div>' +
            '</div>' +
            '</div>' +
            '</div>';
        $(document.body).append(t);
    }
    $('#alertMsg').html(m);
    if (showYes == true) {
        $('#btnYes').show();
        $('#btnClose').hide();
    }
    else {
        $('#btnYes').hide();
        $('#btnClose').show();
    }
    if (showNo == true) {
        $('#btnNo').show();
        $('#btnClose').hide();
    }
    else {
        $('#btnNo').hide();
        $('#btnClose').show();
    }    

    $('#alertModal').modal('show');
}

function MessageAlert(msg) {
    if ($('#messageModal').length !== 0) {
        $('#messageModal').remove();
    }

    if ($('#messageModal').length === 0) {
        var txt =
            '<div id="messageModal" class="modal fade" tabindex="-1" role="dialog">' +
                '<div class="modal-dialog" role="document" style="width:360px">>' +
                    '<div class="modal-content">' +
                        '<div class="modal-header"><h5 id="dialogHeader"></h5>' +
                        '</div>' +
                        '<div class="modal-body" id="infoMsg">' +
                            '<input type="email" id="messageString" class="form-control validate">' +
                            '<input type="text" id="prIdtmp" class="form-control validate" style="display:none">' +
                            '<input type="text" id="nmtmp" class="form-control validate" style="display:none">' +
                        '</div>' +
                        '<div class="modal-footer">' +
                            '<button type="button" id="sendMessage" class="btn btn-default">Send</button>' +
                        '</div>' +
                    '</div>' +
                '</div>' +
            '</div>';
        $(document.body).append(txt);
    }
    $('#dialogHeader').html(msg);
    $('#messageModal').modal('show'); 
}
function SuccessAlert(msg) {
    if ($('#infoModal').length != 0) {
        $('#infoModal').remove();
    }

    if ($('#infoModal').length === 0) {
        var txt =
            '<div id="infoModal" class="modal fade" tabindex="-1" role="dialog">' +
                '<div class="modal-dialog" role="document" style="width:360px">' +
                    '<div class="modal-content">' +
                        '<div class="modal-header"><h5><i class="fa fa-warning"></i> Information</h5>' +
                            '<button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>' +
                        '</div>' +
                        '<div class="modal-body" id="infoMsg"></div>' +
                        '<div class="modal-footer">' +
                            '<button type="button" class="btn btn-default" data-dismiss="modal">Close</button>' +
                        '</div>' +
                    '</div>' +
                '</div>' +
            '</div>';
        $(document.body).append(txt);
    }
    $('#infoMsg').html(msg);
    $('#infoModal').modal('show');
}
function Warning(msg) {
    if ($('#warningModal').length != 0) {
        $('#warningModal').remove();
    }
    if ($('#warningModal').length === 0) {
        var txt =
            '<div id="warningModal" class="modal fade" tabindex="-1" role="dialog">' +
            '<div class="modal-dialog" role="document" style="width:360px">>' +
            '<div class="modal-content">' +
            '<div class="modal-header warning-modal-header"><h5><i class="fa fa-info-circle"></i> Warning</h5>' +
            '<button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>' +
            '</div>' +
            '<div class="modal-body"><p id="warningMsg"></p></div>' +
            '<div class="modal-footer">' +
            '<button type="button" class="btn btn-warning" id="btnProceed">Proceed</button>' +
            '<button type="button" class="btn btn-secondary" data-dismiss="modal">Close</button>' +
            '</div>' +
            '</div>' +
            '</div>' +
            '</div>';
        $(document.body).append(txt);
    }
    $('#warningMsg').html(msg);
    $('#warningModal').modal('show');
}
function Confirm(msg) {
    if ($('#confirmModal').length === 0) {
        var txt =
            '<div id="confirmModal" class="modal fade" tabindex="-1" role="dialog">' +
            '<div class="modal-dialog" role="document" style="width:360px">>' +
            '<div class="modal-content">' +
            '<div class="modal-header confirm-modal-header"><h5><i class="fa fa-info-circle"></i> Confirm</h5>' +
            '<button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>' +
            '</div>' +
            '<div class="modal-body"><p id="confirmMsg"></p></div>' +
            '<div class="modal-footer">' +
            '<button type="button" class="btn btn-primary" id="btnConfirm">Confirm</button>' +
            '<button type="button" class="btn btn-secondary" data-dismiss="modal">Close</button>' +
            '</div>' +
            '</div>' +
            '</div>' +
            '</div>';
        $(document.body).append(txt);
    }
    $('#confirmMsg').html(msg);
    $('#confirmModal').modal('show');
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
function clearSearch() {
    $('#nm').value = '';
    $('.nameListItem').show();
}
function search() {
    var str = $.trim($('#nm').val()).split("`").join("").toLowerCase();

    if (str.length > 0) {
        $('.nameListItem').each(function () {
            if ($(this).text().toLowerCase().indexOf(str) === -1)
                $(this).hide();
            else
                $(this).show();
        });
    }
    else clearSearch();
    return false;
}

//common functions
function findParent(element) {
    var parentElement = $(element).parent();
    if ($(parentElement).hasClass("parent"))
        return parentElement;
    else {
        for (var i = 0; i < 12; i++) {
            parentElement = $(parentElement).parent();
            if ($(parentElement).hasClass("parent"))
                return parentElement;
        }
    }
}
function debugPW(i) {
    var k = i;
}


