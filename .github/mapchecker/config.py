# List of matchers that are always illegal to use. These always supercede CONDITIONALLY_ILLEGAL_MATCHES.
ILLEGAL_MATCHES = [
    "DO NOT MAP",
    "DEBUG",
]
# List of matchers that are illegal to use, unless the map is a ship and the ship belongs to the keyed shipyard.
CONDITIONALLY_ILLEGAL_MATCHES = {
    "Security": [  # These matchers are illegal unless the ship is part of the security shipyard.
        "Security",  # Anything with the word security in it should also only be appearing on security ships.
        "Plastitanium",  # Plastitanium walls should only be appearing on security ships.
    ],
    "BlackMarket": [
        "Plastitanium",  # And also on blackmarket ships cause syndicate.
    ]
}
