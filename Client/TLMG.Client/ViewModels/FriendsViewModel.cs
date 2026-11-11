using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using TLMG.Client.Models;

namespace TLMG.Client.ViewModels
{
    /// <summary>
    /// Gestiona el estado de la lista de amigos, contadores y navegación de la pantalla de amigos.
    /// </summary>
    public class FriendsViewModel : ViewModelBase
    {
        private string _friendsBadgeCount = "0";
        private string _emptyFriendsMessageKey;
        private bool _isEmptyFriendsVisible;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="FriendsViewModel"/>.
        /// </summary>
        public FriendsViewModel()
        {
            FriendsList = new ObservableCollection<FriendEntry>();
            BackCommand = new RelayCommand(ExecuteBack);
        }

        /// <summary>
        /// Se produce cuando el usuario solicita regresar a la pantalla anterior.
        /// </summary>
        public event EventHandler BackRequested;

        /// <summary>Comando para regresar a la pantalla principal o anterior.</summary>
        public ICommand BackCommand { get; }

        /// <summary>
        /// Lista observable de amigos para la pestaña principal.
        /// </summary>
        public ObservableCollection<FriendEntry> FriendsList { get; }

        /// <summary>
        /// Contador de amigos en línea/totales para la insignia de la pestaña.
        /// </summary>
        public string FriendsBadgeCount
        {
            get { return _friendsBadgeCount; }
            set { SetProperty(ref _friendsBadgeCount, value); }
        }

        /// <summary>
        /// Clave de recurso del mensaje a mostrar cuando no hay amigos o se es invitado.
        /// </summary>
        public string EmptyFriendsMessageKey
        {
            get { return _emptyFriendsMessageKey; }
            set { SetProperty(ref _emptyFriendsMessageKey, value); }
        }

        /// <summary>
        /// Indica si el mensaje de lista vacía debe ser visible.
        /// </summary>
        public bool IsEmptyFriendsVisible
        {
            get { return _isEmptyFriendsVisible; }
            set { SetProperty(ref _isEmptyFriendsVisible, value); }
        }

        /// <summary>
        /// Actualiza la lista de amigos y recalcula el estado visual de la interfaz.
        /// </summary>
        public void UpdateFriendList(IEnumerable<FriendEntry> updatedFriends, bool isGuest, 
            string guestMessageKey, string emptyMessageKey)
        {
            FriendsList.Clear();

            int count = 0;
            foreach (FriendEntry friend in updatedFriends)
            {
                FriendsList.Add(friend);
                count++;
            }

            FriendsBadgeCount = count.ToString();

            if (isGuest)
            {
                EmptyFriendsMessageKey = guestMessageKey;
                IsEmptyFriendsVisible = true;
            }
            else
            {
                EmptyFriendsMessageKey = emptyMessageKey;
                IsEmptyFriendsVisible = count == 0;
            }
        }

        private void ExecuteBack()
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}