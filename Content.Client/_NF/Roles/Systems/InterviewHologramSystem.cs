using System.Linq;
using Content.Server._NF.Roles.Systems;
using Content.Shared._NF.Roles.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._NF.Roles.Systems;

public sealed class InterviewHologramSystem : SharedInterviewHologramSystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InterviewHologramComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<InterviewHologramComponent, BeforePostShaderRenderEvent>(OnShaderRender);
    }

    private void OnComponentStartup(Entity<InterviewHologramComponent> entity, ref ComponentStartup ev)
    {
        UpdateHologramSprite(entity);
    }

    private void OnShaderRender(Entity<InterviewHologramComponent> entity, ref BeforePostShaderRenderEvent ev)
    {
        if (ev.Sprite.PostShader == null)
            return;

        UpdateHologramSprite(entity);
    }

    private void UpdateHologramSprite(EntityUid hologram)
    {
        // Get required components
        if (!TryComp<SpriteComponent>(hologram, out var hologramSprite) ||
            !TryComp<InterviewHologramComponent>(hologram, out var hologramComp))
            return;

        // Override specific values
        hologramSprite.Color = Color.White;
        hologramSprite.Offset = hologramComp.Offset;
        hologramSprite.DrawDepth = (int)DrawDepth.Mobs;

        // Remove shading from all layers (except displacement maps)
        for (int i = 0; i < hologramSprite.AllLayers.Count(); i++)
        {
            if (hologramSprite.TryGetLayer(i, out var layer) && layer.ShaderPrototype != "DisplacedStencilDraw")
                hologramSprite.LayerSetShader(i, "unshaded");
        }

        UpdateHologramShader(hologram, hologramSprite, hologramComp);
    }

    private void UpdateHologramShader(EntityUid uid, SpriteComponent sprite, InterviewHologramComponent hologramComp)
    {
        // Find the texture height of the largest layer
        float texHeight = sprite.AllLayers.Max(x => x.PixelSize.Y);

        var instance = _prototype.Index<ShaderPrototype>(hologramComp.ShaderName).InstanceUnique();
        instance.SetParameter("color1", new Vector3(hologramComp.Color1.R, hologramComp.Color1.G, hologramComp.Color1.B));
        instance.SetParameter("color2", new Vector3(hologramComp.Color2.R, hologramComp.Color2.G, hologramComp.Color2.B));
        instance.SetParameter("alpha", hologramComp.Alpha);
        instance.SetParameter("intensity", hologramComp.Intensity);
        instance.SetParameter("texHeight", texHeight);
        instance.SetParameter("t", (float)_timing.CurTime.TotalSeconds * hologramComp.ScrollRate);

        sprite.PostShader = instance;
        sprite.RaiseShaderEvent = true;
    }

    // NOOP, spawn logic handled on server.
    protected override void HandleApprovalChanged(Entity<InterviewHologramComponent> ent)
    {
    }
}
