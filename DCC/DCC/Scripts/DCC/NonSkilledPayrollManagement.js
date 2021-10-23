function getIsolvedPayollReport(i) {
    window.location.href = srvcUrl + '/GetISolvedPayollReport?payrollId=' + i;
}
function getPayollReport(i) {
    window.location.href = srvcUrl + '/GetPayollReport?payrollId=' + i;
}
function getPayollErrors(i) {
    window.location.href = srvcUrl + '/GetPayollErrors?payrollId=' + i;
}
function getPayollExport(i) {
    window.location.href = srvcUrl + '/GetPayollExport?payrollId=' + i;
}

function openIsolvedPayrollPreview(i) {
    waitOn();
    $.ajax({
        type: 'GET',
        url: srvcUrl + '/OpenIsolvedPayrollPreview?payrollId=' + i,
        contentType: 'application/json; charset=utf-8',
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#actionModals').html(data);
        },
        error: ajaxError,
        timeout: 30000
    });
}
function initiateISolvedPayrollReport(i) {
    waitOn();
    $.ajax({
        type: 'GET',
        url: srvcUrl + '/InitiateISolvedPayrollPreview?payrollId=' + i,
        contentType: 'application/json; charset=utf-8',
        dataType: 'html',
        success: function (data) {
            console.log(data);
            $('#actionModal').modal('hide');
            waitOff();
        },
        error: ajaxError,
        timeout: 30000
    });
}

function submitISolvedPayrollReport(i) {
    waitOn();
    $.ajax({
        type: 'GET',
        url: srvcUrl + '/SubmitISolvedPayroll?payrollId=' + i,
        contentType: 'application/json; charset=utf-8',
        dataType: 'html',
        success: function (data) {
            console.log(data);
            $('#actionModal').modal('hide');
            waitOff();
        },
        error: ajaxError,
        timeout: 30000
    });
}

function toggleErrors(checkbox) {
    if (checkbox.checked) {
        $(".modal-body table tr th:nth-child(4)").show();
        $(".modal-body table tr td:nth-child(4)").show();
        $(".modal-body table tr td:nth-child(4):empty").parents("tr").hide();
    } else {
        $(".modal-body table tr th:nth-child(4)").hide();
        $(".modal-body table tr td:nth-child(4)").hide();
        $(".modal-body table tr td:nth-child(4):empty").parents("tr").show();
    }
}