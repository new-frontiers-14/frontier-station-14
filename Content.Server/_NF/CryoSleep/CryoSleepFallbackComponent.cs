namespace Content.Server._NF.CryoSleep;

// In the case a user's body cannot be revived in a cryopod, this component denotes an entity as being
// a fallback to revive them at.
[RegisterComponent]
public sealed partial class CryoSleepFallbackComponent : Component;
