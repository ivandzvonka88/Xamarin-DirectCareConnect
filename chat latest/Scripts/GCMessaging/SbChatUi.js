class ChatUi {
    constructor(options) {
        this.channels = [];
        this.container = null;
        this.options = {};
        this.#init();
        this.#constructSceleton();
    }

    //// PRIVATE METHODS ////
    #init() {
        var container = $("body").find("sb-chat-ui");
        if (container && container.length > 1) {
            console.error("There are more than one sb-chat-ui tags on the page");
            return;
        }

        if (container.length == 0) {
            console.error("There is no sb-chat-ui tag placed on the page");
            return;
        }

        var options_title_max_length = $(container[0]).attr("data-title-max-length");
        var options_last_msg_max_length = $(container[0]).attr("data-last-msg-max-length");

        if (options_title_max_length)
            this.options.title_max_length = options_title_max_length;
        if (options_last_msg_max_length)
            this.options.last_msg_max_length = options_last_msg_max_length;

        this.container = container;
    }

    #constructSceleton() {
        this.divContainer = jQuery('<div/>', {
            "class": "container chat-wrapper",
        });
        var divRow1 = jQuery('<div/>', {
            "class": "row",
        });
        var divCol3 = jQuery('<div/>', {
            "class": "col-3 nopadding",
        });
        this.divButtonsWrapper = jQuery('<div/>', {
            "id": "buttonsWrapper",
        });
        this.divChannelsWrapper = jQuery('<div/>', {
            "id": "channelsWrapper",
        });
        var divCol9 = jQuery('<div/>', {
            "class": "col-9 nopadding",
        });
        this.divChatWrapper = jQuery('<div/>', {
            "id": "chatWrapper",
        });
        var divRow2 = jQuery('<div/>', {
            "class": "row",
        });
        var divCol12 = jQuery('<div/>', {
            "class": "col-12 nopadding",
        });
        this.divMessageComposerWrapper = jQuery('<div/>', {
            "id": "messageComposerWrapper",
        });
        //stacking controls inside each other
        divCol12.append(this.divMessageComposerWrapper);
        divRow2.append(divCol12);
        divCol9.append(this.divChatWrapper);
        divCol3.append(this.divButtonsWrapper).append(this.divChannelsWrapper);
        divRow1.append(divCol3).append(divCol9);
        this.divContainer.append(divRow1).append(divRow2);
        this.container.replaceWith(this.divContainer);
    }
    //////////////////////////

    channelCreate(channel) {
        var parent = this;
        //Check if specified channel id already exists
        var item = this.channelGet(channel.url);
        if (item) {
            console.error("Channel with url " + channel.url + " already exists");
            return;
        }

        var channelbtn = jQuery('<button/>', {
            id: channel.url,
            title: channel.name,
            "data-channel-url": channel.url,
            "class": "list-group-item list-group-item-action "
        });

        //Create channel button click event, make all buttons inactive then make this one active
        channelbtn.on("click", function () {
            parent.setActive(channel.url);
        });

        var headingWrapper = jQuery('<div/>', {
            "class": "d-flex w-100 justify-content-between"
        });

        var title = jQuery('<h5/>', {
            "class": "mb-1"
        });
        var badge = jQuery('<small/>', {
            "class": "unread-msgs"
        });
        var lastmsg = jQuery('<p/>', {
            "class": "mb-1 last-msg"
        });
        var timeago = jQuery('<small/>', {
            "class": "time-ago"
        });

        //Apply options
        if (this.options) {
            if (this.options.title_max_length && channel.name.length > parent.options.title_max_length) {
                channel.name = channel.name.substring(0, parent.options.title_max_length) + "...";
            }
            if (parent.options.last_msg_max_length && channel.lastMessage && channel.lastMessage.message && channel.lastMessage.message.length > parent.options.last_msg_max_length) {
                channel.lastMessage.message = channel.lastMessage.message.substring(0, this.options.last_msg_max_length) + "...";
            }
        }
        /////////////////////////////////
    }

    channelUpdate(channel) {

    }

    channelGet(url) {

    }

    channelRemove(url) {

    }

    channelSetActive(url) {

    }

    channelMoveTop(url) {

    }

    channelMarkAsRead(url) {

    }
}