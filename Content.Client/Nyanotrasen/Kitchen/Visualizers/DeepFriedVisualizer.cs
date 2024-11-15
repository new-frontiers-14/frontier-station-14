using System.Linq;
using Robust.Client.GameObjects;
using static Robust.Client.GameObjects.SpriteComponent;
using Content.Client.Kitchen.Components;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Nyanotrasen.Kitchen.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Nyanotrasen.Kitchen.Prototypes;
using Content.Client.Nyanotrasen.Kitchen.Components;

namespace Content.Client.Kitchen.Visualizers
{
    public sealed class DeepFriedVisualizerSystem : VisualizerSystem<DeepFriedComponent>
    {
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;

        private const string FriedShader = "Crispy";
        private const string SpectralShader = "Spectral";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DeepFriedComponent, HeldVisualsUpdatedEvent>(OnHeldVisualsUpdated);
            SubscribeLocalEvent<DeepFriedComponent, EquipmentVisualsUpdatedEvent>(OnEquipmentVisualsUpdated);
        }

        protected override void OnAppearanceChange(EntityUid uid, DeepFriedComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            // Frontier: get shader to use
            var shader = GetDeepFriedEntityShader(uid, args.Component);
            if (shader == null)
                return;
            // End Frontier

            for (var i = 0; i < args.Sprite.AllLayers.Count(); ++i)
                args.Sprite.LayerSetShader(i, shader); // Frontier: ShaderName<crispinessLevels.Shader
        }

        private void OnHeldVisualsUpdated(EntityUid uid, DeepFriedComponent component, HeldVisualsUpdatedEvent args)
        {
            if (args.RevealedLayers.Count == 0)
            {
                return;
            }

            if (!TryComp(args.User, out SpriteComponent? sprite))
                return;

            // Frontier: get shader to use
            var shader = GetDeepFriedEntityShader(uid);
            if (shader == null)
                return;
            // End Frontier

            foreach (var key in args.RevealedLayers)
            {
                if (!sprite.LayerMapTryGet(key, out var index) || sprite[index] is not Layer layer)
                    continue;

                sprite.LayerSetShader(index, shader); // Frontier: ShaderName<shader
            }
        }

        private void OnEquipmentVisualsUpdated(EntityUid uid, DeepFriedComponent component, EquipmentVisualsUpdatedEvent args)
        {
            if (args.RevealedLayers.Count == 0)
            {
                return;
            }

            if (!TryComp(args.Equipee, out SpriteComponent? sprite))
                return;

            // Frontier: get shader to use
            var shader = GetDeepFriedEntityShader(uid);
            if (shader == null)
                return;
            // End Frontier

            foreach (var key in args.RevealedLayers)
            {
                if (!sprite.LayerMapTryGet(key, out var index) || sprite[index] is not Layer layer)
                    continue;

                sprite.LayerSetShader(index, shader); // Frontier: ShaderName<shader
            }
        }

        private string? GetDeepFriedEntityShader(EntityUid uid, AppearanceComponent? comp = null)
        {
            if (comp == null && !TryComp(uid, out comp))
                return null;

            string? shader = null;
            if (_appearance.TryGetData(uid, DeepFriedVisuals.Fried, out bool isFried, comp) && isFried)
                shader = FriedShader;
            else if (_appearance.TryGetData(uid, DeepFriedVisuals.Spectral, out bool isSpectral, comp) && isSpectral)
                shader = SpectralShader;
            return shader;
        }
    }
}
