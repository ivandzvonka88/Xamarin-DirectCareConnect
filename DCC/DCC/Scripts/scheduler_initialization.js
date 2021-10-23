function pre_init() {
    var update_select_options = function (element, options, selected_id = 0) { // helper function
        element.options.length = 0;
        for (var i = 0; i < options.length; i++) {
            var option = options[i];
            element[i] = new Option(option.label, option.key);
        }

        element.value = selected_id;
    };

    var fetch_clients = function (provider_id) {
        return new Promise((resolve, reject) => {
            $.ajax({
                type: 'Get',
                url: '../Calendar/GetClients?providerID=' + provider_id,
                contentType: 'application/json',
                success: function (data) {
                    resolve(data);
                },
                error: function (error) {
                    ajaxError(error);
                    reject(error);
                },
                timeout: 10000
            });
        });
    };

    var fetch_services = function (client_id) {
        return new Promise((resolve, reject) => {
            $.ajax({
                type: 'Get',
                url: '../Calendar/GetServices?clientID=' + client_id,
                contentType: 'application/json',
                success: function (data) {
                    resolve(data);
                },
                error: function (error) {
                    ajaxError(error);
                    reject(error);
                },
                timeout: 10000
            });
        });
    }

    var client_onchange = function (event) {
        var client_id = this.value;
        waitOn();

        return fetch_services(client_id).then((data) => {
            update_select_options(scheduler.formSection('Service').control, data);
        }).finally(() => waitOff());
    };

    scheduler.attachEvent("onBeforeLightbox", function (id) {
        var ev = scheduler.getEvent(id);

        if (ev.provider_id) { // Edit schedule
            waitOn();

            fetch_clients(ev.provider_id).then((data) => {
                update_select_options(scheduler.formSection('Client').control, data, ev.client_id);
                return fetch_services(ev.client_id);
            }).then((data) => {
                update_select_options(scheduler.formSection('Service').control, data, ev.service_id);
            }).finally(() => waitOff());
        } else {    // New schedule
            var provider_id = $("#ProviderFilterSelection").val();

            if (!provider_id) {
                alert("Please select a provider first.");
                scheduler.deleteEvent(id);
                return false;
            }

            waitOn();
            fetch_clients(provider_id).then((data) => {
                update_select_options(scheduler.formSection('Client').control, data);
                update_select_options(scheduler.formSection('Service').control, []);
            }).finally(() => waitOff());
        }

        return true;
    });

    scheduler.attachEvent("onEventSave", function (id, ev) {
        if (!ev.client_id || ev.client_id === '0') {
            dhtmlx.alert("Client must not be empty");
            return false;
        }
        if (!ev.service_id || ev.service_id === '0') {
            dhtmlx.alert("Service must not be empty");
            return false;
        }
        ev.provider_id = $("#ProviderFilterSelection").val();

        return true;
    });

    scheduler.attachEvent("onEventCollision", function (ev, evs) {
        var collisionEvents = "";
        var recurringPIds = [];
        for (let i = 0; i < evs.length; i++) {
            var pid = evs[i].event_pid;
            if (pid) {
                if (recurringPIds.includes(pid)) {
                    break;
                }
                recurringPIds.push(pid);
            }
            collisionEvents += "<br>- " + (pid ? "Recurring" : "Single") + " event occupying from " +
                evs[i].start_date.toLocaleString() + " to " + evs[i].end_date.toLocaleString();
        }
        dhtmlx.alert({
            title: "Error!",
            type: "alert-error",
            text: "Sorry, there is a collision with the following event(s):" + collisionEvents
        });

        return true;
    });

    scheduler.config.lightbox.sections.splice(0, 1);    // Remove description field
    scheduler.config.lightbox.sections.unshift(
        {
            name: "Client", height: 23, type: "select", options: [],
            map_to: "client_id", onchange: client_onchange
        },
        {
            name: "Service", height: 23, type: "select", options: [],
            map_to: "service_id"
        }
    );
    var format = scheduler.date.date_to_str('%h:%i %A');
    scheduler.templates.time_picker = function (date) {
        return format(date);
    };
 
    scheduler.templates.event_text = function (start, end, ev) {
        if (!ev || !ev.ClientFullName) {
            return '';
        }
        return 'Client: ' + ev.ClientFullName;
    };
}

function post_init() {
}