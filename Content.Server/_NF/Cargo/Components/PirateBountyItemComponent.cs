using Robust.Shared.Prototypes;

namespace Content.Server.Cargo.Systems; // Needs to collide with base namespace

// Component to identify an item as matching a pirate bounty.
// Each item can match at most one bounty type.
[RegisterComponent]
public sealed partial class PirateBountyItemComponent : Component
{
    // The ID of the category to match.
    [IdDataField]
    public string ID;
}
