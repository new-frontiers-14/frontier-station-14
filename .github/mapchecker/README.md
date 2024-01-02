# MapChecker

This directory contains tooling contributed by TsjipTsjip, initially to automate the process of checking if map
contributions in PR's are valid. That is to say, it collects a list of prototypes in the `Resources/Prototypes`
directory which are marked as `DO NOT MAP`, `DEBUG`, ... and verifies that map changes indeed do not use them.

## Usage

Glad I do not have to write this myself! Get detailed help information by running: `./mapchecker.py --help`


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
