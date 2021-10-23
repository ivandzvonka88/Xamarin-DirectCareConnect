var appId = 'F742C54C-036E-4DD0-B74D-E59E38ED7B30';
var currScrollHeight = 0;
var MESSAGE_TEXT_HEIGHT = 27;

var nickname = null;
var userId = null;
var channelListPage = 0;
var currChannelUrl = null;
var currChannelInfo = null;
var leaveChannelUrl = '';
var leaveMessagingChannelUrl = '';
var hideChannelUrl = '';
var userListToken = '';
var userListNext = 0;
var targetAddGroupChannel = null;

var firstChannelUrl = '';

var isOpenChat = false;
var memberList = [];

// 3.0.x
var currentUser;
var otherUser;

var emojiChatTemp = '';

$('#new_chat').click(function () {
    $('.inbox__modal-bg').show();
    $('.modal-messaging').show();
    getUserList();
});

$('.inbox__modal-bg').click(function () {
    $('.inbox__modal-bg').hide();
    $('.modal-messaging').hide();
});

$('.chat-top__button-member').click(function () {
    if ($('.modal-member').is(":visible")) {
      $(this).removeClass('chat-top__button-member--active');
      $('.modal-member').hide();
    } else {
      popupInit();
      $(this).addClass('chat-top__button-member--active');
      getMemberList(currChannelInfo);
      $('.modal-member').show();
    }
  });
  
  $('.chat-top__button-invite').click(function () {
    if ($('.modal-invite').is(":visible")) {
      $(this).removeClass('chat-top__button-invite--active');
      $('.modal-invite').hide();
    } else {
      popupInit();
      $(this).addClass('chat-top__button-invite--active');
      getUserList();
      $('.modal-invite').show();
    }
});

function getMemberList(channel) {
  
    if (channel.isGroupChannel()) {
      var members = channel.members;
      $('.modal-member-list').html('');
  
      var memberListHtml = '';
      members.forEach(function (member) {
        if (member.connectionStatus == 'online') {
          var isOnline = 'online';
          var dateTimeString = '';
        } else {
          var isOnline = '';
          var dateTime = new Date(member.lastSeenAt);
          var dateTimeString = (dateTime.getMonth() + 1) + '/' + dateTime.getDate() + ' ' + dateTime.getHours() + ':' + dateTime.getMinutes();
        }
  
  
        memberListHtml += '' +
          '<div class="modal-member-list__item">' +
          '<div class="modal-member-list__icon"></div>' +
          '  <div class="modal-member-list__name ' + isOnline + '">' +
          (member.nickname.length > 30 ? xssEscape(member.nickname.substring(0, 30)) + '...' : xssEscape(member.nickname)) +
          '  </div>' +
          '</div>';
      });
      $('.modal-member-list').html(memberListHtml);
    }
}

function getUserList() {
    UserListQuery = sb.createUserListQuery();
    if (UserListQuery.hasNext) {
        UserListQuery.next(function (userList, error) {
          if (error) {
            return;
          }
    
          var users = userList;
          $.each(users, function (index, user) {
                UserList[user.userId] = user;
          });
          if (UserListQuery.hasNext) {
            UserListQuery.next(function (userList, error) {
              if (error) {
                return;
              }
        
              var users = userList;
              $.each(users, function (index, user) {
                    UserList[user.userId] = user;
              });
              if (UserListQuery.hasNext) {
                UserListQuery.next(function (userList, error) {
                  if (error) {
                    return;
                  }
            
                  var users = userList;
                  $.each(users, function (index, user) {
                        UserList[user.userId] = user;
                  });
                });
            }
            });
        }
        });
    }

    
  
    // else{
        // break;
    // }
    // }
}
function userClick(obj) {
    var selectCount = $('.modal-messaging-list__icon--select').length;
    var el1 = obj.find('div');
    var flag_mp = 0;
    if (el1.hasClass('modal-messaging-list__icon--select')) {
        el1.removeClass('modal-messaging-list__icon--select');
        flag_mp = 0;
        // }
    // if(selectCount < 1) {
        // var el = obj.find('div');
        // if (el.hasClass('modal-messaging-list__icon--select')) {
        // el.removeClass('modal-messaging-list__icon--select');
        } else {
            el1.addClass('modal-messaging-list__icon--select');
            flag_mp = 1;
        }
    
        if (((selectCount > 1) || ((selectCount == 1 ) && (flag_mp == 1))) && !((selectCount == 2) &&(flag_mp == 0))) {
            if(flag_mp == 1)
                selectCount++;
            else if(flag_mp == 0)
                selectCount--;
        $('.modal-messaging-top__title').html('Group Chat ({})'.format(selectCount));
        } else {
        $('.modal-messaging-top__title').html('New Message');
        }
}

