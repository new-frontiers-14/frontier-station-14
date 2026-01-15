using System.Linq;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Interaction.Events;
using Content.Shared._Starlight.Language;
using Content.Shared._Starlight.Language.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Starlight.Language;

public sealed class DebugLanguageChatterSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DebugLanguageChatterComponent, UseInHandEvent>(OnUse);
    }

    private void OnUse(Entity<DebugLanguageChatterComponent> ent, ref UseInHandEvent args)
    {
        if (ent.Comp.Busy)
            return;

        var user = args.User;
        if (user == EntityUid.Invalid)
            return;

        var languages = _prototypeManager.EnumeratePrototypes<LanguagePrototype>()
            .Where(lang => ent.Comp.IncludeUniversal || lang.ID != SharedLanguageSystem.UniversalPrototype)
            .ToList();

        if (languages.Count == 0)
            return;

        ent.Comp.Busy = true;
        Dirty(ent);

        var delay = TimeSpan.FromSeconds(ent.Comp.IntervalSeconds);
        for (var i = 0; i < languages.Count; i++)
        {
            var language = languages[i];
            Timer.Spawn(delay * i, () =>
            {
                if (!Exists(user))
                    return;

                var message = Loc.GetString(ent.Comp.MessageLocId, ("language", language.ID));
                _chat.TrySendInGameICMessage(user, message, InGameICChatType.Speak, ChatTransmitRange.Normal, languageOverride: language);
            });
        }

        Timer.Spawn(delay * languages.Count, () =>
        {
            if (!Exists(ent))
                return;

            ent.Comp.Busy = false;
            Dirty(ent);
        });
    }
}
