using System;
using System.Collections.Generic;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Test random source without threads or a clock. Returns queued indices followed by 0. Checks every index against
    /// the requested range as required by the engine.
    /// </summary>
    internal sealed class ScriptedRandom
    {
        private readonly Queue<int> _indexes = new Queue<int>();

        /// <summary>Number of index requests.</summary>
        internal int CallCount { get; private set; }

        /// <summary>Queues an index for the next request.</summary>
        internal void Enqueue(int index)
        {
            _indexes.Enqueue(index);
        }

        /// <summary>Returns the next queued index, or 0 if none remains.</summary>
        /// <param name="count">Number of options. The index must be smaller.</param>
        internal int PickIndex(int count)
        {
            CallCount++;
            int index = _indexes.Count > 0 ? _indexes.Dequeue() : 0;

            if (index < 0 || index >= count)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "El índice indicado no cabe en el rango.");
            }

            return index;
        }
    }
}
