using System.Linq;
using System.Text;
using Content.Server.Maps.NameGenerators;
using Content.Shared._NF.Maps.NameGenerators;
using Content.Shared._NF.Shipyard.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._NF.Maps.NameGenerators;

public sealed partial class ConfigurableSuffixNameGenerator : StationNameGenerator
{
    [DataField(required: true)] public ProtoId<SuffixGeneratorPrototype> Generator = default!;
    [DataField] public SuffixGeneratorInputEntry? Designator = default;

    private const int OffsetA = 'A';
    private const int NumLetters = 26;
    public override string FormatName(string input)
    {
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        if (!prototypeManager.TryIndex(Generator, out var suffixGen))
        {
            //Log a warning perhaps?
            return $"Missing Suffix Generator {Generator.Id}"; //This should get attention and point them in the right place
        }

        var random = IoCManager.Resolve<IRobustRandom>();

        var designators = new List<SuffixGeneratorInputEntry>() { Designator ?? suffixGen.DefaultDesignator };

        designators.AddRange(suffixGen.Designators);
        var suffix = string.Format(suffixGen.Format, designators.Select(d => GenerateInput(d, random)).ToArray());
        return string.Format(input, suffix);
    }

    private string GenerateInput(SuffixGeneratorInputEntry entry, IRobustRandom random)
    {
        return entry.Type switch
        {
            SuffixGeneratorInputType.Alpha => GenerateAlpha(entry, random),
            SuffixGeneratorInputType.AlphaNumeric => GenerateAlphaNumeric(entry, random),
            SuffixGeneratorInputType.Numeric => GenerateNumeric(entry, random),
            SuffixGeneratorInputType.List => GenerateList(entry, random),
            _ => ""
        };
    }

    private string GenerateAlpha(SuffixGeneratorInputEntry entry, IRobustRandom random)
    {
        var min = Math.Clamp(entry.Min ?? 1, 1, ShuttleDeedComponent.MaxSuffixLength);
        var max = Math.Clamp(entry.Max ?? 1, min, ShuttleDeedComponent.MaxSuffixLength);
        var count = random.Next(min, max + 1); //Next(,max) is exclusive
        var buffer = new byte[count];
        random.NextBytes(buffer);

        return new string(buffer.Select(b => (char)(b % NumLetters + OffsetA)).ToArray());
    }

    private string GenerateAlphaNumeric(SuffixGeneratorInputEntry entry, IRobustRandom random)
    {
        var min = Math.Clamp(entry.Min ?? 1, 1, ShuttleDeedComponent.MaxSuffixLength);
        var max = Math.Clamp(entry.Max ?? 1, min, ShuttleDeedComponent.MaxSuffixLength);
        var count = random.Next(min, max + 1); //Next(,max) is exclusive
        var builder = new StringBuilder();
        for (var i = 0; i < count; i++)
        {
            var value = random.Next(NumLetters + 10); //There are 10 digits, should I really make a constant?
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

    private string GenerateNumeric(SuffixGeneratorInputEntry entry, IRobustRandom random)
    {
        var min = Math.Max(entry.Min ?? 0, 0);
        var max = Math.Max(entry.Max ?? 9, min);
        var value = random.Next(min, max + 1);
        var format = entry.Format ?? "{0}";
        return string.Format(format, value);
    }

    private string GenerateList(SuffixGeneratorInputEntry entry, IRobustRandom random)
    {
        //If they forgot to fill out the items array just give an empty result
        if (entry.Items is null || entry.Items.Length == 0)
        {
            return "";
        }

        return random.Pick(entry.Items);

    }
}
