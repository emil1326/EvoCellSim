using System;
using System.Collections.Generic;

namespace Assets.EvoCellSim.Core
{
    public enum TokenType
    {
        Opcode,
        Operand,
        Modifier,
        Control,
        Junk
    }

    public readonly struct DecodedToken
    {
        public TokenType Type { get; init; }
        public int TokenId { get; init; }
        public ReadOnlyMemory<byte> RawBytes { get; init; }
    }

    public readonly struct DecodedInstruction
    {
        public bool IsValid { get; init; }
        public int OpcodeId { get; init; }
        public int[] OperandTokenIds { get; init; }
        public int[] ModifierTokenIds { get; init; }
        public int ControlTokenId { get; init; }
        public int StartTokenIndex { get; init; }
        public int EndTokenIndex { get; init; }
        public string? FailureReason { get; init; }
    }

    public sealed class GenomeDecodeResult
    {
        public IReadOnlyList<DecodedToken> Tokens { get; }
        public IReadOnlyList<DecodedInstruction> Instructions { get; }

        public GenomeDecodeResult(IReadOnlyList<DecodedToken> tokens, IReadOnlyList<DecodedInstruction> instructions)
        {
            Tokens = tokens;
            Instructions = instructions;
        }
    }

    public static class GenomeDecoder
    {
        public static GenomeDecodeResult DecodeInstructionGenome(ReadOnlySpan<byte> genome, TokenPatternRegistry tokens, OpcodeRegistry opcodes)
        {
            var decodedTokens = new List<DecodedToken>();
            var tokenIndex = 0;
            var offset = 0;

            while (offset < genome.Length)
            {
                if (tokens.TryMatchLongest(genome, offset, out var match))
                {
                    decodedTokens.Add(new DecodedToken
                    {
                        Type = match.Pattern.Type,
                        TokenId = match.Pattern.Id,
                        RawBytes = genome.Slice(offset, match.Length).ToArray()
                    });

                    offset += match.Length;
                    tokenIndex++;
                    continue;
                }

                decodedTokens.Add(new DecodedToken
                {
                    Type = TokenType.Junk,
                    TokenId = genome[offset],
                    RawBytes = new byte[] { genome[offset] }
                });

                offset += 1;
                tokenIndex++;
            }

            var instructions = new List<DecodedInstruction>();
            for (var i = 0; i < decodedTokens.Count; i++)
            {
                var token = decodedTokens[i];
                if (token.Type != TokenType.Opcode)
                {
                    continue;
                }

                if (!opcodes.TryGetDefinition(token.TokenId, out var definition))
                {
                    instructions.Add(new DecodedInstruction
                    {
                        IsValid = false,
                        OpcodeId = token.TokenId,
                        OperandTokenIds = Array.Empty<int>(),
                        ModifierTokenIds = Array.Empty<int>(),
                        ControlTokenId = 0,
                        StartTokenIndex = i,
                        EndTokenIndex = i + 1,
                        FailureReason = "Opcode not registered"
                    });

                    continue;
                }

                var modifiers = new List<int>();
                var operands = new List<int>();
                var controlTokenId = 0;
                var j = i + 1;

                var invalidModifiers = false;
                while (j < decodedTokens.Count && decodedTokens[j].Type == TokenType.Modifier)
                {
                    if (definition.AllowModifiers)
                    {
                        modifiers.Add(decodedTokens[j].TokenId);
                    }
                    else
                    {
                        invalidModifiers = true;
                    }

                    j++;
                }

                while (j < decodedTokens.Count && operands.Count < definition.RequiredOperands && decodedTokens[j].Type == TokenType.Operand)
                {
                    operands.Add(decodedTokens[j].TokenId);
                    j++;
                }

                while (j < decodedTokens.Count && operands.Count < definition.RequiredOperands + definition.OptionalOperands && decodedTokens[j].Type == TokenType.Operand)
                {
                    operands.Add(decodedTokens[j].TokenId);
                    j++;
                }

                if (j < decodedTokens.Count && decodedTokens[j].Type == TokenType.Control)
                {
                    controlTokenId = decodedTokens[j].TokenId;
                    j++;
                }

                var isValid = operands.Count >= definition.RequiredOperands && !invalidModifiers;
                var failureReason = invalidModifiers
                    ? "Modifier tokens are not allowed for this opcode"
                    : operands.Count >= definition.RequiredOperands
                        ? null
                        : "Missing required operand tokens";

                instructions.Add(new DecodedInstruction
                {
                    IsValid = isValid,
                    OpcodeId = token.TokenId,
                    OperandTokenIds = operands.ToArray(),
                    ModifierTokenIds = modifiers.ToArray(),
                    ControlTokenId = controlTokenId,
                    StartTokenIndex = i,
                    EndTokenIndex = j,
                    FailureReason = failureReason
                });

                i = Math.Max(i, j - 1);
            }

            return new GenomeDecodeResult(decodedTokens, instructions);
        }
    }
}