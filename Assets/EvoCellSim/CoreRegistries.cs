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
        public ConditionRegistry Conditions { get; }

        public CoreRegistries()
        {
            Tokens = new TokenPatternRegistry();
            Opcodes = new OpcodeRegistry();
            Effects = new EffectHandlerRegistry();
            Modules = new ModuleDefinitionRegistry();
            Mutations = new MutationOperatorRegistry();
            Conditions = new ConditionRegistry();

            Tokens.Register(new TokenPattern(1, TokenType.Opcode, new byte[] { 0x10 }));
            Tokens.Register(new TokenPattern(2, TokenType.Opcode, new byte[] { 0x11 }));
            Tokens.Register(new TokenPattern(3, TokenType.Operand, new byte[] { 0x20 }));
            Tokens.Register(new TokenPattern(4, TokenType.Operand, new byte[] { 0x21 }));
            Tokens.Register(new TokenPattern(5, TokenType.Modifier, new byte[] { 0x30 }));
            Tokens.Register(new TokenPattern(6, TokenType.Control, new byte[] { 0xFF }));

            Opcodes.Register(new OpcodeDefinition(1, "Move", requiredOperands: 2, optionalOperands: 0, allowModifiers: false, hasFailureEffect: false));
            Opcodes.Register(new OpcodeDefinition(2, "Wait", requiredOperands: 0, optionalOperands: 0, allowModifiers: false, hasFailureEffect: false));
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

        public OpcodeDefinition(int opcodeId, string name, int requiredOperands, int optionalOperands, bool allowModifiers, bool hasFailureEffect)
        {
            OpcodeId = opcodeId;
            Name = name;
            RequiredOperands = requiredOperands;
            OptionalOperands = optionalOperands;
            AllowModifiers = allowModifiers;
            HasFailureEffect = hasFailureEffect;
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

    public sealed class EffectHandlerRegistry { }
    public sealed class ModuleDefinitionRegistry { }
    public sealed class MutationOperatorRegistry { }
    public sealed class ConditionRegistry { }
}