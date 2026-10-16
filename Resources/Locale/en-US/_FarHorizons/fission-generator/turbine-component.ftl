### Examine

gas-turbine-examine-stator-null = It seems to be missing a stator.
gas-turbine-examine-stator = It has a stator.

gas-turbine-examine-blade-null = It seems to be missing a turbine blade.
gas-turbine-examine-blade = It has a turbine blade.

turbine-spinning-0 = The blades are not spinning.
turbine-spinning-1 = The blades are turning slowly.
turbine-spinning-2 = The blades are spinning.
turbine-spinning-3 = The blades are spinning quickly.
turbine-spinning-4 = [color=red]The blades are spinning out of control![/color]

turbine-damaged-0 = It appears to be in good condition.[/color]
turbine-damaged-1 = The turbine looks a bit scuffed.[/color]
turbine-damaged-2 = [color=yellow]The turbine looks badly damaged.[/color]
turbine-damaged-3 = [color=orange]It's critically damaged![/color]

turbine-ruined = [color=red]It's completely broken![/color]

### Popups

# Shown when an event occurs
turbine-overheat = {$owner} triggers the emergency overheat dump valve!
turbine-explode = The {$owner} tears itself apart!

# Shown when damage occurs
turbine-spark = The {$owner} starts sparking!
turbine-spark-stop = The {$owner} stops sparking.
turbine-smoke = The {$owner} begins to smoke!
turbine-smoke-stop = The {$owner} stops smoking.

# Shown during repairs
gas-turbine-repair-fail-blade = You need to replace the turbine blade before this can be repaired.
gas-turbine-repair-fail-stator = You need to replace the stator before this can be repaired.
turbine-repair-ruined = You repair the {$target}'s casing with the {$tool}.
turbine-repair = You repair some of the damage to the {$target} using the {$tool}.
turbine-no-damage = There is no damage to repair on the {$target} using the {$tool}.
turbine-show-damage = BladeHealth {$health}, BladeHealthMax {$healthMax}.

# Anchoring warnings
turbine-unanchor-warning = You cannot unanchor the gas turbine while the turbine is spinning!
turbine-anchor-warning = Invalid anchor position.

gas-turbine-eject-fail-speed = You cannot remove turbine parts while the turbine is spinning!
gas-turbine-insert-fail-speed = You cannot insert turbine parts while the turbine is spinning!

### UI

# Shown when using the UI
comp-turbine-ui-tab-main = Controls
comp-turbine-ui-tab-parts = Parts

comp-turbine-ui-rpm = RPM

comp-turbine-ui-overspeed = OVERSPEED
comp-turbine-ui-overtemp = OVERTEMP
comp-turbine-ui-stalling = STALLING
comp-turbine-ui-undertemp = UNDERTEMP

comp-turbine-ui-flow-rate = Flow Rate
comp-turbine-ui-stator-load = Stator Load

comp-turbine-ui-blade = Turbine Blade
comp-turbine-ui-blade-integrity = Integrity
comp-turbine-ui-blade-stress = Stress

comp-turbine-ui-stator = Turbine Stator
comp-turbine-ui-stator-potential = Potential
comp-turbine-ui-stator-supply = Supply

comp-turbine-ui-power = { POWERWATTS($power) }

comp-turbine-ui-locked-message = Controls locked.
comp-turbine-ui-footer-left = Danger: fast-moving machinery.
comp-turbine-ui-footer-right = 2.0 REV 1