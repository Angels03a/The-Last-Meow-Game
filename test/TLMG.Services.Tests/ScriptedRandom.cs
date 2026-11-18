using System;
using System.Collections.Generic;

namespace TLMG.Services.Tests
{
    /// <summary>
    /// Azar de prueba, sin hilos ni reloj: devuelve primero los índices que la prueba encoló y después 0. Valida que
    /// cada índice esté dentro del rango pedido, como lo exige el motor.
    /// </summary>
    internal sealed class ScriptedRandom
    {
        private readonly Queue<int> _indexes = new Queue<int>();

        /// <summary>Obtiene cuántas veces se pidió un índice.</summary>
        internal int CallCount { get; private set; }

        /// <summary>Encola un índice que se devolverá en la siguiente petición.</summary>
        internal void Enqueue(int index)
        {
            _indexes.Enqueue(index);
        }

        /// <summary>Devuelve el siguiente índice encolado, o 0 si no hay.</summary>
        /// <param name="count">Cantidad de opciones; el índice debe ser menor.</param>
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
