using System.Linq;
using System.Text.RegularExpressions;
using Content.Client.CharacterInfo;
using Content.Shared.CCVar;
using Content.Shared._DV.CCVars;
using Content.Shared.Dataset;
using Content.Shared.Chat;
using Content.Shared.Chat.TypingIndicator;
using Robust.Shared.Prototypes;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using static Content.Client.CharacterInfo.CharacterInfoSystem;

namespace Content.Client.UserInterface.Systems.Chat;

public sealed partial class ChatUIController : IOnSystemChanged<CharacterInfoSystem>
{
    public ChatSelectChannel CurrentChannel = ChatSelectChannel.None;
    private static readonly ProtoId<TypingIndicatorPrototype> WhisperID = "whisper";
    private static readonly ProtoId<TypingIndicatorPrototype> EmoteID = "emote";
    private static readonly ProtoId<TypingIndicatorPrototype> OocID = "ooc";
    private static readonly ProtoId<TypingIndicatorPrototype> RadioID = "radio";

    /// <summary>
    ///     Notifies and sets what type of typing indicator should be put.
    /// </summary>
    public void NotifySpecificChatTextChange(ChatSelectChannel selectedChannel)
    {
        var channel = CurrentChannel;
        if (CurrentChannel == ChatSelectChannel.None)
            channel = selectedChannel;

        switch (channel)
        {
            case ChatSelectChannel.Whisper:
                _typingIndicator?.ClientAlternateTyping(WhisperID);
                break;

            case ChatSelectChannel.Radio:
                _typingIndicator?.ClientAlternateTyping(RadioID);
                break;

            case ChatSelectChannel.Emotes:
                _typingIndicator?.ClientAlternateTyping(EmoteID);
                break;

            case ChatSelectChannel.LOOC:
            case ChatSelectChannel.OOC:
                _typingIndicator?.ClientAlternateTyping(OocID);
                break;

            default:
                _typingIndicator?.ClientChangedChatText();
                break;
        }
    }
}
