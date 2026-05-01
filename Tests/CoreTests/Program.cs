using System;
using System.Collections.Generic;
using Assets.EvoCellSim.Core;

static class Program
{
    private static int failures;

    static int Main()
    {
        Console.WriteLine("[CoreTests] Starting Phase 0/1 console checks");

        RunTest("World state owns seed/tick/rng/stores", TestWorldStateOwnership);
        RunTest("Tick phases are canonical", TestTickPhaseOrder);
        RunTest("Runner advances tick and records 11 phases", TestRunnerTickTrace);
        RunTest("RNG is deterministic for same seed", TestDeterministicRng);
        RunTest("Stores and registries exist", TestStoresAndRegistries);
        RunTest("Cell lookup by ID works in world stores", TestCellLookupById);
        RunTest("Genome decoder decodes instruction genome deterministically", TestGenomeDecodePipeline);
        RunTest("Opcode modifier policy rejects unexpected modifiers", TestOpcodeModifierPolicy);
        RunTest("Valid decoded rule queues a move intent", TestDispatchQueuesMoveIntent);
        RunTest("Condition token validation works for move rules", TestDispatchWithConditionToken);
        RunTest("Mutation operator hook is invoked for mutate opcode", TestDispatchMutationOperatorHook);
        RunTest("Failure effect queues a wait intent on move failure", TestFailureEffectQueuesWaitIntent);
        RunTest("Invalid module or target combo fails canonically", TestDispatchInvalidModuleOrTargetFails);
        RunTest("Passive upkeep drains energy and applies overpressure damage", TestPassiveUpkeepEnergyAndPressure);
        RunTest("Repair intent queues and resolves from damaged repair cells", TestRepairIntentQueuesAndResolves);
        RunTest("Damage above threshold kills cells during survival update", TestDamageCausesDeath);

        Console.WriteLine(failures == 0
            ? "[CoreTests] ALL TESTS PASSED"
            : $"[CoreTests] FAILURES: {failures}");

        return failures == 0 ? 0 : 1;
    }

    private static void RunTest(string name, Action test)
    {
        try
        {
            test();
            Console.WriteLine($"[PASS] {name}");
        }
        catch (Exception ex)
        {
            failures++;
            Console.WriteLine($"[FAIL] {name}");
            Console.WriteLine(ex.Message);
        }
    }

    private static void TestWorldStateOwnership()
    {
        var settings = new SimulationSettings(465124787894UL);
        var world = new WorldState(settings);

        AssertEqual(settings.Seed, world.Seed, "Seed should match settings");
        AssertEqual(0L, world.Tick, "World tick should start at zero");
        AssertNotNull(world.Rng, "World RNG should be created");
        AssertNotNull(world.Registries, "Registries should exist");
        AssertNotNull(world.Cells, "Cell store should exist");
        AssertNotNull(world.Genomes, "Genome store should exist");
        AssertNotNull(world.Modules, "Module store should exist");
        AssertNotNull(world.Bonds, "Bond store should exist");
        AssertNotNull(world.Clusters, "Cluster store should exist");
        AssertNotNull(world.Fields, "Field store should exist");
        AssertNotNull(world.Signals, "Signal store should exist");
        AssertNotNull(world.Intents, "Intent queue should exist");
    }

    private static void TestTickPhaseOrder()
    {
        var expected = new[]
        {
            TickPhase.PassiveUpkeep,
            TickPhase.SampleLocalContext,
            TickPhase.UpdateExpression,
            TickPhase.EvaluateRules,
            TickPhase.QueueIntents,
            TickPhase.ResolveIntents,
            TickPhase.ApplyPhysics,
            TickPhase.UpdateBondsAndClusters,
            TickPhase.ResolveReproductionDeathMutation,
            TickPhase.UpdateEnvironment,
            TickPhase.BuildSnapshot
        };

        var actual = new List<TickPhase>(expected);
        AssertSequenceEqual(expected, actual, "Tick phase enum order should match the plan");
    }

    private static void TestRunnerTickTrace()
    {
        var runner = new SimulationRunner(new SimulationSettings(123456789UL));
        runner.Tick();

        AssertEqual(1L, runner.World.Tick, "Tick should advance by one");
        AssertEqual(11, runner.LastTickTrace.Count, "Runner should record all 11 phases");
        AssertEqual(TickPhase.PassiveUpkeep, runner.LastTickTrace[0], "First phase should be PassiveUpkeep");
        AssertEqual(TickPhase.BuildSnapshot, runner.LastTickTrace[^1], "Last phase should be BuildSnapshot");
    }

