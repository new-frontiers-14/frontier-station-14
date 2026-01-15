using System.Linq;
using Content.Client.Humanoid;
using Content.Client.Station;
using Content.Shared.Body.Part;
using Content.Shared.Clothing;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Starlight;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

public sealed partial class ProfilePreviewSpriteView
{
    private void ReloadHumanoidEntity(HumanoidCharacterProfile humanoid)
    {
        if (!EntMan.EntityExists(PreviewDummy) ||
            !EntMan.HasComponent<HumanoidAppearanceComponent>(PreviewDummy))
            return;

        EntMan.System<HumanoidAppearanceSystem>().LoadProfile(PreviewDummy, humanoid);

        // Starlight
        var layers = GetCyberneticsLayers(humanoid);
        EntMan.System<HumanoidAppearanceSystem>().AddCustomBaseLayers(PreviewDummy, layers);
    }

    private void LoadHumanoidEntity(HumanoidCharacterProfile humanoid, JobPrototype? job, bool showClothes)
    {
        ProfileName = humanoid.Name;
        JobName = null;
        LoadoutName = null;

        job ??= GetPreferredJob(humanoid);

        RoleLoadout? loadout;

        if (job != null)
        {
            try
            {
                loadout = humanoid.GetLoadoutOrDefault(
                    LoadoutSystem.GetJobPrototype(job.ID),
                    _playerManager.LocalSession,
                    humanoid.Species,
                    EntMan,
                    _prototypeManager);
            }
            catch (UnknownPrototypeException)
            {
                loadout = new RoleLoadout();
            }

            var previewEntity = job.JobPreviewEntity ?? (EntProtoId?) job.JobEntity;

            if (previewEntity != null)
            {
                PreviewDummy = EntMan.SpawnEntity(previewEntity, MapCoordinates.Nullspace);
                JobName = job.LocalizedName;
                LoadoutName = GetLoadoutName(loadout);
                return;
            }
        }

        PreviewDummy = EntMan.SpawnEntity(
            _prototypeManager.Index(humanoid.Species).DollPrototype,
            MapCoordinates.Nullspace);

        ReloadHumanoidEntity(humanoid);

        if (!showClothes)
            return;

        if (job == null)
        {
            job = _prototypeManager.Index<JobPrototype>(SharedGameTicker.FallbackOverflowJob);
        }
        else
        {
            JobName = job.LocalizedName;
        }
        GiveDummyJobClothes(PreviewDummy, humanoid, job);

        if (!_prototypeManager.HasIndex<RoleLoadoutPrototype>(LoadoutSystem.GetJobPrototype(job.ID)))
            return;

        loadout = humanoid.GetLoadoutOrDefault(
            LoadoutSystem.GetJobPrototype(job.ID),
            _playerManager.LocalSession,
            humanoid.Species,
            EntMan,
            _prototypeManager);

        LoadoutName = GetLoadoutName(loadout);

        GiveDummyLoadout(PreviewDummy, loadout);
    }

    private JobPrototype? GetPreferredJob(HumanoidCharacterProfile profile)
    {
        ProtoId<JobPrototype> highPriorityJob = default;
        if (profile.JobPriorities.Count == 1)
        {
            highPriorityJob = profile.JobPriorities.First().Key;
        }
        else
        {
            foreach (var priority in new List<JobPriority> { JobPriority.High, JobPriority.Medium, JobPriority.Low })
            {
                highPriorityJob = profile.JobPriorities.FirstOrDefault(p => p.Value == priority).Key;
                if (highPriorityJob.Id != null)
                    break;
            }
        }

        return highPriorityJob.Id == null ? null : _prototypeManager.Index(highPriorityJob);
    }

    private string? GetLoadoutName(RoleLoadout loadout)
    {
        if (_prototypeManager.TryIndex(loadout.Role, out var roleLoadoutPrototype) &&
            roleLoadoutPrototype.CanCustomizeName)
            return loadout.EntityName;
        return null;
    }

