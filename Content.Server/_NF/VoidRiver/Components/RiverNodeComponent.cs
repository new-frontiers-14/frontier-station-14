using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._NF.VoidRiver.Components
{
    [RegisterComponent]
    public sealed partial class RiverNodeComponent : Component
    {
        /// <summary>
        /// The amount of extra units of velocity perfect utilisation of the river provides.
        /// </summary>
        [DataField]
        public float Boost = 0.3f;

        /// <summary>
        /// The smallest the Slowdown Multiplier can be when flying perfectly against the flow.
        /// </summary>
        [DataField]
        public float SlowdownMultiplier = 0.5f;

        /// <summary>
        /// The direction in which the river flows.
        /// </summary>
        [DataField]
        public Angle FlowDirection = 0d;

        /// <summary>
        /// The distance from the Node that Shuttles are influenced by its effects.
        /// </summary>
        [DataField]
        public float NodeRange = 100.0f; //Figure out a reasonable value
    }
}

