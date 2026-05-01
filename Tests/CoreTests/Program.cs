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
            InstructionGenome = new byte[] { 0x10, 0x20, 0x21, 0xFF }
        };

        world.AddGenome(in genome);
        var resultA = world.DecodeInstructionGenome(1);
        var resultB = world.DecodeInstructionGenome(1);

        AssertEqual(1, resultA.Instructions.Count, "Expected one decoded instruction chain");
        AssertEqual(true, resultA.Instructions[0].IsValid, "Expected the decoded instruction to be valid");
        AssertEqual(1, resultA.Instructions[0].OpcodeId, "Opcode ID should match the registered opcode");
        AssertEqual(2, resultA.Instructions[0].OperandTokenIds.Length, "Expected two required operands");
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
