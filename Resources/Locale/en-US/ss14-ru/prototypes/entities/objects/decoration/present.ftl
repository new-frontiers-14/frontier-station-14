ent-PresentBase = present
    .desc = A little box with incredible surprises inside.
ent-Present = { ent-['PresentBase', 'BaseStorageItem'] }

  .suffix = Empty
  .desc = { ent-['PresentBase', 'BaseStorageItem'].desc }
ent-PresentRandomUnsafe = { ent-['PresentBase', 'BaseItem'] }

  .suffix = Filled, any item
  .desc = { ent-['PresentBase', 'BaseItem'].desc }
ent-PresentRandomInsane = { ent-PresentRandomUnsafe }
    .suffix = Filled, any entity
    .desc = { ent-PresentRandomUnsafe.desc }
ent-PresentRandom = { ent-['PresentBase', 'BaseItem'] }

  .suffix = Filled Safe
  .desc = { ent-['PresentBase', 'BaseItem'].desc }
ent-PresentRandomAsh = { ent-['PresentBase', 'BaseItem'] }

  .suffix = Filled Ash
  .desc = { ent-['PresentBase', 'BaseItem'].desc }
ent-PresentRandomCash = { ent-['PresentBase', 'BaseItem'] }

  .suffix = Filled Cash
  .desc = { ent-['PresentBase', 'BaseItem'].desc }
ent-PresentTrash = Wrapping Paper
    .desc = Carefully folded, taped, and tied with a bow. Then ceremoniously ripped apart and tossed on the floor.
