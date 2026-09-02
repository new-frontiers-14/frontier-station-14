using Robust.Shared.Configuration;

namespace Content.Shared._EE.CCVars;

[CVarDefs]
public sealed partial class EECVars
{
    /// <summary>
    ///     How many lines back in the chat log to look for collapsing repeated messages into one.
    /// </summary>
    public static readonly CVarDef<int> ChatStackLastLines =
        CVarDef.Create("chat.chatstack_last_lines", 1, CVar.CLIENTONLY | CVar.ARCHIVE, "How far into the chat history to look when looking for similiar messages to coalesce them.");
}
