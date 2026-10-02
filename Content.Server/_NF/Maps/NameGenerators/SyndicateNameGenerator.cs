using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Server.Maps.NameGenerators;

[UsedImplicitly]
public sealed partial class SyndicateNameGenerator : StationNameGenerator
{
    ///     Where the map comes from. Should be a two or three letter code, for example "VG" for Packedstation.
    [DataField("prefixCreator")] public string PrefixCreator = default!;

    private string Prefix => "SYN";
    private string[] SuffixCodes => new []{ "CS", "DK", "GLX", "IDP", "WC" }; // CS (Cybersun), DK (DonkCo), GLX (Gorlex Marauder), IDP (Interdyne Pharmaceuticals), WC (WaffleCo)

    public override string FormatName(string input)
    {
        var random = IoCManager.Resolve<IRobustRandom>();

        return string.Format(input, $"{Prefix}{PrefixCreator}", $"{random.Pick(SuffixCodes)}-{random.Next(0, 1000):D3}"); // Note: random.Next's max is exclusive, [0-999] = [0,1000)
    }
}
