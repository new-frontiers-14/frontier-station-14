shipyard-record-paper-name = {$vessel_name} {$time}
# shipyard-record-paper-content =  [head = 1] Vessel ownership record [/head]
#      Designation: [head = 2]{$vessel_name}[/head]
#      Owneer: [head = 2]Captain {$vessel_owner}[/head]
#      Time of purchase: [head = 2]{$time}[/head]
#      Shuttle has been purchased from Nanotrasen as per New Frontiers program contract
#      Captain is held responsible to uphold SpaceLaw as well as betterment of their crew.
#
#
#      Additional notes:

shipyard-record-paper-content =  {"["}color=blue]◥█▄  █  ®[/color]                                                               [color= #009100][italic]Frontier Automated[/italic][/color]
      {"["}color=blue]   █  ▀█◣[/color]                                                          [color= #009100][italic]Vessel Reporting System[/italic][/color]
      __________________________________________________________________

      {"["}head=2]               Ship Deployment Report[/head]
      __________________________________________________________________

      {"["}italic]Ship ID:[/italic] [bold]{$vessel_name}[/bold]
                      {"["}italic]Category:[/italic] [bold]{$vessel_category}[/bold]
                      {"["}italic]Class:[/italic] [bold]{$vessel_class}[/bold]
                      {"["}italic]Shipyard:[/italic] [bold]{$vessel_group}[/bold]
                      {"["}italic]Price:[/italic] [bold]{$vessel_price}[/bold]

                      {"["}italic]Description:[/italic] [bold]{$vessel_description}[/bold]

      {"["}italic]Time Deployed:[/italic] [bold]{$time}[/bold]

      {"["}italic]Captain Name:[/italic] [bold]{$vessel_owner_name}[/bold]
                      {"["}italic]Species:[/italic] [bold]{$vessel_owner_species}[/bold]
                      {"["}italic]Gender:[/italic] [bold]{$vessel_owner_gender}[/bold]
                         {"["}italic]Age:[/italic] [bold]{$vessel_owner_age}[/bold]

                      {"["}italic]Fingerprints:[/italic] [bold]{$vessel_owner_fingerprints}[/bold]
                      {"["}italic]DNA:[/italic] [bold]{$vessel_owner_dna}[/bold]
      __________________________________________________________________

      {"["}color=grey][italic]         END OF REPORT[/italic][/color]

      {"["}color=grey][italic] The below section is dedicated for noting additional information.[/italic][/color]
      __________________________________________________________________
