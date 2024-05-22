execution-verb-name = Казнить
execution-verb-message = Использовать своё оружие, чтобы казнить кого-то.

# All the below localisation strings have access to the following variables
# attacker (the person committing the execution)
# victim (the person being executed)
# weapon (the weapon used for the execution)

execution-popup-gun-initial-internal = Вы приставляете ствол { THE($weapon) }к голове { $victim }.
execution-popup-gun-initial-external = { $attacker } приставляет ствол  { THE($weapon) } к голове { $victim }.
execution-popup-gun-complete-internal = Вы стреляете { $victim } в голову!
execution-popup-gun-complete-external = { $attacker } стреляет { $victim } в голову!
execution-popup-gun-clumsy-internal = Вы промахиваетесь мимо головы { $victim } и стреляете себе в ногу!
execution-popup-gun-clumsy-external = { $attacker } промахивается по { $victim } и стреляет себе в ногу!
execution-popup-gun-empty = { CAPITALIZE(THE($weapon)) } издаёт щелчок.
execution-popup-melee-initial-internal = Вы вставляете { THE($weapon) } в рот { $victim }.
execution-popup-melee-initial-external = { $attacker } вставляет { $weapon } в рот { $victim }.
execution-popup-melee-complete-internal = Вы перерезали горло { $victim }!
execution-popup-melee-complete-external = { $attacker } перерзает горло { $victim }!
