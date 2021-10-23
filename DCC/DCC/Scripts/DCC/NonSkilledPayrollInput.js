var token;
var headers;
$(document).ready(function () {
    token = $('input[name="__RequestVerificationToken"]').val();
    headers = {};
    headers['__RequestVerificationToken'] = token;
});
function selectionChanged() {
    if ($('#providerSelect').val() != 0 && $('#periodSelect').val() != 0) {
        waitOn();
        $.ajax({
            type: 'GET',
            url: srvcUrl + '/getProviderPayroll?providerId=' + $('#providerSelect').val() + '&payrollId=' + $('#periodSelect').val() + '&startEndDates=' + $('#periodSelect option:selected').text(),
            contentType: 'application/json; charset=utf-8',
            headers: headers,
            dataType: 'html',
            success: function (data) {
                waitOff();
                $('#payRollInfo').html(data);
            },
            error: ajaxError,
            timeout: 10000
        });
    }
}

function dateChanged(i) {
    if (!setDateFromInput($('#cSTId' + i + ' .day')))
        Alert('Date in invalid format or day does not fall within timesheet days');
}

function setDateFromInput(e) {
    if (!isNaN(e.val())) {
        for (var i = 0; i < validDates.length; i++) {
            var x = parseInt(e.val());
            if (x >= validDates[i].sdy && x <= validDates[i].edy) {
                e.val(validDates[i].mn + '/' + x + '/' + validDates[i].yr);
                break;
            }
        }
    }
    return getDate(e, false);
}

function getDate(e, emptyOK, NAOK) {
    e.val(e.val().replace(/^\s+|\s+$/g, '').replace('\\', '/').replace('\\', '/').replace('.', '/').replace('.', '/'));
    if (e.val() == '' && emptyOK) {
        e.css('background-color', '#ffffff');
        return true;
    }
    else if (e.val() == '' && !emptyOK) {
        e.css('background-color', '#ffe0e0');
        return false;
    }
    if (e.val() == 'NA' && NAOK) {
        e.css('background-color', '#ffffff');
        return true;
    }
    if (e.val() == 'NA' && !NAOK) {
        e.css('background-color', '#ffffff');
        return true;
    }
    var sP = e.val().split('/');
    if (sP.length == 3 || (sP.length == 1 && (e.val().length == 6 || e.val().length == 8))) {
        if (sP.length == 1) {
            sP[0] = e.val().substr(0, 2);
            sP[1] = e.val().substr(2, 2);
            if (e.val().length == 6) sP[2] = '20' + e.val().substr(4, 2);
            else sP[2] = e.val().substr(4, 4);
        }
        if (!isNaN(sP[2])) {
            var yr = parseInt(sP[2], 10);
            if (isNaN(yr)) return false;
            else if (yr < 100) {
                yr = 2000 + yr;
                sP[2] = '20' + sP[2];
            }
            if (yr > 2110 || yr < 1920) {
                e.css('background-color', '#ffe0e0');
                return false;
            }
        }
        var mn;
        if (!isNaN(sP[0])) {
            mn = parseInt(sP[0], 10);
            if (isNaN(mn)) {
                e.css('background-color', '#ffe0e0');
                return false;
            }
            if (mn > 12 || mn < 1) {
                e.css('background-color', '#ffe0e0');
                return false;
            }
        }
        if (!isNaN(sP[1])) {
            var dy = parseInt(sP[1], 10);
            if (dy < 1) {
                e.css('background-color', '#ffe0e0');
                return false;
            }
            else if (mn == 2 && dy > 29) {
                e.css('background-color', '#ffe0e0');
                return false;
            }
            else if ((mn == 4 || mn == 6 || mn == 9 || mn == 11) && dy > 30) {
                e.css('background-color', '#ffe0e0');
                return false;
            }
            else if (dy > 31) {
                e.css('background-color', '#ffe0e0');
                return false;
            }
        }
        var t = Date.parse(sP[0] + '/' + sP[1] + '/' + sP[2]);
        if (isNaN(t)) {
            e.css('background-color', '#ffe0e0');
            return false;
        }
        else {
            var dt = new Date(sP[0] + '/' + sP[1] + '/' + sP[2]);
            e.val((dt.getMonth() + 1) + '/' + dt.getDate() + '/' + dt.getFullYear());
            e.css('background-color', '#ffffff');
            return true;
        }
    }
    else {
        e.css('background-color', '#ffe0e0');
        return false;
    }
}

