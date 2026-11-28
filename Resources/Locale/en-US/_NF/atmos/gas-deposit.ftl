gas-deposit-drill-no-resources = Nothing to extract here!
gas-deposit-drill-system-examined = The extractor is set to [color={$statusColor}]{PRESSURE($pressure)}[/color].
gas-deposit-drill-system-examined-amount = The extractor reports {
    $value ->
        [0] [color={$statusColor}]barely anything[/color] left.
        *[other] roughly [color={$statusColor}]{GASQUANTITY($value)}[/color] left.
    }

gas-deposit-extraction-rate = extraction rate
gas-deposit-drill-system-examined-yield = The extractor reports that [color={$statusColor}]{NATURALFIXED($yield, 1)}%[/color]{
    $hitMinimum ->
        [false] yield remains.
        *[other] yield remains, and deep reserves have been reached.
    }
