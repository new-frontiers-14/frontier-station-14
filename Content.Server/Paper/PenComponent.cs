using Content.Shared.Paper;
using Robust.Shared.GameStates;

namespace Content.Server.Paper
{
    [RegisterComponent]
    [Access(typeof(PaperSystem))]
    public sealed partial class PenComponent : Component
    {
        /// <summary>
        ///     Current pen mode. Can be switched by user verbs.
        /// </summary>
        [DataField("mode")]
        public PenMode Pen = PenMode.PenWrite;
    }
}