function inviteMember() {
    if ($('.modal-messaging-list__icon--select').length == 0) {
      alert('Please select user');
      return false;
    }
  
    var userIds = [];
    $.each($('.modal-messaging-list__icon--select'), function (index, user) {
      if ($(user).data("guest-id")) {
        userIds.push($(user).data("guest-id").toString());
      }
    });
    console.log(userIds);
    currChannelInfo.inviteWithUserIds(userIds, function (response, error) {
      if (error) {
          console.log(error);
        return;
      }
      currChannelInfo.sendUserMessage("Hi", SendMessageHandler);
      popupInit();
    });
  
  }

  function startMessaging() {
    if ($('.modal-messaging-list__icon--select').length == 0) {
      alert('Please select user');
      return false;
    }
    var id_user;
    var name_user;

    $.each($('.modal-messaging-list__icon--select'), function (index, user) {
        id_user = $(user).data("guest-id");
        name_user = $(user).data("guest-name");
        $.ajax({
            type: 'POST',
            url: 'https://api-F742C54C-036E-4DD0-B74D-E59E38ED7B30.sendbird.com/v3/users',
            headers: { "Api-Token": "42b4f9494342b49f4334e0f8a9a1a65a5a969a6f" },
            data: JSON.stringify({
                user_id: id_user.toString(),
                nickname: name_user.toString(),
                profile_url: ""
            }),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (r) {
             //   console.log(r);
            },
            error: function (r) {
            },
            //timeout: 10000
        });
    });
   // console.log(id_user);
    
    var isDistinct;
    isDistinct = false;
    // startMessagingProcess();  
    var cflag1 = 0;
    var cflag2 = 0;

    

    var startMessagingProcess = function () {
      var users = [];
      $.each($('.modal-messaging-list__icon--select'), function (index, user) {
          console.log("data", $(user).data("guest-id"));
          console.log(UserList);
        users.push(UserList[$(user).data("guest-id")]);
      });
    //  console.log(UserList);
     // console.log("userid", users);
      PreviousMessageListQuery = null;
    //   var userIds = [userId.toString(), id_user.toString()];

    //   sb.GroupChannel.createChannelWithUserIds(userIds, false, id_user.toString(), function (channel, error) {
    //     channel.sendUserMessage("Hi", function (message, error) {
    //         setTimeout(function () {
    //             updateGroupChannelListAll();
    //         }, 1000);
    //     });
    console.log(users);
      sb.GroupChannel.createChannel(users, isDistinct, 'test_name', '', '', function (channel, error) {
        if (error) {
          return;
        }
  
        currChannelInfo = channel;
        currChannelUrl = channel.url;
  
        var members = channel.members;
        var channelTitle = '';
  
        $.each(members, function (index, member) {
          if (!isCurrentUser(member.userId)) {
            channelTitle += xssEscape(member.nickname);
          }
        });
  
        channelTitle = channelTitle.slice(0, -2);
        var channelMemberList = channelTitle;
        if (channelTitle.length > 20) {
          channelTitle = channelTitle.substring(0, 20);
          channelTitle += '...'
        }
        var titleType = 1;
        var isGroup = true;
        if (members.length > 2) {
          channelTitle += '({})'.format(members.length);
          titleType = 2;
        }
  
        $('.chat-empty').hide();
//         console.log("cTitle", channelTitle);
        initChatTitle(channelTitle, titleType);
        $('.chat-canvas').html('');
        $('.chat-input-text__field').val('');
        $('.chat').show();
  
        $('.modal-messaging').hide();
        $('.inbox__modal-bg').hide();

        // navInit();
        // popupInit();
        makeMemberList(members);
  
        isOpenChat = false;
        loadMoreChatMessage(scrollPositionBottom);
        // setWelcomeMessage('Messaging Channel');
        addGroupChannel(true, channelMemberList, currChannelInfo);
        $('.chat-input-text__field').attr('disabled', false);
        channel.sendUserMessage("Hi", SendMessageHandler);
        // setTimeout(function () {
        //     updateGroupChannelListAll();
        // }, 2000);
        location.reload(); 
      });
    };
  
    GroupChannelListQuery1 = sb.GroupChannel.createMyGroupChannelListQuery();

    GroupChannelListQuery1.next(function (channels, error) {
        // console.log("channel", channels);

        if (error) {
            return;
        }
        // channels.forEach(function (channel) {
        //     //console.log("channel", channel);
        //     var channelMemberList = '';
        //     var members = channel.members;
        //     console.log(members);
        //     if(members.length == 2) {
        //         members.forEach(function (member) {
        //             if (id_user == member.userId) {
        //                 cflag1 = 1;
        //                 cflag2 = 1;
        //             //  console.log(id_user);
        //             }

        //         });
        //     }
  
        //     if( cflag1 == 1) {
        //         channel.sendUserMessage("Hi", SendMessageHandler);
        //         // location.reload(); 
        //         cflag1 = 0;

        //     }

        // });
        if(cflag2 == 0)
        {
            startMessagingProcess();
        }
    
    });
   
 
    
    return;
  }

/***********************************************
 *                MESSAGING
 **********************************************/
function isCurrentUser(pUserId) {
    return (userId == pUserId) ? true : false;
}

function moveToTopGroupChat(channelUrl) {
    $('.left-nav-channel-group[data-channel-url=' + channelUrl + ']').prependTo('#messaging_channel_list');
}

// $(document).on('click', '.chat-canvas__list-text', function (e) {
//     var userId = $(this).prev().prev().data('userid');
//     var messageId = $(this).data('messageid');
//     var channelUrl = currChannelInfo.url;
//     var messageObject = $(this);
  
//     // modalConfirm('Are you Sure?', 'Do you want to delete message?', function () {
//         if (confirm('Are you sure you want to delete this message?')) {
//       currChannelInfo.deleteMessage(channelMessageList[channelUrl][messageId]['message'], function (response, error) {
//         if (!error) {
//           delete(channelMessageList[channelUrl][messageId]);
//           messageObject.parent().remove();
//         }
//     //   });
//     });
// }
//   });

