namespace Content.Shared._UNEON.HardsuitUpgrade;

/// <summary>Values and recipes are per operation; material quantities are whole sheets/crystals, not lathe volume.</summary>
public static class HardsuitUpgradeRules
{
    public const string Slot = "uneon-hardsuit";
    public const float Step = 5;

    public static float Limit(HardsuitUpgradeKind kind) => kind switch
    {
        HardsuitUpgradeKind.Blunt or HardsuitUpgradeKind.Slash or HardsuitUpgradeKind.Piercing => 80,
        HardsuitUpgradeKind.Heat or HardsuitUpgradeKind.Radiation => 100,
        HardsuitUpgradeKind.Caustic => 95,
        HardsuitUpgradeKind.Explosion => 70,
        HardsuitUpgradeKind.Speed => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public static string? DamageType(HardsuitUpgradeKind kind) => kind switch
    {
        HardsuitUpgradeKind.Blunt => "Blunt",
        HardsuitUpgradeKind.Slash => "Slash",
        HardsuitUpgradeKind.Piercing => "Piercing",
        HardsuitUpgradeKind.Heat => "Heat",
        HardsuitUpgradeKind.Radiation => "Radiation",
        HardsuitUpgradeKind.Caustic => "Caustic",
        _ => null
    };

    public static float Protection(float coefficient) => MathF.Round((1 - coefficient) * 100, 2);

    public static float Target(HardsuitUpgradeKind kind, float current) => kind == HardsuitUpgradeKind.Speed
        ? MathF.Max(0, MathF.Round(current - Step, 2))
        : MathF.Max(current, MathF.Min(Limit(kind), MathF.Round(current + Step, 2)));

    public static bool CanImprove(HardsuitUpgradeKind kind, float current) =>
        float.IsFinite(current) && (kind == HardsuitUpgradeKind.Speed ? current > 0 : current < Limit(kind));

    public static int Seconds(HardsuitUpgradeKind kind, float target) =>
        kind == HardsuitUpgradeKind.Speed || target > 60 ? 60 : target > 40 ? 30 : 15;

    public static Dictionary<string, int> Cost(HardsuitUpgradeKind kind, float target)
    {
        if (kind == HardsuitUpgradeKind.Speed)
            return new() { ["Bluespace"] = 5 };
        if (target <= 40)
            return new() { ["Steel"] = 30, ["Plasteel"] = 15, ["Plastic"] = 10 };
        if (target <= 60)
            return new() { ["Steel"] = 30, ["Plasteel"] = 30, ["Gold"] = 15,
                ["Uranium"] = 15, ["Plasma"] = 15, ["Plastic"] = 10 };
        return new() { ["Steel"] = 30, ["Plasteel"] = 30, ["Gold"] = 30,
            ["Uranium"] = 30, ["Plasma"] = 30, ["Plastic"] = 30, ["Bluespace"] = 1 };
    }
}
