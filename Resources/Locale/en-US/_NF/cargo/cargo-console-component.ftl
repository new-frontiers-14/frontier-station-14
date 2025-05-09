## UI
cargo-console-menu-nf-populate-orders-cargo-order-row-product-name-text = {CAPITALIZE($productName)} (x{$total}) for {$purchaser}
cargo-console-menu-nf-populate-orders-cargo-order-row-product-quantity-text = {$remaining} left
cargo-console-menu-nf-order-capacity = {$count}/{$capacity}
cargo-console-order-nf-menu-notes-label = Notes:

## Orders
cargo-console-nf-no-bank-account = No bank account found

cargo-console-nf-paper-print-text = [head=2]Order #{$orderNumber}[/head]
    {"[bold]Item:[/bold]"} {$itemName} ({$orderIndex} of {$orderQuantity})
    {"[bold]Purchased by:[/bold]"} {$purchaser}
    {"[bold]Notes:[/bold]"} {$notes}

## Upgrades
cargo-telepad-delay-upgrade = Teleport delay
