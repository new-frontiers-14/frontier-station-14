## UI

cargo-console-menu-title = Консоль заказа грузов
cargo-console-menu-account-name-label = Имя аккаунта:{ " " }
cargo-console-menu-account-name-none-text = Нет
cargo-console-menu-shuttle-name-label = Название шаттла:{ " " }
cargo-console-menu-shuttle-name-none-text = Нет
cargo-console-menu-points-label = Кредиты:{ " " }
cargo-console-menu-shuttle-status-label = Статус шаттла:{ " " }
cargo-console-menu-shuttle-status-away-text = Отбыл
cargo-console-menu-order-capacity-label = Объём заказов:{ " " }
cargo-console-menu-call-shuttle-button = Активировать телепад
cargo-console-menu-permissions-button = Доступы
cargo-console-menu-categories-label = Категории:{ " " }
cargo-console-menu-search-bar-placeholder = Поиск
cargo-console-menu-requests-label = Запросы
cargo-console-menu-orders-label = Заказы
cargo-console-menu-order-reason-description = Причина: { $reason }
cargo-console-menu-populate-categories-all-text = Все
cargo-console-menu-populate-orders-cargo-order-row-product-name-text = { $productName } (x{ $orderAmount }) от { $orderRequester }
cargo-console-menu-cargo-order-row-approve-button = Одобрить
cargo-console-menu-cargo-order-row-cancel-button = Отменить
# Orders
cargo-console-order-not-allowed = Доступ запрещён
cargo-console-station-not-found = Нет доступной станции
cargo-console-invalid-product = Неверный ID продукта
cargo-console-too-many = Слишком много одобренных заказов
cargo-console-snip-snip = Заказ урезан до вместимости
cargo-console-insufficient-funds = Недостаточно средств (требуется { $cost })
cargo-console-unfulfilled = Нет места для выполнения заказа
cargo-console-trade-station = Отправить на { $destination }
cargo-console-unlock-approved-order-broadcast = [bold]Заказ на { $productName } x{ $orderAmount }[/bold], стоимостью [bold]{ $cost }[/bold], был одобрен [bold]{ $approverName }, { $approverJob }[/bold]
cargo-console-paper-print-name = Заказ #{ $orderNumber }
cargo-console-paper-print-text =
    Заказ #{ $orderNumber }
    Товар: { $itemName }
    Кол-во: { $orderQuantity }
    Запросил: { $requester }
    Причина: { $reason }
    Одобрил: { $approver }
# Cargo shuttle console
cargo-shuttle-console-menu-title = Консоль вызова грузового шаттла
cargo-shuttle-console-station-unknown = Неизвестно
cargo-shuttle-console-shuttle-not-found = Не найден
cargo-no-shuttle = Грузовой шаттл не найден!
cargo-shuttle-console-organics = На шаттле обнаружены органические формы жизни
cargo-telepad-delay-upgrade = Откат телепортации
cargo-console-menu-account-name-format = [bold][color={$color}]{$name}[/color][/bold] [font="Monospace"]\[{$code}\][/font]
cargo-console-menu-points-amount = ${$amount}
cargo-console-menu-tab-title-orders = Заказы
cargo-console-menu-tab-title-funds = Переводы
cargo-console-menu-account-action-transfer-limit = [bold]Лимит перевода:[/bold] ${$limit}
cargo-console-menu-account-action-transfer-limit-unlimited-notifier = [color=gold](Неограничен)[/color]
cargo-console-menu-account-action-select = [bold]Действие со счётом:[/bold]
cargo-console-menu-account-action-amount = [bold]Сумма:[/bold] $
cargo-console-menu-account-action-button = Перевести
cargo-console-menu-toggle-account-lock-button = Переключить лимит перевода
cargo-console-menu-account-action-option-withdraw = Снять наличные
cargo-console-menu-account-action-option-transfer = Перевести средства на {$code}
cargo-console-fund-withdraw-broadcast = [bold]{$name} снял(а) {$amount} кредитов со счёта {$name1} [{$code1}][/bold]
cargo-console-fund-transfer-broadcast = [bold]{$name} перевёл(а) {$amount} кредитов со счёта {$name1} [{$code1}] на счёт {$name2} [{$code2}][/bold]
cargo-console-fund-transfer-user-unknown = Неизвестный
cargo-console-paper-reason-default = Не указана
cargo-console-paper-approver-default = Самостоятельно
cargo-funding-alloc-console-menu-title = Консоль управления финансами
cargo-funding-alloc-console-label-account = [bold]Счёт[/bold]
cargo-funding-alloc-console-label-code = [bold]Код[/bold]
cargo-funding-alloc-console-label-balance = [bold]Баланс[/bold]
cargo-funding-alloc-console-label-cut = [bold]Распределение доходов (%)[/bold]
cargo-funding-alloc-console-label-primary-cut = Доля карго от продаж не из ящиков отдела (%):
cargo-funding-alloc-console-label-lockbox-cut = Доля карго от продаж из ящиков отдела (%):
cargo-funding-alloc-console-label-help-non-adjustible = Карго получает {$percent}% прибыли от продаж не из ящиков отделов. Остальное распределяется как ниже:
cargo-funding-alloc-console-label-help-adjustible = Остаток от источников не из ящиков отделов распределяется как указано ниже:
cargo-funding-alloc-console-button-save = Сохранить изменения
cargo-funding-alloc-console-label-save-fail = [bold]Распределение доходов недействительно![/bold] [color=red]({$positive ->
[1] +
*[-1] -
}{$val}%)[/color]
cargo-acquisition-slip-body = [head=3]Детали заказа[/head]
{"[bold]Продукт:[/bold]"} {$product}
{"[bold]Описание:[/bold]"} {$description}
{"[bold]Цена за единицу:[/bold"}] ${$unit}
{"[bold]Количество:[/bold]"} {$amount}
{"[bold]Стоимость:[/bold]"} ${$cost}
{"[head=3]Детали покупки[/head]"}
{"[bold]Заказчик:[/bold]"} {$orderer}
{"[bold]Причина:[/bold]"} {$reason}
