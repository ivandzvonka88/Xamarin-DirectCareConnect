﻿var token;
var headers;
function Question() {
    var questionId;
    var question;
    var orderNumber;
    var prepopulate;
    var isRequired;
    var sharedQuestion;
}

$(document).ready(function () {
    token = $('input[name="__RequestVerificationToken"]').val();
    headers = {};
    headers['__RequestVerificationToken'] = token;
    headers['__CompanyId'] = $('#CurrentCompanyId').val();  
});

function getReportView() {
    if ($('#serviceId').val() !== '0') {
        waitOn();
        var Data = {
            'serviceId': $('#serviceId').val()
        };
        $.ajax({
            type: 'POST',
            url: srvcUrl +'/GetReportsPage',
            data: JSON.stringify(Data),
            contentType: 'application/json; charset=utf-8',
            headers: headers,
            dataType: 'html',
            success: function (data) {
                waitOff();
                $('#reportViewWrapper').html(data);
            },
            error: ajaxError,
            timeout: 10000
        });
    }
}

function getPeriodicMatch() {
    $('#matchingPeriodicQuestions').empty();
    if ($('#newPeriodicQuestion').val().length > 1) {
        $.each($('.allQuestions'), function () {
            if ($(this).find('.quest').val().toLowerCase().indexOf($('#newPeriodicQuestion').val().toLowerCase()) !== -1)
                $('#matchingPeriodicQuestions').append('<div><a href="#" onclick="addPeriodicQuestion(\'' + $(this).find('.questId').val() + '\');return false;">' + $(this).find('.quest').val() + '</a></div>');
        });
    }
}

function addPeriodicQuestion(i) {
    if ($('#periodicQuestionId' + i).length !== 0)
        Alert('The question is already on the list');
    else {
        var orderNum = parseInt($('#nextPeriodicQuestionIndex').val());
        var newQuestion = '';
        $.each($('.allQuestions'), function () {
         
            if ($(this).find('.questId').val() === i) {
                newQuestion = $(this).find('.quest').val();
                return false;
            }
        });
        var nRow = 
            '<tr id="periodicQuestionId' + i + '">' +
            '<td><input type="hidden" class="questionId" value="' + i + '"/><i class="fa fa-trash faBtn red" onclick="deletePeriodicQuestion(' + i + ');"></i></td>' +
            '<td class="question">' + newQuestion + '</td>' +
            '<td><button class="minus btn btn-sm btn-secondary" style="width:25px;display:inline" onclick="decOrderNumber($(this));">-</button><input type="text" class="orderNumber form-control form-control-sm" style="width:40px;display:inline" value="' + orderNum + '" /><button class="plus btn btn-sm btn-secondary" style="width:25px;display:inline" onclick="incOrderNumber($(this));">+</button></td>' +
            '<td class="text-center"><input type="checkbox" class="isRequired"/></td>' +
            '<td class="text-center"><input type="checkbox" class="prepopulate"/></td>' +
            '<td class="text-center"><input type="checkbox" class="sharedQuestion"/></td>' +
            '</tr>';
        $('#periodicQuestions').append(nRow);
        $('#nextPeriodicQuestionIndex').val(orderNum + 1);
        $('#newPeriodicQuestion').val('');
        $('#matchingPeriodicQuestions').empty();
    }
}

function deletePeriodicQuestion(i) {
    $('#periodicQuestionId' + i).remove();
}

function getSessionMatch() {
    $('#matchingSessionQuestions').empty();
    if ($('#newSessionQuestion').val().length > 1) {
        $.each($('.allQuestions'), function () {
            if ($(this).find('.quest').val().toLowerCase().indexOf($('#newSessionQuestion').val().toLowerCase()) !== -1)
                $('#matchingSessionQuestions').append('<div><a href="#" onclick="addSessionQuestion(\'' + $(this).find('.questId').val() + '\');return false;">' + $(this).find('.quest').val() + '</a></div>');
        });
    }
}

function addSessionQuestion(i) {
    if ($('#sessionQuestionId' + i).length !== 0)
        Alert('The question is already on the list');
    else {
        var orderNum = parseInt($('#nextSessionQuestionIndex').val());
        var newQuestion = '';
        $.each($('.allQuestions'), function () {

            if ($(this).find('.questId').val() === i) {
                newQuestion = $(this).find('.quest').val();
                return false;
            }
        });
        var nRow =
            '<tr id="sessionQuestionId' + i + '">' +
            '<td><input type="hidden" class="questionId" value="' + i + '"/><i class="fa fa-trash faBtn red" onclick="deleteSessionQuestion(' + i + ');"></i></td>' +
            '<td class="question">' + newQuestion + '</td>' +
            '<td><button class="minus btn btn-sm btn-secondary" style="width:25px;display:inline" onclick="decOrderNumber($(this));">-</button><input type="text" class="orderNumber form-control form-control-sm" style="width:40px;display:inline" value="' + orderNum + '" /><button class="plus btn btn-sm btn-secondary" style="width:25px;display:inline" onclick="incOrderNumber($(this));">+</button></td>' +
            '<td class="text-center"><input type="checkbox" class="isRequired"/></td>' +
            '<td class="text-center"><input type="checkbox" class="prepopulate"/></td>' +
            '<td class="text-center"><input type="checkbox" class="sharedQuestion"/></td>' +
            '</tr>';
        $('#sessionQuestions').append(nRow);
        $('#nextSessionQuestionIndex').val(orderNum + 1);
        $('#newSessionQuestion').val('');
        $('#matchingSessionQuestions').empty();
    }
}

