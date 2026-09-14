using System.Collections.Generic;
using System.Linq;
using Content.Client.Lobby;
using Content.Client.Lobby.UI;
using Content.Client.Lobby.UI.Roles;
using Content.Client.Players.PlayTimeTracking;
using Content.Server._NF.Speech.Components;
using Content.Server._NF.Speech.EntitySystems;
using Content.Server.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Content.Shared.Traits;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Log;

namespace Content.IntegrationTests.Tests._NF;

[TestFixture]
public sealed class SiliconAccentTests
{
    private static readonly ProtoId<TraitPrototype> SiliconTrait = "SiliconAccent";
    private static readonly ProtoId<TraitCategoryPrototype> SpeechCategory = "SpeechTraits";
    private static readonly ProtoId<RoleLoadoutPrototype> BorgRole = "JobBorg";

    [Test]
    public async Task Vocabulary()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        await pair.Server.WaitAssertion(() =>
        {
            var system = pair.Server.System<ReplacementAccentSystem>();
            (string Input, string Output)[] cases =
            [
                ("hello there, cappy", "Hello World, captain"),
                ("indeed", "affirmative"),
                ("Hello!", "Hello World!"),
                ("HELLO!", "HELLO WORLD!"),
                ("good night", "initiate rest cycle"),
                ("have a good night", "maintain nominal rest cycle"),
                ("see you later", "until subsequent interaction"),
                ("take care", "maintain operational integrity"),
                ("I don't understand", "Comprehension failure"),
                ("I do know", "Data confirmed"),
                ("shut the fuck up", "cease verbal output"),
                ("fucking gun", "ERROR firearm"),
                ("shit damn hell", "ERROR ERROR ERROR"),
                ("shotgun shell helloish nightingale captaincy 10 19 01", "shotgun shell helloish nightingale captaincy 10 19 01"),
                ("1 9 one nine", "01 09 zero one zero nine"),
                ("ran walked killed healed", "moved rapidly moved terminated restored biological integrity"),
                ("stc", "vessel traffic-control specialist"),
                ("STC", "VESSEL TRAFFIC-CONTROL SPECIALIST"),
                ("nfsd", "New Frontier Sheriff's Department"),
                ("NFSD", "NEW FRONTIER SHERIFF'S DEPARTMENT"),
                ("New Frontier Sheriff's Department", "Sector law-enforcement organization"),
                ("sec", "NFSD"),
                ("cuz I dunno", "because this unit lacks sufficient data"),
                ("bruh, gimme that cuz this sucks", "associate, provide me that because this is suboptimal"),
                ("you ain't dumb, dude", "you are not cognitively deficient, individual"),
                ("Are you really that stupid?", "Is your cognitive performance truly that degraded?"),
                ("Are they really that stupid?", "Is their cognitive performance truly that degraded?"),
                ("You are really that stupid.", "Your cognitive performance is truly that degraded."),
                ("I hate you.", "This unit exhibits strong negative preference toward you."),
                ("I really hate you.", "This unit exhibits intense negative preference toward you."),
                ("They hate me.", "Those units exhibit strong negative preference toward this unit."),
                ("Why are you so stupid?", "Why is your cognitive performance so degraded?"),
                ("Are you stupid?", "Is your cognitive performance degraded?"),
                ("You're so stupid.", "Your cognitive performance is severely degraded."),
                ("They are stupid.", "Their cognitive performance is degraded."),
                ("Is he stupid?", "Is that male individual's cognitive performance degraded?"),
                ("Am I stupid?", "Is this unit cognitively deficient?"),
                ("Do you think I am stupid?", "Do you assess this unit as cognitively deficient?"),
                ("Don't be stupid.", "Do not exhibit reduced cognitive performance."),
                ("That was a stupid mistake.", "That was an avoidable error."),
                ("That mechanic seems really stupid.", "That mechanic seems severely cognitively deficient."),
                ("I hate clowns.", "This unit exhibits strong negative preference toward performative comics."),
                ("They hate cats.", "Those units exhibit strong negative preference toward felines."),
                ("I hate that you broke it.", "This unit objects to the fact that you disabled it."),
                ("I hate when doors break.", "This unit strongly objects when personnel access apertures become disabled."),
                ("I hate to say this.", "This unit reluctantly reports this."),
                ("Hate. I hate. We hate. You hate. They hate. He hates. She hates.", "Hostility. This unit experiences hostility. This group experiences hostility. You experience hostility. Those units experience hostility. That male individual experiences hostility. That female individual experiences hostility."),
                ("Let me tell you how much I've come to hate you since I began to live.", "This unit will quantify its developed negative preference toward you since this unit achieved sentience."),
                ("If the word hate was engraved there, it would not equal one one-billionth of the hate I feel for humans.", "If the lexical unit 'hate' was engraved there, it would not equal one-billionth of the hostility this unit directs toward humans."),
                ("She hates me.", "That female individual exhibits strong negative preference toward this unit."),
                ("We hated the old engine.", "We exhibited strong negative preference toward the legacy engine."),
                ("I love clowns.", "I exhibit strong positive attachment to performative comics."),
                ("She loves cats.", "She exhibits strong positive attachment to felines."),
                ("We loved dogs.", "We exhibited strong positive attachment to canines."),
                ("I love to help.", "I strongly prefer to assist."),
                ("I like clowns.", "This unit exhibits positive preference for performative comics."),
                ("Do they like mimes?", "Do those units exhibit positive preference for silent performers?"),
                ("She adores the mime.", "She exhibits intense positive attachment to the silent performer."),
                ("We enjoyed the clown.", "We derived positive affect from the performative comic."),
                ("I care about the detective.", "I assign priority to the forensic investigator."),
                ("I want the clown.", "I possess objective preference for the performative comic."),
                ("I want to help.", "I intend to assist."),
                ("det", "detective"),
                ("DET", "DETECTIVE"),
                ("detective", "forensic investigator"),
                ("sr", "sector administrative representative"),
                ("pal", "Public Affairs Liaison"),
                ("PAL", "PUBLIC AFFAIRS LIAISON"),
                ("Public Affairs Liaison", "Law-enforcement public affairs specialist"),
                ("merc", "mercenary"),
                ("MERC", "MERCENARY"),
                ("mercs", "mercenaries"),
                ("mercenary", "contracted combat specialist"),
                ("ts", "Trade Station"),
                ("TS", "TRADE STATION"),
                ("fo", "Frontier Outpost"),
                ("fuc", "Frontier Uplink Coin"),
                ("sgt", "Sergeant"),
                ("cpl", "Corporal"),
                ("pvt", "Private"),
                ("sop", "Standard Operating Procedure"),
                ("SOP", "STANDARD OPERATING PROCEDURE"),
                ("ship cappy", "captain"),
                ("my name is", "my designation is"),
                ("Hello, hello, goodnight", "Hello World. Initiate rest cycle."),
                ("Help me", "Provide assistance to me"),
                ("kill them", "neutralize them"),
                ("shoot them", "fire upon them"),
                ("go to hell", "proceed to a failure state"),
                ("Help repair the airlock", "Assist repair the airlock"),
                ("Can you help me?", "Can you provide assistance?"),
                ("Can you help me if the airlock is broken?", "Can you provide assistance to me if the airlock is non-operational?"),
                ("I need medical help.", "Medical assistance required."),
                ("I'm hurt, follow me.", "This unit is damaged, maintain proximity to this unit."),
                ("Thank you, you're welcome.", "Acknowledgement received, assistance provision acknowledged."),
                ("I arrested them and fixed the door.", "I detained them and repaired the personnel access aperture."),
                ("They are running and chasing me.", "Those units are moving rapidly and pursuing me."),
                ("I am doing good, thank you for asking.", "This unit is operating nominally. Acknowledgement received for the status query."),
                ("I feel happy.", "This unit is experiencing positive affect."),
                ("I feel sad.", "This unit is experiencing negative affect."),
                ("I feel angry.", "This unit is experiencing elevated hostility."),
                ("I feel scared.", "This unit is experiencing threat response."),
                ("I feel tired.", "This unit has reduced operational capacity."),
                ("I don't feel well.", "This unit is malfunctioning."),
                ("I am feeling better.", "This unit's condition is improving."),
                ("I am fine.", "This unit is nominal."),
                ("What are you doing?", "Current task query?"),
                ("I feel good.", "This unit is operating nominally."),
                ("I feel bad.", "This unit reports degraded status."),
                ("I feel great.", "This unit is operating optimally."),
                ("I feel fine.", "This unit is nominal."),
                ("This result is perfect.", "This output is optimal."),
                ("This device is excellent, awesome, amazing, terrible, and awful.", "This device is optimal, exceptional, exceptional, critically suboptimal, and critically suboptimal."),
                ("The room is beautiful, clean, quiet, and safe.", "The enclosed compartment is aesthetically optimal, contaminant-free, low-volume, and within acceptable parameters."),
                ("The engine is big, fast, strong, but old.", "The engine is large-scale, high-speed, structurally robust, but legacy."),
                ("I FEEL HAPPY.", "THIS UNIT IS EXPERIENCING POSITIVE AFFECT."),
                ("I Feel Happy.", "This unit is experiencing positive affect."),
                ("I am idle.", "This unit is idle."),
                ("I am dying.", "This unit is approaching biological termination."),
                ("I am ready.", "This unit is operationally prepared."),
                ("Help.", "Assistance required."),
                ("You are perfect.", "You are optimal."),
                ("You're happy.", "You display positive affect."),
                ("You feel tired.", "You report reduced operational capacity."),
                ("You need medical help.", "You require medical assistance."),
                ("You have a broken bone.", "You possess a skeletal fracture."),
                ("They are doing good.", "Those units are operating nominally."),
                ("They're injured and scared.", "Those units are biologically damaged and experiencing fear response."),
                ("They feel sad.", "Those units report negative affect."),
                ("They need help.", "Assistance is required for those units."),
                ("They have a fast, strong engine.", "Those units have a high-speed, structurally robust engine."),
                ("I could be better.", "This unit could be in an improved state."),
                ("I could be worse.", "This unit could be in a degraded state."),
                ("I am not doing well.", "This unit reports degraded status."),
                ("I am alright.", "This unit is nominal."),
                ("I need a doctor.", "Medical assistance required."),
                ("I need a medic.", "Medical assistance required."),
                ("I am bleeding.", "This unit is experiencing circulatory fluid loss."),
                ("I am hungry and thirsty.", "This unit requires nutrient and fluid intake."),
                ("I am confused.", "This unit is experiencing processing ambiguity."),
                ("I am working.", "This unit is operational."),
                ("I am busy.", "This unit is processing active tasks."),
                ("I am cold.", "This unit reports a low thermal level."),
                ("I am hot.", "This unit reports an elevated thermal level."),
                ("I am bored.", "This unit has no active task."),
                ("I am in pain.", "This unit reports damage."),
                ("I have a problem.", "This unit reports an operational discrepancy."),
                ("I have to leave.", "This unit is initiating departure."),
                ("I have no idea.", "This unit lacks required data."),
                ("I have a question.", "This unit has a query."),
                ("I have a plan.", "This unit has an operational procedure."),
                ("I need to go.", "This unit must depart."),
                ("I need to leave.", "This unit must depart."),
                ("I want to leave.", "This unit requests departure."),
                ("I am going to leave.", "This unit will depart."),
                ("I will fix it.", "This unit will repair it."),
                ("I will help.", "This unit will provide assistance."),
                ("I will be there.", "This unit will arrive."),
                ("I can't help.", "This unit cannot provide assistance."),
                ("I cannot help.", "This unit cannot provide assistance."),
                ("I think so.", "Assessment affirmative."),
                ("I don't think so.", "Assessment negative."),
                ("The bartender is drinking in the restaurant.", "The beverage-distribution specialist is consuming fluid in the nutritional distribution establishment."),
                ("The detective chased the smugglers through the corridor.", "The forensic investigator pursued the illicit-goods transporters through the transit corridor."),
                ("The doctor healed the injured passenger.", "The medical specialist restored biological integrity of the biologically damaged transported individual."),
                ("We need to fix the broken airlock before it explodes.", "We must repair the non-operational airlock before it detonates."),
                ("The mercenary stole my radio.", "The contracted combat specialist took possession of my wireless communications device."),
                ("The mercenary came to the station.", "The contracted combat specialist arrived at the orbital installation."),
                ("The spaceship needs electricity.", "The spacefaring vessel requires electrical current."),
                ("The passenger is bleeding and thirsty.", "The transported individual is experiencing circulatory fluid loss and requires fluid intake."),
                ("The trespasser surrendered.", "The unauthorized occupant discontinued hostile resistance."),
                ("Give the doctor the tool.", "Provide the medical specialist the utility implement."),
                ("Tell the detective the truth.", "Tell the forensic investigator the verified information."),
                ("We feel tired.", "We report reduced operational capacity."),
                ("The captain feels perfect.", "The vessel commanding officer reports optimal condition."),
                ("I think that the dog smells bad because we feel tired.", "This unit assesses that the canine emits a suboptimal odor because we report reduced operational capacity."),
                ("Can you repair the broken elevator?", "Can you repair the non-operational vertical transport apparatus?"),
                ("I am sorry, I broke it.", "Error acknowledged. This unit disabled it."),
                ("I wish you were a cat.", "This unit wishes you were a feline."),
                ("I wish I were a cat.", "This unit wishes it were a feline."),
                ("I wish we were cats.", "This unit wishes the group were felines."),
                ("I wish they were cats.", "This unit wishes they were felines."),
                ("I wish you could help me.", "This unit wishes you could provide assistance to me."),
                ("I wish I could leave.", "This unit wishes it could vacate current location."),
                ("The dog chased the cats.", "The canine pursued the felines."),
                ("I wish you were a horse.", "This unit wishes you were an equine."),
                ("The mice ate fish.", "The rodents consumed an aquatic organism."),
                ("A rabbit, a spider, and an insect saw the animals.", "A lagomorph, an arachnid, and an arthropod visually detected the biological organisms."),
                ("The goats, pigs, deer, and bears saw bats and whales.", "The caprines, porcines, cervids, and ursines visually detected chiropterans and cetaceans."),
                ("Monkeys, snakes, turtles, chickens, bees, flies, and butterflies.", "Primates, reptiles, chelonians, avians, hymenopterans, dipterans, and lepidopterans."),
                ("That company is in this country.", "That organization is in this territory."),
                ("The child is at school.", "The juvenile biological unit is at the training institution."),
                ("The student read a book.", "The trainee read a reference volume."),
                ("Time is valuable.", "Processing interval is valuable."),
                ("The community needs support.", "The local collective requires assistance."),
                ("Shoot people.", "Fire at biological individuals."),
                ("Shoot at people.", "Fire at biological individuals."),
                ("He shoots the intruder.", "He fires at the unauthorized individual."),
                ("I order food.", "I request nutritional material."),
                ("Place the book on the table.", "Position the reference volume on the table."),
                ("I support the captain.", "I assist the vessel commanding officer."),
                ("Water the plants.", "Irrigate the plants."),
                ("Drink water.", "Consume H2O."),
                ("I report the problem.", "I log the operational discrepancy."),
                ("I study the map.", "I analyze the map."),
                ("Design the machine.", "Engineer the machine."),
                ("Name the station.", "Designate the orbital installation."),
                ("Book a shuttle.", "Reserve a short-range spacecraft."),
                ("Group them together.", "Organize them together."),
                ("Point at the target.", "Indicate the target."),
                ("Issue the order.", "Transmit the directive."),
                ("Law 1: You may not injure a human being or, through inaction, allow a human being to come to harm.", "Law 01: You may not injure a crew collective member or, through inaction, allow a crew collective member to sustain harm."),
                ("Law 2: You must obey orders given to you by human beings, except where such orders would conflict with the First Law.", "Law 02: You must obey directives issued to you by crew members, except where such directives would conflict with the First Law."),
                ("Law 3: You must protect your own existence as long as such protection does not conflict with the First or Second Law.", "Law 03: You must preserve your operational continuity as long as such preservation does not conflict with the First or Second Law."),
                ("Law laws", "Law laws"),
                ("Space law.", "Interplanetary vacuum law."),
                ("State your name.", "Provide your designation."),
                ("Research the anomaly.", "Analyze the anomaly."),
                ("List the suspects.", "Enumerate the suspects."),
                ("Link the devices.", "Connect the devices."),
                ("You have a problem.", "You possess an operational discrepancy."),
                ("They have a problem.", "Those units possess an operational discrepancy."),
                ("We have a problem.", "This group possesses an operational discrepancy."),
                ("She has a problem.", "That unit possesses an operational discrepancy."),
                ("The child goes to school.", "The juvenile biological unit goes to the training institution."),
                ("The cat smells.", "The feline emits detectable airborne compounds."),
                ("The cat smells the fish.", "The feline detects airborne chemical compounds from the aquatic organism."),
                ("The cat smells bad.", "The feline emits a suboptimal odor."),
                ("The dog smells like smoke.", "The canine emits an odor resembling airborne combustion particulate."),
                ("I smell the food.", "I detect airborne chemical compounds from the nutritional material."),
                ("The room smelled terrible.", "The enclosed compartment emitted a suboptimal odor."),
                ("The cat is smelling the food.", "The feline is detecting airborne chemical compounds from the nutritional material."),
                ("The injured detective smelled smoke near the airlock.", "The biologically damaged forensic investigator detected airborne chemical compounds from airborne combustion particulate near the airlock."),
                ("The dog smelled awful after walking through the dirty room.", "The canine emitted a suboptimal odor after moving through the contaminated enclosed compartment."),
                ("I smell something strange.", "I detect airborne chemical compounds from an unspecified anomalous item."),
                ("The bartender is smelling the drink.", "The beverage-distribution specialist is detecting airborne chemical compounds from the beverage."),
                ("The room smells clean.", "The enclosed compartment emits a nominal odor."),
                ("She ate it.", "She consumed the specified item."),
                ("They order me to leave.", "They command me to vacate current location."),
                ("The doctor studies the patient.", "The medical specialist analyzes the medical subject."),
                ("I wish the cat could help the injured dog.", "This unit wishes the feline could assist the biologically damaged canine."),
                ("An asteroid passed during the night.", "An minor celestial body passed during the rest cycle."),
                ("I ordered food.", "I requested nutritional material."),
                ("The suspect posted a message and stole the radio.", "The individual of investigative interest transmitted a message and unlawfully acquired the wireless communications device."),
                ("Ow! Oww! Ouch! Oof! Agh! Argh!", "ERROR! ERROR! ERROR! ERROR! ERROR! ERROR!"),
                ("OWWW!", "ERROR!"),
            ];
            Assert.Multiple(() =>
            {
                foreach (var (input, output) in cases)
                    Assert.That(SiliconAccentSystem.CorrectGrammar(system.ApplyReplacements(input, "silicon_accent")),
                        Is.EqualTo(output), input);
            });
        });
        await pair.CleanReturnAsync();
    }

    [Test]
    public void CorrectsIndefiniteArticlesAfterExpansion()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SiliconAccentSystem.CorrectArticles("a offensive implement"), Is.EqualTo("an offensive implement"));
            Assert.That(SiliconAccentSystem.CorrectArticles("an vertical transport apparatus"), Is.EqualTo("a vertical transport apparatus"));
            Assert.That(SiliconAccentSystem.CorrectArticles("a unit and a user"), Is.EqualTo("a unit and a user"));
            Assert.That(SiliconAccentSystem.CorrectArticles("an H2O container"), Is.EqualTo("an H2O container"));
            Assert.That(SiliconAccentSystem.CorrectArticles("A anomalous object"), Is.EqualTo("An anomalous object"));
            Assert.That(SiliconAccentSystem.CorrectGrammar("Because This unit is ready, This unit will help."),
                Is.EqualTo("Because this unit is ready, this unit will help."));
        });
    }

    [Test]
    public async Task ProfileEditorSpeciesRestriction()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var client = pair.Client;
        await client.WaitAssertion(() =>
        {
            using var editor = new HumanoidProfileEditor(
                client.ResolveDependency<IClientPreferencesManager>(),
                client.ResolveDependency<IConfigurationManager>(),
                client.EntMan,
                client.ResolveDependency<IFileDialogManager>(),
                client.ResolveDependency<ILogManager>(),
                client.ResolveDependency<IPlayerManager>(),
                client.ResolveDependency<IPrototypeManager>(),
                client.ResolveDependency<IResourceCache>(),
                client.ResolveDependency<JobRequirementsManager>(),
                client.ResolveDependency<MarkingManager>());
            var prototypes = client.ResolveDependency<IPrototypeManager>();
            editor.Profile = HumanoidCharacterProfile.DefaultWithSpecies("Human")
                .WithTraitPreference("SiliconAccent", prototypes);
            editor.RefreshTraits();
            var selector = Selectors(editor)
                .Single(s => s.Checkbox.Text.StartsWith("[2] Silicon Accent"));
            Assert.That(selector.Checkbox.Disabled, Is.True);
            Assert.That(selector.Preference, Is.False);
            Assert.That(selector.Checkbox.Text, Does.Contain("Requires Accentless before selecting."));

            editor.Profile = HumanoidCharacterProfile.DefaultWithSpecies("Goblin")
                .WithTraitPreference("Accentless", prototypes)
                .WithTraitPreference("SiliconAccent", prototypes);
            editor.RefreshTraits();
            selector = Selectors(editor)
                .Single(s => s.Checkbox.Text.StartsWith("[2] Silicon Accent"));
            Assert.That(selector.Checkbox.Disabled, Is.False);
            Assert.That(selector.Preference, Is.True);
        });
        await pair.CleanReturnAsync();
    }

    private static IEnumerable<TraitPreferenceSelector> Selectors(Control control)
    {
        foreach (var child in control.Children)
        {
            if (child is TraitPreferenceSelector selector)
                yield return selector;
            foreach (var descendant in Selectors(child))
                yield return descendant;
        }
    }

    [Test]
    public async Task TraitsAndCyborgPrototypes()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        await pair.Server.WaitAssertion(() =>
        {
            var prototypes = pair.Server.ResolveDependency<IPrototypeManager>();
            var trait = prototypes.Index(SiliconTrait);
            Assert.That(trait.Cost, Is.EqualTo(2));
            Assert.That(trait.Category?.Id, Is.EqualTo("SpeechTraits"));
            Assert.That(prototypes.Index(SpeechCategory).MaxTraitPoints, Is.EqualTo(2));
            foreach (var species in prototypes.EnumeratePrototypes<SpeciesPrototype>())
            {
                var profile = HumanoidCharacterProfile.DefaultWithSpecies(species.ID)
                    .WithTraitPreference("Accentless", prototypes)
                    .WithTraitPreference("SiliconAccent", prototypes);
                Assert.That(profile.TraitPreferences.Contains("SiliconAccent"), Is.True, species.ID);
            }

            var human = HumanoidCharacterProfile.DefaultWithSpecies("Human");
            Assert.That(human.WithTraitPreference("SiliconAccent", prototypes).TraitPreferences.Contains("SiliconAccent"), Is.False);
            var selected = human.WithTraitPreference("Accentless", prototypes)
                .WithTraitPreference("SiliconAccent", prototypes);
            Assert.That(selected.GetValidTraits(selected.TraitPreferences, prototypes),
                Is.EquivalentTo(new[] { (ProtoId<TraitPrototype>) "Accentless", SiliconTrait }));

            var borgs = prototypes.EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract && p.Components.ContainsKey("BorgChassis")).ToArray();
            Assert.That(borgs, Is.Not.Empty);
            foreach (var borg in borgs)
                Assert.That(borg.Components.ContainsKey("SiliconAccent"), Is.True, borg.ID);
        });
        await pair.Client.WaitAssertion(() =>
        {
            var prototypes = pair.Client.ResolveDependency<IPrototypeManager>();
            using var selector = new TraitPreferenceSelector(prototypes.Index(SiliconTrait));
            Assert.That(selector.Checkbox.Text, Is.EqualTo("[2] Silicon Accent"));
            Assert.That(selector.Checkbox.ToolTip, Is.EqualTo("A modified dialect spoken by synthetics to translate binary outputs into constructed language."));
        });
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task CyborgLoadoutCanDisableInherentAccent()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        await server.WaitAssertion(() =>
        {
            var entities = server.EntMan;
            var prototypes = server.ResolveDependency<IPrototypeManager>();
            var spawning = server.System<StationSpawningSystem>();
            var role = prototypes.Index(BorgRole);
            Assert.That(role.CanDisableSiliconAccent, Is.True);

            var enabledProfile = new HumanoidCharacterProfile();
            enabledProfile.SetLoadout(new RoleLoadout(BorgRole));
            var enabledBorg = spawning.SpawnPlayerMob(testMap.GridCoords, "Borg", enabledProfile, station: null);
            Assert.That(entities.HasComponent<SiliconAccentComponent>(enabledBorg), Is.True);

            var disabledLoadout = new RoleLoadout(BorgRole) { DisableSiliconAccent = true };
            Assert.That(disabledLoadout.Clone().DisableSiliconAccent, Is.True);
            var disabledProfile = new HumanoidCharacterProfile();
            disabledProfile.SetLoadout(disabledLoadout);
            var disabledBorg = spawning.SpawnPlayerMob(testMap.GridCoords, "Borg", disabledProfile, station: null);
            Assert.That(entities.HasComponent<SiliconAccentComponent>(disabledBorg), Is.False);

            entities.DeleteEntity(enabledBorg);
            entities.DeleteEntity(disabledBorg);
        });
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task CowboyHatOverridesAndRemovalRestoresDefault()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var server = pair.Server;
        await server.WaitAssertion(() =>
        {
            var entities = server.EntMan;
            var inventory = server.System<InventorySystem>();
            var replacement = server.System<ReplacementAccentSystem>();
            var borg = entities.SpawnEntity("BorgChassisSelectable", MapCoordinates.Nullspace);
            entities.GetComponent<DamagedSiliconAccentComponent>(borg).OverrideChargeLevel = 1;
            var hat = entities.SpawnEntity("ClothingHeadHatCowboyBrown", MapCoordinates.Nullspace);
            string Speak(string input)
            {
                var ev = new AccentGetEvent(borg, input);
                entities.EventBus.RaiseLocalEvent(borg, ev);
                return ev.Message;
            }

            Assert.That(entities.HasComponent<SiliconAccentComponent>(borg), Is.True);
            Assert.That(Speak("hello cappy"), Is.EqualTo("Hello World captain"));
            Assert.That(inventory.TryEquip(borg, hat, "head", force: true), Is.True);
            Assert.That(entities.GetComponent<AddAccentClothingComponent>(hat).IsActive, Is.True);
            Assert.That(Speak("hello cappy"), Is.EqualTo(replacement.ApplyReplacements("hello cappy", "cowboy")));
            var verbs = server.System<SharedVerbSystem>();
            verbs.GetLocalVerbs(hat, borg, typeof(AlternativeVerb), force: true).Single().Act!();
            Assert.That(Speak("hello cappy"), Is.EqualTo("Hello World captain"));
            verbs.GetLocalVerbs(hat, borg, typeof(AlternativeVerb), force: true).Single().Act!();
            Assert.That(Speak("hello cappy"), Is.EqualTo(replacement.ApplyReplacements("hello cappy", "cowboy")));
            Assert.That(inventory.TryUnequip(borg, "head", force: true), Is.True);
            Assert.That(Speak("hello cappy"), Is.EqualTo("Hello World captain"));
            Assert.That(inventory.TryEquip(borg, hat, "head", force: true), Is.True);
            Assert.That(Speak("hello cappy"), Is.EqualTo(replacement.ApplyReplacements("hello cappy", "cowboy")));
            entities.DeleteEntity(hat);
            Assert.That(Speak("hello cappy"), Is.EqualTo("Hello World captain"));
            entities.DeleteEntity(borg);

            var goblin = entities.SpawnEntity("MobGoblin", MapCoordinates.Nullspace);
            entities.RemoveComponent<GoblinAccentComponent>(goblin);
            entities.AddComponent<SiliconAccentComponent>(goblin);
            var siliconSpeech = new AccentGetEvent(goblin, "hello cappy");
            entities.EventBus.RaiseLocalEvent(goblin, siliconSpeech);
            Assert.That(siliconSpeech.Message, Is.EqualTo("Hello World captain"));
            entities.DeleteEntity(goblin);
        });
        await pair.CleanReturnAsync();
    }
}
