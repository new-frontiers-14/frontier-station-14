using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Server.Maps.NameGenerators;

[UsedImplicitly]
public sealed partial class FOStaffNameGenerator : StationNameGenerator
{
    [DataField("prefixCreator")] public string PrefixCreator = default!;

    private string Prefix => "NT";
    private string[] SuffixCodes => new []{ "NTS", "FCS", "NS" }; // NTS (Nanotrasen Staff), FCS (Frontier Comissioned Staff), NS (Nano Staff)

    public override string FormatName(string input)
    {
        var random = IoCManager.Resolve<IRobustRandom>();

        return string.Format(input, $"{Prefix}{PrefixCreator}", $"{random.Pick(SuffixCodes)}-{random.Next(0, 1000):D3}");
    }
}
public sealed partial class NFSDNameGenerator : StationNameGenerator
{
    [DataField("prefixCreator")] public string PrefixCreator = default!;

    private string Prefix => "NFSD";
    private string[] SuffixCodes => new[] { "NSF" }; // NSF (Nanotrasen Security Force)

    public override string FormatName(string input)
    {
        var random = IoCManager.Resolve<IRobustRandom>();

        return string.Format(input, $"{Prefix}{PrefixCreator}", $"{random.Pick(SuffixCodes)}-{random.Next(0, 1000):D3}");
    }
}
public sealed partial class SyndicateNameGenerator : StationNameGenerator
{
    [DataField("prefixCreator")] public string PrefixCreator = default!;

    private string Prefix => "SYN";
    private string[] SuffixCodes => new[] { "CS", "DK", "GLX", "IDP", "WC" }; // CS (Cybersun), DK (DonkCo), GLX (Gorlex Marauder), IDP (Interdyne Pharmaceuticals), WC (WaffleCo)

    public override string FormatName(string input)
    {
        var random = IoCManager.Resolve<IRobustRandom>();

        return string.Format(input, $"{Prefix}{PrefixCreator}", $"{random.Pick(SuffixCodes)}-{random.Next(0, 1000):D3}");
    }
}
public sealed partial class CentCommNameGenerator : StationNameGenerator
{
    [DataField("prefixCreator")] public string PrefixCreator = default!;

    private string Prefix => "NT";
    private string[] SuffixCodes => new[] { "CC" }; // CC (Central Command)

    public override string FormatName(string input)
    {
        var random = IoCManager.Resolve<IRobustRandom>();

        return string.Format(input, $"{Prefix}{PrefixCreator}", $"{random.Pick(SuffixCodes)}-{random.Next(0, 100):D2}");
    }
}
public sealed partial class YarrNameGenerator : StationNameGenerator
{
    [DataField("prefixCreator")] public string PrefixCreator = default!;

    private string Prefix => "PIR";
    private string[] SuffixCodes => new[] { "SBF", "FBU", "RM" }; // SBF (Steelbolt Forges), FBU (Freebooters Union), RM (Rum Merchants)

    public override string FormatName(string input)
    {
        var random = IoCManager.Resolve<IRobustRandom>();

        return string.Format(input, $"{Prefix}{PrefixCreator}", $"{random.Pick(SuffixCodes)}-{random.Next(0, 100):D2}");
    }
}