    private void GiveDummyJobClothes(EntityUid dummy, HumanoidCharacterProfile profile, JobPrototype job)
    {
        var inventorySys = EntMan.System<InventorySystem>();
        if (!inventorySys.TryGetSlots(dummy, out var slots))
            return;

        if (profile.Loadouts.TryGetValue(job.ID, out var jobLoadout))
        {
            foreach (var loadouts in jobLoadout.SelectedLoadouts.Values)
            {
                foreach (var loadout in loadouts)
                {
                    if (!_prototypeManager.TryIndex(loadout.Prototype, out var loadoutProto))
                        continue;

                    foreach (var slot in slots)
                    {
                        if (_prototypeManager.TryIndex(loadoutProto.StartingGear, out var loadoutGear))
                        {
                            var itemType = ((IEquipmentLoadout) loadoutGear).GetGear(slot.Name);

                            if (inventorySys.TryUnequip(dummy, slot.Name, out var unequippedItem, silent: true,
                                    force: true, reparent: false))
                            {
                                EntMan.DeleteEntity(unequippedItem.Value);
                            }

                            if (itemType != string.Empty)
                            {
                                var item = EntMan.SpawnEntity(itemType, MapCoordinates.Nullspace);
                                inventorySys.TryEquip(dummy, item, slot.Name, true, true);
                            }
                        }
                        else
                        {
                            var itemType = ((IEquipmentLoadout) loadoutProto).GetGear(slot.Name);

                            if (inventorySys.TryUnequip(dummy, slot.Name, out var unequippedItem, silent: true,
                                    force: true, reparent: false))
                            {
                                EntMan.DeleteEntity(unequippedItem.Value);
                            }

                            if (itemType != string.Empty)
                            {
                                var item = EntMan.SpawnEntity(itemType, MapCoordinates.Nullspace);
                                inventorySys.TryEquip(dummy, item, slot.Name, true, true);
                            }
                        }
                    }
                }
            }
        }

        if (!_prototypeManager.TryIndex(job.StartingGear, out var gear))
            return;

        foreach (var slot in slots)
        {
            var itemType = ((IEquipmentLoadout) gear).GetGear(slot.Name);

            if (inventorySys.TryUnequip(dummy, slot.Name, out var unequippedItem, silent: true, force: true,
                    reparent: false))
            {
                EntMan.DeleteEntity(unequippedItem.Value);
            }

            if (itemType != string.Empty)
            {
                var item = EntMan.SpawnEntity(itemType, MapCoordinates.Nullspace);
                inventorySys.TryEquip(dummy, item, slot.Name, true, true);
            }
        }
    }

    private void GiveDummyLoadout(EntityUid uid, RoleLoadout? roleLoadout)
    {
        if (roleLoadout == null)
            return;

        var spawnSys = EntMan.System<StationSpawningSystem>();

        foreach (var group in roleLoadout.SelectedLoadouts.Values)
        {
            foreach (var loadout in group)
            {
                if (!_prototypeManager.TryIndex(loadout.Prototype, out var loadoutProto))
                    continue;

                spawnSys.EquipStartingGear(uid, loadoutProto);
            }
        }
    }

    private Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> GetCyberneticsLayers(
        HumanoidCharacterProfile humanoid)
    {
        return humanoid.Cybernetics.Select(p =>
        {
            var cyberneticEnt = _prototypeManager.Index<EntityPrototype>(p);
            if (cyberneticEnt.TryGetComponent<BodyPartComponent>(out var part, EntMan.ComponentFactory)
                && cyberneticEnt.TryGetComponent<BaseLayerIdComponent>(out var layer, EntMan.ComponentFactory))
            {
                return (CyberneticImplant.LayerFromBodypart(part), new CustomBaseLayerInfo(layer.Layer));
            }

            return (HumanoidVisualLayers.Special, new CustomBaseLayerInfo());
        }).Where(p => p.Item1 != HumanoidVisualLayers.Special).ToDictionary();
    }
}
