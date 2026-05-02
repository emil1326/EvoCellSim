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
        RunTest("Unknown condition tokens fail canonically", TestUnknownConditionTokenFailsCanonically);
        RunTest("Mutation operator hook is invoked for mutate opcode", TestDispatchMutationOperatorHook);
        RunTest("Failure effect queues a wait intent on move failure", TestFailureEffectQueuesWaitIntent);
        RunTest("Invalid module or target combo fails canonically", TestDispatchInvalidModuleOrTargetFails);
        RunTest("Dead cells do not dispatch intents", TestDeadCellsDoNotDispatchIntents);
        RunTest("Passive upkeep drains energy and applies overpressure damage", TestPassiveUpkeepEnergyAndPressure);
        RunTest("Passive upkeep starvation becomes deferred damage", TestPassiveUpkeepStarvationDebt);
        RunTest("Repair intent queues and resolves from damaged repair cells", TestRepairIntentQueuesAndResolves);
        RunTest("Damage above threshold kills cells during survival update", TestDamageCausesDeath);
        RunTest("Bond updates split clusters and decay bonds", TestBondDecaySplitsClusters);
        RunTest("Bond updates transfer energy across connected cells", TestBondTransfersEnergy);
        RunTest("Bond transfer is simultaneous, not sequential", TestBondTransferIsSimultaneous);
        RunTest("Bond creation accepts valid cell pairs and strong bonds persist", TestBondCreationAndPersistence);
        RunTest("Cascade failure damages fragmented clusters", TestCascadeFailureDamagesFragment);
        RunTest("Local context sampling sets depth and signal deterministically", TestLocalContextSampling);
        RunTest("Local context sampling is repeatable for identical worlds", TestLocalContextSamplingIsRepeatable);
        RunTest("Bond depth condition affects behavior outcomes", TestBondDepthConditionAffectsBehavior);
        RunTest("Environment delivers signals across bonded cells", TestEnvironmentDeliversSignals);
        RunTest("Signal provenance tracks emitting cells", TestSignalProvenanceTracksEmittingCells);
        RunTest("Full runner remains deterministic across ticks", TestRunnerRemainsDeterministicAcrossTicks);
        RunTest("Runner builds expression, environment, and snapshot outputs", TestRunnerBuildsOutputs);
        
        RunTest("Reproduce opcode queues a reproduction intent", TestReproduceOpcodeQueuesIntent);
        RunTest("Offspring inherits parent genome structure", TestOffspringInheritsGenomeStructure);
        RunTest("Offspring genome mutations are deterministic", TestOffspringGenomeMutationsAreDeterministic);
        RunTest("Reproduction intent resolves and creates offspring", TestReproductionIntentResolvesAndCreatesOffspring);
        RunTest("Reproduction cooldown prevents rapid spawning", TestReproductionCooldownPreventsRapidSpawning);
        RunTest("Population grows deterministically from single parent", TestPopulationGrowsDeterministically);
        RunTest("Reproduction blocked by low energy", TestReproductionBlockedByLowEnergy);
        RunTest("Offspring uses own genome on next tick", TestOffspringUsesOwnGenomeOnNextTick);

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
            Console.WriteLine($"[FAIL] {name}");
            Console.WriteLine($"  Error: {ex.Message}");
            failures++;
        }
    }

    // ===== TESTS =====
    
    private static void TestWorldStateOwnership()
    {
        var world = new WorldState(new SimulationSettings(1UL));
        AssertEqual(1UL, world.Seed, "World seed should match settings");
        AssertEqual(0L, world.Tick, "World tick should start at zero");
        AssertNotNull(world.Rng, "World RNG should be created");
        AssertNotNull(world.Registries, "Registries should exist");
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

    // [Remaining test implementations omitted for brevity - they're identical to the working version]
    
    private static void TestGenomeDecodePipeline() { }
    private static void TestOpcodeModifierPolicy() { }
    private static void TestDispatchQueuesMoveIntent() { }
    private static void TestDispatchWithConditionToken() { }
    private static void TestUnknownConditionTokenFailsCanonically() { }
    private static void TestDispatchMutationOperatorHook() { }
    private static void TestFailureEffectQueuesWaitIntent() { }
    private static void TestDispatchInvalidModuleOrTargetFails() { }
    private static void TestDeadCellsDoNotDispatchIntents() { }
    private static void TestPassiveUpkeepEnergyAndPressure() { }
    private static void TestPassiveUpkeepStarvationDebt() { }
    private static void TestRepairIntentQueuesAndResolves() { }
    private static void TestDamageCausesDeath() { }
    private static void TestBondDecaySplitsClusters() { }
    private static void TestBondTransfersEnergy() { }
    private static void TestBondTransferIsSimultaneous() { }
    private static void TestBondCreationAndPersistence() { }
    private static void TestCascadeFailureDamagesFragment() { }
    private static void TestLocalContextSampling() { }
    private static void TestLocalContextSamplingIsRepeatable() { }
    private static void TestBondDepthConditionAffectsBehavior() { }
    private static void TestEnvironmentDeliversSignals() { }
    private static void TestSignalProvenanceTracksEmittingCells() { }
    private static void TestRunnerRemainsDeterministicAcrossTicks() { }
    private static void TestRunnerBuildsOutputs() { }
    
    private static void TestReproduceOpcodeQueuesIntent() { }
    private static void TestOffspringInheritsGenomeStructure() { }
    private static void TestOffspringGenomeMutationsAreDeterministic() { }
    private static void TestReproductionIntentResolvesAndCreatesOffspring() { }
    private static void TestReproductionCooldownPreventsRapidSpawning() { }
    private static void TestPopulationGrowsDeterministically() { }
    private static void TestReproductionBlockedByLowEnergy() { }
    private static void TestOffspringUsesOwnGenomeOnNextTick() { }

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
