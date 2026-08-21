using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._NF.VoidRiver.Components
{
    [RegisterComponent]
    public sealed partial class RiverFlowReceiverComponent : Component
    {
        /// <summary>
        /// A list of Uids of which nodes influence the component holder
        /// </summary>
        [ViewVariables]
        public readonly List<EntityUid> InfluencingNodes = new();

        /// <summary>
        /// Returns true if the entity is currently affected by a river.
        /// </summary>
        [DataField]
        public bool InRiver = false;
    }
}
