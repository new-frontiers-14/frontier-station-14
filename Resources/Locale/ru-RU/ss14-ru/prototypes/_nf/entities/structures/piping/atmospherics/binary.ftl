ent-GasPressurePumpOn = { ent-GasPressurePump }
    .suffix = ВКЛ
    .desc = { ent-GasPressurePump.desc }
ent-GasPressurePumpOnMax = { ent-GasPressurePumpOn }
    .suffix = ВКЛ, Макс.
    .desc = { ent-GasPressurePumpOn.desc }
ent-GasVolumePumpOn = { ent-GasVolumePump }
    .suffix = ВКЛ
    .desc = { ent-GasVolumePump.desc }

ent-BasePressurePumpGaslock = внешний газовый шлюз
    .desc = Соединяет газовые трубы на отдельных шаттлах или станциях вместе, чтобы обеспечить транспортировку газа. Для обеспечения подачи газа оба борта должны быть состыкованы и работать в одном направлении.

ent-BaseGaslock = газовый шлюз

ent-Gaslock = { ent-BasePressurePumpGaslock }
    .desc = { ent-BasePressurePumpGaslock.desc }

ent-GaslockFrame = переносной газовый шлюз
    .desc = Осуществляет подачу газа. Принимает стыковку, но не может стыковаться сам. Для подачи газа необходимо, чтобы обе стороны были соединены и газ перемещался в одном направлении.