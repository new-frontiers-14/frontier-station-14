salvage-expedition-window-finish = Finish expedition
salvage-expedition-announcement-early-finish = The expedition was completed ahead of schedule. Shuttle will depart in {$departTime} seconds.
salvage-expedition-announcement-destruction = { $count ->
    [1] Destroy the {$structure} before the expedition ends.
    *[others] Destroy {$count} {MAKEPLURAL($structure)} before the expedition ends.
}
salvage-expedition-announcement-elimination = { $count ->
    [1] Eliminate the {$target} before the expedition ends.
    *[others] Eliminate {$count} {MAKEPLURAL($target)} before the expedition ends.
}
salvage-expedition-announcement-destruction-entity-fallback = structure
salvage-expedition-announcement-elimination-entity-fallback = target

salvage-expedition-shuttle-not-found = Cannot locate shuttle.
salvage-expedition-not-everyone-aboard = Not all crew aboard! {CAPITALIZE(THE($target))} is still out there!
salvage-expedition-failed = Expedition is failed.

# Salvage mods
salvage-time-mod-standard-time = Normal Duration
salvage-time-mod-rush = Rushs

salvage-weather-mod-heavy-snowfall = Heavy Snowfall
salvage-weather-mod-rain = Rain

salvage-biome-mod-shadow = Shadow

salvage-dungeon-mod-cave-factory = Cave Factory
salvage-dungeon-mod-med-sci = Medical Science Base
salvage-dungeon-mod-factory-dorms = Factory Dorms
salvage-dungeon-mod-lava-mercenary = Lava Mercenary Base
salvage-dungeon-mod-virology-lab = Virology Lab
salvage-dungeon-mod-salvage-outpost = Salvage Outpost

salvage-air-mod-1 = 82 N2, 21 O2
salvage-air-mod-2 = 72 N2, 21 O2, 10 N2O
salvage-air-mod-3 = 72 N2, 21 O2, 10 H2O
salvage-air-mod-4 = 72 N2, 21 O2, 10 NH3
salvage-air-mod-5 = 72 N2, 21 O2, 10 CO2
salvage-air-mod-6 = 79 N2, 21 O2, 5 P
salvage-air-mod-7 = 57 N2, 21 O2, 15 NH3, 5 P, 5 N2O
salvage-air-mod-8 = 57 N2, 21 O2, 15 H2O, 5 NH3, 5 N2O
salvage-air-mod-9 = 57 N2, 21 O2, 15 CO2, 5 P, 5 N2O
salvage-air-mod-10 = 82 CO2, 21 O2
salvage-air-mod-11 = 67 CO2, 31 O2, 5 P
salvage-air-mod-12 = 103 H2O
salvage-air-mod-13 = 103 NH3
salvage-air-mod-14 = 103 N2O
salvage-air-mod-15 = 103 CO2
salvage-air-mod-16 = 34 CO2, 34 NH3, 34 N2O
salvage-air-mod-17 = 34 H2O, 34 NH3, 34 N2O
salvage-air-mod-18 = 34 H2O, 34 N2O, 17 NH3, 17 CO2
salvage-air-mod-unknown = Unknown atmosphere

salvage-expedition-difficulty-NFModerate = Moderate
salvage-expedition-difficulty-NFHazardous = Hazardous
salvage-expedition-difficulty-NFExtreme = Extreme

salvage-expedition-megafauna-remaining = {$count ->
    [one] {$count} target remaining.
    *[other] {$count} targets remaining.
}

salvage-expedition-type-Destruction = Destruction
salvage-expedition-type-Elimination = Elimination