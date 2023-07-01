using Content.Server.Chemistry.EntitySystems;
using Content.Server.Popups;
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
            if (_solutionSystem.TryGetSolution(uid, component.SolutionName, out var solution))
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
            foreach (var autoComp in EntityQuery<AutoRegenReagentComponent>())
            {
                if (autoComp.Solution == null)
                    return;
                autoComp.Accumulator += frameTime;
                if (autoComp.Accumulator < 1f)
                    continue;
                autoComp.Accumulator -= 1f;

                _solutionSystem.TryAddReagent(autoComp.Owner, autoComp.Solution, autoComp.CurrentReagent, autoComp.unitsPerSecond, out var accepted);
            }
        }
    }
}
