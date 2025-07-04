using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Eft.Ws;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Servers.Ws;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using LogLevel = SPTarkov.Server.Core.Models.Spt.Logging.LogLevel;

namespace SPTarkov.Server.Core.Helpers;

[Injectable]
public class NotificationSendHelper(
    ISptLogger<NotificationSendHelper> _logger,
    SptWebSocketConnectionHandler _sptWebSocketConnectionHandler,
    SaveServer _saveServer,
    NotificationService _notificationService,
    TimeUtil _timeUtil,
    JsonUtil _jsonUtil
)
{
    /// <summary>
    ///     Send notification message to the appropriate channel
    /// </summary>
    /// <param name="sessionID">Session/player id</param>
    /// <param name="notificationMessage"></param>
    public void SendMessage(string sessionID, WsNotificationEvent notificationMessage)
    {
        if (_logger.IsLogEnabled(LogLevel.Debug))
        {
            _logger.Debug(
                $"Send message for {sessionID} started, message: {_jsonUtil.Serialize(notificationMessage)}"
            );
        }
        if (_sptWebSocketConnectionHandler.IsWebSocketConnected(sessionID))
        {
            if (_logger.IsLogEnabled(LogLevel.Debug))
            {
                _logger.Debug(
                    $"Send message for {sessionID} websocket available, message being sent"
                );
            }
            _sptWebSocketConnectionHandler.SendMessage(sessionID, notificationMessage);
        }
        else
        {
            if (_logger.IsLogEnabled(LogLevel.Debug))
            {
                _logger.Debug(
                    $"Send message for {sessionID} websocket not available, queuing into profile"
                );
            }
            _notificationService.Add(sessionID, notificationMessage);
        }
    }

    /// <summary>
    ///     Send a message directly to the player
    /// </summary>
    /// <param name="sessionId">Session id</param>
    /// <param name="senderDetails">Who is sending the message to player</param>
    /// <param name="messageText">Text to send player</param>
    /// <param name="messageType">Underlying type of message being sent</param>
    public void SendMessageToPlayer(
        string sessionId,
        UserDialogInfo senderDetails,
        string messageText,
        MessageType messageType
    )
    {
        var dialog = GetDialog(sessionId, messageType, senderDetails);

        dialog.New += 1;
        var message = new Message
        {
            Id = new MongoId(),
            UserId = dialog.Id,
            MessageType = messageType,
            DateTime = _timeUtil.GetTimeStamp(),
            Text = messageText,
            HasRewards = null,
            RewardCollected = null,
            Items = null,
        };
        dialog.Messages.Add(message);

        var notification = new WsChatMessageReceived
        {
            EventType = NotificationEventType.new_message,
            EventIdentifier = message.Id,
            DialogId = message.UserId,
            Message = message,
        };
        SendMessage(sessionId, notification);
    }

    /// <summary>
    ///     Helper function for SendMessageToPlayer(), get new dialog for storage in profile or find existing by sender id
    /// </summary>
    /// <param name="sessionId">Session id</param>
    /// <param name="messageType">Type of message to generate</param>
    /// <param name="senderDetails">Who is sending the message</param>
    /// <returns>Dialogue</returns>
    protected Models.Eft.Profile.Dialogue GetDialog(
        string sessionId,
        MessageType messageType,
        UserDialogInfo senderDetails
    )
    {
        // Use trader id if sender is trader, otherwise use nickname
        var dialogKey = senderDetails.Id;

        // Get all dialogs with pmcs/traders player has
        var dialogueData = _saveServer.GetProfile(sessionId).DialogueRecords;

        // Ensure empty dialog exists based on sender details passed in
        dialogueData.TryAdd(
            dialogKey,
            GetEmptyDialogTemplate(dialogKey, messageType, senderDetails)
        );

        return dialogueData[dialogKey];
    }

    protected Models.Eft.Profile.Dialogue GetEmptyDialogTemplate(
        string dialogKey,
        MessageType messageType,
        UserDialogInfo senderDetails
    )
    {
        return new Models.Eft.Profile.Dialogue
        {
            Id = dialogKey,
            Type = messageType,
            Messages = [],
            Pinned = false,
            New = 0,
            AttachmentsNew = 0,
            Users =
                senderDetails.Info.MemberCategory == MemberCategory.Trader ? null : [senderDetails],
        };
    }
}