function codeChanged(i) {
    /*
    var sP = $('#cSTId' + i + ' .code').val().split('-');
    if (sP.length == '1') {
        $('#cSTId' + i + ' .inTime').val('');
        $('#cSTId' + i + ' .outTime').val('');
        $('#cSTId' + i + ' .units').val('');
        $('#cSTId' + i + ' .inTime').prop('disabled', true);
        $('#cSTId' + i + ' .outTime').prop('disabled', true);
        $('#cSTId' + i + ' .units').prop('disabled', true);
    }
    else if (sP[1] == '0') {
        $('#cSTId' + i + ' .inTime').val('');
        $('#cSTId' + i + ' .outTime').val('');
        $('#cSTId' + i + ' .units').val('');
        $('#cSTId' + i + ' .inTime').prop('disabled', true);
        $('#cSTId' + i + ' .outTime').prop('disabled', true);
        $('#cSTId' + i + ' .units').prop('disabled', false);
    }
    else {
        $('#cSTId' + i + ' .cSTId' + i + ' .inTime').val('');
        $('#cSTId' + i + ' .outTime').val('');
        $('#cSTId' + i + ' .units').val('');
        $('#cSTId' + i + ' .inTime').prop('disabled', false);
        $('#cSTId' + i + ' .outTime').prop('disabled', false);
        $('#cSTId' + i + ' .units').prop('disabled', true);
    }
    */
}
function inputChanged(i) {
    var inTime = $('#cSTId' + i + ' .inTime');
    var outTime = $('#cSTId' + i + ' .outTime');
    var inx = getTimex(inTime);
    var outx = getTimex(outTime);
    if (inx === 1440)
        inx = 0;
    if (outx !== -1 && inx !== -1) {
        if (inx > outx) {
            $('#cSTId' + i + ' .units').val('0.00');
            $('#cSTId' + i + ' .inTime').css('background-color', '#ffe0e0');
            $('#cSTId' + i + ' .outTime').css('background-color', '#ffe0e0');
        }
        else {
            var mins = outx - inx;
            var hrs = parseFloat(mins) / 60;
            var hrsInt = Math.floor(hrs)
            var minsRem = mins - (hrsInt * 60);
            hrs = parseFloat(hrsInt);
            if (minsRem > 52)
                hrs += 1.00;
            else if (minsRem > 37)
                hrs += 0.75;
            else if (minsRem > 22)
                hrs += 0.50;
            else if (minsRem > 7)
                hrs += 0.25;
            $('#cSTId' + i + ' .units').val(hrs.toFixed(2));
        }

    }
    else {
        $('#cSTId' + i + ' .units').val('0.00');


    }


}

function getTimex(s) {
    var x = getTime(s);
    if (x !== -1) s.css('background-color', '#ffffff');
    return x;
}