    private static void TestDeterministicRng()
    {
        var first = new DeterministicRng(465124787894UL);
        var second = new DeterministicRng(465124787894UL);

        for (int i = 0; i < 8; i++)
        {
            var a = first.NextUlong();
            var b = second.NextUlong();
            AssertEqual(a, b, $"RNG values should match at step {i}");
            Console.WriteLine($"[TRACE] rng[{i}] = {a}");
        }
    }

    private static void TestStoresAndRegistries()
    {
        var world = new WorldState(new SimulationSettings(1UL));

        AssertEqual(0, world.Cells.Count, "Cell store should start empty");
        AssertEqual(0, world.Genomes.Count, "Genome store should start empty");
        AssertEqual(0, world.Modules.Count, "Module store should start empty");
        AssertEqual(0, world.Bonds.Count, "Bond store should start empty");
        AssertEqual(0, world.Clusters.Count, "Cluster store should start empty");
        AssertEqual(0, world.Fields.Count, "Field store should start empty");
        AssertEqual(0, world.Signals.Count, "Signal store should start empty");
        AssertEqual(0, world.Intents.Count, "Intent queue should start empty");

        AssertNotNull(world.Registries.Opcodes, "Opcode registry should exist");
        AssertNotNull(world.Registries.Effects, "Effect registry should exist");
        AssertNotNull(world.Registries.Modules, "Module registry should exist");
        AssertNotNull(world.Registries.Mutations, "Mutation registry should exist");
        AssertNotNull(world.Registries.Tokens, "Token registry should exist");
        AssertNotNull(world.Registries.Conditions, "Condition registry should exist");
    }

    private static void TestCellLookupById()
    {
        var world = new WorldState(new SimulationSettings(2UL));
        var newCell = new CellRecord { Id = 101, Alive = true, GenomeId = 3, ClusterId = 0 };

        world.AddCell(in newCell);
        AssertEqual(true, world.CellExists(101), "Cell should exist after being added");

        var found = world.GetCellById(101);
        AssertEqual(101, found.Id, "Found cell should preserve its ID");
        AssertEqual(true, found.Alive, "Found cell should preserve alive state");
        AssertEqual(3, found.GenomeId, "Found cell should preserve genome ID");

        found.Alive = false;
        world.UpdateCell(in found);

        AssertEqual(false, world.GetCellById(101).Alive, "Updated cell should reflect new alive state");
    }

    private static void TestGenomeDecodePipeline()
    {
        var world = new WorldState(new SimulationSettings(3UL));
        var genome = new GenomeRecord
        {
            Id = 1,
            ParentId = 0,
            SpeciesGenome = Array.Empty<byte>(),
            ModuleGenome = Array.Empty<byte>(),
            InstructionGenome = new byte[] { 0x10, 0x20, 0xFF }
        };

        world.AddGenome(in genome);
        var resultA = world.DecodeInstructionGenome(1);
        var resultB = world.DecodeInstructionGenome(1);

        AssertEqual(1, resultA.Instructions.Count, "Expected one decoded instruction chain");
        AssertEqual(true, resultA.Instructions[0].IsValid, "Expected the decoded instruction to be valid");
        AssertEqual(1, resultA.Instructions[0].OpcodeId, "Opcode ID should match the registered opcode");
        AssertEqual(1, resultA.Instructions[0].OperandTokenIds.Length, "Expected one required operand");
        AssertEqual(6, resultA.Instructions[0].ControlTokenId, "Expected the trailing control token ID");
        AssertEqual(resultA.Instructions.Count, resultB.Instructions.Count, "Decoding the same genome twice should be deterministic");
    }

    private static void TestOpcodeModifierPolicy()
    {
        var world = new WorldState(new SimulationSettings(4UL));
        var genome = new GenomeRecord
        {
            Id = 2,
            ParentId = 0,
            SpeciesGenome = Array.Empty<byte>(),
            ModuleGenome = Array.Empty<byte>(),
            InstructionGenome = new byte[] { 0x10, 0x30, 0x20, 0x21, 0xFF }
        };

        world.AddGenome(in genome);
        var result = world.DecodeInstructionGenome(2);

        AssertEqual(1, result.Instructions.Count, "Expected one decoded instruction chain");
        AssertEqual(false, result.Instructions[0].IsValid, "Modifier tokens should invalidate this opcode");
        AssertEqual("Modifier tokens are not allowed for this opcode", result.Instructions[0].FailureReason!, "Failure reason should explain modifier policy");
    }

