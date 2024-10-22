﻿class ChannelsContainer {
    constructor(container, options) {
        this.channels = [];
        this.container = container;
        this.onClick = new CustomEvent("channelChange");
        this.onNewMessage = new CustomEvent("newMessage");
        this.onInvite = new CustomEvent("invite");
        this.onMembersList = new CustomEvent("membersList");
        this.options = options;
        this.#init();
    }

    #init() {
        var parent = this;
        this.container.addClass("list-group");
        //add new message button
        var newMsgBtn = jQuery('<button/>', {
            id: "btnNewMsg",
            title: "Create new channel",
            "class": "btn btn-outline-primary",
            "type": "button"
        });

        newMsgBtn.on("click", function () {
            $(parent).trigger("newMessage");
        });

        var inviteBtn = jQuery('<button/>', {
            id: "btnInvite",
            title: "Invite members to this channel",
            "class": "btn btn-outline-primary",
            "type" : "button"
        });

        inviteBtn.on("click", function () {
            $(parent).trigger("invite");
        });

        var membersBtn = jQuery('<button/>', {
            id: "btnMembers",
            title: "List of this channel members",
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
            "placeholder": "Search channels...",
            "id": "txtChannelsSearch"
        });

        newMsgBtn.empty().append("<i class=\"fas fa-plus\"></i>");
        inviteBtn.empty().append("<i class=\"fas fa-user-plus\">");
        membersBtn.empty().append("<i class=\"fas fa-users\">");
        inviteBtn.prop('disabled', true);
        membersBtn.prop('disabled', true);
        divGroup.append(newMsgBtn);
        divGroup.append(inviteBtn);
        divGroup.append(membersBtn);
        var buttonsWrapper = $("#buttonsWrapper");

        $(txtChannelsSearch).on("keyup", function () {
            var value = $(this).val().toLowerCase();
            $("#channelsWrapper button").filter(function () {
                $(this).toggle($(this).find("h5").text().toLowerCase().indexOf(value) > -1)
            });
        });
        buttonsWrapper.empty().append(divGroup).append(txtChannelsSearch);
    }

    create(channel) {
        var parent = this;
        //Check if specified channel id already exists
        var checkId = this.getById(channel.url);
        if (checkId) {
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
        if (parent.options) {
            if (parent.options.titleMaxLength && channel.name.length > parent.options.titleMaxLength) {
                channel.name = channel.name.substring(0, parent.options.titleMaxLength) + "...";
            }
            if (parent.options.lastMsgMaxLength && channel.lastMessage && channel.lastMessage.message && channel.lastMessage.message.length > parent.options.lastMsgMaxLength) {
                channel.lastMessage.message = channel.lastMessage.message.substring(0, this.options.lastMsgMaxLength) + "...";
            }
        }
        /////////////////////////////////

        if (channel.lastMessage && channel.lastMessage.message) {
            lastmsg.append(channel.lastMessage.message.cleanup());
            timeago.append(timeSince(channel.lastMessage.createdAt));
        }

        if (channel.isFrozen)
            title.append("<i class=\"fas fa-snowflake\"></i> ");

        title.append(channel.name);
        headingWrapper.append(title);
        if (channel.unreadMessageCount > 0)
            $(badge).empty().append(channel.unreadMessageCount);
        headingWrapper.append(badge);
        channelbtn.append(headingWrapper);
        channelbtn.append(lastmsg);
        channelbtn.append(timeago);
        this.container.append(channelbtn);
        this.channels.push(channel);
        return channel;
    }

    setActive(id) {
        $.each(this.container.find("button"), function (index, btn) {
            $(btn).removeClass("active");
            if (btn.id == id)
                $(btn).addClass("active");
        });

        $("#btnInvite").prop('disabled', false);
        $("#btnMembers").prop('disabled', false);
        $(this).trigger("channelChange", id);
    }

    moveTop(id) {
        var target = this.container.find("#" + id);
        this.container.prepend(target);
    }

    markAsRead(id) {
        $.each(this.container.find("button"), function (index, btn) {
            if (btn.id == id) {
                var badge = $(btn).find("small.unread-msgs")[0];
                if(badge)
                    $(badge).empty();
            }
        });
    }

    update(channel) {

        //Apply options
        if (this.options) {
            if (this.options.titleMaxLength && channel.name.length > this.options.titleMaxLength) {
                channel.name = channel.name.substring(0, this.options.titleMaxLength) + "...";
            }
            if (this.options.lastMsgMaxLength && channel.lastMessage && channel.lastMessage.message && channel.lastMessage.message.length > this.options.lastMsgMaxLength) {
                channel.lastMessage.message = channel.lastMessage.message.substring(0, this.options.lastMsgMaxLength) + "...";
            }
        }

        /////////////////////////////////
        var isNewChannel = true;
        $.each(this.container.find("button"), function (index, btn) {
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
            this.create(channel);
            this.moveTop(channel.url);
        }
    }

    getById(url) {
        var array = this.channels.filter((item) => item.url === url);
        return array.length > 0 ? array[0] : null;
    }

    removeById(url) {
        var array = this.channels.filter((item) => item.url == url);
        if (array.length > 0) {

            this.channels = $.grep(this.channels, function (c) {
                return c.url != array[0].url;
            });
            //this.channels.splice(array[0]);
            $.each(this.container.find("button"), function (index, btn) {
                if (btn.id == url)
                    $(btn).remove();
            });
        }
    }
}

class Channel {
    constructor(options) {
        this.id = options.id;
        this.title = options.title;
        this.name = options.name;
        this.titleOriginal = options.title;
        this.lastmsg = options.lastmsg ? options.lastmsg.cleanup() : options.lastmsg;
        this.unreadmsgs = options.unreadmsgs;
        this.active = options.active;
        this.loadedmsgcount = options.loadedmsgcount;
        this.timeago = options.timeago;
        this.frozen = options.frozen;
    }
}

class ChatContainer {
    constructor(container, options) {
        this.container = container;
        this.options = options;
        this.#init();
        this.onChangeTitle = new CustomEvent("changeTitle");
        this.onFreezeChannel = new CustomEvent("freezeChannel");
        this.onUnFreezeChannel = new CustomEvent("unFreezeChannel");
        this.onRemoveChannel = new CustomEvent("removeChannel");
        this.onRemoveMember = new CustomEvent("removeMember");
        this.onLeaveChannel = new CustomEvent("leaveChannel");
        this.onEditMessage = new CustomEvent("editMessage");
        this.onDeleteMessage = new CustomEvent("deleteMessage");
        this.OnResize = new CustomEvent("resize");
        this.OnImageLoaded = new CustomEvent("imageLoaded");
    }

    #init() {
        if (this.options && this.options.title) {
            setTitle(this.options.title, this.options.members);
        }

        var titleWrapper = jQuery('<div/>', {
            "id": "currentChatTitle",
            "class": "hidden"
        });

        var welcomeWrapper = jQuery('<div/>', {
            "id": "welcomeWrapper"
        });

        welcomeWrapper.append("<h1>Welcome</h1>");
        welcomeWrapper.append("<h1><i class=\"fas fa-comments\"></i></h1>");
        welcomeWrapper.append("<p>Please select channel from the left, to start.</p>");

        var conversationWrapper = jQuery('<div/>', {
            "id": "currentConversationWrapper",
            "class": "messages-content"
        });

        this.container.append(titleWrapper);
        this.container.append(welcomeWrapper);
        this.container.append(conversationWrapper);
        conversationWrapper.hide();
    }

    #messageToText(message) {
        var parent = this;
        if (message.messageType == "file") {
            if (message.type.startsWith("image")) {
                var linkWrapper = jQuery('<a/>', {
                    "class": "msg-image",
                    "href": message.fileUrl,
                    "data-lightbox": message.name
                });
                var img = jQuery('<img/>', {
                    "src": message.fileUrl
                });

                linkWrapper.append(img);
                $(img).on("load", function () {
                    $(parent).trigger("imageLoaded");
                });
                return linkWrapper;
            }
            else {
                var linkWrapper = jQuery('<a/>', {
                    "class": "msg-file",
                    "target": "_blank",
                    "title": "Download file",
                    "href": message.fileUrl
                });
                linkWrapper.append(message.name);
                return linkWrapper;
            }
           
            return fileWrapper;
        }
        if (message.messageType == "user") {
            return message.message.cleanup();
        }

        return "[Unsupported message]";
    }

    notify(message) {
        var chatNotifier = $("#chatNotifier").length > 0 ? $("#chatNotifier") : jQuery('<div/>', {
            "id": "chatNotifier",
            "class": "media"
        });
        $("#currentChatTitle").append(chatNotifier);
        $("#chatNotifier").empty().append("<div class=\"admin-message\">" + message + "</div>");

        setTimeout(function () {
            $("#chatNotifier").remove()
        }, 2000);
    }

    setTitle(channelInfo, currentUserId) {
        var parent = this;
        var titleWrapper = this.container.find('#currentChatTitle')[0];

        if (titleWrapper) {
            $(titleWrapper).removeClass("hidden");

            var navBar = jQuery('<nav/>', {
                "class": "navbar navbar-light bg-light"
            });

            var navBarBrand = jQuery('<a/>', {
                "class": "navbar-brand",
                "html": "<i class=\"fas fa-users\"></i> " + channelInfo.name
            });

            var dropDownWrapper = jQuery('<div/>', {
                "class": "dropdown"
            });

            var navBarSettingBtn = jQuery('<button/>', {
                "class": "btn btn-outline-primary",
                "data-toggle": "dropdown",
                "aria-expanded": "false",
                "type": "button",
                "html": "<i class=\"fas fa-cog\"></i>",
                "id": "navBarSettingBtn"
            });

            var navBarSettingBtnDropdown = jQuery('<div/>', {
                "class": "dropdown-menu dropdown-menu-right",
                "aria-labelledby": "navBarSettingBtn"
            });

            //Change title button
            var navBarSettingChangeTitleButton = jQuery('<button/>', {
                "class": "dropdown-item",
                "type": "button",
                "html": "<i class=\"fas fa-pencil-alt\"></i> Change channel name"
            });

            navBarSettingChangeTitleButton.on("click", function () {
                $(parent).trigger("changeTitle");
            });

            //Leave channel button
            var navBarSettingLeaeveChannelButton = jQuery('<button/>', {
                "class": "dropdown-item",
                "type": "button",
                "html": "<i class=\"fas fa-sign-out-alt\"></i> Leave channel"
            });

            navBarSettingLeaeveChannelButton.on("click", function () {
                $(parent).trigger("leaveChannel");
            });

            //Freeze channel button
            var navBarSettingFreezeChannelButton = jQuery('<button/>', {
                "class": "dropdown-item",
                "type": "button",
                "html": channelInfo.isFrozen ? "<i class=\"fas fa-snowflake\"></i> Unfreeze channel" : "<i class=\"fas fa-snowflake\"></i> Freeze channel"
            });

            navBarSettingFreezeChannelButton.on("click", function () {
                $(parent).trigger("freezeChannel");
            });

            if (channelInfo.inviter.userId == currentUserId) {
                navBarSettingBtnDropdown.append(navBarSettingFreezeChannelButton);
                navBarSettingBtnDropdown.append(navBarSettingChangeTitleButton);
                navBarSettingBtnDropdown.append("<div class=\"dropdown-divider\"></div>");
                $.each(channelInfo.members, function (index, member) {
                    if (currentUserId != member.userId) {
                        var nickname = member.nickname ? member.nickname : "<i>[Unknown user]</i>";
                        var navBarSettingBtnDropdownItem = jQuery('<button/>', {
                            "class": "dropdown-item",
                            "type": "button",
                            "html": "<i class=\"fas fa-trash\"></i> Remove " + nickname
                        });

                        navBarSettingBtnDropdownItem.on("click", function () {
                            $(parent).trigger("removeMember", member);
                        });
                        navBarSettingBtnDropdown.append(navBarSettingBtnDropdownItem);
                    }
                });

                if (channelInfo.members.length < 2) {
                    navBarSettingBtnDropdown.append("<a class=\"dropdown-item disabled\" href=\"#\">No members in this channel</a>");
                }
            }
            else {
                var navBarSettingBtnDropdownHeader = jQuery('<h2/>', {
                    "class": "dropdown-item disabled",
                    "html": "Channel members"
                });
                navBarSettingBtnDropdown.append(navBarSettingLeaeveChannelButton);
                navBarSettingBtnDropdown.append(navBarSettingBtnDropdownHeader);

                $.each(channelInfo.members, function (index, member) {
                    var nickname = member.nickname ? member.nickname : "<i>[Unknown user]</i>";
                    var navBarSettingBtnDropdownItem = jQuery('<button/>', {
                        "class": "dropdown-item disabled",
                        "type": "button",
                        "html": "<i class=\"fas fa-user\"></i> " + nickname
                    });
                    navBarSettingBtnDropdown.append(navBarSettingBtnDropdownItem);
                });
            }

            dropDownWrapper.append(navBarSettingBtn);
            dropDownWrapper.append(navBarSettingBtnDropdown);

            navBar.append(navBarBrand);
            navBar.append(dropDownWrapper);
            $(titleWrapper).empty().append(navBar);
        }
    }

    clearChat() {
        var conversationWrapper = this.container.find('#currentConversationWrapper')[0];
        if (conversationWrapper) {
            $(conversationWrapper).empty();
        }
    }

    appendMessage(message, addtime, preload) {
        var parent = this;
        var conversationWrapper = this.container.find('#currentConversationWrapper')[0];

        var mediaWrapper = jQuery('<div/>', {
            "id": message.id,
            "class": "media"
        });
        var iconWrapper = jQuery('<div/>', {
            "class": message.isMyMessage ? "media-right friend-box" : "media-left friend-box"
        });

        if (message.customType != "ADMIN_MESSAGE")
            iconWrapper.append("<i class=\"fas fa-user fa-3x\"></i> ");

        var mediaBodyWrapper = jQuery('<div/>', {
            "class": message.isMyMessage ? "media-body text-right" : "media-body"
        });

        var messageText = jQuery('<div/>', {
            "class": message.isMyMessage ? "msg-reply bg-primary" : "msg-send"
        });

        if (message.customType == "ADMIN_MESSAGE") {
            iconWrapper.attr("class", "");
            mediaBodyWrapper.attr("class", "admin-message");
            messageText.attr("class", "");
        }
        messageText.append(this.#messageToText(message));

        if (!message.isMyMessage && message.customType != "ADMIN_MESSAGE")
            mediaBodyWrapper.append("<div class=\"sender-name\">" + message.sender + "</div>");

        mediaBodyWrapper.append(messageText);

        if (addtime)
            mediaBodyWrapper.append("<span class=\"time-ago\">" + timeSince(message.date) + "</span>");

        if (message.isMyMessage) {
            //create message actions menu for my messages
            var messageActionsMenu = jQuery('<div/>', {
                "class": "message-actions-menu"
            });

            var messageActionEdit = jQuery('<a/>', {
                "title": "Edit message",
                "href": "#",
                "html": "<i class=\"fas fa-edit\"></i>"
            });

            messageActionEdit.on("click", function () {
                $(parent).trigger("editMessage", message.id);
            });

            var messageActionDelete = jQuery('<a/>', {
                "title": "Delete message",
                "href": "#",
                "html": "<i class=\"fas fa-trash\"></i>"
            });

            messageActionDelete.on("click", function () {
                $(parent).trigger("deleteMessage", message.id);
            });

            messageActionsMenu.append(messageActionEdit);
            messageActionsMenu.append(messageActionDelete);

            messageText.append(messageActionsMenu);
            mediaWrapper.append(mediaBodyWrapper);
            mediaWrapper.append(iconWrapper);
        }
        else {
            mediaWrapper.append(iconWrapper);
            mediaWrapper.append(mediaBodyWrapper);
        }

        if (preload)
            $(conversationWrapper).prepend(mediaWrapper);
        else {
            $(conversationWrapper).append(mediaWrapper);
            $(this).trigger("resize");
        }
    }

    loadMessages(messages, preload) {
        var parent = this;
        var conversationWrapper = this.container.find('#currentConversationWrapper')[0];
        $(conversationWrapper).show();
        var welcomeWrapper = this.container.find('#welcomeWrapper')[0];
        if (welcomeWrapper)
            $(welcomeWrapper).remove();
        if (!preload) {
            $(conversationWrapper).empty();
        }

        messages.forEach(function (message) {
            parent.appendMessage(message, message.customType != "ADMIN_MESSAGE", preload);
        });
    }
}

