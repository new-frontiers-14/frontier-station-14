using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Content.Shared.Database;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace Content.Server.Database;

//
// Contains Frontier-specific model definitions.
//

internal static class ModelNF
{
    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NFProfile>()
            .HasIndex(p => new { HumanoidProfileId = p.ProfileId })
            .IsUnique();
    }
}

/// <summary>
/// Frontier additional profile data.
/// </summary>
public sealed class NFProfile
{
    public int Id { get; set; }
    /// <summary>
    /// The base upstream-formatted Profile this NFProfile is associated with.
    /// </summary>
    public Profile Profile { get; set; } = null!;
    public int ProfileId { get; set; }

    /// <summary>
    /// Height scale value for the character associated with the Profile.
    /// </summary>
    public float Size { get; set; }
}
