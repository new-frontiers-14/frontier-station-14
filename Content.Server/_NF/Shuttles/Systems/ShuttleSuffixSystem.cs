using System.Linq;
using System.Text;
using Content.Shared._NF.Shipyard.Components;
using Content.Shared._NF.Shipyard.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._NF.Shuttles.Systems;

public sealed partial class ShuttleSuffixSystem : EntitySystem
{
    private string[] DefaultSuffixCodes => ["LV", "NX", "EV", "QT", "PR"];

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private const int OffsetA = 'A';
    private const int NumLetters = 26;

    public string GenerateSuffix(ProtoId<VesselPrototype> vesselId, SuffixGeneratorInputEntry? vesselDesignator = null)
    {
        if (!_prototypeManager.TryIndex(vesselId, out var vessel) || vessel.SuffixGenerator is null)
        {
            return DefaultSuffixGeneration();
        }

        if (!_prototypeManager.TryIndex(vessel.SuffixGenerator, out var suffixGen))
        {
            //Log a warning perhaps?
            return DefaultSuffixGeneration();
        }

        var designators = new List<SuffixGeneratorInputEntry>() { vesselDesignator ?? suffixGen.DefaultDesignator };

        designators.AddRange(suffixGen.Designators);
        return string.Format(suffixGen.Format, designators.Select(GenerateInput).ToArray());

    }

    //Mimics the NanotrasenNameGenerator suffix, which is what all shuttles used previously
    private string DefaultSuffixGeneration()
    {
        return $"{_random.Pick(DefaultSuffixCodes)}-{_random.Next(0, 1000):D3}";
    }

    private string GenerateInput(SuffixGeneratorInputEntry entry)
    {
        return entry.Type switch
        {
            SuffixGeneratorInputType.Alpha => GenerateAlpha(entry),
            SuffixGeneratorInputType.AlphaNumeric => GenerateAlphaNumeric(entry),
            SuffixGeneratorInputType.Numeric => GenerateNumeric(entry),
            SuffixGeneratorInputType.List => GenerateList(entry),
            _ => ""
        };
    }

    private string GenerateAlpha(SuffixGeneratorInputEntry entry)
    {
        var min = Math.Clamp(entry.Min ?? 1, 1, ShuttleDeedComponent.MaxSuffixLength);
        var max = Math.Clamp(entry.Max ?? 1, min, ShuttleDeedComponent.MaxSuffixLength);
        var count = _random.Next(min, max + 1); //max is exclusive
        var buffer = new byte[count];
        _random.NextBytes(buffer);

        return new string(buffer.Select(b => (char)(b % NumLetters + OffsetA)).ToArray());
    }

    private string GenerateAlphaNumeric(SuffixGeneratorInputEntry entry)
    {
        var min = Math.Clamp(entry.Min ?? 1, 1, ShuttleDeedComponent.MaxSuffixLength);
        var max = Math.Clamp(entry.Max ?? 1, min, ShuttleDeedComponent.MaxSuffixLength);
        var count = _random.Next(min, max + 1); //max is exclusive
        var builder = new StringBuilder();
        for (var i = 0; i < count; i++)
        {
            var value = _random.Next(NumLetters + 10); //There are 10 digits, should I really make a constant?
            if (value < NumLetters)
            {
                builder.Append((char)(value + OffsetA));
            }
            else
            {
                builder.Append(value - NumLetters);
            }
        }
        return builder.ToString();
    }

    private string GenerateNumeric(SuffixGeneratorInputEntry entry)
    {
        var min = Math.Max(entry.Min ?? 0, 0);
        var max = Math.Max(entry.Max ?? 9, min);
        var value = _random.Next(min, max + 1);
        var format = entry.Format ?? "{0}";
        return string.Format(format, value);
    }

    private string GenerateList(SuffixGeneratorInputEntry entry)
    {
        //If they forgot to fill out the items array just give an empty result
        if (entry.Items is null || entry.Items.Length == 0)
        {
            return "";
        }

        return _random.Pick(entry.Items);

    }

}
