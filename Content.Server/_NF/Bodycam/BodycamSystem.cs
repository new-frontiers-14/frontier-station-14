using Content.Server.Access.Systems;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.GameTicking;
using Content.Server.Medical.CrewMonitoring;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Shared.Examine;
using Content.Shared.Inventory.Events;
using Content.Shared._NF.Bodycam;
using Content.Shared.Mobs.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Server.SurveillanceCamera;

namespace Content.Server._NF.Bodycam
{
    public sealed class BodycamSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IdCardSystem _idCardSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SurveillanceCameraSystem _surveillanceCameras = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BodycamComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<BodycamComponent, EntityUnpausedEvent>(OnUnpaused);
            SubscribeLocalEvent<BodycamComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<BodycamComponent, GotUnequippedEvent>(OnUnequipped);
            SubscribeLocalEvent<BodycamComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<BodycamComponent, GetVerbsEvent<Verb>>(OnVerb);
            SubscribeLocalEvent<BodycamComponent, EntGotInsertedIntoContainerMessage>(OnInsert);
            SubscribeLocalEvent<BodycamComponent, EntGotRemovedFromContainerMessage>(OnRemove);
        }

        private void OnUnpaused(EntityUid uid, BodycamComponent component, ref EntityUnpausedEvent args)
        {
            component.NextUpdate += args.PausedTime;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var curTime = _gameTiming.CurTime;
            var cameras = EntityManager.EntityQueryEnumerator<BodycamComponent, DeviceNetworkComponent>();
            bool power = false;

            while (cameras.MoveNext(out var uid, out var camera, out var device))
            {
                if (device.TransmitFrequency is null)
                    continue;

                // check if camera is ready to update
                if (curTime < camera.NextUpdate)
                    continue;

                // TODO: This would cause imprecision at different tick rates.
                camera.NextUpdate = curTime + camera.UpdateRate;

                // get camera status
                var status = GetCameraState(uid, camera);
                if (status == null);
                else
                {
                    power = true;
                }
                _surveillanceCameras.SetActive(uid, power);
                continue;
            }
        }

        private void OnMapInit(EntityUid uid, BodycamComponent component, MapInitEvent args)
        {
            // generate random mode
            if (component.RandomMode)
            {
                //make the camera mode favor on
                var modesDist = new[]
                {
                    BodycamMode.CameraOff,
                    BodycamMode.CameraOn, BodycamMode.CameraOn
                };
                component.Mode = _random.Pick(modesDist);
            }
        }

        private void OnEquipped(EntityUid uid, BodycamComponent component, GotEquippedEvent args)
        {
            if (args.Slot != component.ActivationSlot)
                return;

            component.User = args.Equipee;
        }

        private void OnUnequipped(EntityUid uid, BodycamComponent component, GotUnequippedEvent args)
        {
            if (args.Slot != component.ActivationSlot)
                return;

            component.User = null;
        }

        private void OnExamine(EntityUid uid, BodycamComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            string msg;
            switch (component.Mode)
            {
                case BodycamMode.CameraOff:
                    msg = "bodycam-examine-off";
                    break;
                case BodycamMode.CameraOn:
                    msg = "bodycam-examine-on";
                    break;
                default:
                    return;
            }

            args.PushMarkup(Loc.GetString(msg));
        }

        private void OnVerb(EntityUid uid, BodycamComponent component, GetVerbsEvent<Verb> args)
        {
            // check if user can change camera
            if (component.ControlsLocked)
                return;

            // standard interaction checks
            if (!args.CanAccess || !args.CanInteract || args.Hands == null)
                return;

            args.Verbs.UnionWith(new[]
            {
                CreateVerb(uid, component, args.User, BodycamMode.CameraOff),
                CreateVerb(uid, component, args.User, BodycamMode.CameraOn)
            });
        }

        private void OnInsert(EntityUid uid, BodycamComponent component, EntGotInsertedIntoContainerMessage args)
        {
            if (args.Container.ID != component.ActivationContainer)
                return;

            component.User = args.Container.Owner;
        }

        private void OnRemove(EntityUid uid, BodycamComponent component, EntGotRemovedFromContainerMessage args)
        {
            if (args.Container.ID != component.ActivationContainer)
                return;

            component.User = null;
        }

        private Verb CreateVerb(EntityUid uid, BodycamComponent component, EntityUid userUid, BodycamMode mode)
        {
            return new Verb()
            {
                Text = GetModeName(mode),
                Disabled = component.Mode == mode,
                Priority = -(int) mode, // sort them in descending order
                Category = VerbCategory.PowerBodycam,
                Act = () => SetCamera(uid, mode, userUid, component)
            };
        }

        private string GetModeName(BodycamMode mode)
        {
            string name;
            switch (mode)
            {
                case BodycamMode.CameraOff:
                    name = "bodycam-power-off";
                    break;
                case BodycamMode.CameraOn:
                    name = "bodycam-power-on";
                    break;
                default:
                    return "";
            }

            return Loc.GetString(name);
        }

        public void SetCamera(EntityUid uid, BodycamMode mode, EntityUid? userUid = null,
            BodycamComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.Mode = mode;

            if (userUid != null)
            {
                var msg = Loc.GetString("bodycam-power-state", ("mode", GetModeName(mode)));
                _popupSystem.PopupEntity(msg, uid, userUid.Value);
            }
        }

        public BodycamStatus? GetCameraState(EntityUid uid, BodycamComponent? camera = null, TransformComponent? transform = null)
        {
            if (!Resolve(uid, ref camera, ref transform))
                return null;

            // check if camera is enabled and worn by user
            if (camera.Mode == BodycamMode.CameraOff || camera.User == null)
                return null;

            // try to get mobs id from ID slot
            var userName = Loc.GetString("bodycam-component-unknown-name");
            var userJob = Loc.GetString("bodycam-component-unknown-job");
            if (_idCardSystem.TryFindIdCard(camera.User.Value, out var card))
            {
                if (card.FullName != null)
                    userName = card.FullName;
                if (card.JobTitle != null)
                    userJob = card.JobTitle;
            }

            // finally, form camera status
            var status = new BodycamStatus(uid, userName, userJob);
            return status;
        }
    }
}