function updateGroupChannelLastMessage(message) {
    var lastMessage = '';
    var lastMessageDateString = '';
    if (message) {
        lastMessage = xssEscape(message.message);
        var calcSeconds = (new Date().getTime() - message.createdAt) / 1000;
        var parsedValue;

        if (calcSeconds < 60) {
            parsedValue = parseInt(calcSeconds);
            lastMessageDateString ="less than a minute ago"
        } else if (calcSeconds / 60 < 60) {
            parsedValue = parseInt(calcSeconds / 60);
            if (parsedValue == 1) {
                lastMessageDateString = parsedValue + ' min ago';
            } else {
                lastMessageDateString = parsedValue + ' mins ago';
            }
        } else if (calcSeconds / (60 * 60) < 24) {
            parsedValue = parseInt(calcSeconds / (60 * 60));
            if (parsedValue == 1) {
                lastMessageDateString = parsedValue + ' hour ago';
            } else {
                lastMessageDateString = parsedValue + ' hours ago';
            }
        } else {
            parsedValue = parseInt(calcSeconds / (60 * 60 * 24));
            if (parsedValue == 1) {
                lastMessageDateString = parsedValue + ' day ago';
            } else {
                lastMessageDateString = parsedValue + ' days ago';
            }
        }

        if (lastMessageDateString) {
            lastMessageDateString = '<div><img src="images/icon-time.png" style="vertical-align: moddle; padding-bottom: 2px;" /> <span> ' + lastMessageDateString + '</span></div>';
        }

        $('.left-nav-channel-group[data-channel-url=' + message.channelUrl + '] .left-nav-channel-lastmessage').html(lastMessage);
        emojiChatTemp = $(".left-nav-channel-lastmessage");
        for (var i = 0; i < $(".left-nav-channel-lastmessage").length; i++) {
            if(emojiChatTemp.eq(i).text().includes("<img"))
                $(".left-nav-channel-lastmessage").eq(i).html(emojiChatTemp.eq(i).text());
        }
        $('.left-nav-channel-group[data-channel-url=' + message.channelUrl + '] .left-nav-channel-lastmessagetime').html(lastMessageDateString);
    }
}

function updateGroupChannelListAll() {
    for (var i in groupChannelLastMessageList) {
        var message = groupChannelLastMessageList[i];
        updateGroupChannelLastMessage(message);
    }
    if(firstChannelUrl != '') joinGroupChannel(firstChannelUrl);
    $(".messenger-list").css("display", "table-cell");
    $(".chat-box").css("display", "table-cell");
    waitOff();
}

