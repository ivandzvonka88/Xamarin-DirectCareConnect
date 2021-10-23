class ChatUi {
    constructor(options) {
        this.divButtonsWrapper = null;
        this.divChannelsWrapper = null;
        this.divChatWrapper = null;
        this.divMessageComposerWrapper = null;
        this.channels = [];
        this.container = null;
        this.options = {};
        //Channel events
        this.onChannelChange = new CustomEvent("channelChange");
        this.onNewChannel = new CustomEvent("newChannel");
        this.onNewMembers = new CustomEvent("newMembers");
        this.onMembersList = new CustomEvent("membersList");
        ////////////////////////////////////////

        this._init();
        this._constructSceleton();
        this._constructChannelButtons(this);
    }

    //// PRIVATE METHODS ////
    _init() {
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

    _constructSceleton() {
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

    _constructChannelButtons(parent) {

        var newChannelBtn = jQuery('<button/>', {
            id: "btnNewMsg",
            title: "Create New Group Chat",
            "class": "btn btn-outline-primary",
            "type": "button"
        });

        newChannelBtn.on("click", function () {
            $(parent).trigger("newChannel");
        });

        var inviteBtn = jQuery('<button/>', {
            id: "btnInvite",
            title: "Add members to this group",
            "class": "btn btn-outline-primary",
            "type": "button"
        });

        inviteBtn.on("click", function () {
            $(parent).trigger("newMembers");
        });

        var membersBtn = jQuery('<button/>', {
            id: "btnMembers",
            title: "List of this group members",
            "class": "btn btn-outline-primary",
            "type": "button"
        });

        membersBtn.on("click", function () {
            $(parent).trigger("membersList");
        });

        var divGroup = jQuery('<div/>', {
            "class": "btn-group",
            "role": "group"
        });

        var txtChannelsSearch = jQuery('<input/>', {
            "class": "form-control",
            "placeholder": "Search groups...",
            "id": "txtChannelsSearch"
        });

        newChannelBtn.empty().append("<i class=\"fas fa-plus\"></i>");
        inviteBtn.empty().append("<i class=\"fas fa-user-plus\">");
        membersBtn.empty().append("<i class=\"fas fa-users\">");
        inviteBtn.prop('disabled', true);
        membersBtn.prop('disabled', true);
        divGroup.append(newChannelBtn);
        divGroup.append(inviteBtn);
        divGroup.append(membersBtn);

        $(txtChannelsSearch).on("keyup", function () {
            var value = $(this).val().toLowerCase();
            $("#channelsWrapper button").filter(function () {
                $(this).toggle($(this).find("h5").text().toLowerCase().indexOf(value) > -1)
            });
        });
        parent.divButtonsWrapper.empty().append(divGroup).append(txtChannelsSearch);
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
            if (this.options.title_max_length && channel.name.length > this.options.title_max_length) {
                channel.name = channel.name.substring(0, this.options.title_max_length) + "...";
            }
            if (this.options.last_msg_max_length && channel.lastMessage && channel.lastMessage.message && channel.lastMessage.message.length > this.options.last_msg_max_length) {
                channel.lastMessage.message = channel.lastMessage.message.substring(0, this.options.last_msg_max_length) + "...";
            }
        }
        /////////////////////////////////

        if (channel.lastMessage)
            timeago.append(timeSince(channel.lastMessage.createdAt));

        if (channel.lastMessage && channel.lastMessage.message)
            lastmsg.append(channel.lastMessage.message.cleanup());

        //if last message sent is an image or file
        if (channel.lastMessage && channel.lastMessage.messageType == "file") {
            var file_image = channel.lastMessage.type.startsWith("image") ? "an image" : "a file";
            var lmText = channel.lastMessage._sender.nickname + " sent " + file_image;
            lastmsg.append(lmText);
        }

        title.append(channel.name);
        if (channel.isFrozen)
            title.append(" <i class=\"fas fa-snowflake\"></i>");
        headingWrapper.append(title);
        if (channel.unreadMessageCount > 0)
            $(badge).empty().append(channel.unreadMessageCount);
        headingWrapper.append(badge);
        channelbtn.append(headingWrapper);
        channelbtn.append(lastmsg);
        channelbtn.append(timeago);
        this.divChannelsWrapper.append(channelbtn);
        this.channels.push(channel);
        return channel;
    }

    channelUpdate(channel) {
        //Apply options
        if (this.options) {
            if (this.options.options_title_max_length && channel.name.length > this.options_title_max_length) {
                channel.name = channel.name.substring(0, this.options_title_max_length) + "...";
            }
            if (this.options.last_msg_max_length && channel.lastMessage && channel.lastMessage.message && channel.lastMessage.message.length > this.options.last_msg_max_length) {
                channel.lastMessage.message = channel.lastMessage.message.substring(0, this.options.last_msg_max_length) + "...";
            }
        }

        /////////////////////////////////
        var isNewChannel = true;
        $.each(this.divChannelsWrapper.find("button"), function (index, btn) {
            if (btn.id == channel.url) {
                isNewChannel = false;
                var badge = $(btn).find("small.unread-msgs")[0];
                var title = $(btn).find("h5")[0];
                var last_msg = $(btn).find("p.last-msg")[0];
                var time_ago = $(btn).find("small.time-ago")[0];
                if (channel.unreadMessageCount > 0)
                    $(badge).empty().append(channel.unreadMessageCount);
                else
                    $(badge).empty();

                if (channel.isFrozen)
                    $(title).empty().append("<i class=\"fas fa-snowflake\"></i> " + channel.name);
                else
                    $(title).empty().append(channel.name);

                if (channel.lastMessage && channel.lastMessage.message) {
                    $(last_msg).empty().append(channel.lastMessage.message.cleanup());
                    $(time_ago).empty().append(timeSince(channel.lastMessage.createdAt));
                }
            }
        });

        if (isNewChannel) {
            this.channelCreate(channel);
            this.channelMoveTop(channel.url);
        }
    }

    channelGet(url) {
        var array = this.channels.filter((item) => item.url === url);
        return array.length > 0 ? array[0] : null;
    }

    channelRemove(url) {
        var array = this.channels.filter((item) => item.url == url);
        if (array.length > 0) {

            this.channels = $.grep(this.channels, function (c) {
                return c.url != array[0].url;
            });
            //this.channels.splice(array[0]);
            $.each(this.divChannelsWrapper.find("button"), function (index, btn) {
                if (btn.id == url)
                    $(btn).remove();
            });
        }
    }

    channelSetActive(url) {
        $.each(this.divChannelsWrapper.find("button"), function (index, btn) {
            $(btn).removeClass("active");
            if (btn.id == url)
                $(btn).addClass("active");
        });

        $("#btnInvite").prop('disabled', false);
        $("#btnMembers").prop('disabled', false);
        $(app).trigger("channelChange", url);
    }

    channelMoveTop(url) {
        var target = this.divChannelsWrapper.find("#" + url);
        this.divChannelsWrapper.prepend(target);
    }

    channelMarkAsRead(url) {
        $.each(this.divChannelsWrapper.find("button"), function (index, btn) {
            if (btn.id == url) {
                var badge = $(btn).find("small.unread-msgs")[0];
                if (badge)
                    $(badge).empty();
            }
        });
    }
}