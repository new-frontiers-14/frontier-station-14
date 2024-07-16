using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server._NF.SectorServices.Prototypes;

/// <summary>
/// Prototype that represents game entities.
/// </summary>
[Prototype("sectorService")]
public sealed partial class SectorServicePrototype : IPrototype
{
    /// <summary>
    /// The "in code name" of the object. Must be unique.
    /// </summary>
    [ViewVariables]
    [IdDataFieldAttribute]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// A dictionary mapping the component type list to the YAML mapping containing their settings.
    /// </summary>
    [DataField]
    public ComponentRegistry Components { get; } = new();
}