function addGroupChannel(isGroup, channelMemberList, targetChannel) {
    if (isGroup) {
        groupChannelLastMessageList[targetChannel.url] = targetChannel.lastMessage;
    }

    $.each($('.left-nav-channel'), function (index, channel) {
        $(channel).removeClass('left-nav-channel-open--active');
        $(channel).removeClass('left-nav-channel-messaging--active');
        $(channel).removeClass('left-nav-channel-group--active');
    });

    var addFlag = true;
    $.each($('.left-nav-channel-messaging'), function (index, channel) {
        if (currChannelUrl == $(channel).data('channel-url')) {
            $(channel).addClass('left-nav-channel-messaging--active');
            $(channel).find('div[class="left-nav-channel__unread"]').remove();
            addFlag = false;
        }
    });
    $.each($('.left-nav-channel-group'), function (index, channel) {
        if (currChannelUrl == $(channel).data('channel-url')) {
            $(channel).addClass('left-nav-channel-group--active');
            $(channel).find('div[class="left-nav-channel__unread"]').remove();
            addFlag = false;
        }
    });

    if (channelMemberList.length > 9) {
        channelMemberList = channelMemberList.substring(0, 9) + '...';
    }

    targetAddGroupChannel = targetChannel;
    if (addFlag && !isGroup) {
        $('#messaging_channel_list').append(
            '<div class="left-nav-channel left-nav-channel-messaging left-nav-channel-messaging--active" ' +
            '     onclick="joinGroupChannel(\'' + targetChannel.url + '\')"' +
            '     data-channel-url="' + targetChannel.url + '"' +
            '>' +
            channelMemberList +
            '</div>'
        );

        groupChannelListMembersAndProfileImageUpdate(targetChannel);
    } else if (addFlag && isGroup) {
        $('#messaging_channel_list').append(
            '<div class="left-nav-channel left-nav-channel-group left-nav-channel-group--active" ' +
            '     onclick="joinGroupChannel(\'' + targetChannel.url + '\')"' +
            '     data-channel-url="' + targetChannel.url + '"' +
            '>' +
            '<div class="left-nav-channel-members">' + channelMemberList + '</div>' +
            '<div class="left-nav-channel-lastmessage"></div>' +
            '<div class="left-nav-channel-lastmessagetime"></div>' +
            '</div>'
        );
        targetAddGroupChannel = null;
        groupChannelListMembersAndProfileImageUpdate(targetChannel);
    }

    $('.left-nav-button-guide').hide();
}
function groupChannelListMembersAndProfileImageUpdate(targetChannel) {
    // select profileImage
    var members = targetChannel.members;
    // console.log(members);

    var membersProfileImageUrl = [];
    var membersNickname = '';
    for (var i in members) {
        var member = members[i];
        if (sb.currentUser.userId != member.userId) {
            membersProfileImageUrl.push(member.profileUrl);
            membersNickname += xssEscape(member.nickname) + ', ';

        }

    }
    membersNickname = membersNickname.substring(0, membersNickname.length - 2);

    if (membersNickname.length > 22) {
        membersNickname = membersNickname.substring(0, 22) + '...';
    }

    // console.log(membersProfileImageUrl);
    var selectSequence = parseInt(Math.random() * membersProfileImageUrl.length);
    // console.log(selectSequence);
    var selectedProfileImageUrl = membersProfileImageUrl[selectSequence];
    // console.log(selectedProfileImageUrl);

    var targetElem = $('.left-nav-channel-group[data-channel-url=' + targetChannel.url + ']');
    // $('.left-nav-channel-group[data-channel-url='+targetChannel.url+']')

    targetElem.css('background-image', 'images/user_icon.png');

    // member nickname update
    targetElem.find('.left-nav-channel-members').html(membersNickname);


}
function joinGroupChannel(channelUrl, callback) {
    // console.log('joinGroupChannel:', channelUrl);
    // $(".message__unread").remove(); 
    $('.chat-input-typing').hide();
    $('.chat-input-typing').html('');
    $.ajax({
        type: 'GET',
       url: 'https://api-F742C54C-036E-4DD0-B74D-E59E38ED7B30.sendbird.com/v3/users/'+userId.toString()+'/unread_message_count',
        headers: { "Api-Token": "42b4f9494342b49f4334e0f8a9a1a65a5a969a6f" },
        success: function (r) {
            // console.log(r);
            totalUnreadMessage = r.unread_count;
            if(($(".message__unread").length == 0) && (totalUnreadMessage!=0))
                $('<div class="message__unread">' + totalUnreadMessage + '</div>').prependTo('.message');
            else
               $(".message__unread").html(totalUnreadMessage);  
               if(totalUnreadMessage == 0)
               $(".message__unread").remove();
        },
        error: function (r) {
        },
        //timeout: 10000
        });


    if (channelUrl == currChannelUrl) {
        $.each($('.left-nav-channel'), function (index, channel) {
            if ($(channel).data('channel-url') == channelUrl) {
                $(channel).find('div[class="left-nav-channel__unread"]').remove();
            }
        });
        return false;
    }

    PreviousMessageListQuery = null;
    sb.GroupChannel.getChannel(channelUrl, function (channel, error) {
        if (error) {
            console.error(error);
            return;
        }

        channel.markAsRead();

        currChannelInfo = channel;
        currChannelUrl = channelUrl;

        var members = channel.members;
        var channelTitle = '';

        channel.refresh(function () {
            // TODO
        });

        $.each(members, function (index, member) {
            if (!isCurrentUser(member.userId)) {
                channelTitle += xssEscape(member.nickname) + ',  ';
            }
        });
        // console.log(channelTitle);
        channelTitle = channelTitle.slice(0, -2);
        var channelMemberList = channelTitle;
        if (channelTitle.length > 35) {
            channelTitle = channelTitle.substring(0, 35);
            channelTitle += '...';
        }
        var usertitle = channelTitle;

        var titleType = 1;
        var isGroup = false;
        if (members.length > 2) {
            channelTitle += '({})'.format(members.length);
            titleType = 2;
            isGroup = true;
        }

        $('.chat-empty').hide();
        initChatTitle(channelTitle, titleType);
        $('.chat-canvas').html('');
        $('.chat-input-text__field').val('');
        $('.chat').show();

        makeMemberList(members);

        isOpenChat = false;
        loadMoreChatMessage(scrollPositionBottom);

        addGroupChannel(isGroup, channelMemberList, currChannelInfo);
        $('.chat-input-text__field').attr('disabled', false);

        $('.chat-top__button-hide').show();
        $(".chat-header .header-icon > img").attr(
            "src",
            "images/user_icon.png"
          );
          $(".chat-header .header-details > .user-icon").text(usertitle);

        if (callback) {
            callback();
        }
    });

    return;

}

function getGroupChannelList() {
    GroupChannelListQuery.next(function (channels, error) {
        if (error) {
            return;
        }
        if(channels.length != 0) firstChannelUrl = channels[0].url;
        else waitOff();
        channels.forEach(function (channel) {
            // console.log("channel", channel);
            var channelMemberList = '';
            var members = channel.members;

            members.forEach(function (member) {
                if (currentUser.userId != member.userId) {
                    channelMemberList += xssEscape(member.nickname) + ', ';
                }

            });

            channelMemberList = channelMemberList.slice(0, -2);
            addGroupChannel(true, channelMemberList, channel);

            $.each($('.left-nav-channel'), function (index, item) {
                $(item).removeClass('left-nav-channel-messaging--active');
                $(item).removeClass('left-nav-channel-group--active');
            });

            var targetUrl = channel.url;
            var unread = channel.unreadMessageCount > 9 ? '9+' : channel.unreadMessageCount;
            if (unread != 0) {
                $.each($('.left-nav-channel'), function (index, item) {
                    if ($(item).data("channel-url") == targetUrl) {
                        addUnreadCount(item, unread, targetUrl);
                    }
                });
            }
        });

    });

}