class MessageComposerContainer {
    constructor(container, options) {
        this.container = container;
        this.options = options;
        this.#init();
        this.onMessageSend = new CustomEvent("messageSend");
        this.onTypingStarted = new CustomEvent("typingStarted");
        this.onTypingEnded = new CustomEvent("typingEnded");
        this.onFileUploading = new CustomEvent("fileUploading");
    }

    #init() {
        var parent = this;
        var rows = this.options.rows ? this.options.rows : 3;
        var messageBox = jQuery('<textarea/>', {
            "rows": rows,
            "placeholder": "Type your message here...",
            "class": "message-composer"
        });

        var fileInput = jQuery('<input/>', {
            "type": "file",
            "id": "composer-file-input",
            "class": "message-composer-file-input"
        });

        var btnUploadIcon = jQuery('<i/>', {
            "class": "fas fa-paperclip fa-lg"
        });

        var btnUpload = jQuery('<a/>', {
            "class": "message-composer-upload-btn",
            "title": "Send an image or file",
            "id": "btnUpload"

        });

        var btnUploadProgress = jQuery('<div/>', {
            "class": "progress",
            "style": "height: 15px; display:none;",
            "id": "btnUploadProgress"
        });

        var btnUploadProgressBar = jQuery('<div/>', {
            "class": "progress-bar",
            "role": "progressbar",
            "style": "width: 0%;",
            "aria-valuenow": "0",
            "aria-valuemin": "0",
            "aria-valuemax": "100",
            "id": "btnUploadProgressBar"
        });

