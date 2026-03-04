using Content.Shared.Paper;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using static Content.Shared.Paper.PaperComponent;

namespace Content.Shared._NF.Paper;

[RegisterComponent, NetworkedComponent]
[Access(typeof(PaperBundleSystem), typeof(StaplerSystem))]
public sealed partial class PaperBundleComponent : Component
{
    /// <summary>
    /// The container ID for the papers held inside this bundle.
    /// </summary>
    [DataField]
    public string ContainerId = "bundle_papers";

    /// <summary>
    /// Maximum number of pages this bundle can hold.
    /// </summary>
    [DataField]
    public int MaxPages = 15;

    [Serializable, NetSerializable]
    public enum PaperBundleUiKey : byte
    {
        Key
    }

    [Serializable, NetSerializable]
    public sealed class PaperBundleBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly List<BundlePageData> Pages;

        public PaperBundleBoundUserInterfaceState(List<BundlePageData> pages)
        {
            Pages = pages;
        }
    }

    [Serializable, NetSerializable]
    public sealed class PaperBundleInputTextMessage : BoundUserInterfaceMessage
    {
        public readonly NetEntity PageEntity;
        public readonly string Text;

        public PaperBundleInputTextMessage(NetEntity pageEntity, string text)
        {
            PageEntity = pageEntity;
            Text = text;
        }
    }

}

[Serializable, NetSerializable]
public sealed class BundlePageData
{
    public NetEntity PageEntity;
    public string Text = "";
    public List<StampDisplayInfo> StampedBy = new();
    public PaperAction Mode = PaperAction.Read;
    public int ContentSize = 10000;

    public BundlePageData(NetEntity pageEntity, string text, List<StampDisplayInfo> stampedBy, PaperAction mode, int contentSize)
    {
        PageEntity = pageEntity;
        Text = text;
        StampedBy = stampedBy;
        Mode = mode;
        ContentSize = contentSize;
    }
}
