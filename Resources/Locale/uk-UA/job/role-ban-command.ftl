### Localization for role ban command

cmd-roleban-desc = Bans a player from a role
cmd-roleban-help = Usage: roleban <name or user ID> <job> <reason> [duration in minutes, leave out or 0 for permanent ban]

## Completion result hints
cmd-roleban-hint-1 = <name or user ID>
cmd-roleban-hint-2 = <job>
cmd-roleban-hint-3 = <reason>
cmd-roleban-hint-4 = [duration in minutes, leave out or 0 for permanent ban]
cmd-roleban-hint-5 = [severity]

cmd-roleban-hint-duration-1 = Назавжди
cmd-roleban-hint-duration-2 = 1 день
cmd-roleban-hint-duration-3 = 3 дні
cmd-roleban-hint-duration-4 = 1 тиждень
cmd-roleban-hint-duration-5 = 2 тижні
cmd-roleban-hint-duration-6 = 1 місяць


### Localization for role unban command

cmd-roleunban-desc = Відмінити рол бан гравцю
cmd-roleunban-help = Використання: roleunban <role ban id>

## Completion result hints
cmd-roleunban-hint-1 = <role ban id>


### Localization for roleban list command

cmd-rolebanlist-desc = Перелічує рол бани користувача
cmd-rolebanlist-help = Usage: <name or user ID> [include unbanned]

## Completion result hints
cmd-rolebanlist-hint-1 = <name or user ID>
cmd-rolebanlist-hint-2 = [include unbanned]


cmd-roleban-minutes-parse = {$time} не є дійсною кількістю хвилин.\n{$help}
cmd-roleban-severity-parse = ${severity} не є дійсним ступенем тяжкості\n{$help}.
cmd-roleban-arg-count = Неправильна кількість аргументів.
cmd-roleban-job-parse = Робота {$job} не існує
cmd-roleban-name-parse = Не вдалося знайти гравця з таким іменем.
cmd-roleban-existing = {$target} вже має бан ролі {$role}.
cmd-roleban-success = Видано роль бан {$target} ролі {$role} через причину {$reason} {$length}.

cmd-roleban-inf = назавжди
cmd-roleban-until =  через {$expires}

# Department bans
cmd-departmentban-desc = Забороняє гравця виконувати ролі, що входять до складу відділу
cmd-departmentban-help = Використання: departmentban <name or user ID> <department> <reason> [тривалість у хвилинах, пропустіть або 0 для постійної заборони]
