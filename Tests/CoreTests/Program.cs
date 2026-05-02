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
        RunTest("Offspring is created with correct genome lineage", TestOffspringCreationBasic);
        RunTest("Reproduction deducts energy and sets cooldown on parent", TestReproductionDeductsEnergyAndSetsCooldown);
        RunTest("Reproduction is blocked when parent energy is insufficient", TestReproductionBlockedByInsufficientEnergy);
        RunTest("Species ceiling blocks offspring beyond MaxCellCount", TestSpeciesCeilingBlocksOffspring);
        RunTest("Module ceiling rejects modules beyond MaxModulesPerCell", TestMaxModulesPerCellEnforced);
        RunTest("Genome mutation is deterministic for the same seed", TestMutationIsDeterministic);

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

    private static void TestUnknownConditionTokenFailsCanonically()
    {
        var world = new WorldState(new SimulationSettings(25UL));
        var conditionResult = world.Registries.Conditions.TryEvaluate(99, world, 1, new DecodedInstruction(), out var result);

        AssertEqual(false, conditionResult, "Unknown conditions should not be treated as registered");
        AssertEqual(false, result.Matches, "Unknown conditions should fail deterministically");
        AssertEqual("Condition 99 is not registered.", result.FailureReason!, "Unknown conditions should report a canonical failure reason");
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

    private static void TestDeadCellsDoNotDispatchIntents()
    {
        var world = new WorldState(new SimulationSettings(27UL));
        world.AddCell(new CellRecord { Id = 1, Alive = false, GenomeId = 1, ClusterId = 0 });
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
        var dispatchResult = world.QueueIntentsForAllCells();

        AssertEqual(false, dispatchResult.Success, "Dead cells should not dispatch intents");
        AssertEqual(0, world.Intents.Count, "Dead cells should not queue intents");
    }

    private static void TestPassiveUpkeepEnergyAndPressure()
    {
        var world = new WorldState(new SimulationSettings(12UL));
        world.AddCell(new CellRecord { Id = 10, Alive = true, GenomeId = 1, ClusterId = 0, Energy = 3, MaxEnergy = 10, Damage = 0 });
        world.AddCell(new CellRecord { Id = 11, Alive = true, GenomeId = 1, ClusterId = 0, Energy = 3, MaxEnergy = 10, Damage = 0 });

        world.PassiveUpkeep();
        world.ApplyPhysics();

        var cell = world.GetCellById(10);
        AssertEqual(2, cell.Energy, "Passive upkeep should drain the configured energy cost");
        AssertEqual(true, cell.Pressure > 0, "Pressure should be calculated for cells in a cluster");
        AssertEqual(true, cell.Damage >= 0, "Damage should remain non-negative after upkeep");
    }

    private static void TestPassiveUpkeepStarvationDebt()
    {
        var world = new WorldState(new SimulationSettings(24UL));
        world.AddCell(new CellRecord { Id = 12, Alive = true, GenomeId = 1, ClusterId = 0, Energy = 0, MaxEnergy = 10, Damage = 0 });

        world.PassiveUpkeep();
        AssertEqual(0, world.GetCellById(12).Damage, "Starvation damage should be deferred until the physics phase");

        world.ApplyPhysics();
        AssertEqual(2, world.GetCellById(12).Damage, "Deferred starvation damage should be applied during the physics phase");
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

    private static void TestBondDecaySplitsClusters()
    {
        var world = new WorldState(new SimulationSettings(15UL));

        world.AddCell(new CellRecord { Id = 1, Alive = true, GenomeId = 1, ClusterId = 0, Energy = 5, MaxEnergy = 10 });
        world.AddCell(new CellRecord { Id = 2, Alive = true, GenomeId = 1, ClusterId = 0, Energy = 5, MaxEnergy = 10 });
        world.AddCell(new CellRecord { Id = 3, Alive = true, GenomeId = 1, ClusterId = 0, Energy = 5, MaxEnergy = 10 });
        world.AddBond(new BondRecord { Id = 1, CellAId = 1, CellBId = 2, Strength = 0.08f });
        world.AddBond(new BondRecord { Id = 2, CellAId = 2, CellBId = 3, Strength = 0.08f });

        world.UpdateBondsAndClusters();

        AssertEqual(0, world.Bonds.Count, "Weak bonds should decay below the break threshold");
        AssertEqual(3, world.Clusters.Count, "Each isolated cell should become its own cluster");
        AssertEqual(1, world.GetCellById(1).ClusterId, "First cell should become its own cluster");
        AssertEqual(2, world.GetCellById(2).ClusterId, "Second cell should become its own cluster");
        AssertEqual(3, world.GetCellById(3).ClusterId, "Third cell should become its own cluster");
    }

    private static void TestBondTransfersEnergy()
    {
        var world = new WorldState(new SimulationSettings(16UL));
        world.AddCell(new CellRecord { Id = 10, Alive = true, GenomeId = 1, ClusterId = 0, Energy = 8, MaxEnergy = 10 });
        world.AddCell(new CellRecord { Id = 11, Alive = true, GenomeId = 1, ClusterId = 0, Energy = 2, MaxEnergy = 10 });
        world.AddBond(new BondRecord { Id = 1, CellAId = 10, CellBId = 11, Strength = 1.0f });

        world.UpdateBondsAndClusters();

        AssertEqual(7, world.GetCellById(10).Energy, "Bond update should transfer energy from the higher-energy cell");
        AssertEqual(3, world.GetCellById(11).Energy, "Bond update should transfer energy to the lower-energy cell");
        AssertEqual(1, world.Clusters.Count, "Connected cells should remain in one cluster");
        AssertEqual(10, world.GetCellById(10).ClusterId, "Cluster ID should be derived from the component root cell");
        AssertEqual(10, world.GetCellById(11).ClusterId, "Connected cells should share the same cluster ID");
    }

    private static void TestBondTransferIsSimultaneous()
    {
        var world = new WorldState(new SimulationSettings(19UL));
        world.AddCell(new CellRecord { Id = 41, Alive = true, GenomeId = 1, ClusterId = 0, Energy = 2, MaxEnergy = 10 });
        world.AddCell(new CellRecord { Id = 42, Alive = true, GenomeId = 1, ClusterId = 0, Energy = 0, MaxEnergy = 10 });
        world.AddCell(new CellRecord { Id = 43, Alive = true, GenomeId = 1, ClusterId = 0, Energy = 0, MaxEnergy = 10 });
        world.AddBond(new BondRecord { Id = 1, CellAId = 41, CellBId = 42, Strength = 1.0f });
        world.AddBond(new BondRecord { Id = 2, CellAId = 42, CellBId = 43, Strength = 1.0f });

        world.UpdateBondsAndClusters();

        AssertEqual(1, world.GetCellById(41).Energy, "Source cell should lose one energy from the first bond");
        AssertEqual(1, world.GetCellById(42).Energy, "Middle cell should only receive the first bond transfer in the same tick");
        AssertEqual(0, world.GetCellById(43).Energy, "Simultaneous transfer should prevent chain forwarding within the same tick");
    }

    private static void TestBondCreationAndPersistence()
    {
        var world = new WorldState(new SimulationSettings(17UL));
        world.AddCell(new CellRecord { Id = 21, Alive = true, GenomeId = 1, ClusterId = 0, Energy = 4, MaxEnergy = 10 });
        world.AddCell(new CellRecord { Id = 22, Alive = true, GenomeId = 1, ClusterId = 0, Energy = 4, MaxEnergy = 10 });

        var created = world.TryCreateBond(21, 22, 1.0f, out var bondId);
        AssertEqual(true, created, "Valid cell pairs should create bonds deterministically");
        AssertEqual(1, bondId, "First created bond should receive a stable ID");

        world.UpdateBondsAndClusters();
        world.UpdateBondsAndClusters();

        AssertEqual(1, world.Bonds.Count, "Strong bonds should persist across multiple updates");
        AssertEqual(1, world.Clusters.Count, "Persisting bonds should keep cells in one cluster");
        AssertEqual(21, world.GetCellById(21).ClusterId, "Cluster ID should remain anchored to the root cell");
        AssertEqual(21, world.GetCellById(22).ClusterId, "Both cells should remain in the same cluster");
    }

    private static void TestCascadeFailureDamagesFragment()
    {
        var world = new WorldState(new SimulationSettings(18UL));
        world.AddCell(new CellRecord { Id = 31, Alive = true, GenomeId = 1, ClusterId = 1, Energy = 5, MaxEnergy = 10, Damage = 0 });
        world.AddCell(new CellRecord { Id = 32, Alive = true, GenomeId = 1, ClusterId = 1, Energy = 5, MaxEnergy = 10, Damage = 0 });
        world.AddCell(new CellRecord { Id = 33, Alive = true, GenomeId = 1, ClusterId = 1, Energy = 5, MaxEnergy = 10, Damage = 0 });
        world.AddBond(new BondRecord { Id = 1, CellAId = 31, CellBId = 32, Strength = 1.0f });
        world.AddBond(new BondRecord { Id = 2, CellAId = 32, CellBId = 33, Strength = 0.08f });

        world.UpdateBondsAndClusters();

        AssertEqual(1, world.Bonds.Count, "The strong bond should survive while the weak bond breaks");
        AssertEqual(2, world.Clusters.Count, "The broken chain should split into two clusters");
        AssertEqual(0, world.GetCellById(31).Damage, "Primary fragment should not take cascade damage");
        AssertEqual(0, world.GetCellById(32).Damage, "Primary fragment should not take cascade damage");
        AssertEqual(1, world.GetCellById(33).Damage, "Detached fragment should take cascade damage deterministically");
        AssertEqual(4, world.GetCellById(33).Energy, "Detached fragment should lose one energy during cascade failure");
    }

    private static void TestLocalContextSampling()
    {
        var world = new WorldState(new SimulationSettings(19UL));
        world.AddCell(new CellRecord { Id = 50, Alive = true, GenomeId = 1, ClusterId = 50, Energy = 5, MaxEnergy = 10 });
        world.AddCell(new CellRecord { Id = 51, Alive = true, GenomeId = 1, ClusterId = 50, Energy = 5, MaxEnergy = 10 });
        world.AddBond(new BondRecord { Id = 1, CellAId = 50, CellBId = 51, Strength = 1.0f });
        world.UpdateBondsAndClusters();

        world.SampleLocalContext();

        var root = world.GetCellById(50);
        var leaf = world.GetCellById(51);

        AssertEqual(0, root.BondDepth, "Root cell should sample zero bond depth");
        AssertEqual(1, leaf.BondDepth, "Neighbor cell should sample one bond depth");
        AssertEqual(0, root.ClusterPosition, "Root cell should be first in cluster position order");
        AssertEqual(1, leaf.ClusterPosition, "Neighbor cell should be second in cluster position order");
        AssertEqual(1, root.NeighborCount, "Root cell should see one neighbor");
        AssertEqual(1, leaf.NeighborCount, "Leaf cell should see one neighbor");
        AssertEqual(1f, root.LocalSignal, "Root cell signal should be strongest at depth zero");
        AssertEqual(0.5f, leaf.LocalSignal, "Leaf cell signal should decay with depth");
        AssertEqual(2, world.Signals.Count, "One signal record should be emitted per alive cell");
    }

    private static void TestLocalContextSamplingIsRepeatable()
    {
        var first = new WorldState(new SimulationSettings(20UL));
        var second = new WorldState(new SimulationSettings(20UL));

        first.AddCell(new CellRecord { Id = 60, Alive = true, GenomeId = 1, ClusterId = 60, Energy = 5, MaxEnergy = 10 });
        first.AddCell(new CellRecord { Id = 61, Alive = true, GenomeId = 1, ClusterId = 60, Energy = 5, MaxEnergy = 10 });
        first.AddBond(new BondRecord { Id = 1, CellAId = 60, CellBId = 61, Strength = 1.0f });
        first.UpdateBondsAndClusters();
        first.SampleLocalContext();

        second.AddCell(new CellRecord { Id = 60, Alive = true, GenomeId = 1, ClusterId = 60, Energy = 5, MaxEnergy = 10 });
        second.AddCell(new CellRecord { Id = 61, Alive = true, GenomeId = 1, ClusterId = 60, Energy = 5, MaxEnergy = 10 });
        second.AddBond(new BondRecord { Id = 1, CellAId = 60, CellBId = 61, Strength = 1.0f });
        second.UpdateBondsAndClusters();
        second.SampleLocalContext();

        var firstRoot = first.GetCellById(60);
        var secondRoot = second.GetCellById(60);
        var firstLeaf = first.GetCellById(61);
        var secondLeaf = second.GetCellById(61);

        AssertEqual(firstRoot.BondDepth, secondRoot.BondDepth, "Repeated worlds should sample the same root depth");
        AssertEqual(firstRoot.ClusterPosition, secondRoot.ClusterPosition, "Repeated worlds should sample the same root position");
        AssertEqual(firstLeaf.BondDepth, secondLeaf.BondDepth, "Repeated worlds should sample the same leaf depth");
        AssertEqual(firstLeaf.ClusterPosition, secondLeaf.ClusterPosition, "Repeated worlds should sample the same leaf position");
        AssertEqual(firstRoot.LocalSignal, secondRoot.LocalSignal, "Repeated worlds should sample the same root signal");
        AssertEqual(firstLeaf.LocalSignal, secondLeaf.LocalSignal, "Repeated worlds should sample the same leaf signal");
        AssertEqual(first.Signals.Count, second.Signals.Count, "Repeated worlds should emit the same number of signal records");
    }

    private static void TestBondDepthConditionAffectsBehavior()
    {
        var world = new WorldState(new SimulationSettings(21UL));
        world.AddCell(new CellRecord { Id = 3, Alive = true, GenomeId = 1, ClusterId = 3, Energy = 5, MaxEnergy = 10 });
        world.AddCell(new CellRecord { Id = 70, Alive = true, GenomeId = 1, ClusterId = 70, Energy = 5, MaxEnergy = 10 });
        world.AddCell(new CellRecord { Id = 71, Alive = true, GenomeId = 1, ClusterId = 70, Energy = 5, MaxEnergy = 10 });
        world.AddCell(new CellRecord { Id = 72, Alive = true, GenomeId = 1, ClusterId = 70, Energy = 5, MaxEnergy = 10 });
        world.AddBond(new BondRecord { Id = 1, CellAId = 70, CellBId = 71, Strength = 1.0f });
        world.AddBond(new BondRecord { Id = 2, CellAId = 71, CellBId = 72, Strength = 1.0f });

        world.AddModule(new ModuleRecord { Id = 1, OwnerCellId = 70, ModuleTypeId = 1, Active = true });
        world.AddModule(new ModuleRecord { Id = 2, OwnerCellId = 71, ModuleTypeId = 1, Active = true });
        world.AddModule(new ModuleRecord { Id = 3, OwnerCellId = 72, ModuleTypeId = 1, Active = true });

        var genome = new GenomeRecord
        {
            Id = 1,
            ParentId = 0,
            SpeciesGenome = Array.Empty<byte>(),
            ModuleGenome = Array.Empty<byte>(),
            InstructionGenome = new byte[] { 0x10, 0x41, 0x20, 0xFF }
        };

        world.AddGenome(in genome);
        world.SampleLocalContext();

        var rootResult = world.QueueIntentsFromGenome(70, 1);
        var leafResult = world.QueueIntentsFromGenome(72, 1);

        AssertEqual(false, rootResult.Success, "Root cell should fail the bond-depth condition");
        AssertEqual(true, leafResult.Success, "Leaf cell should satisfy the bond-depth condition");
        AssertEqual(2, world.Intents.Count, "One wait intent and one move intent should be queued deterministically");
        AssertEqual(IntentKind.Wait, world.Intents.Get(0).Kind, "Root cell should queue a wait intent on failure");
        AssertEqual(IntentKind.Move, world.Intents.Get(1).Kind, "Leaf cell should queue a move intent when context passes");
    }

    private static void TestEnvironmentDeliversSignals()
    {
        var world = new WorldState(new SimulationSettings(26UL));
        world.AddCell(new CellRecord { Id = 90, Alive = true, GenomeId = 1, ClusterId = 90, Energy = 5, MaxEnergy = 10 });
        world.AddCell(new CellRecord { Id = 91, Alive = true, GenomeId = 1, ClusterId = 90, Energy = 5, MaxEnergy = 10 });
        world.AddCell(new CellRecord { Id = 92, Alive = true, GenomeId = 1, ClusterId = 90, Energy = 5, MaxEnergy = 10 });
        world.AddBond(new BondRecord { Id = 1, CellAId = 90, CellBId = 91, Strength = 1.0f });
        world.AddBond(new BondRecord { Id = 2, CellAId = 91, CellBId = 92, Strength = 1.0f });

        world.UpdateBondsAndClusters();
        world.SampleLocalContext();
        world.UpdateEnvironment();

        var left = world.GetCellById(90);
        var middle = world.GetCellById(91);
        var right = world.GetCellById(92);

        AssertEqual(true, middle.ReceivedSignal > left.ReceivedSignal, "Middle cell should receive a stronger blended signal than the edge cell");
        AssertEqual(true, middle.ReceivedSignal > right.ReceivedSignal, "Middle cell should receive a stronger blended signal than the other edge cell");
        AssertEqual(3, world.Signals.Count, "Environment should emit one delivered signal per alive cell");
    }

    private static void TestSignalProvenanceTracksEmittingCells()
    {
        var world = new WorldState(new SimulationSettings(28UL));
        world.AddCell(new CellRecord { Id = 100, Alive = true, GenomeId = 1, ClusterId = 100, Energy = 5, MaxEnergy = 10 });
        world.AddCell(new CellRecord { Id = 101, Alive = true, GenomeId = 1, ClusterId = 100, Energy = 5, MaxEnergy = 10 });
        world.AddBond(new BondRecord { Id = 1, CellAId = 100, CellBId = 101, Strength = 1.0f });

        world.SampleLocalContext();

        AssertEqual(2, world.Signals.Count, "Each sampled cell should emit one signal record");
        AssertEqual(100, world.Signals.Get(0).SourceCellId, "First signal should originate from the first sampled cell");
        AssertEqual(101, world.Signals.Get(1).SourceCellId, "Second signal should originate from the second sampled cell");
    }

    private static void TestRunnerRemainsDeterministicAcrossTicks()
    {
        var first = CreateContextSensitiveRunner(22UL);
        var second = CreateContextSensitiveRunner(22UL);

        for (var tick = 0; tick < 3; tick++)
        {
            first.Tick();
            second.Tick();

            AssertEqual(first.World.Tick, second.World.Tick, $"Tick counters should match at step {tick}");
            AssertEqual(first.World.GetCellById(80).Energy, second.World.GetCellById(80).Energy, $"Root energy should match at step {tick}");
            AssertEqual(first.World.GetCellById(81).Energy, second.World.GetCellById(81).Energy, $"Leaf energy should match at step {tick}");
            AssertEqual(first.World.GetCellById(82).Energy, second.World.GetCellById(82).Energy, $"Tail energy should match at step {tick}");
            AssertEqual(first.World.GetCellById(80).ClusterId, second.World.GetCellById(80).ClusterId, $"Root cluster should match at step {tick}");
            AssertEqual(first.World.GetCellById(81).ClusterId, second.World.GetCellById(81).ClusterId, $"Leaf cluster should match at step {tick}");
            AssertEqual(first.World.GetCellById(82).ClusterId, second.World.GetCellById(82).ClusterId, $"Tail cluster should match at step {tick}");
            AssertEqual(first.World.Signals.Count, second.World.Signals.Count, $"Signal count should match at step {tick}");
        }
    }

    private static SimulationRunner CreateContextSensitiveRunner(ulong seed)
    {
        var runner = new SimulationRunner(new SimulationSettings(seed));
        runner.World.AddCell(new CellRecord { Id = 3, Alive = true, GenomeId = 1, ClusterId = 3, Energy = 5, MaxEnergy = 10 });
        runner.World.AddCell(new CellRecord { Id = 80, Alive = true, GenomeId = 1, ClusterId = 80, Energy = 5, MaxEnergy = 10 });
        runner.World.AddCell(new CellRecord { Id = 81, Alive = true, GenomeId = 1, ClusterId = 80, Energy = 5, MaxEnergy = 10 });
        runner.World.AddCell(new CellRecord { Id = 82, Alive = true, GenomeId = 1, ClusterId = 80, Energy = 5, MaxEnergy = 10 });
        runner.World.AddBond(new BondRecord { Id = 1, CellAId = 80, CellBId = 81, Strength = 1.0f });
        runner.World.AddBond(new BondRecord { Id = 2, CellAId = 81, CellBId = 82, Strength = 1.0f });
        runner.World.AddModule(new ModuleRecord { Id = 1, OwnerCellId = 80, ModuleTypeId = 1, Active = true });
        runner.World.AddModule(new ModuleRecord { Id = 2, OwnerCellId = 81, ModuleTypeId = 1, Active = true });
        runner.World.AddModule(new ModuleRecord { Id = 3, OwnerCellId = 82, ModuleTypeId = 1, Active = true });

        var genome = new GenomeRecord
        {
            Id = 1,
            ParentId = 0,
            SpeciesGenome = Array.Empty<byte>(),
            ModuleGenome = Array.Empty<byte>(),
            InstructionGenome = new byte[] { 0x10, 0x41, 0x20, 0xFF }
        };

        runner.World.AddGenome(in genome);
        return runner;
    }

    private static void TestRunnerBuildsOutputs()
    {
        var runner = CreateContextSensitiveRunner(23UL);
        runner.Tick();

        AssertEqual(true, runner.World.Fields.Count >= 4, "Expression and environment phases should populate field outputs");
        AssertEqual(4, runner.World.LastSnapshot.Count, "Snapshot should capture all alive cells deterministically");
        AssertEqual(1f, runner.World.GetCellById(3).LocalSignal, "Root local signal should remain available after the tick");
    }

    private static void TestOffspringCreationBasic()
    {
        var world = new WorldState(new SimulationSettings(200UL));
        var parentGenome = new GenomeRecord
        {
            Id = 1,
            ParentId = 0,
            SpeciesGenome = new byte[] { 0x01, 0x02 },
            ModuleGenome = new byte[] { 0x03 },
            InstructionGenome = new byte[] { 0x10 }
        };
        world.AddGenome(in parentGenome);
        world.AddCell(new CellRecord { Id = 1, Alive = true, GenomeId = 1, ClusterId = 1, Energy = 15, MaxEnergy = 20 });

        var offspringIndex = world.TryCreateOffspring(1, 1);

        AssertEqual(true, offspringIndex.HasValue, "Offspring should be created when parent has sufficient energy");
        AssertEqual(2, world.Cells.Count, "Two cells should exist after reproduction");
        var offspringCell = world.Cells.Get(offspringIndex!.Value);
        AssertEqual(true, offspringCell.Alive, "Offspring cell should be alive");
        var offspringGenome = world.Genomes.GetById(offspringCell.GenomeId);
        AssertEqual(1, offspringGenome.ParentId, "Offspring genome should reference parent genome id");
    }

    private static void TestReproductionDeductsEnergyAndSetsCooldown()
    {
        var world = new WorldState(new SimulationSettings(201UL));
        var genome = new GenomeRecord
        {
            Id = 1, ParentId = 0,
            SpeciesGenome = Array.Empty<byte>(),
            ModuleGenome = Array.Empty<byte>(),
            InstructionGenome = Array.Empty<byte>()
        };
        world.AddGenome(in genome);
        world.AddCell(new CellRecord { Id = 1, Alive = true, GenomeId = 1, ClusterId = 1, Energy = 15, MaxEnergy = 20 });

        world.TryCreateOffspring(1, 1);

        var parent = world.GetCellById(1);
        AssertEqual(15 - world.Settings.ReproductionEnergyCost, parent.Energy, "Parent energy should decrease by reproduction cost");
        AssertEqual(world.Settings.ReproductionCooldown, parent.ReprodCooldown, "Parent cooldown should be set after reproduction");
    }

    private static void TestReproductionBlockedByInsufficientEnergy()
    {
        var world = new WorldState(new SimulationSettings(202UL));
        var genome = new GenomeRecord
        {
            Id = 1, ParentId = 0,
            SpeciesGenome = Array.Empty<byte>(),
            ModuleGenome = Array.Empty<byte>(),
            InstructionGenome = Array.Empty<byte>()
        };
        world.AddGenome(in genome);
        world.AddCell(new CellRecord { Id = 1, Alive = true, GenomeId = 1, ClusterId = 1, Energy = 5, MaxEnergy = 20 });

        var result = world.TryCreateOffspring(1, 1);

        AssertEqual(false, result.HasValue, "Reproduction should fail when energy is below cost");
        AssertEqual(1, world.Cells.Count, "No offspring should be created when energy is insufficient");
    }

    private static void TestSpeciesCeilingBlocksOffspring()
    {
        var settings = new SimulationSettings(203UL) { MaxCellCount = 1 };
        var world = new WorldState(settings);
        var genome = new GenomeRecord
        {
            Id = 1, ParentId = 0,
            SpeciesGenome = Array.Empty<byte>(),
            ModuleGenome = Array.Empty<byte>(),
            InstructionGenome = Array.Empty<byte>()
        };
        world.AddGenome(in genome);
        world.AddCell(new CellRecord { Id = 1, Alive = true, GenomeId = 1, ClusterId = 1, Energy = 15, MaxEnergy = 20 });

        var result = world.TryCreateOffspring(1, 1);

        AssertEqual(false, result.HasValue, "Offspring should be blocked when live cell count equals MaxCellCount");
        AssertEqual(1, world.Cells.Count, "Cell count should not exceed ceiling");
    }

    private static void TestMaxModulesPerCellEnforced()
    {
        var settings = new SimulationSettings(204UL) { MaxModulesPerCell = 2 };
        var world = new WorldState(settings);
        world.AddCell(new CellRecord { Id = 1, Alive = true, Energy = 10, MaxEnergy = 20, ClusterId = 1 });

        var r1 = world.AddModule(new ModuleRecord { Id = 1, OwnerCellId = 1, ModuleTypeId = 1, Active = true });
        var r2 = world.AddModule(new ModuleRecord { Id = 2, OwnerCellId = 1, ModuleTypeId = 2, Active = true });
        var r3 = world.AddModule(new ModuleRecord { Id = 3, OwnerCellId = 1, ModuleTypeId = 3, Active = true });

        AssertEqual(true, r1 >= 0, "First module should be accepted");
        AssertEqual(true, r2 >= 0, "Second module should be accepted");
        AssertEqual(-1, r3, "Third module should be rejected when MaxModulesPerCell is reached");
        AssertEqual(2, world.Modules.Count, "Module store should contain exactly two modules");
    }

    private static void TestMutationIsDeterministic()
    {
        var sourceGenome = new GenomeRecord
        {
            Id = 1, ParentId = 0,
            SpeciesGenome = new byte[] { 0xAA, 0xBB, 0xCC },
            ModuleGenome = new byte[] { 0x11, 0x22 },
            InstructionGenome = new byte[] { 0x10, 0x41, 0xFF }
        };

        var world1 = new WorldState(new SimulationSettings(205UL));
        var g1 = sourceGenome;
        world1.AddGenome(in g1);
        var offspring1 = world1.GenerateOffspringGenome(1);

        var world2 = new WorldState(new SimulationSettings(205UL));
        var g2 = sourceGenome;
        world2.AddGenome(in g2);
        var offspring2 = world2.GenerateOffspringGenome(1);

        AssertSequenceEqual(offspring1.SpeciesGenome, offspring2.SpeciesGenome, "Species genome mutations should be identical for the same seed");
        AssertSequenceEqual(offspring1.ModuleGenome, offspring2.ModuleGenome, "Module genome mutations should be identical for the same seed");
        AssertSequenceEqual(offspring1.InstructionGenome, offspring2.InstructionGenome, "Instruction genome mutations should be identical for the same seed");
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
