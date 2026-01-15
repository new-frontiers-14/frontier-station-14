using Content.Shared.Eye.Blinding.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Shared.Starlight.Overlay;

namespace Content.Client._Starlight.Overlay;

public sealed class NightVisionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly TransformSystem _xformSys = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly FlashImmunitySystem _flashImmunity = default!;

    private NightVisionOverlay _overlay = default!;
    [ViewVariables]
    private EntityUid? _effect;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightVisionComponent, ComponentInit>(OnVisionInit);
        SubscribeLocalEvent<NightVisionComponent, ComponentShutdown>(OnVisionShutdown);

        SubscribeLocalEvent<NightVisionComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<NightVisionComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<NightVisionComponent, FlashImmunityCheckEvent>(OnFlashImmunityChanged);

        _overlay = new(_prototypeManager.Index<ShaderPrototype>("ModernNightVisionShader"));
    }

    private void OnFlashImmunityChanged(Entity<NightVisionComponent> ent, ref FlashImmunityCheckEvent args)
    {
        if (args.IsImmune)
            AttemptRemoveVision(ent.Owner);
        else
            AttemptAddVision(ent.Owner);
    }

    private void OnPlayerAttached(Entity<NightVisionComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        AttemptAddVision(ent.Owner);
    }

    private void OnPlayerDetached(Entity<NightVisionComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        AttemptRemoveVision(ent.Owner, true);
    }

    private void OnVisionInit(Entity<NightVisionComponent> ent, ref ComponentInit args)
    {
        AttemptAddVision(ent.Owner);
    }

    private void OnVisionShutdown(Entity<NightVisionComponent> ent, ref ComponentShutdown args)
    {
        AttemptRemoveVision(ent.Owner);
    }

    private void AttemptAddVision(EntityUid uid)
    {
        if (_player.LocalSession?.AttachedEntity != uid)
            return;

        if (_flashImmunity.HasFlashImmunityVisionBlockers(uid))
            return;

        if (!TryComp<NightVisionComponent>(uid, out var nightVision) || !nightVision.Active)
            return;

        if (_effect != null)
            return;

        _overlayMan.AddOverlay(_overlay);
        _effect = SpawnAttachedTo(nightVision.EffectPrototype, Transform(uid).Coordinates);
        _xformSys.SetParent(_effect.Value, uid);
    }

    private void AttemptRemoveVision(EntityUid uid, bool force = false)
    {
        if (_player.LocalSession?.AttachedEntity != uid && !force)
            return;

        _overlayMan.RemoveOverlay(_overlay);
        Del(_effect);
        _effect = null;
    }
}
