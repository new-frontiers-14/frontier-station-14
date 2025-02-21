## Strings for the "grant_connect_bypass" command.

cmd-grant_connect_bypass-desc = Временно позволяет пользователю обходить регулярные проверки соединения.
cmd-grant_connect_bypass-help =
    Использование: grant_connect_bypass <user> [duration minutes]
    Временно предоставляет пользователю возможность обходить ограничения обычных соединений.
    Обход применяется только к этому игровому серверу и истекает (по умолчанию) через 1 час.
    Они смогут присоединиться к игре независимо от наличия белого списка, бункера или количества игроков.

cmd-grant_connect_bypass-arg-user = <user>
cmd-grant_connect_bypass-arg-duration = [duration minutes]

cmd-grant_connect_bypass-invalid-args = Укажите 1 или 2 аргумента
cmd-grant_connect_bypass-unknown-user = Невозможно найти пользователя '{ $user }'
cmd-grant_connect_bypass-invalid-duration = Недопустимая длительность '{ $duration }'

cmd-grant_connect_bypass-success = Успешно добавлен обход для пользователя '{ $user }'
