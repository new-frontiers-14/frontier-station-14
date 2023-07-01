using Robust.Shared.Serialization;

namespace Content.Shared.Books;

[Serializable, NetSerializable]
public sealed class OpenURLEvent : EntityEventArgs
{
    public string URL { get; }
    public OpenURLEvent(string url)
    {
        URL = url;
    }
}