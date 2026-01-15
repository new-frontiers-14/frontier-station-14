using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences
{
    /// <summary>
    ///     Contains all player characters and the index of the currently selected character.
    ///     Serialized both over the network and to disk.
    /// </summary>
    [Serializable]
    [NetSerializable]
    public sealed class PlayerPreferences
    {
        private Dictionary<int, ICharacterProfile> _characters;

        public PlayerPreferences(IEnumerable<KeyValuePair<int, ICharacterProfile>> characters, int selectedCharacterIndex, Color adminOOCColor,
            List<ProtoId<ConstructionPrototype>> constructionFavorites, Dictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities)
        {
            _characters = new Dictionary<int, ICharacterProfile>(characters);
            SelectedCharacterIndex = selectedCharacterIndex;
            AdminOOCColor = adminOOCColor;
            ConstructionFavorites = constructionFavorites;
            JobPriorities = SanitizeJobPriorities(jobPriorities);
        }

        private static Dictionary<ProtoId<JobPrototype>, JobPriority> SanitizeJobPriorities(
            Dictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities)
        {
            return jobPriorities.Where(kvp => kvp.Value != JobPriority.Never).ToDictionary();
        }

        /// <summary>
        ///     All player characters.
        /// </summary>
        public IReadOnlyDictionary<int, ICharacterProfile> Characters => _characters;

        public ICharacterProfile GetProfile(int index)
        {
            return _characters[index];
        }

        /// <summary>
        ///     Index of the currently selected character.
        /// </summary>
        public int SelectedCharacterIndex { get; }

        /// <summary>
        ///     The currently selected character.
        /// </summary>
        public ICharacterProfile SelectedCharacter => Characters[SelectedCharacterIndex];

        [DataField]
        public Dictionary<ProtoId<JobPrototype>, JobPriority> JobPriorities { get; set; }

        public Color AdminOOCColor { get; set; }

        /// <summary>
        ///    List of favorite items in the construction menu.
        /// </summary>
        public List<ProtoId<ConstructionPrototype>> ConstructionFavorites { get; set; } = [];

        public int IndexOfCharacter(ICharacterProfile profile)
        {
            return _characters.FirstOrNull(p => p.Value == profile)?.Key ?? -1;
        }

        public bool TryIndexOfCharacter(ICharacterProfile profile, out int index)
        {
            return (index = IndexOfCharacter(profile)) != -1;
        }

        public bool TryGetHumanoidInSlot(int slot, [NotNullWhen(true)] out HumanoidCharacterProfile? humanoid)
        {
            humanoid = null;
            if (!Characters.TryGetValue(slot, out var profile))
                return false;

            humanoid = profile as HumanoidCharacterProfile;
            return humanoid != null;
        }
    }
}
