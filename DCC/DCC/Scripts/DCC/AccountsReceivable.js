class AccountsReceivable {
    constructor(clientId, insuranceCompanyId, claimStatusId,  asOfDate, chart, claimAgeBucket) {
        this.clientId = clientId;
        this.insuranceCompanyId = insuranceCompanyId;
        this.claimStatusId = claimStatusId;
        this.asOfDate = asOfDate;
        this.chart = chart;
        this.claimAgeBucket = claimAgeBucket;
    }

    #getAPIFilter() {
        return {
            'clientId': this.clientId,
            'insuranceCompanyId': this.insuranceCompanyId,
            'claimStatusId': this.claimStatusId,
            'asOfDate': this.asOfDate,
            'ClaimAgeRange': this.claimAgeBucket
        }
    }

    async getClaimsCount() {
        var data = this.#getAPIFilter();
        return new Promise(function (resolve, reject) {
            waitOn();
            $.ajax({
                url: srvcUrl + '/GetClaimsCount',
                method: 'POST',
                headers,
                data: JSON.stringify(data),
                contentType: 'application/json; charset=utf-8',
                success: function (data) {
                    waitOff();
                    resolve(data)
                },
                error: function (request, status, error) {
                    reject(request.responseText);
                }
            });
        })
    }

    async getClaimList() {
        var data = this.#getAPIFilter();
        return new Promise(function (resolve, reject) {
            waitOn();
            $.ajax({
                url: srvcUrl + '/GetClaimList',
                method: 'POST',
                headers,
                data: JSON.stringify(data),
                contentType: 'application/json; charset=utf-8',
                success: function (data) {
                    waitOff();
                    resolve(data)
                },
                error: function (request, status, error) {
                    reject(request.responseText);
                }
            });
        })
    }

    accountsReceivableExport() {
        window.location.href = srvcUrl + `/ExportAccountsReceivable?clientId=${this.clientId}&insuranceCompanyId=${this.insuranceCompanyId}&claimStatusId=${this.claimStatusId}&asOfDate=${this.asOfDate}&ClaimAgeRange=${this.claimAgeBucket}`;
    }

    activeFilters() {
        return `
            ${typeof (this.asOfDate) != 'undefined' && this.asOfDate != '' ? ' - As of: ' + moment(this.asOfDate).format('MM/DD/YYYY') : ''}
            ${typeof (this.insuranceCompanyName) != 'undefined' && this.insuranceCompanyName != '' ? ' - ' + this.insuranceCompanyName : ''}
            ${typeof (this.clientName) != 'undefined' && this.clientName != '' ? ' - ' + this.clientName : ''}`
    }

    strAgeBucket() {
        return AR.claimAgeBucket <= 29 ? '< 30 Days' : AR.claimAgeBucket == 59 ? '30 - 59 Days' :
            AR.claimAgeBucket == 89 ? '60-89 Days' : AR.claimAgeBucket == 120 ? '90-120 Days' : '120+ Days'
    }
}

let AR = new AccountsReceivable(0, 0, -1, moment().format('YYYY-MM-DD'), null, 0)

async function initializeChart() {
    try {
        $('#asOfDateFilter').val(AR.asOfDate)
        $('#chartArea').html(await AR.getClaimsCount());
    }
    catch (err) {
        console.log(err)
    }
}

$(document).on('click', '#btnBacktoChart', async function () {
    $(this).hide();
    $('#tableArea').hide();
    $('#chartArea').show();
    renderAutoComplete('insuranceCompany', chartInsuranceCompanyFilterOption)
    renderAutoComplete('client', chartClientFilterOption)
    $('#pageTitle').html('Accounts Receivable -- Summary' + AR.activeFilters())
    $('#chartArea').html(await AR.getClaimsCount());
})

$(document).on('change', '.arChartFilter', async function (e) {
    var pageTitle = ''
    if ($(this).attr('id') == 'hdnInsuranceCompanyFilter') {
        AR.insuranceCompanyId = $(this).val() != '' ? parseInt($(this).val()) : 0
        AR.insuranceCompanyName = $('#insuranceCompanyFilter').val()
    }
    if ($(this).attr('id') == 'hdnClientFilter') {
        AR.clientId = $(this).val() != '' ? parseInt($(this).val()) : 0
        AR.clientName = $('#clientFilter').val()
    }
    if ($(this).attr('id') == 'claimStatusFilter') {
        AR.claimStatusId = parseInt($(this).val())
        console.log($(this).find("option:selected").text())
    }
    if ($(this).attr('id') == 'asOfDateFilter') AR.asOfDate = $(this).val()

    if ($('#tableArea').is(":hidden")) {
        pageTitle = 'Accounts Receivable -- Summary'
        $('#chartArea').html(await AR.getClaimsCount());
    } else {
        pageTitle = `Accounts Receivable -- ${AR.strAgeBucket()}`
        $('#tableArea').html(await AR.getClaimList())
    }
    $('#pageTitle').html(`${pageTitle} ${AR.activeFilters()}`)
})

$(document).on('change', '#customSwitch1', function () {
    if ($(this).prop('checked')) {
        $('.numberToggle').hide();
        $('.dollarAmt').show()
        $('#switchLabel').html('<h1>$</h1>');
        $(this).parent().removeClass('custom-control-right').addClass('custom-control')
    } else {
        $('.numberToggle').show();
        $('.dollarAmt').hide()
        $(this).parent().removeClass('custom-control').addClass('custom-control-right')
        $('#switchLabel').html('<h1>#</h1>');
    }
})

$(document).on('click', '#chartDataTable tr', async function () {
    $('#chartArea').hide();
    $('#tableArea').show();
    $('#btnBacktoChart').show();
    AR.claimAgeBucket = parseInt($(this).data('claim-age-bucket'))
    $('#tableArea').html('')
    $('#tableArea').html(await AR.getClaimList())
    $('#pageTitle').html(`Accounts Receivable -- ${AR.strAgeBucket() + AR.activeFilters()}`)
})

$(document).on('click', '.arChartClearFilter', async function () {
    if ($(this).data('filter') == 'client') {
        $('#clientFilter').val('')
        $('#hdnClientFilter').val('').change()
    }
    if ($(this).data('filter') == 'insuranceCompany') {
        $('#insuranceCompanyFilter').val('')
        $('#hdnInsuranceCompanyFilter').val('').change()
    }
    if ($('#tableArea').is(":hidden")) {
        $('#chartArea').html(await AR.getClaimsCount());
    } else {
        $('#tableArea').html(await AR.getClaimList())
    }
})

$(document).on('click', '#exportAccountsReceivable', function (e) {
    e.stopPropagation();
    AR.accountsReceivableExport();
})

$(document).on('click', '.dropdown .dropdown-menu', function (e) {
    e.stopPropagation();
});