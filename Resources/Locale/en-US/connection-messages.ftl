﻿whitelist-not-whitelisted = You are not whitelisted.

# proper handling for having a min/max or not
whitelist-playercount-invalid = {$min ->
    [0] The whitelist for this server only applies below {$max} players.
    *[other] The whitelist for this server only applies above {$min} {$max ->
        [2147483647] -> players, so you may be able to join later.
       *[other] -> players and below {$max} players, so you may be able to join later.
    }
}
whitelist-not-whitelisted-rp = You are not whitelisted. To become whitelisted, visit our Discord (which can be found at https://spacestation14.io) and check the #rp-whitelist channel.

command-whitelistadd-description = Adds the player with the given username to the server whitelist.
command-whitelistadd-help = whitelistadd <username>
command-whitelistadd-existing = {$username} is already on the whitelist!
command-whitelistadd-added = {$username} added to the whitelist
command-whitelistadd-not-found = Unable to find '{$username}'

command-whitelistremove-description = Removes the player with the given username from the server whitelist.
command-whitelistremove-help = whitelistremove <username>
command-whitelistremove-existing = {$username} is not on the whitelist!
command-whitelistremove-removed = {$username} removed from the whitelist
command-whitelistremove-not-found = Unable to find '{$username}'

command-kicknonwhitelisted-description = Kicks all non-whitelisted players from the server.
command-kicknonwhitelisted-help = kicknonwhitelisted

ban-banned-permanent = This ban will only be removed via appeal.
ban-banned-permanent-appeal = This ban will only be removed via appeal. You can appeal at {$link}
ban-expires = This ban is for {$duration} minutes and will expire at {$time} UTC.
ban-banned-1 = You, or another user of this computer or connection, are banned from playing here.
ban-banned-2 = The ban reason is: "{$reason}"
ban-banned-3 = Attempts to circumvent this ban such as creating a new account will be logged.

soft-player-cap-full = The server is full!
panic-bunker-account-denied = This server is in panic bunker mode. New connections are not being accepted at this time. Try again later
panic-bunker-account-denied-reason = This server is in panic bunker mode and you were rejected. Reason: "{$reason}"
panic-bunker-account-reason-account = The account's age must be older than {$minutes} minutes
panic-bunker-account-reason-overall = The account's overall playtime must be greater than {$hours} hours
