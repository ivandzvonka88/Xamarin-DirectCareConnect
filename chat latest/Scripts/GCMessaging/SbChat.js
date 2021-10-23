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
        this.userId = $("#userPrId").val();
        this.channelHandler = null;
        this.connectionHandler = null;
        this.chatUi = null;
        this.channelsUi = null;
        this.messageComposerUi = null;
        this.messagesQuery = null;
        this.usersQuery = null;
        this.chatScroll = null;
        this.channelsScroll = null;
        this.currScrollHeight = 0;
        this.preloadingMessages = false;
        this.sb = new SendBird({
            appId: chatApp.appId,
        });
        this.createChannelParams = null;
        
        sb.connect(chatApp.userId, function (user, error) {
            if (error) {
                toastr.warning("<strong>Error</strong><br/>Cannot connect to SendBird server");
            } else {
                chatApp.initUi();
            }
        });
    },
    initUi: function initUi() {
        chatApp.chatUi = new ChatUi(); 
    }

};