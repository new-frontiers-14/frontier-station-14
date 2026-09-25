using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Server.Maps.NameGenerators;

[UsedImplicitly]
public sealed partial class CentCommNameGenerator : StationNameGenerator
{
    ///     Where the map comes from. Should be a two or three letter code, for example "VG" for Packedstation.
    [DataField("prefixCreator")] public string PrefixCreator = default!;

    private string Prefix => "NT";
    private string[] SuffixCodes => new []{ "CC" };

    public override string FormatName(string input)
    {
        var random = IoCManager.Resolve<IRobustRandom>();

        return string.Format(input, $"{Prefix}{PrefixCreator}", $"{random.Pick(SuffixCodes)}-{random.Next(0, 100):D3}"); // Note: random.Next's max is exclusive, [0-99] = [0,100)
    }
}
