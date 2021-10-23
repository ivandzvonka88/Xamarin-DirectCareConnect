$(document).ready(function () {
    chatApp.init();
});

var chatApp = {
    init: function init() {
        this.appId = "F742C54C-036E-4DD0-B74D-E59E38ED7B30";
        this.sb = null;
        this.currentUser = null;
        this.groupChannelListQuery = null;
        this.userListQuery = null;
        this.currentChannelInfo = null;
        this.userId = $("#sendbirdId").val();
        this.channelHandler = null;
        this.connectionHandler = null;
        this.chatUi = null;
        this.messagesQuery = null;
        this.usersQuery = null;
        this.chatScroll = null;
        this.channelsScroll = null;
        this.currScrollHeight = 0;
        this.preloadingMessages = false;
        this.adminMessageType = "ADMIN_MESSAGE";
        this.usersList = [];
        this.sb = new SendBird({
            appId: chatApp.appId,
        });
        this.createChannelParams = null;
        this.initEventHandlers();

        sb.connect(chatApp.userId, function (user, error) {
            if (error) {
                toastr.warning("<strong>Error</strong><br/>Cannot connect to SendBird server");
            } else {
                chatApp.initUi(user);
            }
        });
    },
    initUi: function initUi(user) {
        chatApp.chatUi = new ChatUi(); 
        chatApp.currentUser = user;
        chatApp.groupChannelListQuery = sb.GroupChannel.createMyGroupChannelListQuery();
        chatApp.userListQuery = sb.createUserListQuery();
        chatApp.groupChannelListQuery.limit = 100;
        chatApp.groupChannelListQuery.includeEmpty = true;
        chatApp.userListQuery.limit = 100;
        chatApp.initChannelsList();
        //chatApp.initChatMessages();
        //chatApp.initMessageComposer();
        //chatApp.createChannelParams = new sb.GroupChannelParams();
        //chatApp.createChannelParams.operatorUserIds = [user.userId];
        //chatApp.createChannelParams.isDistinct = false;
    },
    initEventHandlers: function initEventHandlers() {
        this.channelHandler = new sb.ChannelHandler();
        this.channelHandler.onMessageReceived = chatApp.messageReceived;
        this.channelHandler.onTypingStatusUpdated = chatApp.typingStatusUpdated;
        this.channelHandler.onChannelChanged = chatApp.channelChanged;
        this.channelHandler.onChannelFrozen = chatApp.channelFrozen;
        this.channelHandler.onChannelUnfrozen = chatApp.channelUnFrozen;
        this.channelHandler.onUserBanned = chatApp.userBanned;
        this.channelHandler.onMessageDeleted = chatApp.messageDeleted;
        this.connectionHandler = new sb.ConnectionHandler();

        this.connectionHandler.onReconnectStarted = function (id) {
            toastr.warning("Reconnecting...");
        };
        this.connectionHandler.onReconnectSucceeded = function (id) {
            toastr.success("Connection restored");
        };
        this.connectionHandler.onReconnectFailed = function (id) {
            toastr.warning("Reconnect failed");
        };
        sb.addChannelHandler("channel_handler", this.channelHandler);
        sb.addConnectionHandler("connection_handler", this.connectionHandler);
    },
    initChannelsList: function initChannelsList() {
        chatApp.channelsScroll = chatApp.chatUi.divChannelsWrapper.niceScroll();

        $(chatApp.chatUi).on("channelChange", function (e, url) {
            chatApp.preloadingMessages = false;
            if (chatApp.currentChannelInfo && url == chatApp.currentChannelInfo.url) {
                return;
            }
            //chatApp.messageComposerUi.show();
            //chatApp.chatUi.clearChat();
            ////retrieve channel messages
            //sb.GroupChannel.getChannel(url, function (channelInfo, error) {
            //    if (error) {
            //        toastr.warning("<strong>Error</strong><br/>Cannot retrieve channel info");
            //        return;
            //    }
            //    //TODO: handle frozen channels UI
            //    if (channelInfo.isFrozen && channelInfo.myRole != "operator") {
            //        chatApp.messageComposerUi.hide();
            //    }
            //    else
            //        chatApp.messageComposerUi.show();
            //    chatApp.chatUi.setTitle(channelInfo, chatApp.currentUser.userId);
            //    chatApp.currentChannelInfo = channelInfo;
            //    chatApp.currentChannelInfo.markAsRead();
            //    chatApp.channelsUi.markAsRead(url);
            //    chatApp.loadMessages();
            //    chatApp.channelsUi.update(channelInfo);
            //});
        });



        chatApp.groupChannelListQuery.next(function (channels, error) {
            if (error) {
                toastr.warning("<strong>Error</strong><br/>Cannot retrieve channels list");
                return;
            }
            channels.forEach(function (channel) {
                chatApp.chatUi.channelCreate(channel);
            });
        });
    },

};