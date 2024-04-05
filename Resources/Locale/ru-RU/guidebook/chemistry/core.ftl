guidebook-reagent-effect-description =
    { $chance ->
        [1] { $effect }
       *[other] Имеет { NATURALPERCENT($chance, 2) } шанс { $effect }
    }{ $conditionCount ->
        [0] .
       *[other] { " " }, пока { $conditions }.
    }
guidebook-reagent-name = [bold][color={ $color }]{ CAPITALIZE($name) }[/color][/bold]
guidebook-reagent-recipes-header = Рецепт
guidebook-reagent-recipes-reagent-display = [bold]{ $reagent }[/bold] \[{ $ratio }\]
guidebook-reagent-sources-header = Sources
guidebook-reagent-sources-ent-wrapper = [bold]{ $name }[/bold] \[1\]
guidebook-reagent-sources-gas-wrapper = [bold]{ $name } (gas)[/bold] \[1\]
guidebook-reagent-recipes-mix = Смешайте
guidebook-reagent-recipes-mix-and-heat = Смешайте при температуре выше { $temperature }К
guidebook-reagent-effects-header = Эффекты
guidebook-reagent-recipes-mix-info =
    { $minTemp ->
        [0]
            { $hasMax ->
                [true] { CAPITALIZE($verb) } below { NATURALFIXED($maxTemp, 2) }K
               *[false] { CAPITALIZE($verb) }
            }
       *[other]
            { CAPITALIZE($verb) } { $hasMax ->
                [true] between { NATURALFIXED($minTemp, 2) }K and { NATURALFIXED($maxTemp, 2) }K
               *[false] above { NATURALFIXED($minTemp, 2) }K
            }
    }
guidebook-reagent-effects-metabolism-group-rate = [bold]{ $group }[/bold] [color=gray]({ $rate } единиц в секунду)[/color]
guidebook-reagent-physical-description = На вид вещество { $description }.
