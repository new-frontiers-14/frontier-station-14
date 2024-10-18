using Content.Shared.Interaction;
using Content.Shared.Books;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Server.Books
{
    public sealed class BookSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HyperlinkBookComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<HyperlinkBookComponent, GetVerbsEvent<AlternativeVerb>>(AddAltVerb);
        }

        private void OnActivate(EntityUid uid, HyperlinkBookComponent component, ActivateInWorldEvent args)
        {
            if (!TryComp<ActorComponent>(args.User, out var actor))
                return;

            OpenURL(actor.PlayerSession, component.URL);
        }

        private void AddAltVerb(EntityUid uid, HyperlinkBookComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (!TryComp<ActorComponent>(args.User, out var actor))
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    OpenURL(actor.PlayerSession, component.URL);
                },
                Text = Loc.GetString("book-read-verb"),
                Priority = -2
            };
            args.Verbs.Add(verb);
        }

        public void OpenURL(ICommonSession session, string url)
        {
            var ev = new OpenURLEvent(url);
            RaiseNetworkEvent(ev, session.Channel);
        }
    }
}
