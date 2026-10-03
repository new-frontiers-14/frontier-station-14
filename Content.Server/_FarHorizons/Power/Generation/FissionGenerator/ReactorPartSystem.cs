// SPDX-FileCopyrightText: 2025 jhrushbe <capnmerry@gmail.com>
// SPDX-FileCopyrightText: 2025 rottenheadphones <juaelwe@outlook.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: CC-BY-NC-SA-3.0

using Content.Server.Atmos.EntitySystems;
using Content.Shared._FarHorizons.Power.Generation.FissionGenerator;
using Content.Shared.Atmos;
using Robust.Shared.Random;
using Content.Shared._FarHorizons.Materials.Systems;
using Content.Shared.Examine;
using Content.Shared.Nutrition;
using Content.Shared.Radiation.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._FarHorizons.Power.Generation.FissionGenerator;

// Ported and modified from goonstation by Jhrushbe.
// CC-BY-NC-SA-3.0
// https://github.com/goonstation/goonstation/blob/master/code/obj/nuclearreactor/reactorcomponents.dm
// Performance optimizations adapted from Far-Horizons-SS14/Far-Horizons-SS14#1000
// and ss14Starlight/space-station-14#3967.

public sealed partial class ReactorPartSystem : SharedReactorPartSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly SharedPointLightSystem _lightSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    /// <summary>
    /// Changes the overall rate of events
    /// </summary>
    private readonly float _rate = 5;

    /// <summary>
    /// Changes the likelyhood of neutron interactions
    /// </summary>
    private readonly float _bias = 1.5f;

    /// <summary>
    /// The amount of a property consumed by a reaction
    /// </summary>
    private readonly float _reactant = 0.01f;

    /// <summary>
    /// The amount of a property resultant from a reaction
    /// </summary>
    private readonly float _product = 0.005f;

    /// <summary>
    /// Temperature (in C) when people's hands can be burnt
    /// </summary>
    private readonly static float _hotTemp = 80;

    /// <summary>
    /// Temperature (in C) when insulated gloves can no longer protect
    /// </summary>
    private readonly static float _burnTemp = 400;

    private readonly static float _burnDiv = (_burnTemp - _hotTemp) / 5; // The 5 is how much heat damage insulated gloves protect from

    private readonly float _threshold = 1f;
    private float _accumulator = 0f;

    #region Item Methods
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ReactorPartComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<ReactorPartComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<ReactorPartComponent, IngestedEvent>(OnIngest);
        SubscribeLocalEvent<ReactorPartComponent, AtmosExposedUpdateEvent>(OnAtmosExposed);
    }

    private void OnInit(EntityUid uid, ReactorPartComponent component, ref MapInitEvent args)
    {
        var radvalue = (component.Properties.Radioactivity * 0.1f) + (component.Properties.NeutronRadioactivity * 0.15f) + (component.Properties.FissileIsotopes * 0.125f);
        if (radvalue > 0)
        {
            var radcomp = EnsureComp<RadiationSourceComponent>(uid);
            radcomp.Intensity = radvalue;
        }

        if (component.Properties.NeutronRadioactivity > 0)
        {
            var lightcomp = _lightSystem.EnsureLight(uid);
            _lightSystem.SetEnergy(uid, component.Properties.NeutronRadioactivity, lightcomp);
            _lightSystem.SetColor(uid, Color.FromHex("#22bbff"), lightcomp);
            _lightSystem.SetRadius(uid, 1.2f, lightcomp);
        }
    }

    private void OnExamine(Entity<ReactorPartComponent> ent, ref ExaminedEvent args)
    {
        var comp = ent.Comp;
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(ReactorPartComponent)))
        {
            switch (comp.Properties.NeutronRadioactivity)
            {
                case > 8:
                    args.PushMarkup(Loc.GetString("reactor-part-nrad-5"));
                    break;
                case > 6:
                    args.PushMarkup(Loc.GetString("reactor-part-nrad-4"));
                    break;
                case > 4:
                    args.PushMarkup(Loc.GetString("reactor-part-nrad-3"));
                    break;
                case > 2:
                    args.PushMarkup(Loc.GetString("reactor-part-nrad-2"));
                    break;
                case > 1:
                    args.PushMarkup(Loc.GetString("reactor-part-nrad-1"));
                    break;
                case > 0:
                    args.PushMarkup(Loc.GetString("reactor-part-nrad-0"));
                    break;
            }

            switch (comp.Properties.Radioactivity)
            {
                case > 8:
                    args.PushMarkup(Loc.GetString("reactor-part-rad-5"));
                    break;
                case > 6:
                    args.PushMarkup(Loc.GetString("reactor-part-rad-4"));
                    break;
                case > 4:
                    args.PushMarkup(Loc.GetString("reactor-part-rad-3"));
                    break;
                case > 2:
                    args.PushMarkup(Loc.GetString("reactor-part-rad-2"));
                    break;
                case > 1:
                    args.PushMarkup(Loc.GetString("reactor-part-rad-1"));
                    break;
                case > 0:
                    args.PushMarkup(Loc.GetString("reactor-part-rad-0"));
                    break;
            }

            if (comp.Temperature > Atmospherics.T0C + _burnTemp)
                args.PushMarkup(Loc.GetString("reactor-part-burning"));
            else if (comp.Temperature > Atmospherics.T0C + _hotTemp)
                args.PushMarkup(Loc.GetString("reactor-part-hot"));
        }
    }

    private void OnIngest(Entity<ReactorPartComponent> ent, ref IngestedEvent args)
    {
        var comp = ent.Comp;
        if (comp.Properties == null)
            return;

        var properties = comp.Properties;

        if (!_entityManager.TryGetComponent<DamageableComponent>(args.Target, out var damageable) || damageable.Damage.DamageDict == null)
            return;

        var dict = damageable.Damage.DamageDict;

        var dmgKey = "Radiation";
        var dmg = (properties.NeutronRadioactivity * 20) + (properties.Radioactivity * 10) + (properties.FissileIsotopes * 5);

        if (!dict.TryAdd(dmgKey, dmg))
        {
            var prev = dict[dmgKey];
            dict.Remove(dmgKey);
            dict.Add(dmgKey, prev + dmg);
        }
    }

    public override void Update(float frameTime)
    {
        _accumulator += frameTime;
        if (_accumulator > _threshold)
        {
            AccUpdate();
            _accumulator = 0;
        }
    }

    private void AccUpdate()
    {
        var query = EntityQueryEnumerator<ReactorPartComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            var gasMix = _atmosphereSystem.GetTileMixture(uid, true) ?? GasMixture.SpaceGas;
            var DeltaT = (component.Temperature - gasMix.Temperature) * 0.01f;

            if (Math.Abs(DeltaT) < 0.1)
                continue;

            // This viloates the laws of physics, but if energy is conserved, then pulling out a hot rod will turn the room into an oven
            // Also does not take into account thermal mass
            component.Temperature -= DeltaT;
            if (!gasMix.Immutable) // This prevents it from heating up space itself
                gasMix.Temperature += DeltaT;

            var burncomp = EnsureComp<DamageOnInteractComponent>(uid);

            burncomp.IsDamageActive = component.Temperature > Atmospherics.T0C + _hotTemp;

            if (burncomp.IsDamageActive)
            {
                var damage = Math.Max((component.Temperature - Atmospherics.T0C - _hotTemp) / _burnDiv, 0);

                // Giant string of if/else that makes sure it will interfere only as much as it needs to
                if (burncomp.Damage == null)
                    burncomp.Damage = new() { DamageDict = new() { { "Heat", damage } } };
                else if (burncomp.Damage.DamageDict == null)
                    burncomp.Damage.DamageDict = new() { { "Heat", damage } };
                else if (!burncomp.Damage.DamageDict.ContainsKey("Heat"))
                    burncomp.Damage.DamageDict.Add("Heat", damage);
                else
                    burncomp.Damage.DamageDict["Heat"] = damage;
            }

            Dirty(uid, burncomp);
        }
    }
    #endregion

    #region Reactor Methods
    /// <summary>
    ///
    /// </summary>
    /// <param name="reactorPart">The reactor part.</param>
    /// <param name="reactorEnt">The entity representing the reactor this part is inserted into.</param>
    /// <param name="inGas">The gas to be processed.</param>
    /// <returns></returns>
    public GasMixture? ProcessGas(ReactorPartComponent reactorPart, Entity<NuclearReactorComponent> reactorEnt, GasMixture inGas)
    {
        if (!reactorPart.HasRodType(ReactorPartComponent.RodTypes.GasChannel))
            return null;

        GasMixture? ProcessedGas = null;
        if (reactorPart.AirContents != null)
        {
            var compTemp = reactorPart.Temperature;
            var gasTemp = reactorPart.AirContents.Temperature;

            var DeltaT = compTemp - gasTemp;
            var DeltaTr = (compTemp + gasTemp) * (compTemp - gasTemp) * (Math.Pow(compTemp, 2) + Math.Pow(gasTemp, 2));

            var k = MaterialSystem.CalculateHeatTransferCoefficient(reactorPart.Properties, null);
            var A = reactorPart.GasThermalCrossSection * (0.4 * 8);

            var ThermalEnergy = _atmosphereSystem.GetThermalEnergy(reactorPart.AirContents);

            var Hottest = Math.Max(gasTemp, compTemp);
            var Coldest = Math.Min(gasTemp, compTemp);

            var MaxDeltaE = Math.Clamp((k * A * DeltaT) + (5.67037442e-8 * A * DeltaTr),
                (compTemp * reactorPart.ThermalMass) - (Hottest * reactorPart.ThermalMass),
                (compTemp * reactorPart.ThermalMass) - (Coldest * reactorPart.ThermalMass));

            reactorPart.AirContents.Temperature = (float)Math.Clamp(gasTemp +
                (MaxDeltaE / _atmosphereSystem.GetHeatCapacity(reactorPart.AirContents, true)), Coldest, Hottest);

            reactorPart.Temperature = (float)Math.Clamp(compTemp -
                ((_atmosphereSystem.GetThermalEnergy(reactorPart.AirContents) - ThermalEnergy) / reactorPart.ThermalMass), Coldest, Hottest);

            if (gasTemp < 0 || compTemp < 0)
                throw new Exception("Reactor part temperature went below 0k.");

            if (reactorPart.Melted)
            {
                var T = _atmosphereSystem.GetTileMixture(reactorEnt.Owner, excite: true);
                if (T != null)
                    _atmosphereSystem.Merge(T, reactorPart.AirContents);
            }
            else
                ProcessedGas = reactorPart.AirContents;
        }

        if (inGas != null && _atmosphereSystem.GetThermalEnergy(inGas) > 0)
        {
            reactorPart.AirContents = inGas.RemoveVolume(reactorPart.GasVolume);
            reactorPart.AirContents.Volume = reactorPart.GasVolume;

            if (reactorPart.AirContents != null && reactorPart.AirContents.TotalMoles < 1)
            {
                if (ProcessedGas != null)
                {
                    _atmosphereSystem.Merge(ProcessedGas, reactorPart.AirContents);
                    reactorPart.AirContents.Clear();
                }
                else
                {
                    ProcessedGas = reactorPart.AirContents;
                    reactorPart.AirContents.Clear();
                }
            }
        }
        return ProcessedGas;
    }

    /// <summary>
    /// Processes heat transfer within the reactor grid.
    /// </summary>
    /// <param name="reactorPart">Reactor part applying the calculations.</param>
    /// <param name="reactorEnt">Reactor housing the reactor part.</param>
    /// <param name="AdjacentComponents">List of reactor parts next to the reactorPart.</param>
    /// <param name="reactorSystem">The SharedNuclearReactorSystem.</param>
    /// <exception cref="Exception">Calculations resulted in a sub-zero value.</exception>
    public void ProcessHeat(ReactorPartComponent reactorPart, Entity<NuclearReactorComponent> reactorEnt, ReactorPartComponent?[] AdjacentComponents, SharedNuclearReactorSystem reactorSystem)
    {
        var reactor = reactorEnt.Comp;

        // Intercomponent calculation
        foreach (var RC in AdjacentComponents)
        {
            if (RC == null)
                continue;

            var DeltaT = reactorPart.Temperature - RC.Temperature;
            var k = MaterialSystem.CalculateHeatTransferCoefficient(reactorPart.Properties, RC.Properties);
            var A = Math.Min(reactorPart.ThermalCrossSection, RC.ThermalCrossSection);

            reactorPart.Temperature = (float)(reactorPart.Temperature - (k * A * (0.5 * 8) / reactorPart.ThermalMass * DeltaT));
            RC.Temperature = (float)(RC.Temperature - (k * A * (0.5 * 8) / RC.ThermalMass * -DeltaT));

            if (RC.Temperature < 0 || reactorPart.Temperature < 0)
                throw new Exception("ReactorPart-ReactorPart temperature calculation resulted in sub-zero value.");

            ProcessHeatEffects(RC);
            ProcessHeatEffects(reactorPart);
        }

        // Component-Reactor calculation
        if (reactor != null)
        {
            var DeltaT = reactorPart.Temperature - reactor.Temperature;

            var k = MaterialSystem.CalculateHeatTransferCoefficient(reactorPart.Properties, _proto.Index(reactor.Material).Properties);
            var A = reactorPart.ThermalCrossSection;

            reactorPart.Temperature = (float)(reactorPart.Temperature - (k * A * (0.5 * 8) / reactorPart.ThermalMass * DeltaT));
            reactor.Temperature = (float)(reactor.Temperature - (k * A * (0.5 * 8) / reactor.ThermalMass * -DeltaT));

            if (reactor.Temperature < 0 || reactorPart.Temperature < 0)
                throw new Exception("Reactor-ReactorPart temperature calculation resulted in sub-zero value.");

            ProcessHeatEffects(reactorPart);
        }
        if (reactorPart.Temperature > reactorPart.MeltingPoint && reactorPart.MeltHealth > 0)
            reactorPart.MeltHealth -= _random.Next(10, 50 + 1);
        if (reactorPart.MeltHealth <= 0)
            Melt(reactorPart, reactorEnt, reactorSystem);

        return;

        // I would really like for these to be defined by the MaterialPrototype, like GasReactionPrototype, but it caused the client and server to fight when I tried
        // Also, function in a function because I found it funny
        void ProcessHeatEffects(ReactorPartComponent part)
        {
            switch (part.Material)
            {
                case "Plasma":
                    PlasmaTemperatureEffects(part);
                    break;
                default:
                    break;
            }
        }

        void PlasmaTemperatureEffects(ReactorPartComponent part)
        {
            var temperatureThreshold = Atmospherics.T0C + 80;
            if (part.Temperature <= temperatureThreshold || part.Properties.ActivePlasma <= 0)
                return;

            var molesPerUnit = 100f; // Arbitrary value for how much gaseous plasma is in each unit of active plasma

            var payload = new GasMixture();
            payload.SetMoles(Gas.Plasma, (float)Math.Min(part.Properties.ActivePlasma * molesPerUnit, Math.Log(((part.Temperature - temperatureThreshold) / 100) + 1)));
            payload.Temperature = part.Temperature;
            part.Properties.ActivePlasma -= payload.GetMoles(Gas.Plasma) / molesPerUnit;

            reactor.AirContents ??= new GasMixture();
            _atmosphereSystem.Merge(reactor.AirContents, payload);
        }
    }

    /// <summary>
    /// Melts the related ReactorPart.
    /// </summary>
    /// <param name="reactorPart">Reactor part to be melted</param>
    /// <param name="reactorEnt">Reactor housing the reactor part</param>
    /// <param name="reactorSystem">The SharedNuclearReactorSystem</param>
    public void Melt(ReactorPartComponent reactorPart, Entity<NuclearReactorComponent> reactorEnt, SharedNuclearReactorSystem reactorSystem)
    {
        if (reactorPart.Melted)
            return;

        reactorPart.Melted = true;
        reactorPart.IconStateCap += "_melted_" + _random.Next(1, 4 + 1);
        reactorSystem.UpdateGridVisual(reactorEnt);
        reactorPart.NeutronCrossSection = 5f;
        reactorPart.ThermalCrossSection = 20f;
        reactorPart.IsControlRod = false;

        if(reactorPart.HasRodType(ReactorPartComponent.RodTypes.GasChannel))
            reactorPart.GasThermalCrossSection = 0.1f;
    }

    /// <summary>
    /// Returns a list of neutrons from the interation of the given ReactorPart and initial neutrons.
    /// </summary>
    /// <param name="reactorPart">Reactor part applying the calculations.</param>
    /// <param name="neutrons">List of neutrons to be processed.</param>
    /// <param name="thermalEnergy">Thermal energy released from the process.</param>
    /// <returns>Post-processing list of neutrons.</returns>
    public List<ReactorNeutron> ProcessNeutrons(ReactorPartComponent reactorPart, List<ReactorNeutron> neutrons, out float thermalEnergy)
    {
        var preCalcTemp = reactorPart.Temperature;
        var result = new List<ReactorNeutron>(neutrons.Count);

        foreach (var neutron in neutrons)
        {
            if (Prob(reactorPart.Properties.Density * _rate * reactorPart.NeutronCrossSection * _bias))
            {
                if (neutron.velocity <= 1 && Prob(_rate * reactorPart.Properties.NeutronRadioactivity * _bias)) // neutron stimulated emission
                {
                    reactorPart.Properties.NeutronRadioactivity -= _reactant;
                    reactorPart.Properties.Radioactivity += _product;
                    for (var i = 0; i < _random.Next(3, 5 + 1); i++) // was 1, 5+1
                    {
                        result.Add(new() { dir = _random.NextAngle().GetDir(), velocity = _random.Next(2, 3 + 1) });
                    }
                    reactorPart.Temperature += 75f; // Was 50, increased to make neutron reactions stronger
                }
                else if (neutron.velocity <= 5 && Prob(_rate * reactorPart.Properties.Radioactivity * _bias)) // stimulated emission
                {
                    reactorPart.Properties.Radioactivity -= _reactant;
                    reactorPart.Properties.FissileIsotopes += _product;
                    for (var i = 0; i < _random.Next(3, 5 + 1); i++)// was 1, 5+1
                    {
                        result.Add(new() { dir = _random.NextAngle().GetDir(), velocity = _random.Next(1, 3 + 1) });
                    }
                    reactorPart.Temperature += 50f; // Was 25, increased to make neutron reactions stronger
                }
                else
                {
                    if (Prob(_rate * reactorPart.Properties.Hardness)) // reflection, based on hardness
                        // A really complicated way of saying do a 180 or a 180+/-45
                        neutron.dir = (neutron.dir.GetOpposite().ToAngle() + (_random.NextAngle() / 4) - (MathF.Tau / 8)).GetDir();
                    else if (reactorPart.IsControlRod)
                        neutron.velocity = 0;
                    else
                        neutron.velocity--;

                    if (neutron.velocity > 0)
                        result.Add(neutron);

                    reactorPart.Temperature += 1; // ... not worth the adjustment
                }
            }
            else
            {
                result.Add(neutron);
            }
        }
        if (Prob(reactorPart.Properties.NeutronRadioactivity * _rate * reactorPart.NeutronCrossSection))
        {
            var count = _random.Next(1, 5 + 1); // Was 3+1
            for (var i = 0; i < count; i++)
            {
                result.Add(new() { dir = _random.NextAngle().GetDir(), velocity = 3 });
            }
            reactorPart.Properties.NeutronRadioactivity -= _reactant / 2;
            reactorPart.Properties.Radioactivity += _product / 2;
            //This code has been deactivated so neutrons would have a bigger impact
            //reactorPart.Temperature += 13; // 20 * 0.65
        }
        if (Prob(reactorPart.Properties.Radioactivity * _rate * reactorPart.NeutronCrossSection))
        {
            var count = _random.Next(1, 5 + 1); // Was 3+1
            for (var i = 0; i < count; i++)
            {
                result.Add(new() { dir = _random.NextAngle().GetDir(), velocity = _random.Next(1, 3 + 1) });
            }
            reactorPart.Properties.Radioactivity -= _reactant / 2;
            reactorPart.Properties.FissileIsotopes += _product / 2;
            //This code has been deactivated so neutrons would have a bigger impact
            //reactorPart.Temperature += 6.5f; // 10 * 0.65
        }

        if (reactorPart.HasRodType(ReactorPartComponent.RodTypes.ControlRod))
        {
            if (!reactorPart.Melted && (reactorPart.NeutronCrossSection != reactorPart.ConfiguredInsertionLevel))
            {
                if (reactorPart.ConfiguredInsertionLevel < reactorPart.NeutronCrossSection)
                    reactorPart.NeutronCrossSection -= Math.Min(0.1f, reactorPart.NeutronCrossSection - reactorPart.ConfiguredInsertionLevel);
                else
                    reactorPart.NeutronCrossSection += Math.Min(0.1f, reactorPart.ConfiguredInsertionLevel - reactorPart.NeutronCrossSection);
            }
        }

        if (reactorPart.HasRodType(ReactorPartComponent.RodTypes.GasChannel))
            result = ProcessNeutronsGas(reactorPart, result);

        thermalEnergy = (reactorPart.Temperature - preCalcTemp) * reactorPart.ThermalMass;
        return result;
    }

    /// <summary>
    /// Processes neutrons interacting with gas in a reactor part.
    /// </summary>
    /// <param name="reactorPart">The reactor part to process neutrons for.</param>
    /// <param name="neutrons">The list of neutrons to process.</param>
    /// <returns>The updated list of neutrons after processing.</returns>
    private List<ReactorNeutron> ProcessNeutronsGas(ReactorPartComponent reactorPart, List<ReactorNeutron> neutrons)
    {
        if (reactorPart.AirContents == null) return neutrons;

        var result = new List<ReactorNeutron>(neutrons.Count + 8);
        foreach (var neutron in neutrons)
        {
            if (neutron.velocity <= 0)
                continue;

            var neutronCount = GasNeutronInteract(reactorPart);
            if (neutronCount > 1)
            {
                for (var i = 0; i < neutronCount; i++)
                    result.Add(new() { dir = _random.NextAngle().GetDir(), velocity = _random.Next(1, 3 + 1) });
            }
            else if (neutronCount >= 1)
            {
                result.Add(neutron);
            }
        }

        return result;
    }

    /// <summary>
    /// Performs neutron interactions with the gas in the reactor part.
    /// </summary>
    /// <param name="reactorPart">The reactor part to process neutron interactions for.</param>
    /// <returns>Change in number of neutrons.</returns>
    private int GasNeutronInteract(ReactorPartComponent reactorPart)
    {
        if (reactorPart.AirContents == null)
            return 1;

        var neutronCount = 1;
        var gas = reactorPart.AirContents;

        if (gas.GetMoles(Gas.Plasma) > 1)
        {
            var reactMolPerLiter = 0.25;
            var reactMol = reactMolPerLiter * gas.Volume;

            var plasma = gas.GetMoles(Gas.Plasma);
            var plasmaReactCount = (int)Math.Round((plasma - (plasma % reactMol)) / reactMol) + (Prob(plasma - (plasma % reactMol)) ? 1 : 0);
            plasmaReactCount = _random.Next(0, plasmaReactCount + 1);
            gas.AdjustMoles(Gas.Plasma, plasmaReactCount * -0.5f);
            gas.AdjustMoles(Gas.Tritium, plasmaReactCount * 2);
            neutronCount += plasmaReactCount;
        }

        if (gas.GetMoles(Gas.CarbonDioxide) > 1)
        {
            var reactMolPerLiter = 0.4;
            var reactMol = reactMolPerLiter * gas.Volume;

            var co2 = gas.GetMoles(Gas.CarbonDioxide);
            var co2ReactCount = (int)Math.Round((co2 - (co2 % reactMol)) / reactMol) + (Prob(co2 - (co2 % reactMol)) ? 1 : 0);
            co2ReactCount = _random.Next(0, co2ReactCount + 1);
            reactorPart.Temperature += Math.Min(co2ReactCount, neutronCount);
            neutronCount -= Math.Min(co2ReactCount, neutronCount);
        }

        if (gas.GetMoles(Gas.Tritium) > 1)
        {
            var reactMolPerLiter = 0.5;
            var reactMol = reactMolPerLiter * gas.Volume;

            var tritium = gas.GetMoles(Gas.Tritium);
            var tritiumReactCount = (int)Math.Round((tritium - (tritium % reactMol)) / reactMol) + (Prob(tritium - (tritium % reactMol)) ? 1 : 0);
            tritiumReactCount = _random.Next(0, tritiumReactCount + 1);
            if (tritiumReactCount > 0)
            {
                gas.AdjustMoles(Gas.Tritium, -1 * tritiumReactCount);
                reactorPart.Temperature += 1 * tritiumReactCount;
                switch (_random.Next(0, 5))
                {
                    case 0:
                        gas.AdjustMoles(Gas.Oxygen, 0.5f * tritiumReactCount);
                        break;
                    case 1:
                        gas.AdjustMoles(Gas.Nitrogen, 0.5f * tritiumReactCount);
                        break;
                    case 2:
                        gas.AdjustMoles(Gas.Ammonia, 0.1f * tritiumReactCount);
                        break;
                    case 3:
                        gas.AdjustMoles(Gas.NitrousOxide, 0.1f * tritiumReactCount);
                        break;
                    case 4:
                        gas.AdjustMoles(Gas.Frezon, 0.1f * tritiumReactCount);
                        break;
                    default:
                        break;
                }
            }
        }

        return neutronCount;
    }

    /// <summary>
    /// Probablity check that accepts chances > 100%
    /// </summary>
    /// <param name="chance">The chance percentage between 0 and 100.</param>
    private bool Prob(double chance) => _random.NextDouble() <= chance / 100;
    #endregion
}