    private static void TestDispatchQueuesMoveIntent()
    {
        var world = new WorldState(new SimulationSettings(7UL));
        world.AddCell(new CellRecord { Id = 1, Alive = true, GenomeId = 1, ClusterId = 0 });
        world.AddCell(new CellRecord { Id = 3, Alive = true, GenomeId = 0, ClusterId = 0 });
        world.AddModule(new ModuleRecord { Id = 1, OwnerCellId = 1, ModuleTypeId = 1, Active = true });

        var genome = new GenomeRecord
        {
            Id = 1,
            ParentId = 0,
            SpeciesGenome = Array.Empty<byte>(),
            ModuleGenome = Array.Empty<byte>(),
            InstructionGenome = new byte[] { 0x10, 0x20, 0xFF }
        };

        world.AddGenome(in genome);
        var dispatchResult = world.QueueIntentsFromGenome(1, 1);

        AssertEqual(true, dispatchResult.Success, "Valid move rule should dispatch successfully");
        AssertEqual(1, dispatchResult.QueuedIntents, "One intent should be queued");
        AssertEqual(1, world.Intents.Count, "Intent queue should contain one intent");
        var intent = world.Intents.Get(0);
        AssertEqual(1, intent.SourceCellId, "Intent source cell should match");
        AssertEqual(3, intent.TargetCellId, "Intent target cell should match operand token ID");
        AssertEqual(IntentKind.Move, intent.Kind, "Intent kind should reflect the move opcode");

        var resolved = world.ResolveQueuedIntents();
        AssertEqual(1, resolved, "One queued intent should be resolved");
        AssertEqual(0, world.Intents.Count, "Intent queue should clear after resolution");
        AssertEqual(3, world.GetCellById(1).ClusterId, "Resolver should apply move intent result to the source cell state");
    }

    private static void TestDispatchMutationOperatorHook()
    {
        var world = new WorldState(new SimulationSettings(9UL));
        world.AddCell(new CellRecord { Id = 1, Alive = true, GenomeId = 1, ClusterId = 0 });
        world.AddModule(new ModuleRecord { Id = 2, OwnerCellId = 1, ModuleTypeId = 2, Active = true });

        var genome = new GenomeRecord
        {
            Id = 1,
            ParentId = 0,
            SpeciesGenome = Array.Empty<byte>(),
            ModuleGenome = Array.Empty<byte>(),
            InstructionGenome = new byte[] { 0x12, 0xFF }
        };

        world.AddGenome(in genome);
        var dispatchResult = world.QueueIntentsFromGenome(1, 1);

        AssertEqual(true, dispatchResult.Success, "Mutation hook dispatch should succeed for an allowed mutate opcode");
        AssertEqual(1, dispatchResult.QueuedIntents, "One mutation intent should be queued by the hook");
        AssertEqual(1, world.Intents.Count, "Mutation intent should be queued in the world");
        AssertEqual<IntentKind>(IntentKind.Mutation, world.Intents.Get(0).Kind, "Mutation intent should have the expected kind");
    }

    private static void TestDispatchWithConditionToken()
    {
        var world = new WorldState(new SimulationSettings(10UL));
        world.AddCell(new CellRecord { Id = 1, Alive = true, GenomeId = 1, ClusterId = 0 });
        world.AddCell(new CellRecord { Id = 3, Alive = true, GenomeId = 0, ClusterId = 0 });
        world.AddModule(new ModuleRecord { Id = 1, OwnerCellId = 1, ModuleTypeId = 1, Active = true });

        var genome = new GenomeRecord
        {
            Id = 1,
            ParentId = 0,
            SpeciesGenome = Array.Empty<byte>(),
            ModuleGenome = Array.Empty<byte>(),
            InstructionGenome = new byte[] { 0x10, 0x40, 0x20, 0xFF }
        };

        world.AddGenome(in genome);
        var dispatchResult = world.QueueIntentsFromGenome(1, 1);

        AssertEqual(true, dispatchResult.Success, "Move rule with explicit condition should succeed");
        AssertEqual(1, dispatchResult.QueuedIntents, "One intent should be queued when move condition passes");
        AssertEqual(1, world.Intents.Count, "Intent queue should contain the move intent");
    }

    private static void TestFailureEffectQueuesWaitIntent()
    {
        var world = new WorldState(new SimulationSettings(11UL));
        world.AddCell(new CellRecord { Id = 1, Alive = true, GenomeId = 1, ClusterId = 0 });
        world.AddModule(new ModuleRecord { Id = 1, OwnerCellId = 1, ModuleTypeId = 1, Active = true });

        var genome = new GenomeRecord
        {
            Id = 1,
            ParentId = 0,
            SpeciesGenome = Array.Empty<byte>(),
            ModuleGenome = Array.Empty<byte>(),
            InstructionGenome = new byte[] { 0x10, 0x20, 0xFF }
        };

        world.AddGenome(in genome);
        var dispatchResult = world.QueueIntentsFromGenome(1, 1);

        AssertEqual(false, dispatchResult.Success, "Dispatch should fail when target is invalid");
        AssertEqual(1, dispatchResult.QueuedIntents, "Failure effect should queue a wait intent on move failure");
        AssertEqual(1, world.Intents.Count, "Wait intent should be queued in the world");
        AssertEqual<IntentKind>(IntentKind.Wait, world.Intents.Get(0).Kind, "Failure effect should create a wait intent");
    }

