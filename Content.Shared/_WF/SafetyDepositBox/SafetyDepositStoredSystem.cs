using Content.Shared._WF.SafetyDepositBox.Components;
using Content.Shared.Examine;
using Robust.Shared.Utility;

namespace Content.Shared._WF.SafetyDepositBox;

public sealed class SafetyDepositStoredSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SafetyDepositStoredComponent, ExaminedEvent>(OnStoredExamined);
        SubscribeLocalEvent<SafetyDepositBoxComponent, ExaminedEvent>(OnBoxExamined);
    }

    private void OnStoredExamined(Entity<SafetyDepositStoredComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("safety-deposit-stored-examine"));
    }

    private void OnBoxExamined(Entity<SafetyDepositBoxComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.BoxId.HasValue)
        {
            var shortId = ent.Comp.BoxId.Value.ToString()[..8];
            args.PushMarkup(Loc.GetString("safety-deposit-box-examine-id", ("id", shortId)));
        }

        if (!string.IsNullOrEmpty(ent.Comp.OwnerName))
        {
            args.PushMarkup(Loc.GetString("safety-deposit-box-examine-owner", ("owner", ent.Comp.OwnerName)));
        }
    }
}
