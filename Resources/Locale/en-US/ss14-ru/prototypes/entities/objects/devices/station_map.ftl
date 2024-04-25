ent-BaseHandheldStationMap = station map
    .desc = Displays a readout of the current station.
ent-HandheldStationMap = { ent-['BaseHandheldStationMap', 'PowerCellSlotSmallItem'] }

  .suffix = Handheld, Powered
  .desc = { ent-['BaseHandheldStationMap', 'PowerCellSlotSmallItem'].desc }
ent-HandheldStationMapEmpty = { ent-HandheldStationMap }
    .suffix = Handheld, Empty
    .desc = { ent-HandheldStationMap.desc }
ent-HandheldStationMapUnpowered = { ent-BaseHandheldStationMap }
    .suffix = Handheld, Unpowered
    .desc = { ent-BaseHandheldStationMap.desc }
