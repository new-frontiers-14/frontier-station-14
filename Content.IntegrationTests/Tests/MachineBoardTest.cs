using System.Collections.Generic;
using System.Linq;
using Content.Server.Construction.Components;
using Content.Shared.Construction.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Content.Shared.Construction.Prototypes; // Frontier

namespace Content.IntegrationTests.Tests;

public sealed class MachineBoardTest
{
    /// <summary>
    /// A list of machine boards that can be ignored by this test.
    /// </summary>
    private readonly HashSet<string> _ignoredPrototypes = new()
    {
        //These have their own construction thing going on here
        "MachineParticleAcceleratorEndCapCircuitboard",
        "MachineParticleAcceleratorFuelChamberCircuitboard",
        "MachineParticleAcceleratorFuelChamberCircuitboard",
        "MachineParticleAcceleratorPowerBoxCircuitboard",
        "MachineParticleAcceleratorEmitterStarboardCircuitboard",
        "MachineParticleAcceleratorEmitterForeCircuitboard",
        "MachineParticleAcceleratorEmitterPortCircuitboard",
        "ParticleAcceleratorComputerCircuitboard"
    };

    /// <summary>
    /// Ensures that every single machine board's corresponding entity
    /// is a machine and can be properly deconstructed.
    /// </summary>
    [Test]
    public async Task TestMachineBoardHasValidMachine()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var compFact = server.ResolveDependency<IComponentFactory>();

