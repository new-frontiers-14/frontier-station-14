cmd-ghostrolewhitelist-ghostrole-does-not-exist = Роль призрака { $ghostRole } не существует.
cmd-ghostrolewhitelist-player-not-found = Игрок { $player } не найден.
cmd-ghostrolewhitelist-hint-player = [игрок]
cmd-ghostrolewhitelist-hint-ghostrole = [призрачная роль]
cmd-ghostrolewhitelistadd-desc = Позволяет игроку играть за определённую роль призрака из белого списка.
cmd-ghostrolewhitelistadd-help = Использование: ghostrolewhitelistadd <username> <ghostrole>
cmd-ghostrolewhitelistadd-already-whitelisted = { $player } уже имеет доступ к игре за { $ghostRole } ({ $ghostRoleName }).
cmd-ghostrolewhitelistadd-added = { $player } добавлен в белый список для { $ghostRoleId } ({ $ghostRoleName }).
cmd-ghostrolewhitelistget-desc = Получить список ролей призрака, доступ к которым имеет игрок.
cmd-ghostrolewhitelistget-help = Использование: ghostrolewhitelistget <username>
cmd-ghostrolewhitelistget-whitelisted-none = Игрок { $player } не имеет доступа ни к одной роли.
cmd-ghostrolewhitelistget-whitelisted-for =
    "Игрок { $player } имеет доступ к следующим ролям:
    { $ghostRoles }"
cmd-ghostrolewhitelistremove-desc = Удалить возможность игрока играть за определённую роль призрака из белого списка.
cmd-ghostrolewhitelistremove-help = Использование: ghostrolewhitelistremove <username> <ghostrole>
cmd-ghostrolewhitelistremove-was-not-whitelisted = { $player } не имел доступ к игре за { $ghostRoleId } ({ $ghostRoleName }).
cmd-ghostrolewhitelistremove-removed = { $player } удалён из белого списка для { $ghostRoleId } ({ $ghostRoleName }).
