# MapChecker

This directory contains tooling contributed by TsjipTsjip, initially to automate the process of checking if map
contributions in PR's are valid. That is to say, it collects a list of prototypes in the `Resources/Prototypes`
directory which are marked as `DO NOT MAP`, `DEBUG`, ... and verifies that map changes indeed do not use them.

## Usage

Glad I do not have to write this myself! Get detailed help information by running: `./mapchecker.py --help`


## Configuration

Matchers are set in `config.py`. Currently it has a global list of matchers that are not allowed anywhere, and a set
of conditional matchers.

The conditional matchers work as follows: All matchers are applied, UNLESS the map is a shuttle, AND it belongs to the
shipyard that is set as the conditional key. For example the current config disallows the usage of Plastitanium walls on
any non-security ship.

A match will attempt to match the following during prototype collection:
- Prototype ID
- Prototype name
- Prototype suffixes (separated per `, `)

## Whitelisting

If a map has a prototype and you believe it should be whitelisted, add a key for your map name (the `id` field of the
gameMap prototype), and add the prototype ID's to its list.

The whitelist the checker uses by default is `.github/mapchecker/whitelist.yml`.
