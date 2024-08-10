using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Shared.Audio;

namespace Content.Server.Corvax.Elzuosa
{
    [RegisterComponent]
    public sealed partial class ElzuosaColorComponent : Component
    {
        public Color SkinColor { get; set; }

        public bool Hacked { get; set; } = false;

        [DataField("cycleRate")]
        public float CycleRate = 1f;
    }
}
