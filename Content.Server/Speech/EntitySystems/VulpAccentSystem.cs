using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Server.VulpTranslator;
using Content.Shared.Inventory;
using Content.Shared.Hands.EntitySystems;
using Content.Server.PowerCell;
using System.Text;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class VulpAccentSystem : EntitySystem
{

    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly IReadOnlyList<string> Syllables = new List<string>{
        "rur","ya","cen","rawr","bar","kuk","tek","qat","uk","wu","vuh","tah","tch","schz","auch","ist","ein","entch","zwichs","tut","mir","wo","bis","es","vor","nic","gro","lll","enem","zandt","tzch","noch","hel","ischt","far","wa","baram","iereng","tech","lach","sam","mak","lich","gen","or","ag","eck","gec","stag","onn","bin","ket","jarl","vulf","einech","cresthz","azunein","ghzth"
    }.AsReadOnly();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VulpAccentComponent, AccentGetEvent>(OnAccent);
    }

    private bool CheckItem(EntityUid uid)
    {
        if (TryComp<VulpTranslatorComponent>(uid, out var device))
        {
            if (_cell.TryUseActivatableCharge(uid))
            {
                return true;
            }
        }
        return false;
    }

    public bool FindTranslator(EntityUid uid)
    {
        foreach (var item in _handsSystem.EnumerateHeld(uid))
        {
            if (CheckItem(item))
            {
                return true;
            }
        }

        if (_inventorySystem.TryGetSlotEntity(uid, "pocket1", out var item2))
            {
                if (item2 is { Valid : true } stationUid && CheckItem(stationUid))
                {
                    return true;
                }
            }
        else if (_inventorySystem.TryGetSlotEntity(uid, "pocket2", out var item3))
            {
                if (item3 is { Valid : true } stationUid && CheckItem(stationUid))
                {
                    return true;
                }
            }

        return false;
    }

    public string Accentuate(string message, VulpAccentComponent component, EntityUid uid)
    {
        var msg = message;

        if (!FindTranslator(uid))
        {
            var words = message.Split();
            var accentedMessage = new StringBuilder(message.Length + 2);
            for (var i = 0; i < words.Length; i++)
            {
                accentedMessage.Append(_random.Pick(Syllables));
                if (i < words.Length - 1)
                    accentedMessage.Append(' ');
            }
            accentedMessage.Append('.');
            msg = accentedMessage.ToString();
        }

        return msg;
    }

    private void OnAccent(EntityUid uid, VulpAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component, uid);
    }
}
