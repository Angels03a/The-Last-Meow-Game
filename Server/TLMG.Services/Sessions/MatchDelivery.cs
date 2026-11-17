using System.Collections.Generic;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Resultado de una operación de partida: la clave de error si se rechazó o las vistas por entregar si se aceptó.
    /// Es inmutable y ya no toca estado compartido.
    /// </summary>
    public sealed class MatchDelivery
    {
        private static readonly IReadOnlyList<MatchViewDelivery> _noViewDeliveries =
            new List<MatchViewDelivery>().AsReadOnly();

        private MatchDelivery(string errorKey, IReadOnlyList<MatchViewDelivery> viewDeliveries)
        {
            ErrorKey = errorKey;
            ViewDeliveries = viewDeliveries;
        }

        /// <summary>Obtiene la clave de error; null si la operación se aceptó.</summary>
        public string ErrorKey { get; }

        /// <summary>Obtiene las vistas por entregar; nunca es null y está vacía si se rechazó.</summary>
        public IReadOnlyList<MatchViewDelivery> ViewDeliveries { get; }

        /// <summary>Crea el resultado de una operación rechazada.</summary>
        /// <param name="errorKey">Clave de error para quien la pidió.</param>
        /// <returns>El resultado, sin vistas.</returns>
        public static MatchDelivery CreateRejected(string errorKey)
        {
            MatchDelivery matchDelivery = new MatchDelivery(errorKey, _noViewDeliveries);

            return matchDelivery;
        }

        /// <summary>Crea el resultado de una operación aceptada.</summary>
        /// <param name="viewDeliveries">Vistas por entregar; la lista no debe modificarse después.</param>
        /// <returns>El resultado, sin clave de error.</returns>
        public static MatchDelivery CreateAccepted(IReadOnlyList<MatchViewDelivery> viewDeliveries)
        {
            MatchDelivery matchDelivery = new MatchDelivery(null, viewDeliveries);

            return matchDelivery;
        }
    }
}
