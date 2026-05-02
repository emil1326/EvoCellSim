using System;
using System.Collections.Generic;

namespace Assets.EvoCellSim.Core
{
    public sealed class BehaviorDispatchResult
    {
        public int QueuedIntents { get; internal set; }
        public List<string> FailureReasons { get; } = new List<string>();
        public bool Success => FailureReasons.Count == 0;
    }

    public static class BehaviorDispatcher
    {
        public static BehaviorDispatchResult QueueIntentsFromGenome(WorldState world, int cellId, int genomeId)
        {
            var result = new BehaviorDispatchResult();

            if (!world.TryGetCell(cellId, out var cell))
            {
                result.FailureReasons.Add($"Cell {cellId} does not exist.");
                return result;
            }

            if (!world.TryGetGenome(genomeId, out var genome))
            {
                result.FailureReasons.Add($"Genome {genomeId} does not exist.");
                return result;
            }

            var decoded = GenomeDecoder.DecodeInstructionGenome(genome.InstructionGenome.AsSpan(), world.Registries.Tokens, world.Registries.Opcodes);

            foreach (var instruction in decoded.Instructions)
            {
                if (!instruction.IsValid)
                {
                    result.FailureReasons.Add(instruction.FailureReason ?? "Invalid decoded instruction.");
                    continue;
                }

                if (!HasPermissionForOpcode(world, cellId, instruction.OpcodeId, out var permissionFailure))
                {
                    result.FailureReasons.Add(permissionFailure);
                    continue;
                }

                if (world.Registries.TargetValidators.TryValidate(instruction.OpcodeId, world, cellId, instruction, out var validationResult) && !validationResult.IsValid)
                {
                    result.FailureReasons.Add(validationResult.FailureReason ?? "Target validation failed.");
                    TryQueueFailureEffect(world, result, cellId, instruction, validationResult.FailureReason ?? "Target validation failed.");
                    continue;
                }

                var conditionFailed = false;
                foreach (var conditionId in instruction.ConditionTokenIds)
                {
                    if (!world.Registries.Conditions.TryEvaluate(conditionId, world, cellId, instruction, out var conditionResult))
                    {
                        result.FailureReasons.Add(conditionResult.FailureReason ?? $"Condition {conditionId} is not registered.");
                        TryQueueFailureEffect(world, result, cellId, instruction, conditionResult.FailureReason ?? $"Condition {conditionId} is not registered.");
                        conditionFailed = true;
                        break;
                    }

                    if (!conditionResult.Matches)
                    {
                        result.FailureReasons.Add(conditionResult.FailureReason ?? "Condition validation failed.");
                        TryQueueFailureEffect(world, result, cellId, instruction, conditionResult.FailureReason ?? "Condition validation failed.");
                        conditionFailed = true;
                        break;
                    }
                }

                if (conditionFailed)
                {
                    continue;
                }

                if (!world.Registries.Effects.TryGetHandler(instruction.OpcodeId, out var handler))
                {
                    result.FailureReasons.Add($"No effect handler registered for opcode {instruction.OpcodeId}.");
                    continue;
                }

                var effectResult = handler(world, cellId, instruction);
                if (!effectResult.Success)
                {
                    result.FailureReasons.Add(effectResult.FailureReason ?? "Effect handler rejected the instruction.");
                    TryQueueFailureEffect(world, result, cellId, instruction, effectResult.FailureReason ?? "Effect handler rejected the instruction.");
                    continue;
                }

                if (effectResult.Intent is { } intent)
                {
                    world.AddIntent(intent);
                    result.QueuedIntents++;
                }

                if (world.Registries.Opcodes.TryGetDefinition(instruction.OpcodeId, out var opcodeDefinition) && opcodeDefinition.MutationOperatorId.HasValue)
                {
                    if (world.Registries.Mutations.TryGetOperator(opcodeDefinition.MutationOperatorId.Value, out var mutationOperator))
                    {
                        var mutationResult = mutationOperator.Handler(world, cellId, instruction);
                        if (!mutationResult.Success)
                        {
                            result.FailureReasons.Add(mutationResult.FailureReason ?? "Mutation hook rejected the instruction.");
                            continue;
                        }

                        if (mutationResult.Intent is { } mutationIntent)
                        {
                            world.AddIntent(mutationIntent);
                            result.QueuedIntents++;
                        }
                    }
                }
            }

            return result;
        }

