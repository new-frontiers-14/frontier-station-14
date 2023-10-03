### Locale for wielding items; i.e. two-handing them

wieldable-verb-text-wield = Заварити
wieldable-verb-text-unwield = Розварити

wieldable-component-successful-wield = Ви заварили { THE($item) }.
wieldable-component-failed-wield = Ви розварили { THE($item) }.
wieldable-component-successful-wield-other = { THE($user) } заварив { THE($item) }.
wieldable-component-failed-wield-other = { THE($user) } розварив { THE($item) }.

wieldable-component-no-hands = У вас нема вільних рук!
wieldable-component-not-enough-free-hands = {$number ->
    [one] Вам потрібна вільна рука для варіння { THE($item) }.
    *[other] Вам треба { $number } вільних рук для варіння { THE($item) }.
}
wieldable-component-not-in-hands = { CAPITALIZE(THE($item)) } не у ваших руках!

wieldable-component-requires = { CAPITALIZE(THE($item))} має бути завареним!

