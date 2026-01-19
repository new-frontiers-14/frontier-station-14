using Content.Shared._NF.Shuttles.Components;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._NF.Shuttles.Systems;
public sealed partial class IFFSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedShuttleSystem _shuttle = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var iffFlashColorsQuery = EntityQueryEnumerator<IFFFlashingColorsComponent>();
        while (iffFlashColorsQuery.MoveNext(out var ent, out var iffFlashComp))
        {
            if (!iffFlashComp.IsActive)
            {
                continue;
            }

            var index = iffFlashComp.CurrentIFFColorIndex;
            if (iffFlashComp.CurrentIFFFlashingSequence is not IFFFlashingColorsSequencePrototype currentSequence)
            {
                StopIFFFlashSequnce(ent, iffFlashComp);
                continue;
            }
            var curColorSequence = currentSequence.FlashingColors;

            if (iffFlashComp.ChangeIndexAt < curTime)
            {
                // Increment if there are more parts of the color sequence, otherwise reset index
                index = (index + 1) % curColorSequence.Count;
                iffFlashComp.CurColor = iffFlashComp.NextColor;
                iffFlashComp.CurDuration = curColorSequence[index].Duration;
                iffFlashComp.ChangeIndexAt = curTime + curColorSequence[index].Duration;
                iffFlashComp.NextColor = curColorSequence[(index + 1) % curColorSequence.Count].Color;
            }

            var difColor = iffFlashComp.NextColor.RGBA - iffFlashComp.CurColor.RGBA;
            var proportionDurationElapsed = (iffFlashComp.ChangeIndexAt - curTime) / iffFlashComp.CurDuration;
            var proportionalColor = Vector4.Multiply((float)proportionDurationElapsed, difColor);

            Color newColor = new Color(proportionalColor + iffFlashComp.CurColor.RGBA);
            _shuttle.SetIFFColor(ent, newColor);
        }
    }

    public void InitalizeIFFFlashSequence(EntityUid gridEnt, IFFFlashingColorsComponent? iffFlashComp, ProtoId<IFFFlashingColorsSequencePrototype> newSequence)
    {
        if (!Resolve(gridEnt, ref iffFlashComp))
        {
            return;
        }

        if (!_proto.TryIndex<IFFFlashingColorsSequencePrototype>(newSequence, out var sequenceProto))
        {
            return;
        }

        iffFlashComp.CurrentIFFFlashingSequence = sequenceProto;
        TryStartIFFFlashSequence(gridEnt, iffFlashComp);
    }
    public bool TryStartIFFFlashSequence(EntityUid gridEnt, IFFFlashingColorsComponent? iffFlashComp)
    {
        if (!Resolve(gridEnt, ref iffFlashComp))
        {
            return false;
        }

        if (iffFlashComp.CurrentIFFFlashingSequence is not IFFFlashingColorsSequencePrototype curSequence)
        {
            return false;
        }

        if (curSequence.FlashingColors.Count < 1)
        {
            return false;
        }

        var flashingStates = curSequence.FlashingColors;
        var curIndex = 0;

        iffFlashComp.IsActive = true;

        iffFlashComp.CurrentIFFColorIndex = curIndex;
        iffFlashComp.CurColor = flashingStates[curIndex].Color;
        iffFlashComp.NextColor = flashingStates[(curIndex + 1) % flashingStates.Count].Color;
        iffFlashComp.CurDuration = flashingStates[curIndex].Duration;

        iffFlashComp.ChangeIndexAt = iffFlashComp.CurDuration + _timing.CurTime;
        iffFlashComp.OriginalColor = _shuttle.GetIFFColor(gridEnt);
        _shuttle.SetIFFColor(gridEnt, iffFlashComp.CurColor);
        return true;
    }

    public void StopIFFFlashSequnce(EntityUid gridEnt, IFFFlashingColorsComponent? iffFlashComp)
    {
        if (!Resolve(gridEnt, ref iffFlashComp))
        {
            return;
        }

        iffFlashComp.IsActive = false;
        if (iffFlashComp.OriginalColor is Color originalColor)
        {
            _shuttle.SetIFFColor(gridEnt, originalColor);
        }
        else
        {
            _shuttle.SetIFFColor(gridEnt, IFFComponent.IFFColor);
        }
    }
}
