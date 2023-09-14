using Content.IntegrationTests.Tests.Interaction;

namespace Content.IntegrationTests.Tests.Construction.Interaction;

public sealed class MachineConstruction : InteractionTest
{
    private const string MachineFrame = "MachineFrame";
    private const string Unfinished = "UnfinishedMachineFrame";
    private const string ProtolatheBoard = "ProtolatheMachineCircuitboard";
    private const string Protolathe = "Protolathe";
    private const string Beaker = "Beaker";

    [Test]
    public async Task ConstructProtolathe()
    {
        await StartConstruction(MachineFrame);
        await Interact(Steel, 5);
        ClientAssertPrototype(Unfinished, ClientTarget);
        Target = CTestSystem.Ghosts[ClientTarget!.Value.GetHashCode()];
        await Interact(Wrench, Cable);
        AssertPrototype(MachineFrame);
        await Interact(ProtolatheBoard, Bin1, Bin1, Manipulator1, Manipulator1, Beaker, Beaker, Screw);
        AssertPrototype(Protolathe);
    }

    [Test]
    public async Task DeconstructProtolathe()
    {
        await StartDeconstruction(Protolathe);
        await Interact(Screw, Pry);
        AssertPrototype(MachineFrame);
        await Interact(Pry, Cut);
        AssertPrototype(Unfinished);
        await Interact(Wrench, Screw);
        AssertDeleted();
        await AssertEntityLookup(
            (Steel, 5),
            (Cable, 1),
            (Beaker, 2),
            (Manipulator1, 2),
            (Bin1, 2),
            (ProtolatheBoard, 1));
    }

    [Test]
    public async Task ChangeMachine()
    {
        // Partially deconstruct a protolathe.
        await SpawnTarget(Protolathe);
        await Interact(Screw, Pry, Pry);
        AssertPrototype(MachineFrame);

        // Change it into an autolathe
        await Interact("AutolatheMachineCircuitboard");
        AssertPrototype(MachineFrame);
        await Interact(Bin1, Bin1, Bin1, Manipulator1, Glass, Screw);
        AssertPrototype("Autolathe");
    }

    [Test]
    public async Task UpgradeLathe()
    {
        // Partially deconstruct a protolathe.
        await SpawnTarget(Protolathe);
        var serverTarget = SEntMan.GetEntity(Target!.Value);

        // Initially has all quality-1 parts.
        foreach (var part in SConstruction.GetAllParts(serverTarget))
        {
            Assert.That(part.Rating, Is.EqualTo(1));
        }

        // Partially deconstruct lathe
        await Interact(Screw, Pry, Pry);
        AssertPrototype(MachineFrame);

        // Reconstruct with better parts.
        await Interact(ProtolatheBoard, Bin4, Bin4, Manipulator4, Manipulator4, Beaker, Beaker);
        await Interact(Screw);
        AssertPrototype(Protolathe);

        // Query now returns higher quality parts.
        foreach (var part in SConstruction.GetAllParts(SEntMan.GetEntity(Target!.Value)))
        {
            Assert.That(part.Rating, Is.EqualTo(4));
        }
    }
}

