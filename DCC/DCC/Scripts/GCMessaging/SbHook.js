$(document).ready(function () {
    //init if user is not on chat page
    if (!window.location.href.includes("GCMessaging"))
        hook.init();
});

var hook = {
    init: function init() {
        this.unreadMessages = 0;
        this.appId = "F742C54C-036E-4DD0-B74D-E59E38ED7B30";
        this.userId = $("#sendBirdUserId").val();
        this.channelHandler = null;
        this.groupChannelListQuery = null;
        this.sb = new SendBird({
            appId: hook.appId,
        });
        if (hook.userId) {
            sb.connect(hook.userId, function (user, error) {
                if (error) {
                    toastr.warning("<strong>Error</strong><br/>Cannot connect to SendBird server");
                }
            });
            this.initEventHandlers();
            this.retrieveChannelsTotalUnreadMessages();
        }
    },
    initEventHandlers: function initEventHandlers() {
        hook.channelHandler = new sb.ChannelHandler();
        hook.channelHandler.onMessageReceived = hook.messageReceived;
        hook.sb.addChannelHandler("channel_handler", this.channelHandler);
    },
    retrieveChannelsTotalUnreadMessages: function retrieveChannelsTotalUnreadMessages() {
        hook.groupChannelListQuery = sb.GroupChannel.createMyGroupChannelListQuery();
        hook.groupChannelListQuery.limit = 100;
        hook.groupChannelListQuery.includeEmpty = true;
        hook.unreadMessages = 0;
        hook.groupChannelListQuery.next(function (channels, error) {
            if (error) {
                return;
            }
            channels.forEach(function (channel) {
                hook.unreadMessages += channel.unreadMessageCount;
                hook.updateUnreadMessages();
            });
        });
    },
    messageReceived: function messageReceived(channel, message) {
        hook.unreadMessages++;
        hook.updateUnreadMessages();
    },
    updateUnreadMessages: function updateUnreadMessages() {
        if ($("#unreadMessagesIndicator").length > 0)
            $("#unreadMessagesIndicator").remove();
        var unrIndicator = jQuery('<span/>', {
            "id": "unreadMessagesIndicator",
            "class": "badge bg-danger",
            "html": hook.unreadMessages,
            "style": "font-size:10px;"
        });
        if (hook.unreadMessages > 0) {
            var link = $(".header .menu .navbar .navbar-nav .message .nav-link")[0];
            if (link) {
                $(link).append(unrIndicator);
            }
        }
        else {
            unrIndicator.remove();
        }
        console.log("Hook -> there are total " + hook.unreadMessages + " unread messages");
    }
};