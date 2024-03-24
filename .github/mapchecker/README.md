# MapChecker

This directory contains tooling contributed by TsjipTsjip, initially to automate the process of checking if map
contributions in PR's are valid. That is to say, it collects a list of prototypes in the `Resources/Prototypes`
directory which are marked as `DO NOT MAP`, `DEBUG`, ... and verifies that map changes indeed do not use them.

## Usage

Glad I do not have to write this myself! Get detailed help information by running:
`python3 .github/mapchecker/mapchecker.py --help`

The following help block is printed:
```
usage: mapchecker.py [-h] [-v] [-p PROTOTYPES_PATH [PROTOTYPES_PATH ...]] [-m MAP_PATH [MAP_PATH ...]] [-w WHITELIST]

Map prototype usage checker for Frontier Station 14.

options:
  -h, --help            show this help message and exit
  -v, --verbose         Sets log level to DEBUG if present, spitting out a lot more information. False by default,.
  -p PROTOTYPES_PATH [PROTOTYPES_PATH ...], --prototypes_path PROTOTYPES_PATH [PROTOTYPES_PATH ...]
                        Directory holding entity prototypes. Default: All entity prototypes in the Frontier Station 14 codebase.
  -m MAP_PATH [MAP_PATH ...], --map_path MAP_PATH [MAP_PATH ...]
                        Map PROTOTYPES or directory of map prototypes to check. Can mix and match.Default: All maps in the Frontier Station 14 codebase.
  -w WHITELIST, --whitelist WHITELIST
                        YML file that lists map names and prototypes to allow for them.
```

You should generally not need to configure `-p`, `-m` or `-w`, as they are autofilled with sensible defaults. You can do
this:
- Set `-p` to only check against prototypes in a specific directory.
- Set `-m` to just check a specific map. (Make sure to **point it at the prototype**, not the map file itself!)
- Set `-v` with `-m` set as per above to get detailed information about a possible rejection for just that map.

## Configuration

Matchers are set in `config.py`. Currently it has a global list of matchers that are not allowed anywhere, and a set
of conditional matchers.

For each map, a set of applicable matchers is constructed according to this workflow:
1. Add all global illegal matchers.
2. Add all conditional matchers for non-matching shipyard groups
3. Remove all conditional matchers from the matching shipyard group (if it exists), to support duplicates across
   shipyard groups

A match will attempt to match the following during prototype collection:
- Prototype ID (contains matcher, case insensitive)
- Prototype name (contains matcher, case insensitive)
- Prototype suffixes (separated per `, `) (exact, case insensitive)

## Whitelisting

If a map has a prototype and you believe it should be whitelisted, add a key for your map name (the `id` field of the
gameMap prototype), and add the prototype ID's to its list.

The whitelist the checker uses by default is `.github/mapchecker/whitelist.yml`.

## Shuttle group override

It is possible that a shuttle is set to group `None` because it is only used in custom shipyard listings. In this case,
you can force the MapChecker script to treat it as a different shipyard group by adding the following to the vessel
prototype:

```yml
  ...
  group: None
  # Add this line below.
  mapchecker_group_override: ShipyardGroupHere
  ...
```

Note that for now this will cause a warning to be generated, but it will not cause a failure if the shuttle matches the
criteria for the overridden group.
