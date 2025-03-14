## Survivor

roles-antag-survivor-name = Выживший
# It's a Halo reference
roles-antag-survivor-objective = Текущая цель: Выжить
survivor-role-greeting =
    Вы - выживший.
    Прежде всего, вам нужно вернуться в CentComm живым.
    Соберите столько огнестрельного оружия, сколько необходимо, чтобы гарантировать свое выживание.
    Не доверяйте никому.
survivor-round-end-dead-count =
    { $deadCount ->
        [one] [color=red]{ $deadCount }[/color] выживший умер.
       *[other] [color=red]{ $deadCount }[/color] выживших погибли.
    }
survivor-round-end-alive-count =
    { $aliveCount ->
        [one] [color=yellow]{ $aliveCount }[/color] выживший остался на станции.
       *[other] [color=yellow]{ $aliveCount }[/color] выживших остались на станции.
    }
survivor-round-end-alive-on-shuttle-count =
    { $aliveCount ->
        [one] [color=green]{ $aliveCount }[/color] выживший выбрался живым.
       *[other] [color=green]{ $aliveCount }[/color] выжившим удалось выбраться живыми.
    }

## Wizard

objective-issuer-swf = [color=turquoise]Федерация Космических Волшебников[/color]
wizard-title = Волшебник
wizard-description = На станции волшебник! Никогда не знаешь, что они могут сделать.
roles-antag-wizard-name = Волшебник
roles-antag-wizard-objective = Преподайте им урок, который они никогда не забудут.
wizard-role-greeting =
    ВЫ ВОЛШЕБНИК!
    Между Федерацией Космических Волшебников и компанией NanoTrasen возникли разногласия.
    Итак, вы были выбраны Федерацией Космических Волшебников, чтобы нанести визит на станцию.
    Продемонстрируйте им свои способности.
    Что вы будете делать - решать вам, но помните, что Федерация хочет, чтобы вы выбрались живыми.
wizard-round-end-name = волшебник

## TODO: Wizard Apprentice (Coming sometime post-wizard release)

