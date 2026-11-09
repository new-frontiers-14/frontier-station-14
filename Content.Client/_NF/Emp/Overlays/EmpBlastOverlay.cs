using System.Numerics;
using Content.Shared._NF.Emp.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._NF.Emp.Overlays
{
    public sealed class EmpBlastOverlay : Overlay
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        private TransformSystem? _transform;

        private const float PvsDist = 25.0f;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        public override bool RequestScreenTexture => true;

        private readonly ShaderInstance _baseShader;
        private readonly Dictionary<EntityUid, (ShaderInstance shd, EmpShaderInstance instance)> _blasts = new();

        public EmpBlastOverlay()
        {
            IoCManager.InjectDependencies(this);
            _baseShader = _prototypeManager.Index<ShaderPrototype>("Emp").Instance().Duplicate();
        }

        protected override bool BeforeDraw(in OverlayDrawArgs args)
        {
            EmpQuery(args.Viewport.Eye);
            return _blasts.Count > 0;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (ScreenTexture == null)
                return;

            var worldHandle = args.WorldHandle;
            var viewport = args.Viewport;

            foreach ((var shd, var instance) in _blasts.Values)
            {
                if (instance.CurrentMapCoords.MapId != args.MapId)
                    continue;

                // To be clear, this needs to use "inside-viewport" pixels.
                // In other words, specifically NOT IViewportControl.WorldToScreen (which uses outer coordinates).
                var tempCoords = viewport.WorldToLocal(instance.CurrentMapCoords.Position);
                tempCoords.Y = viewport.Size.Y - tempCoords.Y;
                shd?.SetParameter("renderScale", viewport.RenderScale);
                shd?.SetParameter("positionInput", tempCoords);
                shd?.SetParameter("range", instance.Range);
                var life = (_gameTiming.RealTime - instance.Start).TotalSeconds / instance.Duration;
                shd?.SetParameter("life", (float)life);

                // There's probably a very good reason not to do this.
                // Oh well!
                shd?.SetParameter("SCREEN_TEXTURE", viewport.RenderTarget.Texture);

                worldHandle.UseShader(shd);
                worldHandle.DrawRect(Box2.CenteredAround(instance.CurrentMapCoords.Position, new Vector2(instance.Range, instance.Range) * 2f), Color.White);
            }

            worldHandle.UseShader(null);
        }

        //Queries all blasts on the map and either adds or removes them from the list of rendered blasts based on whether they should be drawn (in range? on the same z-level/map? blast entity still exists?)
        private void EmpQuery(IEye? currentEye)
        {
            _transform ??= _entityManager.System<TransformSystem>();

            if (currentEye == null)
            {
                _blasts.Clear();
                return;
            }

            var currentEyeLoc = currentEye.Position;

            var blasts = _entityManager.EntityQueryEnumerator<EmpBlastComponent>();
            //Add all blasts that are not added yet but qualify
            while (blasts.MoveNext(out var blastEntity, out var blast))
            {
                if (!_blasts.ContainsKey(blastEntity) && BlastQualifies(blastEntity, currentEyeLoc, blast))
                {
                    _blasts.Add(
                            blastEntity,
                            (
                                _baseShader.Duplicate(),
                                new EmpShaderInstance(
                                    _transform.GetMapCoordinates(blastEntity),
                                    blast.VisualRange,
                                    blast.StartTime,
                                    blast.VisualDuration
                                )
                            )
                    );
                }
            }

            var activeShaderIds = _blasts.Keys;
            foreach (var blastEntity in activeShaderIds) //Remove all blasts that are added and no longer qualify
            {
                if (_entityManager.EntityExists(blastEntity) &&
                    _entityManager.TryGetComponent(blastEntity, out EmpBlastComponent? blast) &&
                    BlastQualifies(blastEntity, currentEyeLoc, blast))
                {
                    var shaderInstance = _blasts[blastEntity];
                    shaderInstance.instance.CurrentMapCoords = _transform.GetMapCoordinates(blastEntity);
                    shaderInstance.instance.Range = blast.VisualRange;
                }
                else
                {
                    _blasts[blastEntity].shd.Dispose();
                    _blasts.Remove(blastEntity);
                }
            }

        }

        private bool BlastQualifies(EntityUid blastEntity, MapCoordinates currentEyeLoc, EmpBlastComponent blast)
        {
            var transformComponent = _entityManager.GetComponent<TransformComponent>(blastEntity);
            var transformSystem = _entityManager.System<SharedTransformSystem>();
            return transformComponent.MapID == currentEyeLoc.MapId
                && transformSystem.InRange(transformComponent.Coordinates, transformSystem.ToCoordinates(transformComponent.ParentUid, currentEyeLoc), PvsDist + blast.VisualRange);
        }

        private sealed record EmpShaderInstance(MapCoordinates CurrentMapCoords, float Range, TimeSpan Start, float Duration)
        {
            public MapCoordinates CurrentMapCoords = CurrentMapCoords;
            public float Range = Range;
            public TimeSpan Start = Start;
            public float Duration = Duration;
        };
    }
}

