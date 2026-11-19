using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using TLMG.Client.Models;
using TLMG.Client.Navigation;

namespace TLMG.Client.ViewModels
{
    /// <summary>Estado de presentación y previsualización del gatito fugitivo.</summary>
    public sealed class FugitiveKittenViewModel : ViewModelBase
    {
        private readonly INavigationManager _navigation;
        private KittenSceneState _sceneState;
        private KittenColor _kittenColor = KittenColor.Orange;
        private int _remainingSeconds = 15;
        private string _winnerNickname = string.Empty;

        public FugitiveKittenViewModel(INavigationManager navigation, bool isPreview)
        {
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            IsPreview = isPreview;
            CatchKittenCommand = new RelayCommand(ExecuteCatchKitten, CanCatchKitten);
            BackCommand = new RelayCommand(ExecuteBack);
        }

        public bool IsPreview { get; }
        public bool IsCatchAvailable => false;
        public ICommand CatchKittenCommand { get; }
        public ICommand BackCommand { get; }
        public KittenSceneState SceneState
        {
            get { return _sceneState; }
            private set { SetProperty(ref _sceneState, value); }
        }
        public KittenColor KittenColor
        {
            get { return _kittenColor; }
            private set { SetProperty(ref _kittenColor, value); }
        }
        public int RemainingSeconds
        {
            get { return _remainingSeconds; }
            private set { SetProperty(ref _remainingSeconds, value); }
        }
        public string WinnerNickname
        {
            get { return _winnerNickname; }
            private set { SetProperty(ref _winnerNickname, value); }
        }

        /// <summary>Selecciona un cuadro fijo únicamente para la previsualización local.</summary>
        public void SelectPreviewScene(KittenSceneState sceneState)
        {
            if (!IsPreview)
            {
                return;
            }

            WinnerNickname = sceneState == KittenSceneState.CaughtByOther ? "N_Stella" : string.Empty;
            switch (sceneState)
            {
                case KittenSceneState.Running:
                case KittenSceneState.Caught:
                    RemainingSeconds = 14;
                    break;
                case KittenSceneState.Hidden:
                    RemainingSeconds = 11;
                    break;
                case KittenSceneState.CaughtByOther:
                    RemainingSeconds = 10;
                    break;
                case KittenSceneState.Escaped:
                    RemainingSeconds = 0;
                    break;
                default:
                    RemainingSeconds = 15;
                    break;
            }

            SceneState = sceneState;
        }

        /// <summary>Cambia el color únicamente en la previsualización local.</summary>
        public void CyclePreviewColor()
        {
            if (IsPreview)
            {
                switch (KittenColor)
                {
                    case KittenColor.Orange: KittenColor = KittenColor.White; break;
                    case KittenColor.White: KittenColor = KittenColor.Gray; break;
                    default: KittenColor = KittenColor.Orange; break;
                }
            }
        }

        private bool CanCatchKitten() { return IsCatchAvailable; }
        private void ExecuteCatchKitten() { }
        private void ExecuteBack() { _navigation.GoBack(); }
    }
}