        btnUpload.on("click", function () {
            $(fileInput).trigger("click");
        });

        fileInput.on("change", function () {
            var file = $(this)[0].files[0]
            $(parent).trigger("fileUploading", file);
        });


        $(messageBox).on('keypress', function (e) {
            if (e.which === 13 && !e.shiftKey) {
                $(parent).trigger("messageSend", $(messageBox).val());
                $(parent).trigger("typingEnded");
                $(messageBox).val("");
                e.preventDefault();
            }
        });
        $(messageBox).on('keyup', function (e) {
            var currText = $(messageBox).val();
            if (currText.length > 0)
                $(parent).trigger("typingStarted");
            else
                $(parent).trigger("typingEnded");

        });

        btnUploadProgress.append(btnUploadProgressBar);
        btnUpload.append(btnUploadIcon);

        this.container.append(messageBox);
        this.container.append(btnUpload);
        this.container.append(btnUploadProgress);
    }

    hide() {
        $(this.container).hide();
    }

    show() {
        $(this.container).show();
    }

    fileUploadUpdate(percent) {
        var btnUpload = this.container.find('#btnUpload')[0];
        $(btnUpload).hide();

        var btnUploadProgress = this.container.find('#btnUploadProgress')[0];
        var btnUploadProgressBar = this.container.find('#btnUploadProgressBar')[0];
        $(btnUploadProgress).show();
        $(btnUploadProgressBar).attr("style", "width:" + percent + "%");
        $(btnUploadProgressBar).attr("aria-valuenow", percent);
        $(btnUploadProgressBar).empty().append(percent + "%");
    }

    fileUploadComplete() {
        var btnUploadProgress = this.container.find('#btnUploadProgress')[0];
        $(btnUploadProgressBar).attr("style", "width:0%");
        $(btnUploadProgressBar).attr("aria-valuenow", 0);
        $(btnUploadProgressBar).empty();
        $(btnUploadProgress).hide();
        var btnUpload = this.container.find('#btnUpload')[0];
        $(btnUpload).show();
    }

    startTyping(members) {
        var prevTypingIndicator = this.container.find('#typingIndicator')[0];
        if (prevTypingIndicator)
            prevTypingIndicator.remove();

        var typingIndicator = jQuery('<div/>', {
            "class": "message-type-status",
            "id": "typingIndicator"
        });

        var typingNote = "";
        members.forEach(function (member) {
            typingNote += member.nickname + ", ";
        });
        if (typingNote.length > 2)
            typingNote = typingNote.slice(0, -2);
        typingNote += " typing...";

        $(typingIndicator).append("<small><em>" + typingNote + "</em></small>");
        this.container.append(typingIndicator);
    }

    endTyping() {
        var typingIndicator = this.container.find('#typingIndicator')[0];
        if (typingIndicator)
            typingIndicator.remove();
    }

    #parseMessage(message) {
        var result = message;
        result = result.linkify();
        return result.replace(/(?:\r\n|\r|\n)/g, '<br>');
    }
}

