mail-recipient-mismatch = Имя или должность получателя не совпадают.
mail-recipient-mismatch-name = Имя получателя не совпадает.
mail-invalid-access = Имя и должность получателя совпадают, но доступ не соответствует ожидаемому..
mail-locked = Противоугонный замок не снят. Используйте удостоверение личности получателя.
mail-desc-far = Посылка.
mail-desc-close = Посылка адресована { CAPITALIZE($name) }, { $job }. Последнее известное местоположение: { $station }.
mail-desc-fragile = На ней красный ярлык[color=red]"ХРУПКОЕ"[/color].
mail-desc-priority = Активирована [color=yellow]желтая лента противоугонного замка для приоритетных направлений[/color].
mail-desc-priority-inactive = [color=#886600]Желтая лента противоугонного замка для приоритетных направлений[/color] не активна.
mail-unlocked = Противоугонная система разблокирована.
mail-unlocked-by-emag = Противоугонная система *БЗЗТ*.
mail-unlocked-reward = Противоугонная система разблокирована.
mail-penalty-lock = ПРОТИВОУГОННЫЙ ЗАМОК ВЗЛОМАН. СТАНЦИЯ БУДЕТ ОШТРАФОВАНА НА { $credits } СПЕСОСОВ.
mail-penalty-fragile = ЦЕЛОСТНОСТЬ НАРУШЕНА. СТАНЦИЯ БУДЕТ ОШТРАФОВАНА НА { $credits } СПЕСОСОВ.
mail-penalty-expired = СРОК ДОСТАВКИ ИСТЕК. СТАНЦИЯ БУДЕТ ОШТРАФОВАНА НА { $credits } СПЕСОСОВ.
mail-item-name-unaddressed = посылка
mail-item-name-addressed = посылка ({ $recipient })
command-mailto-description = Добавить в очередь посылку для доставки сущности. Пример использования: mailto 1234 5678 false false. Содержимое целевого контейнера будет перенесено в почтовую посылку.
command-mailto-help = Использование: { $command } <идентификатор сущности-получателя> <идентификатор сущности-контейнера> [хрупкость: true или false] [приоритетность: true или false]
command-mailto-no-mailreceiver = Целевая сущность-получатель не имеет { $requiredComponent }.
command-mailto-no-blankmail = Прототип { $blankMail } не существует. Что-то пошло не так. Обратитесь к программисту.
command-mailto-bogus-mail = { $blankMail } не имеет { $requiredMailComponent }. Что-то пошло не так. Обратитесь к программисту.
command-mailto-invalid-container = Целевая сущность-контейнер не имеет необходимого контейнера { $requiredContainer }.
command-mailto-unable-to-receive = Не удалось подготовить целевую сущность-получателя к получению почты. Возможно, отсутствует идентификационный номер.
command-mailto-no-teleporter-found = Не удалось сопоставить целевую сущность-получателя ни с одним почтовым телепортом станции. Получатель может находиться вне станции.
command-mailto-success = Успех! Посылка добавлена в очередь для следующей телепортации через { $timeToTeleport } секунд.
command-mailnow = Принудительно заставить все почтовые телепорты доставить следующий почтовые отправления как можно скорее. Это не обойдет лимит недоставленной почты.
command-mailnow-help = Использование: { $command }
command-mailnow-success = Успех! Все почтовые телепорты скоро доставят еще одни отправления!
