## Strings for the "grant_connect_bypass" command.

cmd-grant_connect_bypass-desc = Временно разрешает пользователю обходить обычные проверки подключения.
cmd-grant_connect_bypass-help =
    Использование: grant_connect_bypass <пользователь> [продолжительность минут]
    Временно предоставляет пользователю возможность обходить обычные ограничения подключения.
    Обход действует только на этом игровом сервере и истечет через (по умолчанию) 1 час.
    Пользователь сможет присоединиться независимо от белого списка, режима паники или ограничения по количеству игроков.
cmd-grant_connect_bypass-arg-user = <пользователь>
cmd-grant_connect_bypass-arg-duration = [продолжительность минут]
cmd-grant_connect_bypass-invalid-args = Ожидалось 1 или 2 аргумента
cmd-grant_connect_bypass-unknown-user = Не удалось найти пользователя '{ $user }'
cmd-grant_connect_bypass-invalid-duration = Недопустимая продолжительность '{ $duration }'
cmd-grant_connect_bypass-success = Обход для пользователя '{ $user }' успешно добавлен