class ModalComposer {
    constructor(options) {
        this.modal = null;
        this.#init(options);
        this.onSubmit = new CustomEvent("submit");
    }

    #init(options) {
        var parent = this;
        var divModal = jQuery('<div/>', {
            "class": "modal",
            "tabindex": "-1",
            "role": "dialog"
        });

        var divModalDialog = jQuery('<div/>', {
            "class": "modal-dialog",
            "role": "document"
        });

        var divModalContent = jQuery('<div/>', {
            "class": "modal-content"
        });

        var divModalHeader = jQuery('<div/>', {
            "class": "modal-header"
        });

        var modalTitle = jQuery('<h5/>', {
            "class": "modal-title"
        });

        var btnCloseIcon = jQuery('<button/>', {
            "class": "close",
            "type": "button",
            "data-dismiss": "modal",
            "aria-label": options.closeButtonText
        });

        var divModalFooter = jQuery('<div/>', {
            "class": "modal-footer"
        });

        var divModalbody = jQuery('<div/>', {
            "class": "modal-body"
        });

        var btnClose = jQuery('<button/>', {
            "class": "btn btn-secondary",
            "type": "button",
            "data-dismiss": "modal"
        });

        var btnSubmit = jQuery('<button/>', {
            "class": "btn btn-primary",
            "type": "button"
        });

