using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using TLMG.Client.Navigation;
using TLMG.Client.Models;

namespace TLMG.Client.ViewModels
{
    /// <summary>Estado y comandos de la demostración de moderación.</summary>
    public class ModerationViewModel : ViewModelBase
    {
        private readonly INavigationManager _navigation;
        private ModerationMode _mode;
        private bool _isCompleted;
        /// <summary>Inicializa la moderación del jugador seleccionado.</summary>
        public ModerationViewModel(INavigationManager navigation, string targetPlayerName, ModerationMode mode)
        {
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            TargetPlayerName = targetPlayerName;
            _mode = mode;
            ReportTabCommand = new RelayCommand(ExecuteReportTab);
            BlockTabCommand = new RelayCommand(ExecuteBlockTab);
            SubmitCommand = new RelayCommand(ExecuteSubmit);
            ReturnCommand = new RelayCommand(ExecuteReturn);
        }
        /// <summary>Jugador seleccionado.</summary>
        public string TargetPlayerName { get; }
        /// <summary>Modo seleccionado.</summary>
        public ModerationMode Mode { get { return _mode; } private set { SetProperty(ref _mode, value); } }
        /// <summary>Indica que terminó el flujo de demostración.</summary>
        public bool IsCompleted { get { return _isCompleted; } private set { SetProperty(ref _isCompleted, value); } }
        /// <summary>Selecciona reportar.</summary>
        public ICommand ReportTabCommand { get; }
        /// <summary>Selecciona bloquear.</summary>
        public ICommand BlockTabCommand { get; }
        /// <summary>Completa la demostración.</summary>
        public ICommand SubmitCommand { get; }
        /// <summary>Regresa a la partida.</summary>
        public ICommand ReturnCommand { get; }
        private void ExecuteReportTab() { Mode = ModerationMode.Report; }
        private void ExecuteBlockTab() { Mode = ModerationMode.Block; }
        private void ExecuteSubmit()
        {
            // FIXME: Flujo de demostración existente; todavía no envía reportes ni bloqueos al servidor. (BL-165).
            IsCompleted = true;
        }
        private void ExecuteReturn() { _navigation.NavigateToWithoutHistory(ScreenId.Gameplay); }
    }
}