function makeMemberList(members) {
    var item = {};
    //Clear memberList before updating it
    memberList = [];
    $.each(members, function (index, member) {
        item = {};
        if (!isCurrentUser(member['user_id'])) {
            item["user_id"] = member["user_id"];
            item["name"] = xssEscape(member["name"]);
            memberList.push(item);
        }
    });
}
/***********************************************
 *            // END MESSAGING
 **********************************************/


/***********************************************
 *            SendBird Settings
 **********************************************/
var sb;

var GroupChannelListQuery;
var OpenChannelParticipantListQuery;

var UserListQuery;
var SendMessageHandler;

var UserList = {};
var isInit = false;

var channelMessageList = {};
var groupChannelLastMessageList = {};

function startSendBird(userId, nickName) {
    sb = new SendBird({
        appId: appId
    });

    sb.connect(userId, function (user, error) {
        if (error) {
            return;
        } else {
            initPage(user);
        }
    });

    var initPage = function (user) {
        isInit = true;
        $('.init-check').hide();

        currentUser = user;
        sb.updateCurrentUserInfo(nickName, '', function (response, error) {
            // console.log(response, error);
        });

        GroupChannelListQuery = sb.GroupChannel.createMyGroupChannelListQuery();
        UserListQuery = sb.createUserListQuery();

        GroupChannelListQuery.limit = 100;
        GroupChannelListQuery.includeEmpty = true;

        UserListQuery.limit = 100;

        getGroupChannelList();

        setTimeout(function () {
            updateGroupChannelListAll();
            setInterval(function () {
                updateGroupChannelListAll();
            }, 60000);
        }, 1000);
  
    };

    var ConnectionHandler = new sb.ConnectionHandler();
    ConnectionHandler.onReconnectStarted = function (id) {
        console.log('onReconnectStarted');
    };

    ConnectionHandler.onReconnectSucceeded = function (id) {
        console.log('onReconnectSucceeded');
        if (!isInit) {
            initPage();
        }



        // GroupChannel list reset
        GroupChannelListQuery = sb.GroupChannel.createMyGroupChannelListQuery();
        $('#messaging_channel_list').html('');
        getGroupChannelList();

        setTimeout(function () {
            updateGroupChannelListAll();
            setInterval(function () {
                updateGroupChannelListAll();
            }, 60000);
        }, 1000);
    };

    ConnectionHandler.onReconnectFailed = function (id) {
        console.log('onReconnectFailed');
    };
    sb.addConnectionHandler('uniqueID', ConnectionHandler);

    var ChannelHandler = new sb.ChannelHandler();
    ChannelHandler.onMessageReceived = function (channel, message) {
        $.ajax({
            type: 'GET',
           url: 'https://api-F742C54C-036E-4DD0-B74D-E59E38ED7B30.sendbird.com/v3/users/'+userId.toString()+'/unread_message_count',
            headers: { "Api-Token": "42b4f9494342b49f4334e0f8a9a1a65a5a969a6f" },
            success: function (r) {
                // console.log(r);
                totalUnreadMessage = r.unread_count;
                if(($(".message__unread").length == 0) && (totalUnreadMessage!=0))
                    $('<div class="message__unread">' + totalUnreadMessage + '</div>').prependTo('.message');
                else
                   $(".message__unread").html(totalUnreadMessage);  
                if(totalUnreadMessage == 0)
                    $(".message__unread").remove();
            },
            error: function (r) {
            },
            //timeout: 10000
            });
        var isCurrentChannel = false;

        if (currChannelInfo == channel) {
            isCurrentChannel = true;
        }

        channel.refresh(function () { });

        // update last message
        if (channel.isGroupChannel()) {
            groupChannelLastMessageList[channel.url] = message;
            updateGroupChannelLastMessage(message);
            moveToTopGroupChat(channel.url);
        }

        if (isCurrentChannel && channel.isGroupChannel()) {
            channel.markAsRead();
        } else {
            if (channel.isGroupChannel()) {
                unreadCountUpdate(channel);
            }
        }

        if (!document.hasFocus()) {
            notifyMessage(channel, xssEscape(message.message));
        }

        if (message.isUserMessage() && isCurrentChannel) {
            setChatMessage(message);
        }

        if (message.isFileMessage() && isCurrentChannel) {
            $('.chat-input-file').removeClass('file-upload');
            $('#chat_file_input').val('');

            if (message.type.match(/^image\/.+$/)) {
                setImageMessage(message);
            } else {
                setFileMessage(message);
            }
        }


    };

    SendMessageHandler = function (message, error) {
        if (error) {
            if (error.code == 900050) {
                setSysMessage({
                    'message': 'This channel is freeze.'
                });
                return;
            } else if (error.code == 900041) {
                setSysMessage({
                    'message': 'You are muted.'
                });
                return;
            }
        }

        // update last message
        if (groupChannelLastMessageList.hasOwnProperty(message.channelUrl)) {
            groupChannelLastMessageList[message.channelUrl] = message;
            updateGroupChannelLastMessage(message);
        }


        if (message.isUserMessage()) {
            setChatMessage(message);
        }

        if (message.isFileMessage()) {
            $('.chat-input-file').removeClass('file-upload');
            $('#chat_file_input').val('');

            if (message.type.match(/^image\/.+$/)) {
                setImageMessage(message);
            } else {
                setFileMessage(message);
            }
        }


    };

    ChannelHandler.onReadReceiptUpdated = function (channel) {
//         console.log('ChannelHandler.onReadReceiptUpdated: ', channel);
        updateChannelMessageCacheAll(channel);
    };

    ChannelHandler.onTypingStatusUpdated = function (channel) {
//         console.log('ChannelHandler.onTypingStatusUpdated: ', channel);

        if (channel == currChannelInfo) {
            showTypingUser(channel);
        }
    };

    sb.addChannelHandler('channel', ChannelHandler);
}

