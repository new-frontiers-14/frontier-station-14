### Special messages used by internal localizer stuff.

# Used internally by the GASQUANTITY() function.
zzzz-fmt-gas-quantity = { TOSTRING($divided, "F1") } { $places ->
    [0] mol
    [1] kmol
    [2] Mmol
    [3] Gmol
    [4] Tmol
    *[5] ???
}
