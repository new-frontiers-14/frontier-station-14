using Content.Client.Items;
using Content.Client._NF.Bodycam.Components;
using Content.Shared._NF.Bodycam;
using Content.Shared.Toggleable;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client._NF.Bodycam;

public sealed class BodycamSystem : SharedBodycamSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodycamComponent, ItemStatusCollectMessage>(OnGetStatusControl);
        SubscribeLocalEvent<BodycamComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private static void OnGetStatusControl(EntityUid uid, BodycamComponent component, ItemStatusCollectMessage args)
    {
        args.Controls.Add(new BodycamStatus(component));
    }

    private void OnAppearanceChange(EntityUid uid, BodycamComponent? component, ref AppearanceChangeEvent args)
    {
        if (!Resolve(uid, ref component))
        {
            return;
        }

        if (!_appearance.TryGetData<bool>(uid, ToggleableLightVisuals.Enabled, out var enabled, args.Component))
        {
            return;
        }
    }
}
