device-pda-slot-component-slot-name-cartridge = Картридж
default-program-name = Программа
notekeeper-program-name = Заметки
news-read-program-name = Новости станции
crew-manifest-program-name = Манифест экипажа
crew-manifest-cartridge-loading = Загрузка...
net-probe-program-name = Зонд сетей
net-probe-scan = Просканирован { $device }!
net-probe-label-name = Название
net-probe-label-address = Адрес
net-probe-label-frequency = Частота
net-probe-label-network = Сеть
log-probe-program-name = Зонд логов
log-probe-scan = Загружены логи устройства { $device }!
log-probe-label-time = Время
log-probe-label-accessor = Использовано:
log-probe-label-number = #
nano-task-program-name = НаноДела
log-probe-print-button = Печать логов
log-probe-printout-device = Сканированное устройство: {$name}
log-probe-printout-header = Последние логи:
log-probe-printout-entry = #{$number} / {$time} / {$accessor}
astro-nav-program-name = Астронав
med-tek-program-name = МедТек

# NanoTask cartridge
nano-task-ui-heading-high-priority-tasks = { $amount ->
[zero] Нет важных задач
[one] 1 важная задача
*[other] {$amount} важных задач
}
nano-task-ui-heading-medium-priority-tasks = { $amount ->
[zero] Нет задач средней важности
[one] 1 задача средней важности
*[other] {$amount} задач средней важности
}
nano-task-ui-heading-low-priority-tasks = { $amount ->
[zero] Нет неважных задач
[one] 1 неважная задача
*[other] {$amount} неважных задач
}
nano-task-ui-done = Готово
nano-task-ui-revert-done = Отменить
nano-task-ui-priority-low = Низкий
nano-task-ui-priority-medium = Средний
nano-task-ui-priority-high = Высокий
nano-task-ui-cancel = Отмена
nano-task-ui-print = Печатать
nano-task-ui-delete = Удалить
nano-task-ui-save = Сохранить
nano-task-ui-new-task = Новая задача
nano-task-ui-description-label = Описание:
nano-task-ui-description-placeholder = Сделать что-то важное
nano-task-ui-requester-label = Инициатор:
nano-task-ui-requester-placeholder = Джон Нанотрейзен
nano-task-ui-item-title = Редактировать задачу
nano-task-printed-description = Описание: {$description}
nano-task-printed-requester = Инициатор: {$requester}
nano-task-printed-high-priority = Приоритет: Высокий
nano-task-printed-medium-priority = Приоритет: Средний
nano-task-printed-low-priority = Приоритет: Низкий
wanted-list-program-name = Список разыскиваемых
wanted-list-label-no-records = Всё спокойно, ковбой
wanted-list-search-placeholder = Поиск по имени и статусу
wanted-list-age-label = [color=darkgray]Возраст:[/color] [color=white]{$age}[/color]
wanted-list-job-label = [color=darkgray]Должность:[/color] [color=white]{$job}[/color]
wanted-list-species-label = [color=darkgray]Раса:[/color] [color=white]{$species}[/color]
wanted-list-gender-label = [color=darkgray]Пол:[/color] [color=white]{$gender}[/color]
wanted-list-reason-label = [color=darkgray]Причина:[/color] [color=white]{$reason}[/color]
wanted-list-unknown-reason-label = причина неизвестна
wanted-list-initiator-label = [color=darkgray]Инициатор:[/color] [color=white]{$initiator}[/color]
wanted-list-unknown-initiator-label = инициатор неизвестен
wanted-list-status-label = [color=darkgray]Статус:[/color] {$status ->
[suspected] [color=yellow]подозреваемый[/color]
[wanted] [color=red]в розыске[/color]
[detained] [color=#b18644]задержан[/color]
[paroled] [color=green]освобождён по УДО[/color]
[discharged] [color=green]освобождён[/color]
*[other] нет
}
wanted-list-history-table-time-col = Время
wanted-list-history-table-reason-col = Причина
wanted-list-history-table-initiator-col = Инициатор