        await server.WaitAssertion(() =>
        {
            foreach (var p in protoMan.EnumeratePrototypes<EntityPrototype>()
                         .Where(p => !p.Abstract)
                         .Where(p => !pair.IsTestPrototype(p))
                         .Where(p => !_ignoredPrototypes.Contains(p.ID)))
            {
                if (!p.TryGetComponent<MachineBoardComponent>(out var mbc, compFact))
                    continue;
                var mId = mbc.Prototype;

                Assert.Multiple(() =>
                {
                    Assert.That(protoMan.TryIndex<EntityPrototype>(mId, out var mProto),
                        $"Machine board {p.ID}'s corresponding machine has an invalid prototype.");
                    Assert.That(mProto.TryGetComponent<MachineComponent>(out var mComp, compFact),
                        $"Machine board {p.ID}'s corresponding machine {mId} does not have MachineComponent");
                    Assert.That(mComp.Board, Is.EqualTo(p.ID),
                        $"Machine {mId}'s BoardPrototype is not equal to it's corresponding machine board, {p.ID}");
                });
            }
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Ensures that every single computer board's corresponding entity
    /// is a computer that can be properly deconstructed to the correct board
    /// </summary>
    [Test]
    public async Task TestComputerBoardHasValidComputer()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var compFact = server.ResolveDependency<IComponentFactory>();

        await server.WaitAssertion(() =>
        {
            foreach (var p in protoMan.EnumeratePrototypes<EntityPrototype>()
                         .Where(p => !p.Abstract)
                         .Where(p => !pair.IsTestPrototype(p))
                         .Where(p => !_ignoredPrototypes.Contains(p.ID)))
            {
                if (!p.TryGetComponent<ComputerBoardComponent>(out var cbc, compFact))
                    continue;
                var cId = cbc.Prototype;

                Assert.Multiple(() =>
                {
                    Assert.That(cId, Is.Not.Null, $"Computer board \"{p.ID}\" does not have a corresponding computer.");
                    Assert.That(protoMan.TryIndex<EntityPrototype>(cId, out var cProto),
                        $"Computer board \"{p.ID}\"'s corresponding computer has an invalid prototype.");
                    Assert.That(cProto.TryGetComponent<ComputerComponent>(out var cComp, compFact),
                        $"Computer board {p.ID}'s corresponding computer \"{cId}\" does not have ComputerComponent");
                    Assert.That(cComp.BoardPrototype, Is.EqualTo(p.ID),
                        $"Computer \"{cId}\"'s BoardPrototype is not equal to it's corresponding computer board, \"{p.ID}\"");
                });
            }
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Ensures that every single computer board's corresponding entity
    /// is a computer that can be properly deconstructed to the correct board
    /// </summary>
    [Test]
    public async Task TestValidateBoardComponentRequirements()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.ResolveDependency<IEntityManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            foreach (var p in protoMan.EnumeratePrototypes<EntityPrototype>()
                         .Where(p => !p.Abstract)
                         .Where(p => !pair.IsTestPrototype(p))
                         .Where(p => !_ignoredPrototypes.Contains(p.ID)))
            {
                if (!p.TryGetComponent<MachineBoardComponent>(out var board, entMan.ComponentFactory))
                    continue;

                Assert.Multiple(() =>
                {
                    foreach (var component in board.ComponentRequirements.Keys)
                    {
                        Assert.That(entMan.ComponentFactory.TryGetRegistration(component, out _), $"Invalid component requirement {component} specified on machine board entity {p}");
                    }
                });
            }
        });

        await pair.CleanReturnAsync();
    }

    // Frontier: machine part tests
    /// <summary>
    /// Invalid stack types for MachineBoard components, should be listed as requirements.
    /// </summary>
    private readonly HashSet<string> _invalidStackTypes = new()
    {
    };

    /// <summary>
    /// Invalid tags for MachineBoard components, should be listed as requirements.
    /// </summary>
    private readonly HashSet<string> _invalidTags = new()
    {
    };

    /// <summary>
    /// Invalid components for MachineBoard components, should be listed as requirements.
    /// </summary>
    private readonly HashSet<string> _invalidComponents = new()
    {
        "PowerCell"
    };

    /// <summary>
    /// Check machine requirements for miscategorized machine part requirements.
    /// </summary>
    [Test]
    public async Task TestValidateBoardMachinePartRequirements()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.ResolveDependency<IEntityManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var compFact = server.ResolveDependency<IComponentFactory>();

        await server.WaitAssertion(() =>
        {
            HashSet<EntProtoId> machinePartEntities = new();
            foreach (var p in protoMan.EnumeratePrototypes<MachinePartPrototype>()
                                .Where(p => !pair.IsTestPrototype(p))
                                .Where(p => !_ignoredPrototypes.Contains(p.ID)))
            {
                machinePartEntities.Add(p.StockPartPrototype);
            }

            Assert.Multiple(() =>
            {
                foreach (var p in protoMan.EnumeratePrototypes<EntityPrototype>()
                            .Where(p => !p.Abstract)
                            .Where(p => !pair.IsTestPrototype(p))
                            .Where(p => !_ignoredPrototypes.Contains(p.ID)))
                {
                    if (!p.TryGetComponent<MachineBoardComponent>(out var mbc, compFact))
                        continue;

                    foreach (var stackReq in mbc.StackRequirements.Keys)
                    {
                        if (_invalidStackTypes.Contains(stackReq))
                        {
                            Assert.Fail($"Entity {p.ID} has a stackRequirement for {stackReq}, which should be converted into a machine part requirement.");
                            continue;
                        }

                        if (!protoMan.TryIndex(stackReq, out var stack))
                        {
                            Assert.Fail($"Entity {p.ID} has a stackRequirement for {stackReq}, which could not be resolved.");
                            continue;
                        }

                        if (machinePartEntities.Contains(stack.Spawn))
                        {
                            Assert.Fail($"Entity {p.ID} has a stackRequirement for {stackReq}, which is a machine part, and should be in requirements.");
                            continue;
                        }
                    }

                    foreach (var tagReq in mbc.TagRequirements.Keys)
                    {
                        if (_invalidTags.Contains(tagReq))
                        {
                            Assert.Fail($"Entity {p.ID} has a tagRequirement for {tagReq}, which should be converted into a machine part requirement.");
                            continue;
                        }
                    }

                    foreach (var compReq in mbc.ComponentRequirements.Keys)
                    {
                        if (_invalidComponents.Contains(compReq))
                        {
                            Assert.Fail($"Entity {p.ID} has a componentRequirement for {compReq}, which should be converted into a machine part requirement.");
                            continue;
                        }
                    }
                }
            });
        });

        await pair.CleanReturnAsync();
    }
    // End Frontier: machine part tests
}
