using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Chemistry.Components;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Server.Chemistry.AutoRegenReagent
{
    public sealed class AutoRegenReagentSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly PopupSystem _popups = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AutoRegenReagentComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<AutoRegenReagentComponent, GetVerbsEvent<AlternativeVerb>>(AddSwitchVerb);
        }

        private void OnInit(EntityUid uid, AutoRegenReagentComponent component, ComponentInit args)
        {
            if (component.SolutionName == null)
                return;
            if (_solutionSystem.TryGetSolution(uid, component.SolutionName, out var _, out var solution))
                component.Solution = solution;
            component.CurrentReagent = component.Reagents[component.CurrentIndex];
        }

        private void AddSwitchVerb(EntityUid uid, AutoRegenReagentComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            if (component.Reagents.Count <= 1)
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    SwitchReagent(component, args.User);
                },
                Text = Loc.GetString("autoreagent-switch"),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }


        private string SwitchReagent(AutoRegenReagentComponent component, EntityUid user)
        {
            if (component.CurrentIndex + 1 == component.Reagents.Count)
                component.CurrentIndex = 0;
            else
                component.CurrentIndex++;

            if (component.Solution != null)
                component.Solution.ScaleSolution(0);

            component.CurrentReagent = component.Reagents[component.CurrentIndex];

            _popups.PopupEntity(Loc.GetString("autoregen-switched", ("reagent", component.CurrentReagent)), user, user);

            return component.CurrentReagent;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<AutoRegenReagentComponent, SolutionComponent>();

            while (query.MoveNext(out var solution, out var regenComp, out var solutionComp))
            {
                regenComp.Accumulator += frameTime;
                if (regenComp.Accumulator < 1f)
                    continue;
                regenComp.Accumulator -= 1f;
                _solutionSystem.TryAddReagent((solution, solutionComp), regenComp.CurrentReagent,
                    regenComp.unitsPerSecond);
            }
        }
    }
}
