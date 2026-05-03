using System;
using System.Collections.Generic;

namespace Assets.EvoCellSim.Core
{
    public sealed class CoreRegistries
    {
        public OpcodeRegistry Opcodes { get; }
        public EffectHandlerRegistry Effects { get; }
        public ModuleDefinitionRegistry Modules { get; }
        public MutationOperatorRegistry Mutations { get; }
        public TokenPatternRegistry Tokens { get; }
        public TargetValidationRegistry TargetValidators { get; }
        public FailureEffectRegistry FailureEffects { get; }
        public ConditionRegistry Conditions { get; }

        public CoreRegistries()
        {
            Tokens = new TokenPatternRegistry();
            Opcodes = new OpcodeRegistry();
            Effects = new EffectHandlerRegistry();
            Modules = new ModuleDefinitionRegistry();
            Mutations = new MutationOperatorRegistry();
            TargetValidators = new TargetValidationRegistry();
            FailureEffects = new FailureEffectRegistry();
            Conditions = new ConditionRegistry();

            Tokens.Register(new TokenPattern(1, TokenType.Opcode, new byte[] { 0x10 }));
            Tokens.Register(new TokenPattern(2, TokenType.Opcode, new byte[] { 0x11 }));
            Tokens.Register(new TokenPattern(3, TokenType.Operand, new byte[] { 0x20 }));
            Tokens.Register(new TokenPattern(4, TokenType.Operand, new byte[] { 0x21 }));
            Tokens.Register(new TokenPattern(5, TokenType.Modifier, new byte[] { 0x30 }));
            Tokens.Register(new TokenPattern(6, TokenType.Control, new byte[] { 0xFF }));
            Tokens.Register(new TokenPattern(7, TokenType.Condition, new byte[] { 0x40 }));
            Tokens.Register(new TokenPattern(8, TokenType.Opcode, new byte[] { 0x12 }));
            Tokens.Register(new TokenPattern(9, TokenType.Condition, new byte[] { 0x41 }));
            Tokens.Register(new TokenPattern(10, TokenType.Opcode, new byte[] { 0x13 }));

            Opcodes.Register(new OpcodeDefinition(1, "Move", requiredOperands: 1, optionalOperands: 0, allowModifiers: false, hasFailureEffect: true));
            Opcodes.Register(new OpcodeDefinition(2, "Wait", requiredOperands: 0, optionalOperands: 0, allowModifiers: false, hasFailureEffect: false));
            Opcodes.Register(new OpcodeDefinition(10, "Reproduce", requiredOperands: 0, optionalOperands: 0, allowModifiers: false, hasFailureEffect: true));

            Modules.Register(new ModuleDefinition(1, "Movement", new HashSet<int> { 1 }));
            Modules.Register(new ModuleDefinition(2, "Mutation", new HashSet<int> { 8 }));
            Modules.Register(new ModuleDefinition(3, "Repair", new HashSet<int>()));
            Modules.Register(new ModuleDefinition(4, "Reproduction", new HashSet<int> { 10 }));
            Effects.Register(1, MoveEffect);
            Effects.Register(2, WaitEffect);
            Effects.Register(10, ReproduceEffect);
            Effects.Register(8, MutateEffect);
            FailureEffects.Register(1, MoveFailureEffect);
            FailureEffects.Register(10, ReproduceFailureEffect);
            Mutations.Register(new MutationOperator(1, "SimplePointMutation", MutateHook));
            TargetValidators.Register(1, MoveTargetValidator);
            Conditions.Register(new ConditionDefinition(7, "HasTarget"), HasTargetConditionEvaluator);
            Conditions.Register(new ConditionDefinition(9, "HasBondDepth"), HasBondDepthConditionEvaluator);
            Opcodes.Register(new OpcodeDefinition(8, "Mutate", requiredOperands: 0, optionalOperands: 0, allowModifiers: false, hasFailureEffect: true, mutationOperatorId: 1));
        }

        private static ValidationResult MoveTargetValidator(WorldState world, int sourceCellId, DecodedInstruction instruction)
        {
            if (instruction.OperandTokenIds.Length < 1)
            {
                return ValidationResult.Invalid("Move target operand is missing.");
            }

            var targetId = instruction.OperandTokenIds[0];
            if (!world.CellExists(targetId))
            {
                return ValidationResult.Invalid($"Move target cell {targetId} does not exist.");
            }

            if (!world.TryGetCell(targetId, out var targetCell) || !targetCell.Alive)
            {
                return ValidationResult.Invalid($"Move target cell {targetId} is not valid for this action.");
            }

            return ValidationResult.Valid();
        }