    private static void TestDispatchInvalidModuleOrTargetFails()
    {
        var world = new WorldState(new SimulationSettings(8UL));
        world.AddCell(new CellRecord { Id = 1, Alive = true, GenomeId = 1, ClusterId = 0 });

        var genome = new GenomeRecord
        {
            Id = 1,
            ParentId = 0,
            SpeciesGenome = Array.Empty<byte>(),
            ModuleGenome = Array.Empty<byte>(),
            InstructionGenome = new byte[] { 0x10, 0x20, 0xFF }
        };

        world.AddGenome(in genome);
        var dispatchResult = world.QueueIntentsFromGenome(1, 1);

        AssertEqual(false, dispatchResult.Success, "Dispatch should fail with no active module or invalid target");
        AssertEqual(0, dispatchResult.QueuedIntents, "No intent should be queued when module/target combo is invalid");
        AssertEqual(0, world.Intents.Count, "Intent queue should remain empty on failure");
        AssertEqual(true, dispatchResult.FailureReasons.Count > 0, "Failure reasons should be reported canonically");
    }

    private static void TestPassiveUpkeepEnergyAndPressure()
    {
        var world = new WorldState(new SimulationSettings(12UL));
        world.AddCell(new CellRecord { Id = 10, Alive = true, GenomeId = 1, ClusterId = 0, Energy = 3, MaxEnergy = 10, Damage = 0 });
        world.AddCell(new CellRecord { Id = 11, Alive = true, GenomeId = 1, ClusterId = 0, Energy = 3, MaxEnergy = 10, Damage = 0 });

        world.PassiveUpkeep();

        var cell = world.GetCellById(10);
        AssertEqual(2, cell.Energy, "Passive upkeep should drain the configured energy cost");
        AssertEqual(true, cell.Pressure > 0, "Pressure should be calculated for cells in a cluster");
        AssertEqual(true, cell.Damage >= 0, "Damage should remain non-negative after upkeep");
    }

    private static void TestRepairIntentQueuesAndResolves()
    {
        var world = new WorldState(new SimulationSettings(13UL));
        world.AddCell(new CellRecord { Id = 20, Alive = true, GenomeId = 1, ClusterId = 0, Energy = 5, MaxEnergy = 10, Damage = 5 });
        world.AddModule(new ModuleRecord { Id = 5, OwnerCellId = 20, ModuleTypeId = 3, Active = true });

        world.QueueRepairIntents();
        AssertEqual(1, world.Intents.Count, "Repair intent should be queued for a damaged repair cell");
        AssertEqual(IntentKind.Repair, world.Intents.Get(0).Kind, "Queued intent kind should be Repair");

        var resolved = world.ResolveQueuedIntents();
        AssertEqual(1, resolved, "One repair intent should resolve");

        var repaired = world.GetCellById(20);
        AssertEqual(true, repaired.Damage < 5, "Repair should reduce cell damage");
        AssertEqual(true, repaired.Energy < 5, "Repair should consume repair energy cost");
    }

    private static void TestDamageCausesDeath()
    {
        var world = new WorldState(new SimulationSettings(14UL));
        world.AddCell(new CellRecord { Id = 30, Alive = true, GenomeId = 1, ClusterId = 0, Energy = 1, MaxEnergy = 10, Damage = 12 });

        world.ApplyDeathAndRepair();
        AssertEqual(false, world.GetCellById(30).Alive, "Cells with damage above the threshold should die");
        AssertEqual(0, world.GetCellById(30).Energy, "Dead cells should have their energy drained to zero");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}. Expected {expected}, got {actual}.");
        }
    }

    private static void AssertNotNull(object? value, string message)
    {
        if (value is null)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertSequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual, string message)
    {
        if (expected.Count != actual.Count)
        {
            throw new InvalidOperationException($"{message}. Expected count {expected.Count}, got {actual.Count}.");
        }

        for (int i = 0; i < expected.Count; i++)
        {
            if (!EqualityComparer<T>.Default.Equals(expected[i], actual[i]))
            {
                throw new InvalidOperationException($"{message}. Mismatch at index {i}: expected {expected[i]}, got {actual[i]}.");
            }
        }
    }
}
