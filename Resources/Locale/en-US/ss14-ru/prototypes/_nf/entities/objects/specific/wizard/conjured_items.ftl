ent-ConjuredObject10 = { "" }
    .desc = A magically created entity, that'll vanish from existence eventually.
    .suffix = Conjured
ent-SoapConjured = soap

  .desc = { ent-['BaseBullet', 'Soap', 'ConjuredObject10'].desc }
ent-SoapletBloodCult = soaplet
    .desc = { ent-SoapConjured.desc }
ent-SoapConjuredBloodCultCluster = soap

  .desc = { ent-['Soap', 'ConjuredObject10'].desc }
ent-ShellSoapConjuredBloodCultCluster = { ent-['SoapConjured', 'BaseCartridge'] }

  .desc = { ent-['SoapConjured', 'BaseCartridge'].desc }