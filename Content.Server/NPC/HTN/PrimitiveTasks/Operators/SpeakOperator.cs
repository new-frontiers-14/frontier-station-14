using Content.Server.Chat.Systems;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class SpeakOperator : HTNOperator
{
    private ChatSystem _chat = default!;

    [DataField("speech", required: true)]
    public string Speech = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _chat = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ChatSystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var speaker = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        _chat.TrySendInGameICMessage(speaker, Loc.GetString(Speech), InGameICChatType.Speak, false);
        return base.Update(blackboard, frameTime);
    }
}