var showTypingUser = function (channel) {
    if (!channel.isGroupChannel()) {
        return;
    }

    if (!channel.isTyping()) {
        $('.chat-input-typing').hide();
        $('.chat-input-typing').html('');
        return;
    }

    var typingUser = channel.getTypingMembers();

    var nicknames = '';
    for (var i in typingUser) {
        var nickname = xssEscape(typingUser[i].nickname);
        nicknames += nickname + ', ';
    }
    if (nicknames.length > 2) {
        nicknames = nicknames.substring(0, nicknames.length - 2);
        $('.chat-input-typing').html('{} typing...'.format(nicknames));
        $('.chat-input-typing').show();
    } else {
        $('.chat-input-typing').hide();
        $('.chat-input-typing').html('');
    }
};


/***********************************************
 *          // END SendBird Settings
 **********************************************/


/***********************************************
 *              Common Function
 **********************************************/
function initChatTitle(title, index) {
    $('.chat-top__title').html(title);
    $('.chat-top__title').show();
}

var scrollPositionBottom = function () {
    var scrollHeight = $('.chat-canvas')[0].scrollHeight;
    $('.chat-canvas')[0].scrollTop = scrollHeight;
    currScrollHeight = scrollHeight;
};

function afterImageLoad(obj) {
    $('.chat-canvas')[0].scrollTop = $('.chat-canvas')[0].scrollTop + obj.height + $('.chat-canvas__list').height();
}

function setChatMessage(message) {
    // console.log(message);
    $('.chat-canvas').append(messageList(message));    
    emojiChatTemp = $(".chat-canvas__list-text");
    for (var i = 0; i < $(".chat-canvas__list-text").length; i++) {
        if(emojiChatTemp.eq(i).text().includes("<img"))
            $(".chat-canvas__list-text").eq(i).html(emojiChatTemp.eq(i).text());
    }
    updateChannelMessageCache(currChannelInfo, message);

    scrollPositionBottom();
}

var PreviousMessageListQuery = null;

function loadMoreChatMessage(func) {
    if (!PreviousMessageListQuery) {
        PreviousMessageListQuery = currChannelInfo.createPreviousMessageListQuery();
    }
    PreviousMessageListQuery.load(30, false, function (messages, error) {
        if (error) {
            return;
        }

        var moreMessage = messages;
        var msgList = '';

        messages.forEach(function (message) {
            switch (message.messageType) {
                case "user":
                    msgList += messageList(message);
                    break;
                case "file":
                    $('.chat-input-file').removeClass('file-upload');
                    $('#chat_file_input').val('');
                    if (message.type.match(/^image\/.+$/)) {
                        msgList += imageMessageList(message);
                    } else {
                        msgList += fileMessageList(message);
                    }
                    break;
                default:
            }
        });
        //   console.log(msgList);
        $('.chat-canvas').prepend(msgList);
        // console.log($('.chat-canvas'));
        $('.chat-canvas')[0].scrollTop = (moreMessage.length * MESSAGE_TEXT_HEIGHT);
        emojiChatTemp = $(".chat-canvas__list-text");
        for (var i = 0; i < $(".chat-canvas__list-text").length; i++) {
            if(emojiChatTemp.eq(i).text().includes("<img"))
                $(".chat-canvas__list-text").eq(i).html(emojiChatTemp.eq(i).text());
        }
        for (var i in messages) {
            var message = messages[i];
            updateChannelMessageCache(currChannelInfo, message);
        }
        $(".loading_history").remove();
        if (func) {
            func();
        }
    });
}

function messageList(message) {
    var msgList = '';
    var user = message.sender;
    var channel = currChannelInfo;

    if (message.isAdminMessage()) {
        // console.log(message);
    } else {
        if (isCurrentUser(user.userId)) {
            var readReceiptHtml = '  <label class="chat-canvas__list-readreceipt"></label>';

            var msg = '' +
                '<div class="chat-canvas__list">' +
                '  <label class="chat-canvas__list-name chat-canvas__list-name__user" data-userid="%userid%">' +
                xssEscape(user.nickname) +
                '  </label>' +
                '  <label class="chat-canvas__list-separator">:</label>' +
                '  <label class="chat-canvas__list-text" data-messageid="%messageid%">%message%</label>' +
                '</div>';
            msg = msg.replace('%message%', convertLinkMessage(xssEscape(message.message)));
            msg = msg.replace('%userid%', user.userId).replace('%messageid%', message.messageId);
            msgList += msg;
        } else {
            var msg = '' +
                '<div class="chat-canvas__list">' +
                '  <label class="chat-canvas__list-name" data-userid="%userid%" data-nickname="%nickname%">' +
                xssEscape(user.nickname) +
                '  </label>' +
                '  <label class="chat-canvas__list-separator">:</label>' +
                '  <label class="chat-canvas__list-text" data-messageid="%messageid%">' +
                convertLinkMessage(xssEscape(message.message)) +
                '  </label>' +
                '</div>';
            msgList += msg.replace('%userid%', user.userId).replace('%nickname%', xssEscape(user.nickname)).replace('%messageid%', message.messageId);
        }
    }

    return msgList;
}

