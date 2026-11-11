// A source of random numbers from 0 (included) to 1 (not included), like JavaScript's Math.random.
// The rules take one as an argument, so a test can pass a repeatable one and replay a round.
using System;

namespace Tanghalan
{
    public delegate double Rand();

    public static class Rng
    {
        static readonly Random shared = new Random();

        /// <summary>Ordinary random numbers.</summary>
        public static double Next() { lock (shared) return shared.NextDouble(); }

        /// <summary>
        /// The repeatable generator of the web app's tests (tests/logic.test.mjs): the same seed gives the
        /// same numbers here and in JavaScript, so both build the same rounds.
        /// </summary>
        public static Rand Seeded(long seed)
        {
            long n = seed;
            return () => { n = (n * 16807L) % 2147483647L; return (n - 1) / 2147483646.0; };
        }
    }
}
