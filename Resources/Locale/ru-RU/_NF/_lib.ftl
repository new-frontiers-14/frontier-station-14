### Special messages used by internal localizer stuff.

# Used internally by the GASQUANTITY() function.
zzzz-fmt-gas-quantity =
    { TOSTRING($divided, "F1") } { $places ->
        [0] моль
        [1] кмоль
        [2] ммоль
        [3] гмоль
        [4] тмоль
       *[5] ???
    }
