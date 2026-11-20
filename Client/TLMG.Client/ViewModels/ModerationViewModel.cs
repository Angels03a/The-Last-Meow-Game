using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using TLMG.Client.Models;
using TLMG.Client.Navigation;

namespace TLMG.Client.ViewModels
{
    public class ModerationViewModel : ViewModelBase
    {
        private readonly INavigationManager _navigation;
        private ModerationMode _mode;
        private bool _isCompleted;

        public ModerationViewModel(INavigationManager navigation, string targetPlayerName, ModerationMode mode)
        {
            // FIXME: navigation never could be null because of MainWindow.xaml.cs.
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            TargetPlayerName = targetPlayerName;
            _mode = mode;
            ReportTabCommand = new RelayCommand(ExecuteReportTab);
            BlockTabCommand = new RelayCommand(ExecuteBlockTab);
            SubmitCommand = new RelayCommand(ExecuteSubmit);
            ReturnCommand = new RelayCommand(ExecuteReturn);
        }

        public string TargetPlayerName { get; }

        public ModerationMode Mode
        {
            get { return _mode; }
            private set { SetProperty(ref _mode, value); }
        }

        public bool IsCompleted
        {
            get { return _isCompleted; }
            private set { SetProperty(ref _isCompleted, value); }
        }

        public ICommand ReportTabCommand { get; }

        public ICommand BlockTabCommand { get; }

        public ICommand SubmitCommand { get; }

        public ICommand ReturnCommand { get; }

        private void ExecuteReportTab()
        {
            Mode = ModerationMode.Report;
        }

        private void ExecuteBlockTab()
        {
            Mode = ModerationMode.Block;
        }

        private void ExecuteSubmit()
        {
            // FIXME: Today it doesnt send reports or blocks to the server.
            IsCompleted = true;
        }

        private void ExecuteReturn()
        {
            _navigation.NavigateToWithoutHistory(ScreenId.Gameplay);
        }
    }
}