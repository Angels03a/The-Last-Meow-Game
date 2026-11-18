using System.Collections.Generic;

namespace TLMG.Services.Sessions
{
    /// <summary>
    /// Resultado de una operación de partida: la clave de error si se rechazó y las vistas por entregar si la
    /// partida cambió.
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

        /// <summary>
        /// Crea el resultado de una operación rechazada que aun así cambió la partida, porque antes venció un plazo.
        /// </summary>
        /// <param name="errorKey">Clave de error para quien la pidió.</param>
        /// <param name="viewDeliveries">Vistas del cambio previo; la lista no debe modificarse después.</param>
        /// <returns>El resultado, con la clave de error y las vistas.</returns>
        public static MatchDelivery CreateRejected(string errorKey, IReadOnlyList<MatchViewDelivery> viewDeliveries)
        {
            MatchDelivery matchDelivery = new MatchDelivery(errorKey, viewDeliveries);

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