function updateChannelMessageCache(channel, message) {
    var readReceipt = -1;
    if (channel.isGroupChannel()) {
        readReceipt = channel.getReadReceipt(message);
    }
    if (!channelMessageList.hasOwnProperty(channel.url)) {
        channelMessageList[channel.url] = {};
    }

    if (!channelMessageList[channel.url].hasOwnProperty(message.messageId)) {
        channelMessageList[channel.url][message.messageId] = {};
    }

    channelMessageList[channel.url][message.messageId]['message'] = message;

    // if (channel.isGroupChannel() && readReceipt >= 0) {
    //     channelMessageList[channel.url][message.messageId]['readReceipt'] = readReceipt;

    //     var elemString = '.chat-canvas__list-text[data-messageid=' + message.messageId + ']';
    //     var elem = $(elemString).next();
    //     if (readReceipt == 0) {
    //         elem.html('').hide();
    //     } else {
    //         elem.html(readReceipt);
    //         if (!elem.is(':visible')) {
    //             elem.show();
    //         }
    //     }
    // } else {
    //     return;
    // }
}

function updateChannelMessageCacheAll(channel) {
    for (var i in channelMessageList[channel.url]) {
        var message = channelMessageList[channel.url][i]['message'];
        updateChannelMessageCache(channel, message);
    }
}

function fileMessageList(message) {
    var msgList = '';
    var user = message.sender;
    if (isCurrentUser(user.userId)) {
        msgList += '' +
            '<div class="chat-canvas__list">' +
            '  <label class="chat-canvas__list-name chat-canvas__list-name__user">' +
            xssEscape(user.nickname) +
            '  </label>' +
            '  <label class="chat-canvas__list-separator">:</label>' +
            '  <label class="chat-canvas__list-text" data-messageid="%messageid%">'.replace('%messageid%', message.messageId) +
            '    <label class="chat-canvas__list-text-file">FILE</label>' +
            '    <a href="' + xssEscape(message.url) + '" target="_blank">' + xssEscape(message.name) + '</a>' +
            '  </label>' +
            '</div>';
    } else {
        msgList += '' +
            '<div class="chat-canvas__list">' +
            '  <label class="chat-canvas__list-name" data-userid="%userid%" data-nickname="%nickname%">'.replace('%userid%', user.userId).replace('%nickname%', xssEscape(user.nickname)) +
            xssEscape(user.nickname) +
            '  </label>' +
            '  <label class="chat-canvas__list-separator">:</label>' +
            '  <label class="chat-canvas__list-text" data-messageid="%messageid%">'.replace('%messageid%', message.messageId) +
            '    <label class="chat-canvas__list-text-file">FILE</label>' +
            '    <a href="' + xssEscape(message.url) + '" target="_blank">' + xssEscape(message.name) + '</a>' +
            '  </label>' +
            '</div>';
    }

    // var channel = currChannelInfo;
    // updateChannelMessageCache(channel, message);

    return msgList;
}

function imageMessageList(obj) {
    var msgList = '';
    var message = obj;
    var user = message.sender;
    // console.log(message.url);
    if (isCurrentUser(user.userId)) {
        msgList += '' +
            '<div class="chat-canvas__list">' +
            '  <label class="chat-canvas__list-name chat-canvas__list-name__user">' +
            xssEscape(user.nickname) +
            '  </label>' +
            '  <label class="chat-canvas__list-separator">:</label>' +
            '  <label class="chat-canvas__list-text"  onclick="window.open(\'' + xssEscape(message.url) + '\', \'_blank\');" data-messageid="%messageid%">'.replace('%messageid%', message.messageId) +
            xssEscape(message.name) +
            '  </label>' +
            '  <div class="chat-canvas__list-file" onclick="window.open(\'' + xssEscape(message.url) + '\', \'_blank\');">' +
            '<img src="' + xssEscape(message.url) + '" class="chat-canvas__list-file-img" onload="afterImageLoad(this)">' +
            '  </div>' +
            '</div>';
    } else {
        msgList += '' +
            '<div class="chat-canvas__list">' +
            '  <label class="chat-canvas__list-name" data-userid="%userid%" data-nickname="%nickname%">'.replace('%userid%', user.userId).replace('%nickname%', xssEscape(user.nickname)) +
            xssEscape(user.nickname) +
            '  </label>' +
            '  <label class="chat-canvas__list-separator">:</label>' +
            '  <label class="chat-canvas__list-text"  onclick="window.open(\'' + xssEscape(message.url) + '\', \'_blank\');" data-messageid="%messageid%">'.replace('%messageid%', message.messageId) +
            xssEscape(message.name) +
            '  </label>' +
            '  <div class="chat-canvas__list-file" onclick="window.open(\'' + xssEscape(message.url) + '\', \'_blank\');">' +
            '<img src="' + xssEscape(message.url) + '" class="chat-canvas__list-file-img" onload="afterImageLoad(this)">' +
            '  </div>' +
            '</div>';
    }
    // console.log(msgList);
    return msgList;
}



// $('.chat-input-text__field').keypress(function (event) {
//     if (event.keyCode == 13 && !event.shiftKey) {
//         event.preventDefault();
//         if (!$.trim(this.value).isEmpty()) {
//             event.preventDefault();
//             this.value = $.trim(this.value);

//             currChannelInfo.sendUserMessage($.trim(this.value), '', SendMessageHandler);

