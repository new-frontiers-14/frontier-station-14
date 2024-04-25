ent-ThrusterSecurity = thruster

  .suffix = Security
  .desc = { ent-['BaseStructureUnanchorable', 'Thruster'].desc }
ent-ThrusterSecurityUnanchored = { ent-ThrusterUnanchored }
    .suffix = Unanchored, Security
    .desc = { ent-ThrusterUnanchored.desc }
ent-DebugThrusterSecurity = thruster

  .suffix = DEBUG, Security
  .desc = { ent-['BaseStructureDisableToolUse', 'DebugThruster'].desc }
ent-GyroscopeSecurity = { ent-['BaseStructureDisableToolUse', 'Gyroscope'] }

  .suffix = Security
  .desc = { ent-['BaseStructureDisableToolUse', 'Gyroscope'].desc }
ent-GyroscopeSecurityUnanchored = { ent-GyroscopeSecurity }
    .suffix = Unanchored, Security
    .desc = { ent-GyroscopeSecurity.desc }
ent-DebugGyroscopeSecurity = gyroscope

  .suffix = DEBUG, Security
  .desc = { ent-['BaseStructureDisableToolUse', 'DebugGyroscope'].desc }
ent-SmallGyroscopeSecurity = small gyroscope
    .suffix = Security
    .desc = { ent-GyroscopeSecurity.desc }
ent-SmallGyroscopeSecurityUnanchored = { ent-SmallGyroscopeSecurity }
    .suffix = Unanchored, Security
    .desc = { ent-SmallGyroscopeSecurity.desc }
