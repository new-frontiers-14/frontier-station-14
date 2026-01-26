// Copyright 2025 - 2025, GnKonkort and the Expedition 14 contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Verbs;
using Content.Shared._CitadelStation.ERP.Examination.Components;
using Robust.Shared.Utility;
using Content.Shared.Examine;

namespace Content.Shared._CitadelStation.ERP.Examination.Systems;


public sealed class ERPExaminationSystem : EntitySystem {

    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        Logger.Debug("AAAAA");
        SubscribeLocalEvent<ERPExaminableComponent, GetVerbsEvent<ExamineVerb>>(AddExamineVerb);
    }

    private void AddExamineVerb(EntityUid uid, ERPExaminableComponent component, GetVerbsEvent<ExamineVerb> ev)
    {
        //throw new NotImplementedException();
        //if (!TryComp<DamageableComponent>(uid, out var damage))
        //    return;
        //Logger.Debug("AAAAA");
        var detailsRange = _examineSystem.IsInDetailsRange(ev.User, uid);

        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                var markup = CreateMarkup(uid, component);
                _examineSystem.SendExamineTooltip(ev.User, uid, markup, false, false);
            },
            Text = Loc.GetString("ERP Status"),
            Category = VerbCategory.Examine,
            Disabled = !detailsRange,
            Message = detailsRange ? null : Loc.GetString(""),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/insert.svg.192dpi.png"))
        };

        ev.Verbs.Add(verb);
    }

    private FormattedMessage CreateMarkup(EntityUid uid, ERPExaminableComponent component)
    {
        //throw new NotImplementedException();
        var msg = new FormattedMessage();
        string chosenNewString = string.Empty;

        switch (component.ERPAccessLevel) {
            case ERPStatus.Prohibited:
                chosenNewString = Loc.GetString($"erp-status-prohibited");
                break;
            case ERPStatus.Partial:
                chosenNewString = Loc.GetString($"erp-status-partial");
                break;
            case ERPStatus.Complete:
                chosenNewString = Loc.GetString($"erp-status-complete");
                break;
            case ERPStatus.Completer:
                chosenNewString = Loc.GetString($"erp-status-completer");
                break;
        }

        msg.AddMarkupOrThrow(chosenNewString);

        return msg;
    }
};
