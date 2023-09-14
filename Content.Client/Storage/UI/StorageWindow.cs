using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.IdentityManagement;
using Content.Shared.Item;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Robust.Client.UserInterface;
using Robust.Shared.Containers;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using Direction = Robust.Shared.Maths.Direction;

namespace Content.Client.Storage.UI
{
    /// <summary>
    /// GUI class for client storage component
    /// </summary>
    public sealed class StorageWindow : FancyWindow
    {
        private readonly IEntityManager _entityManager;

        private readonly Label _information;
        public readonly ContainerButton StorageContainerButton;
        public readonly ListContainer EntityList;
        private readonly StyleBoxFlat _hoveredBox = new() { BackgroundColor = Color.Black.WithAlpha(0.35f) };
        private readonly StyleBoxFlat _unHoveredBox = new() { BackgroundColor = Color.Black.WithAlpha(0.0f) };

        public StorageWindow(IEntityManager entityManager)
        {
            _entityManager = entityManager;
            SetSize = new Vector2(240, 320);
            Title = Loc.GetString("comp-storage-window-title");
            RectClipContent = true;

            StorageContainerButton = new ContainerButton
            {
                Name = "StorageContainerButton",
                MouseFilter = MouseFilterMode.Pass,
            };

            ContentsContainer.AddChild(StorageContainerButton);

            var innerContainerButton = new PanelContainer
            {
                PanelOverride = _unHoveredBox,
            };

            StorageContainerButton.AddChild(innerContainerButton);

            Control vBox = new BoxContainer()
            {
                Orientation = LayoutOrientation.Vertical,
                MouseFilter = MouseFilterMode.Ignore,
                Margin = new Thickness(5),
            };

            StorageContainerButton.AddChild(vBox);

            _information = new Label
            {
                Text = Loc.GetString("comp-storage-window-volume", ("itemCount", 0), ("usedVolume", 0), ("maxVolume", 0)),
                VerticalAlignment = VAlignment.Center
            };

            vBox.AddChild(_information);

            EntityList = new ListContainer
            {
                Name = "EntityListContainer",
            };

            vBox.AddChild(EntityList);

            EntityList.OnMouseEntered += _ =>
            {
                innerContainerButton.PanelOverride = _hoveredBox;
            };

            EntityList.OnMouseExited += _ =>
            {
                innerContainerButton.PanelOverride = _unHoveredBox;
            };
        }

        /// <summary>
        /// Loops through stored entities creating buttons for each, updates information labels
        /// </summary>
        public void BuildEntityList(EntityUid entity, StorageComponent component)
        {
            var storedCount = component.Container.ContainedEntities.Count;
            var list = new List<EntityListData>(storedCount);

            foreach (var uid in component.Container.ContainedEntities)
            {
                list.Add(new EntityListData(uid));
            }

            EntityList.PopulateList(list);

            // Sets information about entire storage container current capacity
            if (component.StorageCapacityMax != 0)
            {
                _information.Text = Loc.GetString("comp-storage-window-volume", ("itemCount", storedCount),
                    ("usedVolume", component.StorageUsed), ("maxVolume", component.StorageCapacityMax));
            }
            else
            {
                _information.Text = Loc.GetString("comp-storage-window-volume-unlimited", ("itemCount", storedCount));
            }
        }

        /// <summary>
        /// Button created for each entity that represents that item in the storage UI, with a texture, and name and size label
        /// </summary>
        public void GenerateButton(ListData data, ListContainerButton button)
        {
            if (data is not EntityListData {Uid: var entity}
                || !_entityManager.EntityExists(entity))
                return;

            _entityManager.TryGetComponent(entity, out ItemComponent? item);
            _entityManager.TryGetComponent(entity, out StackComponent? stack);
            var count = stack?.Count ?? 1;
            var size = item?.Size;

            var spriteView = new SpriteView
            {
                HorizontalAlignment = HAlignment.Left,
                VerticalAlignment = VAlignment.Center,
                SetSize = new Vector2(32.0f, 32.0f),
                OverrideDirection = Direction.South,
            };
            spriteView.SetEntity(entity);
            button.AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                SeparationOverride = 2,
                Children =
                    {
                        spriteView,
                        new Label
                        {
                            HorizontalExpand = true,
                            ClipText = true,
                            Text = _entityManager.GetComponent<MetaDataComponent>(Identity.Entity(entity, _entityManager)).EntityName +
                                   (count > 1 ? $" x {count}" : string.Empty),
                        },
                        new Label
                        {
                            Align = Label.AlignMode.Right,
                            Text = size.ToString() ?? Loc.GetString("comp-storage-no-item-size"),
                        }
                    }
            });
            button.StyleClasses.Add(StyleNano.StyleClassStorageButton);
            button.EnableAllKeybinds = true;
        }
    }
}
