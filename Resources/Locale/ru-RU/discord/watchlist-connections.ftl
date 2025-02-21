discord-watchlist-connection-header =
    { $players ->
        [one] {$players} игрок в списке наблюдателей
        *[other] {$players} игроки в списке наблюдателей
    } Подключён к {$serverName}

discord-watchlist-connection-entry = - {$playerName} с сообщением "{$message}"{ $expiry ->
        [0] {""}
        *[other] {" "}(expires <t:{$expiry}:R>)
    }{ $otherWatchlists ->
        [0] {""}
        [one] {" "}and {$otherWatchlists} другой список наблюдения
        *[other] {" "}and {$otherWatchlists} другие списки наблюдения
    }
