ent-ComputerBankATMBase = { "" }
    .desc = { "" }
ent-ComputerBankATMDeposit = bank atm
    .desc = Used to deposit and withdraw funds from a personal bank account.
ent-ComputerBankATMWithdraw = bank atm withdraw-only
    .desc = Used to withdraw funds from a personal bank account, unable to deposit.
ent-ComputerBankATM = { ent-['ComputerBankATMBase', 'ComputerBankATMDeposit', 'BaseStructureDisableToolUse', 'BaseStructureIndestructible', 'BaseStructureComputer'] }

  .desc = { ent-['ComputerBankATMBase', 'ComputerBankATMDeposit', 'BaseStructureDisableToolUse', 'BaseStructureIndestructible', 'BaseStructureComputer'].desc }
ent-ComputerWithdrawBankATM = { ent-['ComputerBankATMBase', 'ComputerBankATMWithdraw', 'BaseStructureDisableToolUse', 'BaseStructureIndestructible', 'BaseStructureComputer'] }

  .desc = { ent-['ComputerBankATMBase', 'ComputerBankATMWithdraw', 'BaseStructureDisableToolUse', 'BaseStructureIndestructible', 'BaseStructureComputer'].desc }
ent-ComputerWallmountBankATM = { ent-['ComputerBankATMBase', 'ComputerBankATMDeposit', 'BaseStructureDisableToolUse', 'BaseStructureIndestructible', 'BaseStructureWallmount', 'BaseStructureComputer'] }

  .suffix = Wallmount
  .desc = { ent-['ComputerBankATMBase', 'ComputerBankATMDeposit', 'BaseStructureDisableToolUse', 'BaseStructureIndestructible', 'BaseStructureWallmount', 'BaseStructureComputer'].desc }
ent-ComputerWallmountWithdrawBankATM = { ent-['ComputerBankATMBase', 'ComputerBankATMWithdraw', 'BaseStructureDisableToolUse', 'BaseStructureIndestructible', 'BaseStructureWallmount', 'BaseStructureComputer'] }

  .suffix = Wallmount
  .desc = { ent-['ComputerBankATMBase', 'ComputerBankATMWithdraw', 'BaseStructureDisableToolUse', 'BaseStructureIndestructible', 'BaseStructureWallmount', 'BaseStructureComputer'].desc }
ent-ComputerBlackMarketBankATM = { ent-['ComputerBankATMBase', 'ComputerBankATMDeposit', 'BaseStructureDisableToolUse', 'BaseStructureDestructible', 'BaseStructureComputer'] }

  .desc = Has some sketchy looking modifications and a sticker that says DEPOSIT FEE 30%
  .suffix = BlackMarket
ent-StationAdminBankATM = station administration console
    .desc = Used to pay out from the station's bank account