        private static ConditionResult HasTargetConditionEvaluator(WorldState world, int sourceCellId, DecodedInstruction instruction)
        {
            if (instruction.OperandTokenIds.Length < 1)
            {
                return ConditionResult.Failure("HasTarget condition requires a target operand.");
            }

            var targetId = instruction.OperandTokenIds[0];
            if (!world.CellExists(targetId))
            {
                return ConditionResult.Failure($"Target cell {targetId} does not exist.");
            }

            if (!world.TryGetCell(targetId, out var targetCell) || !targetCell.Alive)
            {
                return ConditionResult.Failure($"Target cell {targetId} is not alive.");
            }

            return ConditionResult.Success();
        }

        private static ConditionResult HasBondDepthConditionEvaluator(WorldState world, int sourceCellId, DecodedInstruction instruction)
        {
            if (!world.TryGetCell(sourceCellId, out var sourceCell))
            {
                return ConditionResult.Failure($"Source cell {sourceCellId} does not exist.");
            }

            if (sourceCell.BondDepth <= 0)
            {
                return ConditionResult.Failure($"Source cell {sourceCellId} is not deep enough in the cluster.");
            }

            return ConditionResult.Success();
        }

        private static EffectResult MoveEffect(WorldState world, int sourceCellId, DecodedInstruction instruction)
        {
            var targetId = instruction.OperandTokenIds[0];
            var intent = new IntentRecord
            {
                Id = world.Intents.Count + 1,
                SourceCellId = sourceCellId,
                TargetCellId = targetId,
                Kind = IntentKind.Move
            };

            return new EffectResult(true, intent: intent);
        }

        private static IntentRecord? MoveFailureEffect(WorldState world, int sourceCellId, DecodedInstruction instruction, string failureReason)
        {
            return new IntentRecord
            {
                Id = world.Intents.Count + 1,
                SourceCellId = sourceCellId,
                TargetCellId = sourceCellId,
                Kind = IntentKind.Wait
            };
        }

        private static EffectResult WaitEffect(WorldState world, int sourceCellId, DecodedInstruction instruction)
        {
            return new EffectResult(true, intent: null);
        }

        private static EffectResult MutateEffect(WorldState world, int sourceCellId, DecodedInstruction instruction)
        {
            return new EffectResult(true, intent: null);
        }

        private static EffectResult ReproduceEffect(WorldState world, int sourceCellId, DecodedInstruction instruction)
        {
            var intent = new IntentRecord
            {
                Id = world.Intents.Count + 1,
                SourceCellId = sourceCellId,
                TargetCellId = sourceCellId,
                Kind = IntentKind.Reproduction
            };

            return new EffectResult(true, intent: intent);
        }

        private static IntentRecord? ReproduceFailureEffect(WorldState world, int sourceCellId, DecodedInstruction instruction, string failureReason)
        {
            return new IntentRecord
            {
                Id = world.Intents.Count + 1,
                SourceCellId = sourceCellId,
                TargetCellId = sourceCellId,
                Kind = IntentKind.Wait
            };
        }

        private static MutationResult MutateHook(WorldState world, int sourceCellId, DecodedInstruction instruction)
        {
            var intent = new IntentRecord
            {
                Id = world.Intents.Count + 1,
                SourceCellId = sourceCellId,
                TargetCellId = sourceCellId,
                Kind = IntentKind.Mutation
            };

            return MutationResult.Ok(intent);
        }
    }

    public sealed class OpcodeRegistry
    {
        private readonly Dictionary<int, OpcodeDefinition> definitions = new Dictionary<int, OpcodeDefinition>();

        public void Register(OpcodeDefinition definition)
        {
            definitions[definition.OpcodeId] = definition;
        }

        public bool TryGetDefinition(int opcodeId, out OpcodeDefinition definition)
        {
            return definitions.TryGetValue(opcodeId, out definition);
        }
    }