function getTime(t) {
    t.css('background-color', 'transparent');
    var returnMinutes = -1;
    v = t.val().split(' ').join('').toUpperCase();
    if (v == '') {
        t.val('');
        t.css('background-color', '#ffe0e0');
        return -1;
    }
    else {
        v = v.split('.').join(':');
        var colonCount = 0;
        var hasMeridian = false;
        for (var i = 0; i < v.length; i++) {
            var ch = v.substring(i, i + 1);
            if ((ch < '0') || (ch > '9')) {
                if ((ch != ':') && (ch != 'A') && (ch != 'P') && (ch != 'M')) {
                    t.css('background-color', '#ffe0e0');
                    return -1;
                }
            }
            if (ch == ':') colonCount++;
            if ((ch == 'P') || (ch == 'A')) hasMeridian = true;
        }
        if (colonCount > 1) {
            t.css('background-color', '#ffe0e0');
            return -1;
        }
        else if (colonCount == 0) {
            var intCnt = 0;
            for (var i = 0; i < v.length; i++) {
                var ch = v.substring(i, i + 1);
                if ((ch >= '0') && (ch <= '9')) intCnt++;
            }
            if (intCnt < 3 || intCnt > 4) {
                t.css('background-color', '#ffe0e0');
                return -1;
            }
            else {
                var intCntNew = 0;
                for (var i = 0; i < v.length && intCntNew < intCnt - 2; i++) {
                    var ch = v.substring(i, i + 1);
                    if ((ch >= '0') && (ch <= '9')) intCntNew++;
                }
                v = v.substring(0, i) + ':' + v.substring(i);
            }
        }
        var hh = v.substring(0, v.indexOf(":"));
        if (hasMeridian) {
            if ((parseInt(hh) < 1) || (parseInt(hh) > 12)) {
                t.css('background-color', '#ffe0e0');
                return -1;
            }
        }
        else {
            if ((parseInt(hh) < 0) || (parseInt(hh) > 23)) {
                t.css('background-color', '#ffe0e0');
                return -1;
            }
        }
        var mm = v.substring(v.indexOf(":") + 1, v.length);
        if ((parseInt(mm) < 0) || (parseInt(mm) > 59)) {
            t.css('background-color', '#ffe0e0');
            return -1;
        }
        var hours;
        var mins = '';
        var mer = '';
        if (hasMeridian) {
            hours = parseInt(hh);
            for (var i = 0; i < mm.length; i++) {
                var ch = mm.substring(i, i + 1);
                if (ch >= '0' && ch <= '9') mins = mins + ch;
                else mer = mer + ch;
            }
            if (mer.substring(0, 1) == 'P') mer = ' PM';
            else mer = ' AM';
        }
        else {
            if (parseInt(hh) >= 12) {
                if (parseInt(hh) != 12) hours = parseInt(hh) - 12;
                else hours = parseInt(hh);
                mer = ' PM';
            }
            else if (parseInt(hh) == 0) {
                hours = 12; mer = ' AM';
            }
            else {
                hours = parseInt(hh);
                mer = ' AM';
            }
            mins = mm;
        }
        var M;
        mins = parseInt(mins);
        if (mins < 10) M = '0' + mins;
        else M = '' + mins;
        v = hours + ':' + M + mer;
        if (t.val() != v) t.val(v);
        if (hours == 12 && mer == ' PM' && mins == 0) returnMinutes = 720;
        else if (hours == 12 && mer == ' AM' && mins == 0) returnMinutes = 1440;
        else {
            if (hours == 12) hours = 0;
            if (mer == ' AM') returnMinutes = 0 + (hours * 60) + mins;
            else returnMinutes = 720 + (hours * 60) + mins;
        }
    }

    return returnMinutes;
}

function insertPayrollRecord(i) {

    var inx = getTimex($('#cSTId' + i + ' .inTime'));
    var outx = getTimex($('#cSTId' + i + ' .outTime'));
    var dateObj = $('#cSTId' + i + ' .day');


    setDateFromInput(dateObj);
    if (inx === 1440)
        inx = 0;
    if (inx < 0)
        Alert('In time is in incorrect format');
    else if (outx < 0)
        Alert('Out time is in incorrect format');
    else if (outx < inx)
        Alert('Out time is smaller than in time');
    else if (!setDateFromInput(dateObj))
        Alert('Invalid date format or date was not within timesheet period');
    else if ($('#cSTId' + i + ' .code').val() === '')
        Alert('No payroll code has been selected');
    else {

        var sP = $('#cSTId' + i + ' .code').val().split('-'); 
        var Data = {
            'Id': i,
            'providerId': $('#providerId').val(),
            'payrollId': $('#periodSelect').val(),
            'payrollCode': sP[0],
            'startEndDates': $('#periodSelect option:selected').text(),
            'date': dateObj.val(),
            'inOffsetMinutes': inx,
            'outOffsetMinutes': outx,
            'units': $('#cSTId' + i + ' .units').val()
        };

        waitOn();

        $.ajax({
            type: 'POST',
            url: srvcUrl + '/InsertProviderPayrollRecord',
            data: JSON.stringify(Data),
            contentType: 'application/json; charset=utf-8',
            headers: headers,
            dataType: 'html',
            success: function (data) {
                waitOff();
                $('#payRollInfo').html(data);
            },
            error: ajaxError,
            timeout: 10000
        });
    }
}


function openDeletePayrollRecord(i, dt, code, start, end) {
    waitOn();
    $.ajax({
        type: 'GET',
        url: srvcUrl + '/openDeletePayrollRecord?id=' + i + '&Code=' + code + '&Date=' + dt + '&Start=' + start + '&End='  +end ,
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

function deletePayrollRecord(i) {
    $('#actionModal').modal('hide');
    waitOn();
    var Data = {
        'Id': i,
        'providerId': $('#providerId').val(),
        'payrollId': $('#periodSelect').val(),
        'startEndDates': $('#periodSelect option:selected').text(),

    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/DeletePayrollRecord',
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#payRollInfo').html(data);
        },
        error: ajaxError,
        timeout: 10000
    });


}