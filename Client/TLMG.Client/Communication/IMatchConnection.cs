using System;
using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    /// <summary>
    /// Área de partida de la conexión con el servidor: seguir la partida autorizada por la sala y colocar cartas.
    /// GameplayViewModel depende solo de esta interfaz; la implementa ServerConnectionManager. Los eventos se disparan
    /// en el hilo de la interfaz y llevan la ErrorKey tal como la manda el servidor, sin textos. Las operaciones solo
    /// envían: el resultado llega por evento y toda decisión de juego es del servidor.
    /// </summary>
    public interface IMatchConnection
    {
        /// <summary>Se dispara con cada vista privada nueva; una vista con versión igual o menor se descarta.</summary>
        event EventHandler<ServerEventArgs<MatchView>> MatchViewChanged;

        /// <summary>Se dispara con la ErrorKey de una suscripción o de una carta que el servidor rechazó.</summary>
        event EventHandler<ServerEventArgs<string>> MatchErrorReceived;

        /// <summary>Se dispara cuando la partida pasa a estar disponible o deja de estarlo.</summary>
        event EventHandler MatchAvailabilityChanged;

        /// <summary>Obtiene si el servidor aceptó la suscripción actual a la partida.</summary>
        bool IsMatchSubscribed { get; }

        /// <summary>Obtiene la última vista aceptada, o null si todavía no llega ninguna.</summary>
        MatchView CurrentMatchView { get; }

        /// <summary>Pide suscribirse a la partida de la sala; el resultado y la vista llegan por evento.</summary>
        /// <returns>true si se envió; false si no hay sesión registrada, partida autorizada o falló el envío.</returns>
        bool SubscribeToCurrentMatch();

        /// <summary>Pide colocar una carta de la mano; el servidor decide y responde por evento.</summary>
        /// <param name="cardKind">Tipo de la carta que se quiere colocar.</param>
        /// <returns>true si se envió; false si no hay suscripción vigente o falló el envío.</returns>
        bool PlaceCard(MatchCardKind cardKind);

        /// <summary>Envía una apuesta para que el servidor la valide.</summary>
        bool PlaceBet(int betValue);

        /// <summary>Solicita retirarse de la apuesta en curso.</summary>
        bool WithdrawFromBet();
    }
}