        private static void TryQueueFailureEffect(WorldState world, BehaviorDispatchResult result, int cellId, DecodedInstruction instruction, string failureReason)
        {
            if (world.Registries.Opcodes.TryGetDefinition(instruction.OpcodeId, out var opcodeDefinition) && opcodeDefinition.HasFailureEffect && world.Registries.FailureEffects.TryGetHandler(instruction.OpcodeId, out var failureHandler))
            {
                var failureIntent = failureHandler(world, cellId, instruction, failureReason);
                if (failureIntent is { } intentRecord)
                {
                    world.AddIntent(intentRecord);
                    result.QueuedIntents++;
                }
            }
        }

        private static bool HasPermissionForOpcode(WorldState world, int cellId, int opcodeId, out string failureReason)
        {
            failureReason = string.Empty;
            var hasAllowedModule = false;

            foreach (var module in world.Modules.Records)
            {
                if (module.OwnerCellId != cellId || !module.Active)
                {
                    continue;
                }

                if (!world.Registries.Modules.TryGetDefinition(module.ModuleTypeId, out var moduleDefinition))
                {
                    continue;
                }

                if (moduleDefinition.AllowsOpcode(opcodeId))
                {
                    hasAllowedModule = true;
                    break;
                }
            }

            if (!hasAllowedModule)
            {
                failureReason = $"Cell {cellId} lacks an active module that permits opcode {opcodeId}.";
            }

            return hasAllowedModule;
        }

        public static int ResolveQueuedIntents(WorldState world)
        {
            var resolvedCount = 0;
            var intentCount = world.Intents.Count;

            for (var i = 0; i < intentCount; i++)
            {
                var intent = world.Intents.Get(i);

                switch (intent.Kind)
                {
                    case IntentKind.Move:
                        if (world.TryGetCell(intent.SourceCellId, out var sourceCell))
                        {
                            sourceCell.ClusterId = intent.TargetCellId;
                            world.UpdateCell(in sourceCell);
                            resolvedCount++;
                        }
                        break;
                    case IntentKind.Mutation:
                        if (world.TryGetCell(intent.SourceCellId, out var sourceCellForMutation) && world.TryGetGenome(sourceCellForMutation.GenomeId, out var genome))
                        {
                            if (genome.InstructionGenome.Length > 0)
                            {
                                genome.InstructionGenome[0] = (byte)~genome.InstructionGenome[0];
                            }
                            else
                            {
                                genome.InstructionGenome = new byte[] { 0xFF };
                            }

                            world.UpdateGenome(in genome);
                            resolvedCount++;
                        }
                        break;
                    case IntentKind.Repair:
                        if (world.TryGetCell(intent.SourceCellId, out var repairCell) && repairCell.Energy >= world.Settings.RepairEnergyCost)
                        {
                            repairCell.Damage -= world.Settings.RepairPower;
                            repairCell.Energy -= world.Settings.RepairEnergyCost;
                            if (repairCell.Damage < 0)
                            {
                                repairCell.Damage = 0;
                            }
                            world.UpdateCell(in repairCell);
                            resolvedCount++;
                        }
                        break;
                    case IntentKind.Reproduction:
                        if (world.TryGetCell(intent.SourceCellId, out var reproCell) && reproCell.Energy >= world.Settings.ReproductionEnergyCost && reproCell.ReprodCooldown == 0)
                        {
                            var result = world.TryCreateOffspring(intent.SourceCellId, reproCell.GenomeId);
                            if (result.HasValue)
                            {
                                resolvedCount++;
                            }
                        }
                        else
                        {
                            resolvedCount++;
                        }
                        break;
                    default:
                        resolvedCount++;
                        break;
                }
            }

            world.Intents.Clear();
            return resolvedCount;
        }
    }
}
