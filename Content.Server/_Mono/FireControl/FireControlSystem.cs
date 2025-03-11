using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._Mono.FireControl;
using Content.Shared.Power;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using System.Linq;
using Content.Shared.Physics;
using System.Numerics;

namespace Content.Server._Mono.FireControl;

public sealed partial class FireControlSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FireControlServerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<FireControlServerComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<FireControllableComponent, PowerChangedEvent>(OnControllablePowerChanged);
        SubscribeLocalEvent<FireControllableComponent, ComponentShutdown>(OnControllableShutdown);

        InitializeConsole();
    }

    private void OnPowerChanged(EntityUid uid, FireControlServerComponent component, PowerChangedEvent args)
    {
        if (args.Powered)
            TryConnect(uid, component);
        else
            Disconnect(uid, component);
    }

    private void OnShutdown(EntityUid uid, FireControlServerComponent component, ComponentShutdown args)
    {
        Disconnect(uid, component);
    }

    private void OnControllablePowerChanged(EntityUid uid, FireControllableComponent component, PowerChangedEvent args)
    {
        if (args.Powered)
            TryRegister(uid, component);
        else
            Unregister(uid, component);
    }

    private void OnControllableShutdown(EntityUid uid, FireControllableComponent component, ComponentShutdown args)
    {
        if (component.ControllingServer != null && TryComp<FireControlServerComponent>(component.ControllingServer, out var server))
        {
            Unregister(uid, component);

            foreach (var console in server.Consoles)
            {
                if (TryComp<FireControlConsoleComponent>(console, out var consoleComp))
                {
                    UpdateUi(console, consoleComp);
                }
            }
        }
    }

    private void Disconnect(EntityUid server, FireControlServerComponent? component = null)
    {
        if (!Resolve(server, ref component))
            return;

        if (!Exists(component.ConnectedGrid) || !TryComp<FireControlGridComponent>(component.ConnectedGrid, out var controlGrid))
            return;

        if (controlGrid.ControllingServer == server)
        {
            controlGrid.ControllingServer = null;
            RemComp<FireControlGridComponent>((EntityUid)component.ConnectedGrid);
        }

        foreach (var controllable in(component.Controlled))
            Unregister(controllable);

        foreach (var console in component.Consoles)
            UnregisterConsole(console);
    }

    public void RefreshControllables(EntityUid grid, FireControlGridComponent? component = null)
    {
        if (!Resolve(grid, ref component))
            return;

        if (component.ControllingServer == null || !TryComp<FireControlServerComponent>(component.ControllingServer, out var server))
            return;

        server.Controlled.Clear();

        var query = EntityQueryEnumerator<FireControllableComponent>();

        while (query.MoveNext(out var controllable, out var controlComp))
        {
            if (_xform.GetGrid(controllable) == grid)
                TryRegister(controllable, controlComp);
        }

        foreach (var console in server.Consoles)
            UpdateUi(console);
    }

    private bool TryConnect(EntityUid server, FireControlServerComponent? component = null)
    {
        if (!Resolve(server, ref component))
            return false;

        var grid = _xform.GetGrid(server);

        if (grid == null)
            return false;

        var controlGrid = EnsureComp<FireControlGridComponent>((EntityUid)grid);

        if (controlGrid.ControllingServer != null)
            return false;

        controlGrid.ControllingServer = server;
        component.ConnectedGrid = grid;

        RefreshControllables((EntityUid)grid, controlGrid);

        return true;
    }

    private void Unregister(EntityUid controllable, FireControllableComponent? component = null)
    {
        if (!Resolve(controllable, ref component))
            return;

        if (component.ControllingServer == null || !TryComp<FireControlServerComponent>(component.ControllingServer, out var controlComp))
            return;

        controlComp.Controlled.Remove(controllable);
        component.ControllingServer = null;
    }

    private bool TryRegister(EntityUid controllable, FireControllableComponent? component = null)
    {
        if (!Resolve(controllable, ref component))
            return false;

        var gridServer = TryGetGridServer(controllable);

        if (gridServer.ServerComponent == null)
            return false;

        if (gridServer.ServerComponent.Controlled.Add(controllable))
        {
            component.ControllingServer = gridServer.ServerUid;
            return true;
        }
        else
        {
            return false;
        }
    }

    private (EntityUid? ServerUid, FireControlServerComponent? ServerComponent) TryGetGridServer(EntityUid uid)
    {
        var grid = _xform.GetGrid(uid);

        if (grid == null)
            return (null, null);

        if (!TryComp<FireControlGridComponent>(grid, out var controlGrid))
            return (null, null);

        if (controlGrid.ControllingServer == null || !TryComp<FireControlServerComponent>(controlGrid.ControllingServer, out var server))
            return (null, null);

        return (controlGrid.ControllingServer, server);
    }

    public void FireWeapons(EntityUid server, List<NetEntity> weapons, NetCoordinates coordinates, FireControlServerComponent? component = null)
    {
        if (!Resolve(server, ref component))
            return;

        var targetCoords = GetCoordinates(coordinates);

        foreach (var weapon in weapons)
        {
            var localWeapon = GetEntity(weapon);
            if (!component.Controlled.Contains(localWeapon))
                continue;

            if (!TryComp<GunComponent>(localWeapon, out var gun))
                continue;

            var weaponXform = Transform(localWeapon);
            var targetPos = targetCoords.ToMap(EntityManager, _xform);

            if (targetPos.MapId != weaponXform.MapID)
                continue;

            var weaponPos = _xform.GetWorldPosition(weaponXform);

            var direction = (targetPos.Position - weaponPos);
            var distance = direction.Length();
            if (distance <= 0)
                continue;

            direction = Vector2.Normalize(direction);

            var ray = new CollisionRay(weaponPos, direction, collisionMask: (int)(CollisionGroup.Opaque | CollisionGroup.Impassable));
            var rayCastResults = _physics.IntersectRay(weaponXform.MapID, ray, distance, localWeapon, false).ToList();

            if (rayCastResults.Count == 0)
            {
                _gun.AttemptShoot(localWeapon, localWeapon, gun, targetCoords);
            }
        }
    }
}

public sealed class FireControllableStatusReportEvent : EntityEventArgs
{
    public List<(string type, string content)> StatusReports = new();
}
