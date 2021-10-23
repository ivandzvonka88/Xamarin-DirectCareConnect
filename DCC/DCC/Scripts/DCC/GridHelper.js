/*
include('/Content/datatable/jquery.dataTables.css');
include('/scripts/datatable/jquery.datatables.min.js');
include('/scripts/datatable/jquery.datatables.js');
*/
function createGridForAjax(jquerySelector, url, columnsDefs, options) {
    if ($.fn.DataTable) {
        if ($.fn.DataTable.isDataTable(jquerySelector) === false) { 
            var options = _parseOptions(options);
            $(jquerySelector).on('preXhr.dt', function (e, settings, data) {
                $(jquerySelector).closest(".ibox-content").addClass('sk-loading');
            });

            $(jquerySelector).on('xhr.dt', function (e, settings, data) {
                $(jquerySelector).closest(".ibox-content").removeClass('sk-loading');
            });
            var headers = {};
            var dataTableInstance = $(jquerySelector).DataTable({
                dom: 'Blfrtip',
                buttons: options.effectiveButtons,
                rowReorder: options.effectiveRowReordering,
                //sScrollX: "100%",
                //sScrollXInner: "98%",
                serverSide: false,
                responsive: true,
                paging: options.effectivePaging,
                info: options.effectiveInfo,
                searching: options.effectiveSearching,
                ordering: options.effectiveOrdering,
                ajax:
                {
                    type: "POST",
                    contentType: 'application/json; charset=utf-8',
                    url: url,
                    headers: headers,
                    dataSrc: options.effectiveAjaxDataSrc

                },
                columns: columnsDefs,
                searchDelay: 800,
                stateSave: options.effectiveStateSave,
                destroy: options.effectiveDestroy,
                pageLength: options.effectivePageLength,
                lengthChange: options.effectiveLengthChange,
                lengthMenu: [[10, 25, 50, 100], [10, 25, 50, 100]],
                language: {
                    emptyTable: options.effectiveEmptyTableMessage
                },
                drawCallback: options.effectiveGridFinishedRenderingEventHandler
            });

            return dataTableInstance;
        }
    }
}

var _parseOptions = function (options) {
    var effectiveGridFinishedRenderingEventHandlerVar = _gridFinishedRenderingEventHandler;
    var gridFinishedRenderingUserValue = _getOption(options, 'onGridFinishedRender', null);
    if (gridFinishedRenderingUserValue != null) {
        effectiveGridFinishedRenderingEventHandlerVar = function (settings) {
            gridFinishedRenderingUserValue(settings);
            _gridFinishedRenderingEventHandler(settings);
        };
    }

    return {
        effectiveSearching: _getOption(options, 'searching', true),
        effectiveDestroy: _getOption(options, 'destroy', false),
        effectiveEmptyTableMessage: _getOption(options, 'emptyTableMessage', 'No records found.'),
        effectiveGridFinishedRenderingEventHandler: effectiveGridFinishedRenderingEventHandlerVar,
        effectivePaging: _getOption(options, 'paging', true),
        effectiveInfo: _getOption(options, 'info', true),
        effectivePageLength: _getOption(options, 'pageLength', 10),
        effectiveOrdering: _getOption(options, 'ordering', true),
        effectiveRowReordering: _getOption(options, 'rowReorder', false),
        effectiveStateSave: _getOption(options, 'stateSave', false),
        effectiveLengthChange: _getOption(options, 'lengthChange', true),
        effectiveButtons: _getOption(options, 'buttons', ['excel']),
        includeShowAllPageSize: _getOption(options, 'includeShowAllPageSize', false),
        effectiveSearchDelay: _getOption(options, 'searchDelay', 0),
        effectiveAjaxDataSrc: _getOption(options, 'ajaxDataSrc', ''),
    }
}


var _gridFinishedRenderingEventHandler = function (settings) {
    // apply tooltips for the rendered grid
    $('[data-toggle="tooltip"]').tooltip();
}

var _getOption = function (options, property, defaultValue) {
    if (typeof (options) != undefined &&
        options != null &&
        typeof (options[property]) != undefined &&
        options[property] != null) {
        return options[property];
    }
    return defaultValue;
}
/*
function include(file)  {
    if (file == '/Content/datatable/jquery.dataTables.css') {
        var link = document.createElement('link');
        link.href = file;
        link.rel = "stylesheet";
        document.getElementsByTagName('body').item(0).appendChild(link);
    }
    else {
        var script = document.createElement('script');
        script.src = file;
        script.type = 'text/javascript';
        script.defer = true;
        document.getElementsByTagName('body').item(0).appendChild(script);
    }
}
*/