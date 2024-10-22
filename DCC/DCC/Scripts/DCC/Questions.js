﻿var token;
var headers;
$(document).ready(function () {
    token = $('input[name="__RequestVerificationToken"]').val();
    headers = {};
    headers['__RequestVerificationToken'] = token;
    headers['__CompanyId'] = $('#CurrentCompanyId').val();
    $(function () {
        var columns = [
            {
                data: "questionId", orderable: false, mRender: function (data, type, full) {
                    var toReturn = '';
                    toReturn = '<i class="fa fa-edit faBtn" onclick="openQuestionEdit(' + data + ');"></i>';
                    return toReturn;
                }
            },
            {
                data: "title", sWidth: '70%'
            },
            {
                data: "valueType"
            },
            {
                data: "minValue"
            },
            {
                data: "maxValue"
            },
            {
                data: "isActiveStr"
            }
        ];
        createGridForAjax('#questionsTable', 'Questions/GetAllQuestions', columns, null);
    });

});

function openQuestionEdit(qId) {
    if (qId === 0) {
        $('#questionId').val(0);
        $('#title').val('');
        $('#valueTypeId').val(2);
        $('#minValue').val('');
        $('#maxValue').val('');
        $('#isActive').prop('checked', false);
        $('#editQHdr').text('New Question');
        $('#QBtn').text('Add New Question');
    }
    else {
        $.ajax({
            type: 'GET',
            url: srvcUrl + '/GetAllQuestions?questionId=' + qId,
            headers: headers,
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (data) {
                waitOff();
                $('#questionId').val(data[0].questionId);
                $('#title').val(data[0].title);
                $('#valueTypeId').val(data[0].valueTypeId);
                $('#minValue').val(data[0].minValue);
                $('#maxValue').val(data[0].maxValue);
                $('#isActive').prop('checked', data[0].isActive);
                $('#editQHdr').text('Update Question');
                $('#QBtn').text('Update Question');
            },
            error: ajaxError,
            timeout: 10000
        });
    }
    $('.editQuestionModal').modal('show');
}

function saveQuestion() {
    if ($('#title').val().length > 100)
        Alert('Question is too long');
    else if ($('#valueTypeId').val() !== '2' && (!isInteger($('#maxValue').val()) || !isInteger($('#minValue').val())))
        Alert('The Answer type requires a minimum and maximum value');
    else {
        waitOn();
        $('.editQuestionModal').modal('hide');
        var Data = {
            'questionId': $('#questionId').val(),
            'title': $('#title').val(),
            'valueTypeId': $('#valueTypeId').val(),
            'minValue': $('#minValue').val(),
            'maxValue': $('#maxValue').val(),
            'isActive': $('#isActive').prop('checked')
        };
        $.ajax({
            type: 'POST',
            url: srvcUrl + '/SaveQuestion',
            headers: headers,
            data: JSON.stringify(Data),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (r) {
                waitOff();
                if (r.er.code === 1) Alert('System Error - ' + r.er.msg);
                else if (r.er.code === 2) Alert('Error - ' + r.er.msg);
                $('#questionsTable').DataTable().ajax.reload();
            },
            error: ajaxError,
            timeout: 10000
        });
    }
}
