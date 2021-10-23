$(document).ready(function () {
    chatApp.init();
});

var chatApp = {
    init: function init() {
        this.appId = "F742C54C-036E-4DD0-B74D-E59E38ED7B30";
        this.userId = $("#sendbirdId").val();
        this.nickname = $("#staffName").val();
        this.currentUser = null;
        this.groupChannelListQuery = null;
        this.userListQuery = null;
        this.currentChannelInfo = null;
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
        this.adminMessageType = "ADMIN_MESSAGE";
        this.usersList = [];
        this.usersLoaded = false;
        this.sb = new SendBird({
            appId: chatApp.appId,
        });
        this.createChannelParams = null;
        this.initEventHandlers();
        
        sb.connect(chatApp.userId, function (user, error) {
            if (error) {
                toastr.warning("<strong>Error</strong><br/>Cannot connect to SendBird server");
            } else {
                chatApp.initChat(user);
            }
        });

        this.createUsersQuery();
        this.loadUsers();
        $("body").tooltip({
            selector: '[data-toggle="tooltip"]'
        });
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
        this.channelHandler.onMessageUpdated = chatApp.messageUpdated;
        this.channelHandler.onReadReceiptUpdated = chatApp.readReceiptUpdated;
        this.connectionHandler = new sb.ConnectionHandler();
        this.connectionHandler.onReconnectStarted = function (id) {
            toastr.warning("Reconnecting...");
            chatApp.messageComposerUi.disable();
        };
        this.connectionHandler.onReconnectSucceeded = function (id) {
            toastr.success("Connection restored");
            chatApp.messageComposerUi.enable();
            if (chatApp.currentChannelInfo) {
                chatApp.currentChannelInfo.refresh(function (response, error) {
                    if (error) {
                        toastr.error(error.message);
                        return;
                    }
                });
            }
        };
        this.connectionHandler.onReconnectFailed = function (id) {
            toastr.warning("Reconnect failed");
        };
        sb.addChannelHandler("channel_handler", this.channelHandler);
        sb.addConnectionHandler("connection_handler", this.connectionHandler);
    },
    initChat: function initChat(user) {
        chatApp.currentUser = user;
        chatApp.groupChannelListQuery = sb.GroupChannel.createMyGroupChannelListQuery();
        chatApp.userListQuery = sb.createUserListQuery();
        chatApp.groupChannelListQuery.limit = 100;
        chatApp.groupChannelListQuery.includeEmpty = true;
        chatApp.userListQuery.limit = 100;
        chatApp.initChannelsList();
        chatApp.initChatMessages();
        chatApp.initMessageComposer();
        chatApp.createChannelParams = new sb.GroupChannelParams();
        chatApp.createChannelParams.operatorUserIds = [user.userId];
        chatApp.createChannelParams.isDistinct = false;
    },
    initChannelsList: function initChannelsList() {
        chatApp.channelsUi = new ChannelsContainer($("#channelsWrapper"), {
            titleMaxLength: 30,
            lastMsgMaxLength: 40
        });

        chatApp.channelsScroll = $("#channelsWrapper").niceScroll();

        $(chatApp.channelsUi).on("channelChange", function (e, url) {
            chatApp.preloadingMessages = false;
            if (chatApp.currentChannelInfo && url == chatApp.currentChannelInfo.url) {
                return;
            }
            chatApp.messageComposerUi.show();
            chatApp.messageComposerUi.cancelReply();
            chatApp.chatUi.clearChat();
            //retrieve channel messages
            sb.GroupChannel.getChannel(url, function (channelInfo, error) {
                if (error) {
                    toastr.warning("<strong>Error</strong><br/>Cannot retrieve channel info");
                    return;
                }
                //TODO: handle frozen channels UI
                if (channelInfo.isFrozen && channelInfo.myRole != "operator") {
                    chatApp.messageComposerUi.hide();
                }
                else
                    chatApp.messageComposerUi.show();
                chatApp.chatUi.setTitle(channelInfo, chatApp.currentUser.userId);
                chatApp.currentChannelInfo = channelInfo;
                chatApp.currentChannelInfo.markAsRead();
                chatApp.channelsUi.markAsRead(url);
                chatApp.loadMessages();
                chatApp.channelsUi.update(channelInfo);
            });
        });
        $(chatApp.channelsUi).on("newMessage", function () {
            var selUsers = chatApp.buildUsersSelector();
            var channelNameInput = chatApp.buildChannelNameTextBox();
            var bodyElements = [];
            bodyElements.push(channelNameInput);
            bodyElements.push(selUsers);
            var modal = new ModalComposer({ closeButtonText: "Close", submitButtonText: "Start conversation", bodyElements: bodyElements, bodyText: "Specify group chat name and select members", titleText: "New group chat" });
            $(selUsers).niceScroll();

            modal.show();
            $(modal).on("submit", function () {
                if (!chatApp.usersLoaded)
                    return;
                var channelName = $(bodyElements[0]).val();
                var userIds = chatApp.extractSelectedUsersFromUsersSelector($(bodyElements[1])[0]);
                if (!channelName) {
                    toastr.warning("Please specify group chat name");
                    return;
                }
                if (userIds.length == 0) {
                    toastr.warning("Please select at least one member");
                    return;
                }
                var users = chatApp.usersList;
                var selectedUsers = [];
                $.each(userIds, function (index, userId) {
                    var user = users.find(o => o.userId === userId)
                    if(user)
                        selectedUsers.push(user);
                });
                ///// creating channel code here
                chatApp.createChannelParams = new sb.GroupChannelParams();
                chatApp.createChannelParams.operatorUserIds = [chatApp.currentUser.userId];
                chatApp.createChannelParams.isDistinct = false;
                chatApp.createChannelParams.addUserIds(userIds);
                chatApp.createChannelParams.name = channelName;
                chatApp.sb.GroupChannel.createChannel(chatApp.createChannelParams, function (channel, error) {
                    if (error) {
                        toastr.error(error.message);
                        return;
                    }
                    var targetUrl = channel.url;
                    chatApp.channelsUi.create(channel);
                    chatApp.channelsUi.setActive(targetUrl);
                    chatApp.channelsUi.moveTop(targetUrl);
                });
                ///////////////////////////
                //hide modal and destroy
                modal.hide();
                modal.destroy();
            });
        });
        $(chatApp.channelsUi).on("invite", function () {
            var selUsers = chatApp.buildUsersSelector(true);
            var bodyElements = [];
            bodyElements.push(selUsers);
            var modal = new ModalComposer({ closeButtonText: "Close", submitButtonText: "Invite", bodyElements: bodyElements, bodyText: "Select members to add to this group", titleText: "Invite members" });
            $(selUsers).niceScroll();
            modal.show();

            $(modal).on("submit", function () {
                if (!chatApp.usersLoaded)
                    return;
                var userIds = chatApp.extractSelectedUsersFromUsersSelector($(bodyElements[0])[0]);
                if (userIds.length == 0) {
                    toastr.warning("Please select at least one member");
                    return;
                }
                var users = chatApp.usersList;
                var selectedUsers = [];
                var selectedNicknames = [];
                $.each(userIds, function (index, userId) {
                    var user = users.find(o => o.userId === userId)
                    if (user) {
                        selectedUsers.push(user);
                        selectedNicknames.push(user.nickname);
                    }
                });
                //invite with user ids
                chatApp.currentChannelInfo.inviteWithUserIds(userIds, function (response, error) {
                    if (error) {
                        toastr.error(error.message);
                        return;
                    }
                    chatApp.chatUi.setTitle(chatApp.currentChannelInfo, chatApp.currentUser.userId);
                    chatApp.sendCustomMessage(chatApp.currentUser.nickname + " added " + selectedNicknames.toString() + " to the group", chatApp.adminMessageType);

                    modal.hide();
                });

            });
        });
        $(chatApp.channelsUi).on("membersList", function () {
            var usersList = chatApp.buildUsersList();
            var bodyElements = [];
            bodyElements.push(usersList);
            var modal = new ModalComposer({ closeButtonText: "Close", submitButtonText: "", bodyElements: bodyElements, bodyText: "", titleText: "Mebers List" });
            $(usersList).niceScroll();
            modal.show();
        });

        chatApp.groupChannelListQuery.next(function (channels, error) {
            if (error) {
                toastr.warning("<strong>Error</strong><br/>Cannot retrieve channels list");
                return;
            }
            channels.forEach(function (channel) {
                chatApp.channelsUi.create(channel);
            });
        });
    },
    initMessageComposer: function initMessageComposer() {
        chatApp.messageComposerUi = new MessageComposerContainer($("#messageComposerWrapper"), { rows: 3 });
        chatApp.messageComposerUi.hide();
        $(chatApp.messageComposerUi).on("messageSend", function (e, message) {
            var replyMsgId = chatApp.messageComposerUi.getReplyMessageId();
            var replyMsgText = chatApp.messageComposerUi.getReplyMessageText();
            chatApp.messageComposerUi.cancelReply();
            if (chatApp.currentChannelInfo) {
                const params = new chatApp.sb.UserMessageParams();
                params.message = message;
                if (replyMsgId) {
                    params.parentMessageId = replyMsgId;
                    params.data = JSON.stringify({ parentMessageId: replyMsgId, parentMessageText: replyMsgText });
                }
                chatApp.currentChannelInfo.sendUserMessage(params, chatApp.sendMessageHandler);
                chatApp.channelsUi.moveTop(chatApp.currentChannelInfo.url);
            }
            else {
                toastr.warning("<strong>Channel not selected</strong><br/>Message not sent");
            }
        });

        $(chatApp.messageComposerUi).on("typingStarted", function (e) {
            if (chatApp.currentChannelInfo)
                chatApp.currentChannelInfo.startTyping();
        });

        $(chatApp.messageComposerUi).on("typingEnded", function (e) {
            if (chatApp.currentChannelInfo)
                chatApp.currentChannelInfo.endTyping();
        });

        $(chatApp.messageComposerUi).on("fileUploading", function (e, file) {
            if (chatApp.currentChannelInfo) {
                var replyMsgId = chatApp.messageComposerUi.getReplyMessageId();
                var replyMsgText = chatApp.messageComposerUi.getReplyMessageText();
                const params = new chatApp.sb.FileMessageParams();
                if (replyMsgId) {
                    params.parentMessageId = replyMsgId;
                    params.data = JSON.stringify({ parentMessageId: replyMsgId, parentMessageText: replyMsgText });
                }
                params.file = file;
                chatApp.currentChannelInfo.sendFileMessage(params, chatApp.sendFileHandler, chatApp.sendMessageHandler);
            }
        });
    },
    initChatMessages: function initChatMessages() {
        chatApp.chatUi = new ChatContainer($("#chatWrapper"), { currentUser: chatApp.currentUser});
        chatApp.chatScroll = $("#currentConversationWrapper").niceScroll();

        //Change title clicked event
        $(chatApp.chatUi).on("changeTitle", function () {
            var channelNameInput = chatApp.buildChannelNameTextBox(chatApp.currentChannelInfo.name);
            var bodyElements = [];
            bodyElements.push(channelNameInput);
            var modal = new ModalComposer({ closeButtonText: "Close", submitButtonText: "Save", bodyElements: bodyElements, bodyText: "Type new name for the group", titleText: "Change group name" });
            modal.show();
            $(modal).on("submit", function () {
                var channelName = $(bodyElements[0]).val();
                //rename channel
                chatApp.currentChannelInfo.updateChannel(channelName, null, null, function (channel, error) {
                    if (error) {
                        toastr.error(error.message);
                        return;
                    }
                    chatApp.chatUi.setTitle(channel, chatApp.currentUser.userId);
                    chatApp.sendCustomMessage("Channel name changed to " + channelName, chatApp.adminMessageType);
                });
                modal.hide();
                modal.destroy();
            });
        });

        //Freeze channel clicked event
        $(chatApp.chatUi).on("freezeChannel", function () {

            var submitButtonText = chatApp.currentChannelInfo.isFrozen ? "Yes, unfreeze group" : "Yes, freeze group";
            var bodyText = chatApp.currentChannelInfo.isFrozen ? "Are you sure you want to unfreeze this group? Users will be able to post messages here again." : "Are you sure you want to freeze this group? Users will not be able to post messages here anymore, until you unfreeze it.";
            var modal = new ModalComposer({
                closeButtonText: "No",
                submitButtonText: submitButtonText,
                bodyElements: null,
                bodyText: bodyText,
                titleText: chatApp.currentChannelInfo.isFrozen ? "Unfreeze group" : "Freeze group"
            });
            modal.show();


            $(modal).on("submit", function () {

                if (chatApp.currentChannelInfo.isFrozen) {
                    chatApp.currentChannelInfo.unfreeze(function (response, error) {
                        if (error) {
                            toastr.error(error.message);
                        }
                        else {
                            toastr.success(chatApp.currentChannelInfo.name + " channel unfrozen successfully");
                            chatApp.sendCustomMessage("Channel unfrozen by " + chatApp.currentUser.nickname, chatApp.adminMessageType);
                        }
                    });
                }
                else {
                    chatApp.currentChannelInfo.freeze(function (response, error) {
                        if (error) {
                            toastr.error(error.message);
                        }
                        else {
                            toastr.success(chatApp.currentChannelInfo.name + " channel frozen successfully");
                            chatApp.sendCustomMessage("Channel frozen by " + chatApp.currentUser.nickname, chatApp.adminMessageType);
                        }
                    });
                }
                
                modal.hide();
                modal.destroy();
            });
        });

        //Archive channel clicked event
        $(chatApp.chatUi).on("archiveChannel", function () {

            var submitButtonText = "Yes, archive group";
            var bodyText = "Are you sure you want to archive this group? It will be removed from the list of your groups.";
            var modal = new ModalComposer({
                closeButtonText: "No",
                submitButtonText: submitButtonText,
                bodyElements: null,
                bodyText: bodyText,
                titleText: "Archive group"
            });
            modal.show();


            $(modal).on("submit", function () {
                var removingChannelUrl = chatApp.currentChannelInfo.url;

                chatApp.currentChannelInfo.hide(false, false, function (response, error) {
                    if (error) {
                        toastr.error(error.message);
                    }
                    else {
                        chatApp.channelsUi.removeById(removingChannelUrl);
                        if (chatApp.channelsUi.channels.length > 0)
                            chatApp.channelsUi.setActive(chatApp.channelsUi.channels[0].url);
                        toastr.success("Group archived successfully");
                    }
                });

                modal.hide();
                modal.destroy();
            });
        });

        //Resize chat messages wrapper event
        $(chatApp.chatUi).on("resize", function () {
            chatApp.resizeToBottom();
        });

        //Image loaded event
        $(chatApp.chatUi).on("imageLoaded", function () {
            //if not preloading messages then resize and scroll to the bottom
            if (!chatApp.preloadingMessages)
                chatApp.resizeToBottom();
            else {
                console.log("preloading, not scroll to bottom");
            }
        });

        //Remove member clicked event
        $(chatApp.chatUi).on("removeMember", function (e, member) {
            var modal = new ModalComposer({ closeButtonText: "No", submitButtonText: "Yes, remove", bodyElements: null, bodyText: "Are you sure you want to remove " + member.nickname + " from this group ?", titleText: "Remove from group" });
            modal.show();
            $(modal).on("submit", function () {
                chatApp.currentChannelInfo.banUserWithUserId(member.userId, 3, "", function (response, error) {
                    if (error) {
                        toastr.error(error.message);
                    }
                    else {
                        toastr.success(member.nickname + " was removed from this group");
                        chatApp.chatUi.setTitle(chatApp.currentChannelInfo, chatApp.currentUser.userId);
                        chatApp.sendCustomMessage(chatApp.currentUser.nickname + " kicked " + member.nickname + " from the group", chatApp.adminMessageType);
                    }
                });
                modal.hide();
                modal.destroy();
            });
        });

        //Leave channel clicked event
        $(chatApp.chatUi).on("leaveChannel", function () {
            var modal = new ModalComposer({ closeButtonText: "No", submitButtonText: "Yes, leave this group", bodyElements: null, bodyText: "Are you sure you want to leave this group?", titleText: "Leave group" });
            modal.show();

            $(modal).on("submit", function () {
                chatApp.sendCustomMessage(chatApp.currentUser.nickname + " left the group", chatApp.adminMessageType);
                chatApp.currentChannelInfo.leave(function (response, error) {
                    if (error) {
                        toastr.error(error.message);
                        return;
                    }
                    toastr.success("You leaved " + chatApp.currentChannelInfo.name + " group");
                    chatApp.channelsUi.removeById(chatApp.currentChannelInfo.url);
                    var allChannels = chatApp.channelsUi.channels;
                    if (allChannels.length > 0) {
                        chatApp.channelsUi.setActive(allChannels[0].url);
                    }
                    chatApp.channelsScroll.doScrollTop(0, 0);
                });
                modal.hide();
                modal.destroy();
            });
        });

        //Edit message clicked event
        $(chatApp.chatUi).on("editMessage", function (e, message) {
            var messageUpdateInput = chatApp.buildMessageUpdateTextBox(message.message);
            var bodyElements = [];
            bodyElements.push(messageUpdateInput);
            var modal = new ModalComposer({ closeButtonText: "Cancel", submitButtonText: "Update", bodyElements: bodyElements, bodyText: "", titleText: "Update message" });
            modal.show();

            $(modal).on("submit", function () {
                var newTextMessage = $(bodyElements[0]).val();
                const params = new sb.UserMessageParams();
                params.message = newTextMessage;
                chatApp.currentChannelInfo.updateUserMessage(message.messageId, params, function (newmessage, error) {
                    if (error) {
                        toastr.error(error.message);
                        return;
                    }
                    chatApp.chatUi.updateMessage(newmessage);
                });
                modal.hide();
                modal.destroy();
            });
        });

        //Copy message clicked event
        $(chatApp.chatUi).on("copyMessage", function (e, copyMessage) {
            var channelSelector = chatApp.buildChannelSelector();
            var bodyElements = [];
            bodyElements.push(channelSelector);
            var modal = new ModalComposer({ closeButtonText: "Cancel", submitButtonText: "Copy message", bodyElements: bodyElements, bodyText: "", titleText: "Copy message" });
            modal.show();

            $(modal).on("submit", function () {
                var selectedChannel = $(bodyElements[0]).val();
                if (selectedChannel) {
                    sb.GroupChannel.getChannel(selectedChannel, function (selectedChannel, error) {
                        if (error) {
                            toastr.error(error.message);
                            return;
                        }

                        //if its a text message
                        if (copyMessage.messageType == "user") {
                            const params = new chatApp.sb.UserMessageParams();
                            params.message = copyMessage.message;
                            if (copyMessage.parentMessageId) {
                                params.parentMessageId = copyMessage.parentMessageId;
                                params.data = JSON.stringify({ parentMessageId: copyMessage.parentMessageId, parentMessageText: copyMessage.parentMessageText, copyFromMessage: copyMessage.messageId, copyFrpmChannel: chatApp.currentChannelInfo.url });
                            }
                            else
                                params.data = JSON.stringify({ copyFromMessage: copyMessage.messageId, copyFrpmChannel: chatApp.currentChannelInfo.url });

                            selectedChannel.sendUserMessage(params, function (message, error) {
                                if (error) {
                                    toastr.error(error.message);
                                    return;
                                }
                                else {
                                    chatApp.channelsUi.setActive(selectedChannel.url);
                                    chatApp.channelsUi.moveTop(selectedChannel.url);
                                }
                            });
                        }

                        //if its a file message
                        if (copyMessage.messageType == "file") {
                            const params = new chatApp.sb.FileMessageParams();
                            params.fileUrl = copyMessage.url;
                            params.fileName = copyMessage.name;
                            params.fileSize = copyMessage.size;
                            params.mimeType = copyMessage.type;
                            if (copyMessage.parentMessageId) {
                                params.parentMessageId = copyMessage.parentMessageId;
                                params.data = JSON.stringify({ parentMessageId: copyMessage.parentMessageId, parentMessageText: copyMessage.parentMessageText, copyFromMessage: copyMessage.messageId, copyFrpmChannel: chatApp.currentChannelInfo.url });
                            }
                            else
                                params.data = JSON.stringify({ copyFromMessage: copyMessage.messageId, copyFrpmChannel: chatApp.currentChannelInfo.url });

                            selectedChannel.sendFileMessage(params, function (message, error) {
                                if (error) {
                                    toastr.error(error.message);
                                    return;
                                }
                                else {
                                    chatApp.channelsUi.setActive(selectedChannel.url);
                                    chatApp.channelsUi.moveTop(selectedChannel.url);
                                }
                            });
                        }
                    });
                }
                else {
                    toastr.warning("Channel is not selected");
                    return;
                }
                modal.hide();
                modal.destroy();
            });
        });

        //Delete message clicked event
        $(chatApp.chatUi).on("deleteMessage", function (e, messageId) {
            var modal = new ModalComposer({ closeButtonText: "No", submitButtonText: "Yes, remove", bodyElements: null, bodyText: "Are you sure you want to remove this message ?", titleText: "Remove message" });
            modal.show();

            $(modal).on("submit", function () {
                var message = new chatApp.sb.UserMessage();
                message.messageId = messageId;
                chatApp.currentChannelInfo.deleteMessage(message, function (response, error) {
                    if (error) {
                        toastr.error(error.message);
                        return;
                    }
                });
                modal.hide();
                chatApp.chatUi.deleteMessage(messageId);
            });
        });

        //Scroll to message event
        $(chatApp.chatUi).on("scrollToMessage", function (e, messageId) {
            
        });

        //Reply to message clicked event
        $(chatApp.chatUi).on("replyMessage", function (e, message) {
            chatApp.messageComposerUi.startReply(message);
        });

        //Scrolled to top event
        $("#currentConversationWrapper").scroll(function () {
            if ($("#currentConversationWrapper").scrollTop() == 0) {
                var height = $("#currentConversationWrapper")[0].scrollHeight;
                if (height > 600) {
                    chatApp.loadMoreMessages();
                }
            }
        });
    },
    resizeToBottom: function resizeToBottom() {
        chatApp.calcCurrScrollHeight();
        chatApp.chatScroll.resize();
        chatApp.scrollPositionBottom();
    },
    loadMessages: function loadMessages() {
        chatApp.messagesQuery = chatApp.currentChannelInfo.createPreviousMessageListQuery();
        chatApp.messagesQuery.load(20, false, function (messages, error) {
            if (messages) {
                chatApp.chatUi.loadMessages(messages, false);
                chatApp.chatUi.updateLastMessageReadMembers(chatApp.currentChannelInfo);
                chatApp.resizeToBottom();
            }
        });
    },
    loadMoreMessages: function loadMoreMessages() {
        if (!chatApp.messagesQuery)
            chatApp.messagesQuery = chatApp.currentChannelInfo.createPreviousMessageListQuery();
        chatApp.messagesQuery.load(10, false, function (messages, error) {
            if (error) {
                return;
            }
            if (messages && messages.length == 0) {
                chatApp.chatUi.notify("You reach the beginning of this conversation");
            }
            else {
                chatApp.preloadingMessages = true;
                //var msgs = [];
                //messages.forEach(function (message) {
                //    var msg = chatApp.sbMessageConvert(message);
                //    msgs.push(msg);
                //});
                chatApp.chatUi.loadMessages(messages, true);
                $("#currentConversationWrapper")[0].scrollTop = chatApp.calcCurrScrollHeight();
            }
        });
    },
    createUsersQuery: function createUsersQuery() {
        chatApp.usersQuery = chatApp.sb.createUserListQuery();
        chatApp.usersQuery.limit = 100;
    },
    loadUsers: function loadUsers() {
        if (chatApp.usersQuery && chatApp.usersQuery.hasNext) {
            chatApp.usersQuery.next(function (userList, error) {
                if (error) {
                    return;
                }
                chatApp.usersList = chatApp.usersList.concat(userList);
                chatApp.loadUsers();
            });
        }
        else {
            chatApp.usersLoaded = true;
        }
    },
    messageReceived: function messageReceived(channel, message) {
        console.log("<< messageReceived ");
        chatApp.channelsUi.moveTop(channel.url);
        if (chatApp.currentChannelInfo && channel.url == chatApp.currentChannelInfo.url) {
            chatApp.chatUi.addMessage(message, false, true);
            chatApp.currentChannelInfo.markAsRead();
            chatApp.resizeToBottom();
        }
    },
    channelChanged: function channelChanged(channel) {
        console.log("<< channelChanged");
        chatApp.channelsUi.update(channel);
        chatApp.resizeToBottom();
    },
    channelFrozen: function channelFrozen(channel) {
        console.log("<< channelFrozen");
        chatApp.chatUi.setTitle(channel, chatApp.currentUser.userId);
        chatApp.channelsUi.update(channel);
        if (channel.url == chatApp.currentChannelInfo.url && channel.myRole != "operator") {
            chatApp.messageComposerUi.hide();
        }
    },
    channelUnFrozen: function channelUnFrozen(channel) {
        console.log("<< channelUnFrozen");
        chatApp.chatUi.setTitle(channel, chatApp.currentUser.userId);
        chatApp.channelsUi.update(channel);
        if (channel.url == chatApp.currentChannelInfo.url) {
            chatApp.messageComposerUi.show();
        }
    },
    messageDeleted: function messageDeleted(channel, messageId) {
        if (chatApp.currentChannelInfo && channel.url == chatApp.currentChannelInfo.url) {
            chatApp.chatUi.deleteMessage(messageId);
        }
    },
    messageUpdated: function messageUpdated(channel, message) {
        if (chatApp.currentChannelInfo && channel.url == chatApp.currentChannelInfo.url) {
            chatApp.chatUi.updateMessage(message);
        }
    },
    readReceiptUpdated: function readReceiptUpdated(channel) {
        console.log("<< Delivery Receipt Updated");
        //retrieve list of members who reads the last message of the channel
        if (channel.lastMessage && chatApp.currentChannelInfo && channel.url == chatApp.currentChannelInfo.url) {
            chatApp.chatUi.updateLastMessageReadMembers(channel);
            chatApp.resizeToBottom();
        }
    },
    userBanned: function userBanned(channel, member) {
        console.log("<< userBanned " + member.nickname);
        //if its me, remove the channel from my list
        if (member.userId == chatApp.currentUser.userId) {
            chatApp.channelsUi.removeById(channel.url);
            //if this was the current channel, select the top one
            if (channel.url == chatApp.currentChannelInfo.url) {
                var allChannels = chatApp.channelsUi.channels;
                if (allChannels.length > 0) {
                    chatApp.channelsUi.setActive(allChannels[0].url);
                }
                chatApp.channelsScroll.doScrollTop(0, 0);
            }
        }
        else {
            chatApp.chatUi.setTitle(channel, chatApp.currentUser.userId);
        }
    },
    typingStatusUpdated: function typingStatusUpdated(channel) {
        var members = channel.getTypingMembers();
        if (chatApp.currentChannelInfo && channel.url == chatApp.currentChannelInfo.url) {
            if (members.length > 0)
                chatApp.chatUi.startTyping(members);
            else
                chatApp.chatUi.endTyping(members);
        }
        else {
            chatApp.channelsUi.userTyping(channel);
        }
    },
    scrollPositionBottom: function scrollPositionBottom() {
        var scrollHeight = $("#currentConversationWrapper")[0].scrollHeight;
        $("#currentConversationWrapper")[0].scrollTop = scrollHeight;
    },
    sbMessageConvert: function sbMessageConvert(sbMessage) {
        var msg = {};
        msg.date = sbMessage.createdAt;
        msg.error = sbMessage.errorCode;
        msg.message = sbMessage.message;
        msg.id = sbMessage.messageId;
        msg.messageType = sbMessage.messageType;
        msg.type = sbMessage.type;
        msg.sender = sbMessage._sender ? sbMessage._sender.nickname : "";
        msg.isMyMessage = sbMessage._sender ? sbMessage._sender.userId == chatApp.currentUser.userId : false;
        msg.fileUrl = sbMessage.url;
        msg.name = sbMessage.name;
        msg.size = sbMessage.size;
        msg.customType = sbMessage.customType;
        return msg;
    },
    sendFileHandler: function sendFileHandler(event) {
        var percentUploaded = parseInt(Math.floor(event.loaded / event.total * 100));
        chatApp.messageComposerUi.fileUploadUpdate(percentUploaded);
    },
    sendMessageHandler: function sendMessageHandler(message, error) {
        chatApp.messageComposerUi.fileUploadComplete();
        if (error) {
            toastr.warning(error.message);
            return;
        }
        chatApp.chatUi.addMessage(message, false, true);
    },
    membersToTitle: function membersToTitle(members) {
        var title = "";
        members.forEach(function (member) {
            if (chatApp.currentUser.userId != member.userId) {
                title += member.nickname + ", ";
            }
        });
        title = title.slice(0, -2);
        return title;
    },
    calcCurrScrollHeight: function calcCurrScrollHeight() {
        var oldHeight = chatApp.currScrollHeight;
        var newHeight = $("#currentConversationWrapper")[0].scrollHeight;
        chatApp.currScrollHeight = newHeight;
        return diff = newHeight - oldHeight ;
    },
    buildUsersSelector: function buildUsersSelector(excludeCurrentMembers) {

        var filterInput = jQuery('<input />', {
            "class": "form-control",
            "id": "txtUserFilter",
            "type": "text",
            "placeholder": "Search members..."
        });

        var selWrapper = jQuery('<div />', {
            "class": "list-group",
            "id": "newMsgUsersList"
        });

        $(filterInput).on("keyup", function () {
            var value = $(this).val().toLowerCase();
            $("#newMsgUsersList label").filter(function () {
                $(this).toggle($(this).text().toLowerCase().indexOf(value) > -1)
            });
        });

        selWrapper.append(filterInput);
        selWrapper.append("<br>");

        var staffList = JSON.parse($("#staffList").val());

        staffList.forEach(function (user) {
            var isMember = false;
            if (excludeCurrentMembers) {
                isMember = chatApp.currentChannelInfo.members.find(o => o.userId == user.id);
            }
            if (!isMember) {
                var label = jQuery('<label />', {
                    "class": "list-group-item",
                    "id": "lblUser" + user.id
                });

                var input = jQuery('<input />', {
                    "class": "form-check-input me-1",
                    "type": "checkbox",
                    "value": user.id
                });
                label.append(input);
                label.append(user.name);
                selWrapper.append(label);
            }
        });

        return selWrapper;
    },
    buildUsersList: function buildUsersList() {

        var selWrapper = jQuery('<div />', {
            "class": "list-group",
            "id": "newMsgUsersList"
        });

        chatApp.currentChannelInfo.members.forEach(function (user) {
            var label = jQuery('<label />', {
                "class": "list-group-item"
            });

            if(user.connectionStatus == "online")
                label.append("<span class=\"badge rounded-pill bg-warning\">&nbsp;&nbsp;&nbsp;</span> ");
            else
                label.append("<span class=\"badge rounded-pill bg-danger\">&nbsp;&nbsp;&nbsp;</span> ");

            if (user.nickname)
                label.append(user.nickname);
            else
                label.append("[Unknown user]");
            if (user.role == "operator")
                label.append(" (operator)");
            selWrapper.append(label);
        });

        return selWrapper;
    },
    buildChannelNameTextBox: function buildChannelNameTextBox(val) {
        var input = jQuery('<input />', {
            "class": "form-control",
            "placeholder":"Group chat name...",
            "id": "txtNewChannelName"
        });

        if (val)
            input.val(val);
        return input;
    },
    buildMessageUpdateTextBox: function buildMessageUpdateTextBox(val) {
        var input = jQuery('<textarea />', {
            "class": "message-composer",
            "id": "txtMessageUpdate",
            "rows": "5"
        });

        if (val)
            input.val(val);
        return input;
    },
    buildChannelSelector: function buildChannelSelector() {
        var selWrapper = jQuery('<select />', {
            "class": "form-control",
            "id": "channelsSelectorList",
            "aria-label": "Select group to copy message to"
        });

        var emptyOption = jQuery('<option  />', {
            "value": "",
            "text": "Select group to copy message to"
        });
        selWrapper.append(emptyOption);
        chatApp.groupChannelListQuery = sb.GroupChannel.createMyGroupChannelListQuery();
        chatApp.groupChannelListQuery.next(function (channels, error) {
            if (error) {
                toastr.warning("<strong>Error</strong><br/>Cannot retrieve channels list");
                return;
            }
            channels.forEach(function (channel) {
                if (channel.url != chatApp.currentChannelInfo.url) {
                    var option = jQuery('<option  />', {
                        "value": channel.url,
                        "text": channel.name
                    });
                    selWrapper.append(option);
                }
            });

        });
        return selWrapper;
    },
    extractSelectedUsersFromUsersSelector: function extractSelectedUsersFromUsersSelector(selUsers) {
        var userids = [];
        $.each(selUsers.children, function (index, label) {
            var chkBox = $(label).find("input")[0];
            var checked = $(chkBox).is(':checked')
            var id = $(chkBox).val();
            if (checked)
                userids.push(id);
        });
        return userids;
    },
    sendCustomMessage: function sendCustomMessage(message, custom_type) {
        if (chatApp.currentChannelInfo) {
            const params = new chatApp.sb.UserMessageParams();
            params.message = message;
            params.customType = custom_type;
            chatApp.currentChannelInfo.sendUserMessage(params, chatApp.sendMessageHandler);
        }
    }
};