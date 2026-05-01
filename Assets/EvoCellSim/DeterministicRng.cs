using System;

namespace Assets.EvoCellSim.Core
{
    public struct DeterministicRng
    {
        private ulong state;

        public DeterministicRng(ulong seed)
        {
            state = MixSeed(seed);
        }

        public ulong State => state;

        public void SetState(ulong newState)
        {
            state = newState == 0 ? 0x9E3779B97F4A7C15UL : newState;
        }

        public ulong NextUlong()
        {
            state += 0x9E3779B97F4A7C15UL;
            ulong z = state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }

            return (int)(NextUlong() % (uint)maxExclusive);
        }

        public float NextFloat01()
        {
            return (NextUlong() >> 40) * (1.0f / (1u << 24));
        }

        private static ulong MixSeed(ulong seed)
        {
            ulong z = seed + 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