//             scrollPositionBottom();
//         }
//         this.value = "";
//     } else {
//         if (!$.trim(this.value).isEmpty()) {
//             if (!currChannelInfo.isOpenChannel()) {
//                 currChannelInfo.startTyping();
//             }
//         }
//     }
// });
$("#attachmend_file").click(function () {
    $("#chat_file_input").click();
})
$('#chat_file_input').change(function () {
    if ($('#chat_file_input').val().trim().length == 0) return;
    var file = $('#chat_file_input')[0].files[0];
    $('.chat-input-file').addClass('file-upload');

    currChannelInfo.sendFileMessage(file, SendMessageHandler);
});

function setImageMessage(obj) {
    $('.chat-canvas').append(imageMessageList(obj));
    scrollPositionBottom();
}

function setFileMessage(obj) {
    $('.chat-canvas').append(fileMessageList(obj));
    scrollPositionBottom();
}

$('.chat-canvas').on('scroll', function () {

    setTimeout(function () {
        var currHeight = $('.chat-canvas').scrollTop();
        if (currHeight == 0) {
            if ($('.chat-canvas')[0].scrollHeight > $('.chat-canvas').height()) {
                if($(".loading_history").length == 0){
                    $('.chat-canvas').append('<p class="loading_history">Loading History...</p>');
                }
                loadMoreChatMessage();
            }
        }
    }, 200);
});

function setSysMessage(obj) {
    $('.chat-canvas').append(
        '<div class="chat-canvas__list-notice">' +
        '  <label class="chat-canvas__list-system">' +
        xssEscape(obj['message']) +
        '  </label>' +
        '</div>'
    );
    scrollPositionBottom();
}



function unreadCountUpdate(channel) {
    var targetUrl = channel.url;

    var callAdd = true;
    var unread = channel.unreadMessageCount > 9 ? '9+' : channel.unreadMessageCount;
    if (unread > 0 || unread == '9+') {
        $.each($('.left-nav-channel'), function (index, item) {
            if ($(item).data("channel-url") == targetUrl) {
                addUnreadCount(item, unread, targetUrl);
                callAdd = false;
            }
        });

        if (callAdd) {
            showChannel(channel, unread, targetUrl);
        }
    } else {
        showChannel(channel, unread, targetUrl);
    }
}

function addUnreadCount(item, unread, targetUrl) {
    if (targetUrl == currChannelUrl) {
        if (document.hasFocus()) {
            return;
        }
    }

    if ($(item).find('div[class="left-nav-channel__unread"]').length != 0) {
        $(item).find('div[class="left-nav-channel__unread"]').html(unread);
    } else {
        $('<div class="left-nav-channel__unread">' + unread + '</div>')
            .prependTo('.left-nav-channel-group[data-channel-url=' + targetUrl + ']');
    }
}

function showChannel(channel, unread, targetUrl) {
    var members = channel.members;
    var channelMemberList = '';
    $.each(members, function (index, member) {
        if (currentUser.userId != member.userId) {
            channelMemberList += xssEscape(member.nickname) + ', ';
        }
    });
    channelMemberList = channelMemberList.slice(0, -2);
    // addGroupChannel(true, channelMemberList, channel);

    if (unread != 0) {
        $.each($('.left-nav-channel'), function (index, item) {
            if ($(item).data("channel-url") == targetUrl) {
                addUnreadCount(item, unread, targetUrl);
            }
        });
    }
}
/***********************************************
 *          // END Common Function
 **********************************************/



function init() {
    userId = $("#userPrid").val();
    nickname = $("#staffname").val();
    // userId = checkUserId(userId);
    // nickname = decodeURI(decodeURIComponent(getUrlVars()['nickname']));
    // nickname = "TTT";

    $('.init-check').show();
    startSendBird(userId, nickname);
    $('.left-nav-user-nickname').html(xssEscape(nickname));

}

$(document).ready(function () {
    waitOn();
    notifyMe();
    init();
});

window.onfocus = function () {
    // $(".message__unread").remove(); 
    $.ajax({
        type: 'GET',
       url: 'https://api-F742C54C-036E-4DD0-B74D-E59E38ED7B30.sendbird.com/v3/users/'+userId.toString()+'/unread_message_count',
        headers: { "Api-Token": "42b4f9494342b49f4334e0f8a9a1a65a5a969a6f" },
        success: function (r) {
            totalUnreadMessage = r.unread_count;
            if(($(".message__unread").length == 0) && (totalUnreadMessage!=0))
                $('<div class="message__unread">' + totalUnreadMessage + '</div>').prependTo('.message');
            else
               $(".message__unread").html(totalUnreadMessage);  
               if(totalUnreadMessage == 0)
               $(".message__unread").remove();

        },
        error: function (r) {
        },
        //timeout: 10000
        });


    if (currChannelInfo && !currChannelInfo.isOpenChannel()) {
        currChannelInfo.markAsRead();
    }
    $.each($('.left-nav-channel'), function (index, item) {
        if ($(item).data("channel-url") == currChannelUrl) {
            $(item).find('div[class="left-nav-channel__unread"]').remove();
        }
    });
};

function popupInit() {
    $('.modal-member').hide();
    $('.chat-top__button-member').removeClass('chat-top__button-member--active');
    $('.modal-invite').hide();
    $('.chat-top__button-invite').removeClass('chat-top__button-invite--active');
  }