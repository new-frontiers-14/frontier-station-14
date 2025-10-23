namespace Content.Server.Nutrition;

/// <summary>
/// Raised on a food being sliced.
/// Used by deep frier to apply friedness to slices (e.g. deep fried pizza)
/// </summary>
/// <remarks>
/// Not to be confused with upstream SliceFoodEvent which doesn't pass the slice entities, and is only raised once.
/// </remarks>
[ByRefEvent]
public sealed class FoodSlicedEvent : EntityEventArgs
{
    /// <summary>
    /// Who did the slicing?
    /// <summary>
    public EntityUid User;

    /// <summary>
    /// What has been sliced?
    /// <summary>
    /// <remarks>
    /// This could soon be deleted if there was not enough food left to
    /// continue slicing.
    /// </remarks>
    public EntityUid Food;

    /// <summary>
    /// What is the slice?
    /// <summary>
    public EntityUid Slice;

    public FoodSlicedEvent(EntityUid user, EntityUid food, EntityUid slice)
    {
        User = user;
        Food = food;
        Slice = slice;
    }
}
