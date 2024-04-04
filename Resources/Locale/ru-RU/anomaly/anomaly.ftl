anomaly-component-contact-damage = Аномалия сдирает с вас кожу!
anomaly-vessel-component-anomaly-assigned = Аномалия присвоена сосуду.
anomaly-vessel-component-not-assigned = Этому сосуду не присвоена ни одна аномалия. Попробуйте использовать на нём сканер.
anomaly-vessel-component-assigned = Этому сосуду уже присвоена аномалия.
anomaly-vessel-component-upgrade-output = генерация очков
anomaly-particles-delta = Дельта-частицы
anomaly-particles-epsilon = Эпсилон-частицы
anomaly-particles-zeta = Зета-частицы
anomaly-particles-omega = Омега-частицы
anomaly-scanner-component-scan-complete = Сканирование завершено!
anomaly-scanner-ui-title = сканер аномалий
anomaly-scanner-no-anomaly = Нет просканированной аномалии.
anomaly-scanner-severity-percentage = Текущая опасность: [color=gray]{ $percent }[/color]
anomaly-scanner-stability-low = Текущее состояние аномалии: [color=gold]Распад[/color]
anomaly-scanner-stability-medium = Текущее состояние аномалии: [color=forestgreen]Стабильное[/color]
anomaly-scanner-stability-high = Текущее состояние аномалии: [color=crimson]Рост[/color]
anomaly-scanner-point-output = Пассивная генерация очков: [color=gray]{ $point }[/color]
anomaly-scanner-particle-readout = Анализ реакции на частицы:
anomaly-scanner-particle-danger = - [color=crimson]Опасный тип:[/color] { $type }
anomaly-scanner-particle-unstable = - [color=plum]Нестабильный тип:[/color] { $type }
anomaly-scanner-particle-containment = - [color=goldenrod]Сдерживающий тип:[/color] { $type }
anomaly-scanner-pulse-timer = Время до следующего импульса: [color=gray]{ $time }[/color]
anomaly-gorilla-core-slot-name = Ядро Аномалии
anomaly-gorilla-charge-none = Не имеет [bold]Ядра Аномалии[/bold] внутри.
anomaly-gorilla-charge-limit =
    It has [color={ $count ->
        [3] green
        [2] yellow
        [1] orange
        [0] red
       *[other] purple
    }]{ $count } { $count ->
        [one] charge
       *[other] charges
    }[/color] remaining.
anomaly-gorilla-charge-infinite = Теперь они [color=gold]бесконечны[/color]. [italic]Пока что...[/italic]
anomaly-sync-connected = Аномалия успешно привязана
anomaly-sync-disconnected = Соединение с аномалией было потеряно!
anomaly-sync-no-anomaly = Нет аномалий в радиусе действия.
anomaly-sync-examine-connected = Это [color=darkgreen]прикреплено[/color] к аномалии.
anomaly-sync-examine-not-connected = Это [color=darkred]не прикреплено[/color] к аномалии.
anomaly-sync-connect-verb-text = Прикрепить аномалию
anomaly-sync-connect-verb-message = Прикрепить аномалию к { THE($machine) }.
anomaly-generator-ui-title = генератор аномалий
anomaly-generator-fuel-display = Топливо:
anomaly-generator-cooldown = Перезарядка: [color=gray]{ $time }[/color]
anomaly-generator-no-cooldown = Перезарядка: [color=gray]Завершена[/color]
anomaly-generator-yes-fire = Статус: [color=forestgreen]Готов[/color]
anomaly-generator-no-fire = Статус: [color=crimson]Не готов[/color]
anomaly-generator-generate = Создать аномалию
anomaly-generator-charges =
    { $charges ->
        [one] { $charges } заряд
        [few] { $charges } заряда
       *[other] { $charges } зарядов
    }
anomaly-generator-announcement = Аномалия была создана!
anomaly-command-pulse = Вызывает импульс аномалии
anomaly-command-supercritical = Целевая аномалия переходит в суперкритическое состояние
# Flavor text on the footer
anomaly-generator-flavor-left = Аномалия может возникнуть внутри оператора.
anomaly-generator-flavor-right = v1.1
