mail-recipient-mismatch = Имя или должность получателя не совпадают.
mail-recipient-mismatch-name = Имя получателя не совпадает.
mail-invalid-access = Имя и должность получателя совпадают, но доступ не соответствует ожиданиям.
mail-locked = Антивандальный замок не снят. Коснитесь ID получателя.
mail-desc-far = Посылка.
mail-desc-close = Посылка адресована { CAPITALIZE($name) }, { $job }. Последнее известное местоположение: { $station }.
mail-desc-fragile = На ней [color=red]красная наклейка «Осторожно, хрупкое»[/color].
mail-desc-priority = На антивандальном замке активирована [color=yellow]жёлтая лента приоритета[/color].
mail-desc-priority-inactive = На антивандальном замке [color=#886600]жёлтая лента приоритета[/color] не активна.
mail-unlocked = Антивандальная система разблокирована.
mail-unlocked-by-emag = Антивандальная система *БЗЗТ*.
mail-unlocked-reward = Антивандальная система разблокирована. На ваш счет зачислено { $bounty } кредитов.
mail-penalty-lock = АНТИВАНДАЛЬНЫЙ ЗАМОК ПОВРЕЖДЕН. С банковского счета станции списано { $credits } кредитов.
mail-penalty-fragile = ЦЕЛОСТНОСТЬ НАРУШЕНА. С банковского счета станции списано { $credits } кредитов.
mail-penalty-expired = СРОК ДОСТАВКИ ИСТЕК. С банковского счета станции списано { $credits } кредитов.
mail-item-name-unaddressed = посылка
mail-item-name-addressed = посылка ({ $recipient })
command-mailto-description = Поставить в очередь отправку посылки получателю. Пример использования: `mailto 1234 5678 false false`. Содержимое целевого контейнера будет перенесено в почтовую посылку.
command-mailto-help = Использование: { $command } <идентификатор получателя> <идентификатор контейнера> [хрупкость: true или false] [приоритет: true или false] [большой размер: true или false, опционально]
command-mailto-no-mailreceiver = Целевой сущности-получателю не хватает { $requiredComponent }.
command-mailto-no-blankmail = Прототип { $blankMail } отсутствует. Что-то пошло не так. Обратитесь к программисту.
command-mailto-bogus-mail = У { $blankMail } отсутствует { $requiredMailComponent }. Что-то пошло не так. Обратитесь к программисту.
command-mailto-invalid-container = Целевая сущность-контейнер не имеет необходимого { $requiredContainer }.
command-mailto-unable-to-receive = Целевой сущности-получателю не удалось настроить прием почты. Возможно, отсутствует ID.
command-mailto-no-teleporter-found = Целевая сущность-получатель не соответствует ни одному почтовому телепортеру станции. Возможно, получатель находится вне станции.
command-mailto-success = Успех! Посылка добавлена в очередь следующего телепорта через { $timeToTeleport } секунд.
command-mailnow = Принудительно активировать отправку всех посылок через телепортеры как можно скорее. Это не обойдет ограничение на недоставленные посылки.
command-mailnow-help = Использование: { $command }
command-mailnow-success = Успех! Все почтовые телепортеры скоро отправят новую партию посылок.
command-mailtestbulk = Отправить по одному экземпляру каждого типа посылки на указанный почтовый телепортер. Неявно вызывает mailnow.
command-mailtestbulk-help = Использование: { $command } <идентификатор телепортера>
command-mailtestbulk-success = Успех! Все почтовые телепортеры скоро отправят новую партию посылок.
command-mailto-completion-recipient = <сущность получателя>
command-mailto-completion-container = <сущность содержимого>
command-mailto-completion-fragile = [хрупкость: true|false]
command-mailto-completion-priority = [приоритет: true|false]
command-mailto-completion-large = [крупный: true|false]
