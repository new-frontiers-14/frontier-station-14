<<<<<<< HEAD
namespace Content.Server.GameTicking.Rules.Components;
=======
/*
 * New Frontiers - This file is licensed under AGPLv3
 * Copyright (c) 2024 New Frontiers
 * See AGPLv3.txt for details.
 */
namespace Content.Server._NF.GameRule;
>>>>>>> 06142d863efd76a2b3d6f9762fe8de95924c9c10

[RegisterComponent, Access(typeof(NfAdventureRuleSystem))]
public sealed partial class AdventureRuleComponent : Component
{
<<<<<<< HEAD
    public readonly List<EntityUid> NFPlayerMinds = new();
    public readonly List<EntityUid> CargoDepots = new();
    public readonly List<EntityUid> MarketStations = new();
    public readonly List<EntityUid> RequiredPOIs = new();
    public readonly List<EntityUid> OptionalPOIs = new();
    public readonly List<EntityUid> UniquePOIs = new();
=======
    public List<EntityUid> NFPlayerMinds = new();
    public List<EntityUid> CargoDepots = new();
    public List<EntityUid> MarketStations = new();
    public List<EntityUid> RequiredPois = new();
    public List<EntityUid> OptionalPois = new();
    public List<EntityUid> UniquePois = new();
>>>>>>> 06142d863efd76a2b3d6f9762fe8de95924c9c10
}