    public readonly struct OpcodeDefinition
    {
        public int OpcodeId { get; }
        public string Name { get; }
        public int RequiredOperands { get; }
        public int OptionalOperands { get; }
        public bool AllowModifiers { get; }
        public bool HasFailureEffect { get; }
        public int? MutationOperatorId { get; }

        public OpcodeDefinition(int opcodeId, string name, int requiredOperands, int optionalOperands, bool allowModifiers, bool hasFailureEffect, int? mutationOperatorId = null)
        {
            OpcodeId = opcodeId;
            Name = name;
            RequiredOperands = requiredOperands;
            OptionalOperands = optionalOperands;
            AllowModifiers = allowModifiers;
            HasFailureEffect = hasFailureEffect;
            MutationOperatorId = mutationOperatorId;
        }
    }

    public sealed class TokenPatternRegistry
    {
        private readonly List<TokenPattern> patterns = new List<TokenPattern>();

        public void Register(TokenPattern pattern)
        {
            patterns.Add(pattern);
        }

        public bool TryMatchLongest(ReadOnlySpan<byte> genome, int offset, out TokenPatternMatch match)
        {
            match = default;
            var bestLength = 0;

            foreach (var pattern in patterns)
            {
                var bytes = pattern.Bytes;
                if (bytes.Length == 0 || offset + bytes.Length > genome.Length)
                {
                    continue;
                }

                if (bytes.Length < bestLength)
                {
                    continue;
                }

                if (genome.Slice(offset, bytes.Length).SequenceEqual(bytes))
                {
                    bestLength = bytes.Length;
                    match = new TokenPatternMatch(pattern, bytes.Length);
                }
            }

            return bestLength > 0;
        }
    }

    public readonly struct TokenPattern
    {
        public int Id { get; }
        public TokenType Type { get; }
        public byte[] Bytes { get; }

        public TokenPattern(int id, TokenType type, byte[] bytes)
        {
            Id = id;
            Type = type;
            Bytes = bytes;
        }
    }

    public readonly struct TokenPatternMatch
    {
        public TokenPattern Pattern { get; }
        public int Length { get; }

        public TokenPatternMatch(TokenPattern pattern, int length)
        {
            Pattern = pattern;
            Length = length;
        }
    }

    public delegate EffectResult EffectHandler(WorldState world, int sourceCellId, DecodedInstruction instruction);

    public readonly struct EffectResult
    {
        public bool Success { get; }
        public IntentRecord? Intent { get; }
        public string? FailureReason { get; }

        public EffectResult(bool success, IntentRecord? intent = null, string? failureReason = null)
        {
            Success = success;
            Intent = intent;
            FailureReason = failureReason;
        }
    }

    public sealed class EffectHandlerRegistry
    {
        private readonly Dictionary<int, EffectHandler> handlers = new Dictionary<int, EffectHandler>();

        public void Register(int opcodeId, EffectHandler handler)
        {
            handlers[opcodeId] = handler;
        }

        public bool TryGetHandler(int opcodeId, out EffectHandler handler)
        {
            return handlers.TryGetValue(opcodeId, out handler!);
        }
    }

    public delegate IntentRecord? FailureEffectHandler(WorldState world, int sourceCellId, DecodedInstruction instruction, string failureReason);

    public sealed class FailureEffectRegistry
    {
        private readonly Dictionary<int, FailureEffectHandler> handlers = new Dictionary<int, FailureEffectHandler>();

        public void Register(int opcodeId, FailureEffectHandler handler)
        {
            handlers[opcodeId] = handler;
        }

        public bool TryGetHandler(int opcodeId, out FailureEffectHandler handler)
        {
            return handlers.TryGetValue(opcodeId, out handler!);
        }
    }

    public readonly struct ModuleDefinition
    {
        public int ModuleTypeId { get; }
        public string Name { get; }
        public HashSet<int> AllowedOpcodes { get; }

        public ModuleDefinition(int moduleTypeId, string name, HashSet<int> allowedOpcodes)
        {
            ModuleTypeId = moduleTypeId;
            Name = name;
            AllowedOpcodes = allowedOpcodes;
        }

        public bool AllowsOpcode(int opcodeId)
        {
            return AllowedOpcodes.Contains(opcodeId);
        }
    }

    public sealed class ModuleDefinitionRegistry
    {
        private readonly Dictionary<int, ModuleDefinition> definitions = new Dictionary<int, ModuleDefinition>();

        public void Register(ModuleDefinition definition)
        {
            definitions[definition.ModuleTypeId] = definition;
        }

