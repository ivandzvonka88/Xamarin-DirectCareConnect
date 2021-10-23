var token;
var headers;

$(document).ready(function () {
    token = $('input[name="__RequestVerificationToken"]').val();
    headers = {};
    headers['__RequestVerificationToken'] = token;

});

function openAddCompanyModal(i) {
    $('.companyModal').find('input:text').val('');

    if (i > 0) {
        $.ajax({
            type: 'POST',
            url: srvcUrl + '/EditCompany?insuranceCompanyId=' + i,
            dataType: 'json',
            contentType: 'application/json; charset=utf-8',
            headers: headers,
            success: function (data) {
                $('#insPayerId').val(data.res.InsuranceCompanyPayerId);
                $('#payerId').val(data.res.PayerId);
                $('#name').val(data.res.Name);
                $('#line1').val(data.res.Line1);
                $('#line2').val(data.res.Line2);
                $('#city').val(data.res.City);
                $('#stateSelect').val(data.res.State);
                $('#postalCode').val(data.res.PostalCode);
                $('#insuranceCompanyId').val(data.res.InsuranceCompanyId);
                $('#mcid').val(data.res.MCID);

                $('#clearingHousesEligibilityId').val(data.res.EligibilityCheckId);
                $('#clearingHousesStatusCheckId').val(data.res.StatusCheckId);
                $('#clearingHouseId').val(data.res.ClearinghouseId);


                $("#btnSave").attr('Value', 'Update');
                $('.companyModal').modal('show');

                $('#headingEdit').show();
                $('#headingAdd').hide();


                $.each(data.res.GovtProgramLinksList, function (key, row) {
                    $("#tblGovtProgLinks tbody tr").each(function () {

                        var id = $(this).find("td").eq(0).find("span").text();
                        if (row.ID == id) {
                            $(this).find("td").eq(1).find("#govtProgAlternateId").val(row.Code);
                        }
                    });
                });
            },
            error: function (jqXHR, textStatus, errorThrown) {
                console.log('error');
            }
        });
    }
    else {
        console.log("test");
        $.ajax({
            type: 'POST',
            url: srvcUrl + '/OpenManageCompanyView',
            contentType: 'application/json; charset=utf-8',
            headers: headers,
            dataType: 'html',
            success: function (data) {
                $('#divCompanyModal').html('').html(data);
                $('.companyModal').modal('show');
                $('#headingEdit').hide();
                $('#headingAdd').show();
            },
            error: function (jqXHR, textStatus, errorThrown) {
                console.log('failed', textStatus);
            }
        });
    }
}


function ManageCompany() {
    //var id = $('#payerId').val();
    var btn = $("#btnSave").val();
    var id = $('#insuranceCompanyId').val();
    var govtProgramLinks = [];

    $("#tblGovtProgLinks tbody tr").each(function () {

        var id = $(this).find("td").eq(0).find("span").text();
        var code = $(this).find("td").eq(1).find("#govtProgAlternateId").val();
        if (code > 0) {
            govtProgramLinks.push({ 'ID': id, 'Code': code });
        }
    });

    var Data = {
        'InsPayerId': $('#insPayerId').val(),
        'State': $('#stateSelect :selected').val(),
        'PayerId': $('#payerId').val(),
        'Name': $('#name').val(),
        'Line1': $('#line1').val(),
        'Line2': $('#line2').val(),
        'City': $('#city').val(),
        'PostalCode': $('#postalCode').val(),
        'InsuranceCompanyId': $('#insuranceCompanyId').val(),
        'EligibilityCheckId': $('#clearingHousesEligibilityId').val(),
        'StatusCheckId': $('#clearingHousesStatusCheckId').val(),
        'ClearinghouseId': $('#clearingHouseId').val(),
        'GovtProgramLinksList': govtProgramLinks,
        'MCID': $('#mcid').val(),
    };
    waitOn();

    $.ajax({
        type: 'POST',
        url: srvcUrl + '/ManageCompany',
        data: JSON.stringify(Data),
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'json',
        success: function (data) {
            waitOff();
            $('.companyModal').modal('hide');
            $('#insuranceCompanyTable').DataTable().ajax.reload();
        },
        error: function (jqXHR, textStatus, errorThrown) {
            console.log('error', textStatus);
        }
    });
}
function deleteInsuranceCompany(i) {
    var id = i;
    waitOn();
    $.ajax({
        type: 'POST',
        url: srvcUrl + '/DeleteCompany?insuranceCompanyID=' + id,
        contentType: 'application/json; charset=utf-8',
        headers: headers,
        dataType: 'json',
        success: function (data) {
            waitOff();
            $('#insuranceCompanyTable').DataTable().ajax.reload();
        },
        error: function (jqXHR, textStatus, errorThrown) {
            console.log('error', textStatus);
            timeout: 10000
        }
    });
}

