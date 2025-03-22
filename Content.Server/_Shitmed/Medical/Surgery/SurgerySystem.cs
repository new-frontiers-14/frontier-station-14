using Content.Server.Atmos.Rotting;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Server.Popups;
using Content.Shared.Bed.Sleep;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared._Shitmed.Medical.Surgery;
using Content.Shared._Shitmed.Medical.Surgery.Conditions;
using Content.Shared._Shitmed.Medical.Surgery.Effects.Step;
using Content.Shared._Shitmed.Medical.Surgery.Steps;
using Content.Shared._Shitmed.Medical.Surgery.Steps.Parts;
using Content.Shared._Shitmed.Medical.Surgery.Tools;
using Content.Shared.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server._Shitmed.Medical.Surgery;

public sealed class SurgerySystem : SharedSurgerySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly RottingSystem _rot = default!;
    [Dependency] private readonly BlindableSystem _blindableSystem = default!;

    private readonly List<EntProtoId> _surgeries = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryToolComponent, AfterInteractEvent>(OnToolAfterInteract);
        SubscribeLocalEvent<SurgeryTargetComponent, SurgeryStepDamageEvent>(OnSurgeryStepDamage);
        // You might be wondering "why aren't we using StepEvent for these two?" reason being that StepEvent fires off regardless of success on the previous functions
        // so this would heal entities even if you had a used or incorrect organ.
        SubscribeLocalEvent<SurgerySpecialDamageChangeEffectComponent, SurgeryStepDamageChangeEvent>(OnSurgerySpecialDamageChange);
        SubscribeLocalEvent<SurgeryDamageChangeEffectComponent, SurgeryStepDamageChangeEvent>(OnSurgeryDamageChange);
        SubscribeLocalEvent<SurgeryStepEmoteEffectComponent, SurgeryStepEvent>(OnStepScreamComplete);
        SubscribeLocalEvent<SurgeryStepSpawnEffectComponent, SurgeryStepEvent>(OnStepSpawnComplete);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        LoadPrototypes();
    }

    protected override void RefreshUI(EntityUid body)
    {
        var surgeries = new Dictionary<NetEntity, List<EntProtoId>>();
        foreach (var surgery in _surgeries)
        {
            if (GetSingleton(surgery) is not { } surgeryEnt)
                continue;

            foreach (var part in _body.GetBodyChildren(body))
            {
                var ev = new SurgeryValidEvent(body, part.Id);
                RaiseLocalEvent(surgeryEnt, ref ev);

                if (ev.Cancelled)
                    continue;

                surgeries.GetOrNew(GetNetEntity(part.Id)).Add(surgery);
            }

        }
        _ui.SetUiState(body, SurgeryUIKey.Key, new SurgeryBuiState(surgeries));
        /*
            Reason we do this is because when applying a BUI State, it rolls back the state on the entity temporarily,
            which just so happens to occur right as we're checking for step completion, so we end up with the UI
            not updating at all until you change tools or reopen the window. I love shitcode.
        */
        _ui.ServerSendUiMessage(body, SurgeryUIKey.Key, new SurgeryBuiRefreshMessage());
    }
    private void SetDamage(EntityUid body,
        DamageSpecifier damage,
        float partMultiplier,
        EntityUid user,
        EntityUid part)
    {
        if (!TryComp<BodyPartComponent>(part, out var partComp))
            return;

        _damageable.TryChangeDamage(body,
            damage,
            true,
            origin: user,
            canSever: false,
            partMultiplier: partMultiplier,
            targetPart: _body.GetTargetBodyPart(partComp));
    }

    private void OnToolAfterInteract(Entity<SurgeryToolComponent> ent, ref AfterInteractEvent args)
    {
        var user = args.User;
        if (args.Handled
            || !args.CanReach
            || args.Target == null
            || !HasComp<SurgeryTargetComponent>(args.Target)
            || !TryComp<SurgeryTargetComponent>(args.User, out var surgery)
            || !surgery.CanOperate
            || !IsLyingDown(args.Target.Value, args.User))
        {
            return;
        }

        if (user == args.Target && !_config.GetCVar(CCVars.CanOperateOnSelf))
        {
            _popup.PopupEntity(Loc.GetString("surgery-error-self-surgery"), user, user);
            return;
        }

        args.Handled = true;
        _ui.OpenUi(args.Target.Value, SurgeryUIKey.Key, user);
        RefreshUI(args.Target.Value);
    }

    private void OnSurgeryStepDamage(Entity<SurgeryTargetComponent> ent, ref SurgeryStepDamageEvent args) =>
        SetDamage(args.Body, args.Damage, args.PartMultiplier, args.User, args.Part);

    private void OnSurgeryDamageChange(Entity<SurgeryDamageChangeEffectComponent> ent, ref SurgeryStepDamageChangeEvent args)
    {
        var damageChange = ent.Comp.Damage;
        if (HasComp<ForcedSleepingComponent>(args.Body))
            damageChange = damageChange * ent.Comp.SleepModifier;

        SetDamage(args.Body, damageChange, 0.5f, args.User, args.Part);
    }

    private void OnSurgerySpecialDamageChange(Entity<SurgerySpecialDamageChangeEffectComponent> ent, ref SurgeryStepDamageChangeEvent args)
    {
        if (ent.Comp.DamageType == "Rot")
            _rot.ReduceAccumulator(args.Body, TimeSpan.FromSeconds(2147483648)); // BEHOLD, SHITCODE THAT I JUST COPY PASTED. I'll redo it at some point, pinky swear :)
        else if (ent.Comp.DamageType == "Eye"
            && TryComp(ent, out BlindableComponent? blindComp)
            && blindComp.EyeDamage > 0)
            _blindableSystem.AdjustEyeDamage((args.Body, blindComp), -blindComp!.EyeDamage);
    }

    private void OnStepScreamComplete(Entity<SurgeryStepEmoteEffectComponent> ent, ref SurgeryStepEvent args)
    {
        if (HasComp<ForcedSleepingComponent>(args.Body))
            return;

        _chat.TryEmoteWithChat(args.Body, ent.Comp.Emote);
    }
    private void OnStepSpawnComplete(Entity<SurgeryStepSpawnEffectComponent> ent, ref SurgeryStepEvent args) =>
        SpawnAtPosition(ent.Comp.Entity, Transform(args.Body).Coordinates);

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<EntityPrototype>())
            return;

        LoadPrototypes();
    }

    private void LoadPrototypes()
    {
        _surgeries.Clear();
        foreach (var entity in _prototypes.EnumeratePrototypes<EntityPrototype>())
            if (entity.HasComponent<SurgeryComponent>())
                _surgeries.Add(new EntProtoId(entity.ID));
    }
}