        public bool TryGetDefinition(int moduleTypeId, out ModuleDefinition definition)
        {
            return definitions.TryGetValue(moduleTypeId, out definition);
        }
    }

    public sealed class TargetValidationRegistry
    {
        private readonly Dictionary<int, Func<WorldState, int, DecodedInstruction, ValidationResult>> validators = new Dictionary<int, Func<WorldState, int, DecodedInstruction, ValidationResult>>();

        public void Register(int opcodeId, Func<WorldState, int, DecodedInstruction, ValidationResult> validator)
        {
            validators[opcodeId] = validator;
        }

        public bool TryValidate(int opcodeId, WorldState world, int cellId, DecodedInstruction instruction, out ValidationResult result)
        {
            if (validators.TryGetValue(opcodeId, out var validator))
            {
                result = validator(world, cellId, instruction);
                return true;
            }

            result = ValidationResult.Valid();
            return false;
        }
    }

    public readonly struct ValidationResult
    {
        public bool IsValid { get; }
        public string? FailureReason { get; }

        public ValidationResult(bool isValid, string? failureReason = null)
        {
            IsValid = isValid;
            FailureReason = failureReason;
        }

        public static ValidationResult Valid() => new ValidationResult(true);
        public static ValidationResult Invalid(string reason) => new ValidationResult(false, reason);
    }

    public delegate MutationResult MutationHandler(WorldState world, int sourceCellId, DecodedInstruction instruction);

    public readonly struct MutationOperator
    {
        public int Id { get; }
        public string Name { get; }
        public MutationHandler Handler { get; }

        public MutationOperator(int id, string name, MutationHandler handler)
        {
            Id = id;
            Name = name;
            Handler = handler;
        }
    }

    public sealed class MutationOperatorRegistry
    {
        private readonly Dictionary<int, MutationOperator> operators = new Dictionary<int, MutationOperator>();

        public void Register(MutationOperator mutationOperator)
        {
            operators[mutationOperator.Id] = mutationOperator;
        }

        public bool TryGetOperator(int id, out MutationOperator mutationOperator)
        {
            return operators.TryGetValue(id, out mutationOperator!);
        }
    }

    public readonly struct MutationResult
    {
        public bool Success { get; }
        public IntentRecord? Intent { get; }
        public string? FailureReason { get; }

        public MutationResult(bool success, IntentRecord? intent = null, string? failureReason = null)
        {
            Success = success;
            Intent = intent;
            FailureReason = failureReason;
        }

        public static MutationResult Ok(IntentRecord? intent = null) => new MutationResult(true, intent, null);
        public static MutationResult Failure(string reason) => new MutationResult(false, null, reason);
    }

    public delegate ConditionResult ConditionEvaluator(WorldState world, int sourceCellId, DecodedInstruction instruction);

    public readonly struct ConditionDefinition
    {
        public int Id { get; }
        public string Name { get; }

        public ConditionDefinition(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public readonly struct ConditionResult
    {
        public bool Matches { get; }
        public string? FailureReason { get; }

        public ConditionResult(bool matches, string? failureReason = null)
        {
            Matches = matches;
            FailureReason = failureReason;
        }

        public static ConditionResult Success() => new ConditionResult(true);
        public static ConditionResult Failure(string reason) => new ConditionResult(false, reason);
    }

    public sealed class ConditionRegistry
    {
        private readonly Dictionary<int, (ConditionDefinition Definition, ConditionEvaluator Evaluator)> definitions = new Dictionary<int, (ConditionDefinition, ConditionEvaluator)>();

        public void Register(ConditionDefinition condition, ConditionEvaluator evaluator)
        {
            definitions[condition.Id] = (condition, evaluator);
        }

        public bool TryGetDefinition(int id, out ConditionDefinition condition)
        {
            if (definitions.TryGetValue(id, out var entry))
            {
                condition = entry.Definition;
                return true;
            }

            condition = default!;
            return false;
        }

        public bool TryEvaluate(int id, WorldState world, int sourceCellId, DecodedInstruction instruction, out ConditionResult result)
        {
            if (definitions.TryGetValue(id, out var entry))
            {
                result = entry.Evaluator(world, sourceCellId, instruction);
                return true;
            }

            result = ConditionResult.Failure($"Condition {id} is not registered.");
            return false;
        }
    }
}