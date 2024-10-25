using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.ContentPack;
using Robust.Shared.Map.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Content.Server.Maps;

/// <summary>
///     Performs basic map migration operations by listening for engine <see cref="MapLoaderSystem"/> events.
/// </summary>
public sealed class MapMigrationSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IResourceManager _resMan = default!;

    private const string MigrationFile = "/";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BeforeEntityReadEvent>(OnBeforeReadEvent);

#if DEBUG
        if (!TryReadFile(out var mappings))
            return;

        // Verify that all of the entries map to valid entity prototypes.
        foreach (var mapping in mappings)
        {
            foreach (var node in mapping.Values)
            {
                var newId = ((ValueDataNode)node).Value;
                if (!string.IsNullOrEmpty(newId) && newId != "null")
                    DebugTools.Assert(_protoMan.HasIndex<EntityPrototype>(newId),
                        $"{newId} is not an entity prototype.");
            }
        }
#endif
    }

    private bool TryReadFile([NotNullWhen(true)] out List<MappingDataNode>? mappings)
    {
        mappings = null;

        var files = _resMan.ContentFindFiles(MigrationFile)
            .Where(f => f.ToString().Contains("migration.yml"))
            .ToList();

        if (files.Count == 0)
            return false;

        foreach (var file in files)
        {
            if (!_resMan.TryContentFileRead(file, out var stream))
                continue;

            using var reader = new StreamReader(stream, EncodingHelpers.UTF8);
            var documents = DataNodeParser.ParseYamlStream(reader).FirstOrDefault();

            if (documents == null)
                continue;

            mappings = mappings ?? new List<MappingDataNode>();
            mappings.Add((MappingDataNode)documents.Root);
        }

        return mappings != null && mappings.Count > 0;
    }

    private void OnBeforeReadEvent(BeforeEntityReadEvent ev)
    {
        if (!TryReadFile(out var mappings))
            return;

        foreach (var mapping in mappings)
        {
            foreach (var (key, value) in mapping)
            {
                if (key is not ValueDataNode keyNode || value is not ValueDataNode valueNode)
                    continue;

                if (string.IsNullOrWhiteSpace(valueNode.Value) || valueNode.Value == "null")
                    ev.DeletedPrototypes.Add(keyNode.Value);
                else
                    ev.RenamedPrototypes.Add(keyNode.Value, valueNode.Value);
            }
        }
    }
}