        btnSubmit.on("click", function () {
            $(parent).trigger("submit");
        });

        var btnCloseIconInner = jQuery('<span/>', {
            "aria-hidden": "true",
            "type": "button"
        });

        btnCloseIconInner.append("&times;");
        btnCloseIcon.append(btnCloseIconInner);

        btnClose.append(options.closeButtonText);
        btnSubmit.append(options.submitButtonText);
        divModalbody.append(options.bodyText);
        var bodyElements = options.bodyElements;
        $.each(bodyElements, function (index, elem) {
            divModalbody.append(elem);
            divModalbody.append("<hr />");
        });


        modalTitle.append(options.titleText);

        if (options.closeButtonText)
            divModalFooter.append(btnClose);
        if (options.submitButtonText)
            divModalFooter.append(btnSubmit);
        divModalHeader.append(modalTitle);
        divModalHeader.append(btnCloseIcon);
        divModalContent.append(divModalHeader);
        divModalContent.append(divModalbody);

        divModalContent.append(divModalFooter);
        divModalDialog.append(divModalContent);
        divModal.append(divModalDialog);
        $("body").append(divModal);
        this.modal = divModal;
    }

    show() {
        if (this.modal) {
            $(this.modal).modal("show");
        }
    }

    hide() {
        if (this.modal) {
            $(this.modal).modal("hide");
        }
    }

    destroy() {
        this.modal.remove();
    }
}
