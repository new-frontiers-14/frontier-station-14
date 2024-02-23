mail-recipient-mismatch = Имя или должность получателя не совпадает.
mail-invalid-access = Имя и должность получателя совпадают, но доступ отсутствует.
mail-locked = Защитная щёлочка не снята. Приложите ID получателя.
mail-desc-far = Почтовый конверт. Вы не можете рассмотреть, кому он адресован.
mail-desc-close = Почтовый конверт, адресованный {CAPITALIZE($name)}, {$job}.
mail-desc-fragile = У него [color=red]красная маркировка хрупкого груза[/color].
mail-desc-priority = На защитной щёлочке активна [color=yellow]приоритетная маркировка[/color]. Лучше доставить конверт вовремя!
mail-desc-priority-inactive = На защитной щёлочке имеется неактивная [color=#886600]приоритетная маркировка[/color].
mail-unlocked = Защитная щёлочка снята.
mail-unlocked-by-emag = Защитная щёлочка*БЖЖЖЖТ*
mail-unlocked-reward = Защитная щёлочка снята. {$bounty} кредитов добавлены в счёт карго.
mail-penalty-lock = ЗАЩИТНАЯ ЩЁЛОЧКА УНИЧТОЖЕНА. СЧЁТ КАРГО ПОЛУЧАЕТ ШТРАФ В СУММЕ {$credits} КРЕДИТОВ.
mail-penalty-fragile = ЦЕЛОСТНОСТЬ НАРУШЕНА. СЧЁТ КАРГО ПОЛУЧАЕТ ШТРАФ В СУММЕ {$credits} КРЕДИТОВ.
mail-penalty-expired = ВРЕМЯ ДОСТАВКИ ВЫШЛО. СЧЁТ КАРГО ПОЛУЧАЕТ ШТРАФ В СУММЕ {$credits} КРЕДИТОВ.

command-mailto-description = Queue a parcel to be delivered to an entity. Example usage: `mailto 1234 5678 false false`. The target container's contents will be transferred to an actual mail parcel.
command-mailto-help = Usage: {$command} <recipient entityUid> <container entityUid> [is-fragile: true or false] [is-priority: true or false]
command-mailto-no-mailreceiver = Target recipient entity does not have a {$requiredComponent}.
command-mailto-no-blankmail = The {$blankMail} prototype doesn't exist. Something is very wrong. Contact a programmer.
command-mailto-bogus-mail = {$blankMail} did not have {$requiredMailComponent}. Something is very wrong. Contact a programmer.
command-mailto-invalid-container = Target container entity does not have a {$requiredContainer} container.
command-mailto-unable-to-receive = Target recipient entity was unable to be setup for receiving mail. ID may be missing.
command-mailto-no-teleporter-found = Target recipient entity was unable to be matched to any station's mail teleporter. Recipient may be off-station.
command-mailto-success = Success! Mail parcel has been queued for next teleport in {$timeToTeleport} seconds.

command-mailnow = Force all mail teleporters to deliver another round of mail as soon as possible. This will not bypass the undelivered mail limit.
command-mailnow-help = Usage: {$command}
command-mailnow-success = Success! All mail teleporters will be delivering another round of mail soon.