function deleteSessionQuestion(i) {
    $('#sessionQuestionId' + i).remove();
}

function incOrderNumber(x) {
    var y = x.parent().find('input');
    var i = parseInt(y.val());
    y.val(i + 1);

}

function decOrderNumber(x) {
    var y = x.parent().find('input');
    var i = parseInt(y.val());
    if (i > 1)
        y.val(i - 1);
}

function updatePeriodicQuestions() {
    var qInfo = [];

    $('#periodicQuestions').find('tr').each(function () {
        var q = new Question();
        q.questionId = $(this).find('.questionId').val();
        q.orderNumber = $(this).find('.orderNumber').val();
        q.prepopulate = $(this).find('.prepopulate').prop('checked');
        q.isRequired = $(this).find('.isRequired').prop('checked');
        q.sharedQuestion = $(this).find('.sharedQuestion').prop('checked');
        q.supervisorOnly = $(this).find('.supervisorOnly').prop('checked');
        qInfo.push(q);
    });
    waitOn();
    var Data = {
        'serviceId': $('#currentServiceId').val(),
        'periodicQuestions': qInfo
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/SavePeriodicQuestions',
        headers: headers,
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#periodicQuestionWrapper').html(data);
        },
        error: ajaxError,
        timeout: 10000
    });
} 

function cancelPeriodicChanges() {
    var Data = {
        'serviceId': $('#currentServiceId').val()
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/GetPeriodicReportView',
        headers: headers,
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#periodicQuestionWrapper').html(data);
        },
        error: ajaxError,
        timeout: 10000
    });
}

function updateSessionQuestions() {
    var qInfo = [];

    $('#sessionQuestions').find('tr').each(function () {
        var q = new Question();
        q.questionId = $(this).find('.questionId').val();
        q.orderNumber = $(this).find('.orderNumber').val();
        q.prepopulate = $(this).find('.prepopulate').prop('checked');
        q.isRequired = $(this).find('.isRequired').prop('checked');
        q.sharedQuestion = $(this).find('.sharedQuestion').prop('checked');
        qInfo.push(q);
    });
    waitOn();
    var Data = {
        'serviceId': $('#currentServiceId').val(),
        'sessionQuestions': qInfo
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/SaveSessionQuestions',
        headers: headers,
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#sessionQuestionWrapper').html(data);
        },
        error: ajaxError,
        timeout: 10000
    });
}

function cancelSessionChanges() {
    var Data = {
        'serviceId': $('#currentServiceId').val()
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/GetSessionReportView',
        headers: headers,
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#sessionQuestionWrapper').html(data);
        },
        error: ajaxError,
        timeout: 10000
    });
}

function openGoalAreaModal(i) {
    $('#goalAreaId').val(i);
    if (i === 0)
        $('#goalAreaName').val('');
    else
        $('#goalAreaName').val($('#goalArea' + i).find('.name').text());
    $('.addGoalAreaModal').modal('show');

}
function saveGoalArea() {
    if ($('#goalAreaName').val().length < 5)
        Alert('Please enter a goal area name');
    else {
        $('.addGoalAreaModal').modal('hide');
        waitOn();
        var Data = {
            'name': $('#goalAreaName').val(),
            'goalAreaId': $('#goalAreaId').val(),
            'serviceId': $('#currentServiceId').val()
        };
        $.ajax({
            type: 'POST',
            url: srvcUrl + '/SaveGoalArea',
            headers: headers,
            data: JSON.stringify(Data),
            contentType: 'application/json; charset=utf-8',
            dataType: 'html',
            success: function (data) {
                waitOff();
                $('#goalAreaWrapper').html(data);
            },
            error: ajaxError,
            timeout: 10000
        });
    }
}

function openRemoveGoalAreaModal(i) {
    $('#goalAreaId').val(i);
    $('#goalAreaToDelete').text($('#goalArea' + i).find('.name').text());
    $('.removeGoalAreaModal').modal('show');
}

function removeGoalArea() {
    $('.removeGoalAreaModal').modal('hide');
    waitOn();
    var Data = {
        'goalAreaId': $('#goalAreaId').val(),
        'serviceId': $('#currentServiceId').val()
    };
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/RemoveGoalArea',
        headers: headers,
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        dataType: 'html',
        success: function (data) {
            waitOff();
            $('#goalAreaWrapper').html(data);
        },
        error: ajaxError,
        timeout: 10000
    });

}

