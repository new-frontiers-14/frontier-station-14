using Content.Shared.Books;
using Robust.Client.UserInterface;

namespace Content.Client.Books
{
    public sealed class BooksSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<OpenURLEvent>(OnOpenURL);
        }

        private void OnOpenURL(OpenURLEvent args)
        {
            var uriOpener = IoCManager.Resolve<IUriOpener>();
            uriOpener.OpenUri(args.URL);
        }
    }
}
