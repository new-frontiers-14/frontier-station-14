ore-silo-ui-nf-itemlist-entry = {$linked ->
    [true] {"[Linked] "}
    *[False] {""}
} {$name} {$inRange ->
    [true] {""}
    *[false] (Out of Range)
}
