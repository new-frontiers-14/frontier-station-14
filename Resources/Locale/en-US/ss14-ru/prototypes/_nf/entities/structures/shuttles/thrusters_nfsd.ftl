ent-ThrusterNfsd = { ent-['BaseStructureUnanchorable', 'ThrusterSecurity'] }

  .suffix = NFSD
  .desc = { ent-['BaseStructureUnanchorable', 'ThrusterSecurity'].desc }
ent-ThrusterNfsdUnanchored = { ent-ThrusterSecurityUnanchored }
    .suffix = Unanchored, NFSD
    .desc = { ent-ThrusterSecurityUnanchored.desc }
ent-DebugThrusterNfsd = { ent-['BaseStructureDisableToolUse', 'DebugThrusterSecurity'] }

  .suffix = DEBUG, NFSD
  .desc = { ent-['BaseStructureDisableToolUse', 'DebugThrusterSecurity'].desc }
ent-GyroscopeNfsd = { ent-GyroscopeSecurity }
    .suffix = NFSD
    .desc = { ent-GyroscopeSecurity.desc }
ent-GyroscopeNfsdUnanchored = { ent-GyroscopeSecurityUnanchored }
    .suffix = Unanchored, NFSD
    .desc = { ent-GyroscopeSecurityUnanchored.desc }
ent-DebugGyroscopeNfsd = { ent-DebugGyroscopeSecurity }
    .suffix = DEBUG, NFSD
    .desc = { ent-DebugGyroscopeSecurity.desc }
ent-SmallGyroscopeNfsd = small gyroscope
    .suffix = NFSD
    .desc = { ent-GyroscopeSecurity.desc }
ent-SmallGyroscopeNfsdUnanchored = { ent-SmallGyroscopeSecurityUnanchored }
    .suffix = Unanchored, NFSD
    .desc = { ent-SmallGyroscopeSecurityUnanchored.desc }
