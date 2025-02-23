# List of matchers that are always illegal to use. These always supercede CONDITIONALLY_ILLEGAL_MATCHES.
ILLEGAL_MATCHES = [
    "DO NOT MAP",
    "DEBUG",
    "Admeme",
    "CaptainSabre",
    "ClothingBeltSheath",
    "MagazinePistolHighCapacity",
    "MagazinePistolHighCapacityRubber",
    "EncryptionKeyCommand",
    "SurveillanceCameraWireless",
    "CrewMonitoringServer",
    "APCHighCapacity",
    "APCSuperCapacity",
    "APCHyperCapacity",
    "PDA",
    "SpawnPointPassenger",
    "Python",
    "SalvageShuttleMarker",
    "FTLPoint"
]
# List of specific legal entities that override the above.  Does not check suffixes.
LEGAL_OVERRIDES = [
    "ButtonFrameCautionSecurity", # red button
    "PosterLegitPDAAd",
    "ShowcaseRobot" # decoration
]
# List of matchers that are illegal to use, unless the map is a ship and the ship belongs to the keyed shipyard.
CONDITIONALLY_ILLEGAL_MATCHES = {
    "Shipyard": [
    ],
    "Scrap": [
    ],
    "Expedition": [
    ],
    "Custom": [
    ],
    "Security": [  # These matchers are illegal unless the ship is part of the security shipyard.
        "Security",  # Anything with the word security in it should also only be appearing on security ships.
        "Plastitanium",  # Plastitanium walls should only be appearing on security ships.
        "Kammerer", # Opportunity
        "HighSecDoor",
        "ShuttleGun",
    ],
    "Syndicate": [
        "Plastitanium",  # And also on blackmarket ships cause syndicate.
        "ShuttleGun",
    ],
    "BlackMarket": [
        "Plastitanium",  # And also on blackmarket ships cause syndicate.
        "ShuttleGun",
    ],
    "Sr": [
    ],
    "Medical": [
    ],
    # It is assumed that mapped instances of plastitanium, security gear, etc. are deemed acceptable
    "PointOfInterest": [
        "Plastitanium",
        "Security",
        "HighSecDoor"
    ]
}
