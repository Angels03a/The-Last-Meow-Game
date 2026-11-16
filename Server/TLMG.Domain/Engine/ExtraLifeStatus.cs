namespace TLMG.Domain.Engine
{
    /// <summary>Resultado de otorgar la vida extra a un jugador; Services lo traduce a ErrorKey.</summary>
    public enum ExtraLifeStatus
    {
        /// <summary>La vida extra quedó asignada al jugador.</summary>
        Granted,

        /// <summary>El jugador no forma parte de la partida.</summary>
        PlayerNotInMatch,

        /// <summary>La modalidad de la partida no admite vida extra; solo la de 8 cartas.</summary>
        ModeDoesNotAllowExtraLife,

        /// <summary>La vida extra ya se otorgó en esta partida; solo un jugador la obtiene, una vez.</summary>
        ExtraLifeAlreadyGranted
    }
}
