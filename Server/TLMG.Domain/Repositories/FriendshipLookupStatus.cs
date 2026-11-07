namespace TLMG.Domain.Repositories
{
    /// <summary>
    /// Resultado de comprobar si dos jugadores son amigos; separa "no son amigos" de un fallo de datos.
    /// </summary>
    public enum FriendshipLookupStatus
    {
        /// <summary>Existe una amistad aceptada entre los dos jugadores, en cualquier sentido.</summary>
        Accepted,

        /// <summary>No existe una amistad aceptada: no hay fila, está pendiente o fue rechazada.</summary>
        NotAccepted,

        /// <summary>La consulta falló por un problema de datos; el detalle está en el log.</summary>
        DataFailure
    }
}