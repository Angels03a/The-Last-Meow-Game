using System.Runtime.Serialization;

namespace TLMG.Contracts.DataContracts
{
    /// <summary>
    /// Datos de un jugador en la tabla de posiciones.
    /// </summary>
    [DataContract]
    public class LeaderboardPlayer
    {
        /// <summary>Nickname del jugador.</summary>
        [DataMember]
        public string Nickname { get; set; }

        /// <summary>Imagen de perfil del jugador; puede ser null.</summary>
        [DataMember]
        public string ProfileImage { get; set; }

        /// <summary>Puntos totales acumulados.</summary>
        [DataMember]
        public int TotalPoints { get; set; }

        /// <summary>Partidas ganadas.</summary>
        [DataMember]
        public int TotalWins { get; set; }

        /// <summary>Partidas jugadas.</summary>
        [DataMember]
        public int TotalGamesPlayed { get; set; }
    }
}