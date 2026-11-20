using System;
using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel;
using TLMG.Client.Models;
using TLMG.Client.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace TLMG.Client.Views
{
    /// <summary>
    /// Pantalla del minijuego El Gatito Fugitivo. Las decisiones llegan del servidor a través del ViewModel.
    /// </summary>
    public partial class FugitiveKittenScreen : LocalizableScreen
    {
        private const int UrgentCountdownSeconds = 3;

        private const int DoorGlowLeadMilliseconds = 300;
        private const int CounterBeatMilliseconds = 1000;
        private const int CounterBeatPeakMilliseconds = 250;
        private const int CounterBeatSettleMilliseconds = 600;
        private const int KittenVanishMilliseconds = 250;
        private const int CatchPuffMilliseconds = 600;
        private const int CatchPuffPeakMilliseconds = 180;
        private const int CatchPuffStaggerMilliseconds = 40;
        private const int DoorShutMilliseconds = 450;
        private const int ResultFadeInMilliseconds = 350;
        private const int CaughtByOtherPanelDelayMilliseconds = 350;

        private const double NormalScale = 1.0;
        private const double CounterBeatPeakScale = 1.18;
        private const double VanishedKittenScale = 0.4;
        private const double PuffStartScale = 0.2;
        private const double PuffEndScale = 1.8;
        private const double PuffPeakOpacity = 0.8;
        private const double HiddenOpacity = 0.0;
        private const double VisibleOpacity = 1.0;
        private const double ClosedShutterScale = 0.0;

        private const double IntroFrameGlowLevel = 0.5;
        private const double HiddenFrameGlowLevel = 1.0 / 3.0;
        private const double NoGlowLevel = 0.0;
        private const double FullStageOpacity = 1.0;
        private const double DimmedStageOpacity = 0.28;

        private const int StarCount = 70;
        private const int StarHorizontalStep = 197;
        private const int StarHorizontalOffset = 53;
        private const int StarVerticalStep = 131;
        private const int StarVerticalOffset = 29;
        private const int LargeStarInterval = 7;
        private const int StarOpacityLevels = 4;
        private const double LargeStarDiameter = 2.8;
        private const double SmallStarDiameter = 1.8;
        private const double StarBaseOpacity = 0.1;
        private const double StarOpacityStep = 0.06;
        private const int SceneWidth = 1280;
        private const int SceneHeight = 720;

        private const string CaughtByOtherFormatKey = "FugitiveKitten_CaughtByOtherFormat";
        private const string OtherExtraLifeFormatKey = "FugitiveKitten_OtherExtraLifeFormat";
        private const string NicknamePlaceholderFormat = "{0}";
        private const string StarBrushKey = "Brush_White";
        private const string CountdownBrushKey = "Brush_Gold";
        private const string UrgentCountdownBrushKey = "Brush_Error";
        private const string ResultPanelBorderBrushKey = "Brush_Border";
        private const string CaughtPanelBorderBrushKey = "Brush_FugitiveKittenCaughtPanelBorder";
        private const string KittenBodyBrushKey = "Brush_FugitiveKittenBody";
        private const string KittenStripeBrushKey = "Brush_FugitiveKittenStripe";
        private const string KittenCreamBrushKey = "Brush_FugitiveKittenCream";
        private const string KittenBodyBrushKeyFormat = "Brush_FugitiveKitten{0}Body";
        private const string KittenStripeBrushKeyFormat = "Brush_FugitiveKitten{0}Stripe";
        private const string KittenCreamBrushKeyFormat = "Brush_FugitiveKitten{0}Cream";

        private readonly FugitiveKittenViewModel _viewModel;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="FugitiveKittenScreen"/> en el estado Arranque.
        /// </summary>
        public FugitiveKittenScreen(FugitiveKittenViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;
            Loaded += OnFugitiveKittenScreenLoaded;
            Unloaded += OnFugitiveKittenScreenUnloaded;
            DrawStars();
            ApplyKittenColor(_viewModel.KittenColor);
            ShowSceneState(_viewModel.SceneState);
        }

        /// <inheritdoc />
        protected override void UpdateDynamicLabels()
        {
            lblCaughtByOther.Text = FormatWithWinnerNickname(CaughtByOtherFormatKey);
            lblOtherExtraLife.Text = FormatWithWinnerNickname(OtherExtraLifeFormatKey);
        }

        private string FormatWithWinnerNickname(string formatKey)
        {
            string messageFormat = TryFindResource(formatKey) as string ?? NicknamePlaceholderFormat;

            return string.Format(CultureInfo.CurrentCulture, messageFormat, _viewModel.WinnerNickname);
        }

        private void DrawStars()
        {
            for (int starIndex = 0; starIndex < StarCount; starIndex++)
            {
                double starDiameter = starIndex % LargeStarInterval == 0 ? LargeStarDiameter : SmallStarDiameter;
                Ellipse star = new Ellipse
                {
                    Width = starDiameter,
                    Height = starDiameter,
                    Fill = (Brush)FindResource(StarBrushKey),
                    Opacity = StarBaseOpacity + (starIndex % StarOpacityLevels) * StarOpacityStep
                };

                Canvas.SetLeft(star, (starIndex * StarHorizontalStep + StarHorizontalOffset) % SceneWidth);
                Canvas.SetTop(star, (starIndex * StarVerticalStep + StarVerticalOffset) % SceneHeight);
                pnlStars.Children.Add(star);
            }
        }

        private void ApplyKittenColor(KittenColor kittenColor)
        {
            Resources[KittenBodyBrushKey] = FindKittenBrush(KittenBodyBrushKeyFormat, kittenColor);
            Resources[KittenStripeBrushKey] = FindKittenBrush(KittenStripeBrushKeyFormat, kittenColor);
            Resources[KittenCreamBrushKey] = FindKittenBrush(KittenCreamBrushKeyFormat, kittenColor);
        }

        private Brush FindKittenBrush(string brushKeyFormat, KittenColor kittenColor)
        {
            string brushKey = string.Format(CultureInfo.InvariantCulture, brushKeyFormat, kittenColor);

            return (Brush)FindResource(brushKey);
        }

            private void ShowSceneState(KittenSceneState sceneState)
        {
            ResetScene();

            switch (sceneState)
            {
                case KittenSceneState.Running:
                    ShowRunningFrame();
                    break;
                case KittenSceneState.Hidden:
                    ShowHiddenFrame();
                    break;
                case KittenSceneState.Leaving:
                    ShowCountdown();
                    break;
                case KittenSceneState.Caught:
                    ShowCaughtFrame();
                    break;
                case KittenSceneState.CaughtByOther:
                    ShowCaughtByOtherFrame();
                    break;
                case KittenSceneState.Escaped:
                    ShowEscapedFrame();
                    break;
                default:
                    ShowIntroFrame();
                    break;
            }
        }

        private void ResetScene()
        {
            StopOpacityAnimations();
            pnlStage.Opacity = FullStageOpacity;
            pnlKitten.Visibility = Visibility.Collapsed;
            pnlDoorBottomLeft.Visibility = Visibility.Visible;
            pnlDoorTopLeftGlow.Opacity = NoGlowLevel;
            pnlDoorTopRightGlow.Opacity = NoGlowLevel;
            pnlDoorBottomLeftGlow.Opacity = NoGlowLevel;
            pnlDoorBottomRightGlow.Opacity = NoGlowLevel;
            pnlBoxTopGlow.Opacity = NoGlowLevel;
            pnlBoxBottomGlow.Opacity = NoGlowLevel;
            pnlDoorTopLeftHoleGlow.Visibility = Visibility.Collapsed;
            pnlDoorTopRightHoleGlow.Visibility = Visibility.Collapsed;
            pnlDoorBottomLeftHoleGlow.Visibility = Visibility.Collapsed;
            pnlDoorBottomRightHoleGlow.Visibility = Visibility.Collapsed;
            rectCaughtOverlay.Visibility = Visibility.Collapsed;
            pnlClosedDoor.Visibility = Visibility.Collapsed;
            pnlCatchPuff.Visibility = Visibility.Collapsed;
            lblGetReady.Visibility = Visibility.Collapsed;
            grdHidingMessage.Visibility = Visibility.Collapsed;
            lblCatchHint.Visibility = Visibility.Visible;
            brdRewardPill.Visibility = Visibility.Visible;
            brdResultPanel.Visibility = Visibility.Collapsed;
            brdResultPanel.BorderBrush = (Brush)FindResource(ResultPanelBorderBrushKey);
            pnlCaughtResult.Visibility = Visibility.Collapsed;
            pnlCaughtByOtherResult.Visibility = Visibility.Collapsed;
            pnlEscapedResult.Visibility = Visibility.Collapsed;
        }

        private void ShowIntroFrame()
        {
            lblGetReady.Visibility = Visibility.Visible;
            AnimateDoorGlow(pnlDoorTopLeftGlow, pnlDoorTopLeftHoleGlow, IntroFrameGlowLevel);
            ShowCountdown();
        }

        private void ShowRunningFrame()
        {
            pnlKitten.Visibility = Visibility.Visible;
            ShowCountdown();
        }

        private void ShowHiddenFrame()
        {
            grdHidingMessage.Visibility = Visibility.Visible;
            AnimateDoorGlow(pnlDoorBottomLeftGlow, pnlDoorBottomLeftHoleGlow, HiddenFrameGlowLevel);
            ShowCountdown();
        }

        private void ShowCaughtFrame()
        {
            ShowResultPanel(pnlCaughtResult, TimeSpan.Zero);
            rectCaughtOverlay.Visibility = Visibility.Visible;
            brdResultPanel.BorderBrush = (Brush)FindResource(CaughtPanelBorderBrushKey);
            ShowCountdown();
        }

        private void ShowCaughtByOtherFrame()
        {
            ShowResultPanel(pnlCaughtByOtherResult, TimeSpan.FromMilliseconds(CaughtByOtherPanelDelayMilliseconds));
            AnimateKittenVanish();
            AnimateCatchPuff();
            ShowCountdown();
        }

        private void ShowEscapedFrame()
        {
            ShowResultPanel(pnlEscapedResult, TimeSpan.Zero);
            pnlDoorBottomLeft.Visibility = Visibility.Collapsed;
            pnlClosedDoor.Visibility = Visibility.Visible;
            AnimateDoorShut();
            ShowCountdown();
        }

        private void ShowResultPanel(StackPanel resultContent, TimeSpan fadeInDelay)
        {
            pnlStage.Opacity = DimmedStageOpacity;
            lblCatchHint.Visibility = Visibility.Collapsed;
            brdRewardPill.Visibility = Visibility.Collapsed;
            brdResultPanel.Visibility = Visibility.Visible;
            resultContent.Visibility = Visibility.Visible;

            DoubleAnimation fadeInAnimation = new DoubleAnimation(
                HiddenOpacity,
                VisibleOpacity,
                TimeSpan.FromMilliseconds(ResultFadeInMilliseconds))
            {
                BeginTime = fadeInDelay
            };

            brdResultPanel.Opacity = HiddenOpacity;
            brdResultPanel.BeginAnimation(OpacityProperty, fadeInAnimation);
        }

        private void ShowCountdown()
        {
            int remainingSeconds = _viewModel.RemainingSeconds;
            string countdownBrushKey = remainingSeconds <= UrgentCountdownSeconds
                ? UrgentCountdownBrushKey
                : CountdownBrushKey;

            lblCountdown.Text = remainingSeconds.ToString(CultureInfo.CurrentCulture);
            lblCountdown.Foreground = (Brush)FindResource(countdownBrushKey);

            bool isResultShown = brdResultPanel.Visibility == Visibility.Visible;

            if (remainingSeconds <= UrgentCountdownSeconds && !isResultShown)
            {
                AnimateCountdownBeat();
            }
        }

        private void StopOpacityAnimations()
        {
            pnlDoorTopLeftGlow.BeginAnimation(OpacityProperty, null);
            pnlDoorBottomLeftGlow.BeginAnimation(OpacityProperty, null);
            brdResultPanel.BeginAnimation(OpacityProperty, null);
            brdResultPanel.Opacity = VisibleOpacity;
        }

        private void AnimateDoorGlow(UIElement doorGlow, UIElement doorHoleGlow, double glowLevel)
        {
            doorHoleGlow.Visibility = Visibility.Visible;
            TimeSpan glowRiseTime = TimeSpan.FromMilliseconds(DoorGlowLeadMilliseconds * glowLevel);
            DoubleAnimation glowAnimation = new DoubleAnimation(NoGlowLevel, glowLevel, glowRiseTime);

            doorGlow.BeginAnimation(OpacityProperty, glowAnimation);
        }

        private void AnimateCountdownBeat()
        {
            DoubleAnimationUsingKeyFrames beatAnimation = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromMilliseconds(CounterBeatMilliseconds)
            };
            beatAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(
                CounterBeatPeakScale,
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(CounterBeatPeakMilliseconds)),
                new QuadraticEase { EasingMode = EasingMode.EaseOut }));
            beatAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(
                NormalScale,
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(CounterBeatSettleMilliseconds)),
                new QuadraticEase { EasingMode = EasingMode.EaseOut }));

            ScaleTransform beatTransform = new ScaleTransform();
            lblCountdown.RenderTransform = beatTransform;
            beatTransform.BeginAnimation(ScaleTransform.ScaleXProperty, beatAnimation);
            beatTransform.BeginAnimation(ScaleTransform.ScaleYProperty, beatAnimation);
        }

        private void AnimateKittenVanish()
        {
            TimeSpan vanishTime = TimeSpan.FromMilliseconds(KittenVanishMilliseconds);
            QuadraticEase vanishEase = new QuadraticEase { EasingMode = EasingMode.EaseIn };
            DoubleAnimation shrinkAnimation = new DoubleAnimation(NormalScale, VanishedKittenScale, vanishTime)
            {
                EasingFunction = vanishEase
            };
            DoubleAnimation fadeOutAnimation = new DoubleAnimation(VisibleOpacity, HiddenOpacity, vanishTime)
            {
                EasingFunction = vanishEase
            };

            ScaleTransform vanishTransform = new ScaleTransform();
            pnlVanishingKitten.RenderTransform = vanishTransform;
            pnlCatchPuff.Visibility = Visibility.Visible;
            vanishTransform.BeginAnimation(ScaleTransform.ScaleXProperty, shrinkAnimation);
            vanishTransform.BeginAnimation(ScaleTransform.ScaleYProperty, shrinkAnimation);
            pnlVanishingKitten.BeginAnimation(OpacityProperty, fadeOutAnimation);
        }

        private void AnimateCatchPuff()
        {
            int cloudIndex = 0;

            foreach (UIElement puffCloud in pnlCatchPuffClouds.Children)
            {
                TimeSpan cloudDelay = TimeSpan.FromMilliseconds(cloudIndex * CatchPuffStaggerMilliseconds);
                AnimatePuffCloud(puffCloud, cloudDelay);
                cloudIndex++;
            }
        }

        private void AnimatePuffCloud(UIElement puffCloud, TimeSpan cloudDelay)
        {
            TimeSpan puffTime = TimeSpan.FromMilliseconds(CatchPuffMilliseconds);
            DoubleAnimation growAnimation = new DoubleAnimation(PuffStartScale, PuffEndScale, puffTime)
            {
                BeginTime = cloudDelay,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            DoubleAnimationUsingKeyFrames opacityAnimation = new DoubleAnimationUsingKeyFrames
            {
                BeginTime = cloudDelay,
                Duration = puffTime
            };
            opacityAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(
                PuffPeakOpacity,
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(CatchPuffPeakMilliseconds))));
            opacityAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(HiddenOpacity, KeyTime.FromTimeSpan(puffTime)));

            ScaleTransform puffTransform = new ScaleTransform(PuffStartScale, PuffStartScale);
            puffCloud.RenderTransform = puffTransform;
            puffTransform.BeginAnimation(ScaleTransform.ScaleXProperty, growAnimation);
            puffTransform.BeginAnimation(ScaleTransform.ScaleYProperty, growAnimation);
            puffCloud.BeginAnimation(OpacityProperty, opacityAnimation);
        }

        private void AnimateDoorShut()
        {
            DoubleAnimation shutAnimation = new DoubleAnimation(
                ClosedShutterScale,
                NormalScale,
                TimeSpan.FromMilliseconds(DoorShutMilliseconds))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            ScaleTransform shutTransform = new ScaleTransform(NormalScale, ClosedShutterScale);
            pnlDoorShutter.RenderTransform = shutTransform;
            shutTransform.BeginAnimation(ScaleTransform.ScaleYProperty, shutAnimation);
        }

        private void OnFugitiveKittenScreenLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.Activate();
            UpdatePosition();
            UpdateConnectionDisplay();
            ApplyKittenColor(_viewModel.KittenColor);
            ShowSceneState(_viewModel.SceneState);
            Focus();
        }

        private void OnFugitiveKittenScreenUnloaded(object sender, RoutedEventArgs eventArgs)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.Deactivate();
            StopOpacityAnimations();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            if (eventArgs.PropertyName == nameof(FugitiveKittenViewModel.SceneState))
            {
                ShowSceneState(_viewModel.SceneState);
            }
            else if (eventArgs.PropertyName == nameof(FugitiveKittenViewModel.KittenColor))
            {
                ApplyKittenColor(_viewModel.KittenColor);
            }
            else if (eventArgs.PropertyName == nameof(FugitiveKittenViewModel.WinnerNickname))
            {
                UpdateDynamicLabels();
            }
            else if (eventArgs.PropertyName == nameof(FugitiveKittenViewModel.RemainingSeconds))
            {
                ShowCountdown();
            }
            else if (eventArgs.PropertyName == nameof(FugitiveKittenViewModel.Progress)
                || eventArgs.PropertyName == nameof(FugitiveKittenViewModel.Origin)
                || eventArgs.PropertyName == nameof(FugitiveKittenViewModel.Destination))
            {
                UpdatePosition();
            }
            else if (eventArgs.PropertyName == nameof(FugitiveKittenViewModel.IsConnected)
                || eventArgs.PropertyName == nameof(FugitiveKittenViewModel.IsWaiting))
            {
                UpdateConnectionDisplay();
            }
            else if (eventArgs.PropertyName == nameof(FugitiveKittenViewModel.AlternatePose))
            {
                UpdatePosition();
            }
            else if (eventArgs.PropertyName == nameof(FugitiveKittenViewModel.GlowPoint))
            {
                UpdateGlow();
            }
        }

        private void UpdateConnectionDisplay()
        {
            lblConnection.Visibility = _viewModel.IsConnected ? Visibility.Collapsed : Visibility.Visible;
            lblWaiting.Visibility = _viewModel.IsConnected && _viewModel.IsWaiting
                ? Visibility.Visible : Visibility.Collapsed;
            if (_viewModel.IsWaiting) pnlKitten.Visibility = Visibility.Collapsed;
        }

        private void UpdateGlow()
        {
            pnlDoorTopLeftGlow.BeginAnimation(OpacityProperty, null);
            pnlDoorTopRightGlow.BeginAnimation(OpacityProperty, null);
            pnlDoorBottomLeftGlow.BeginAnimation(OpacityProperty, null);
            pnlDoorBottomRightGlow.BeginAnimation(OpacityProperty, null);
            pnlDoorTopLeftGlow.Opacity = _viewModel.GlowPoint == "P1" ? 1 : 0;
            pnlDoorTopRightGlow.Opacity = _viewModel.GlowPoint == "P2" ? 1 : 0;
            pnlDoorBottomLeftGlow.Opacity = _viewModel.GlowPoint == "P3" ? 1 : 0;
            pnlDoorBottomRightGlow.Opacity = _viewModel.GlowPoint == "P4" ? 1 : 0;
            pnlBoxTopGlow.Opacity = _viewModel.GlowPoint == "E1" ? 1 : 0;
            pnlBoxBottomGlow.Opacity = _viewModel.GlowPoint == "E2" ? 1 : 0;
        }

        private static Point GetPosition(string point)
        {
            switch (point)
            {
                case "P1": return new Point(82, 326);
                case "P2": return new Point(1102, 326);
                case "P3": return new Point(82, 536);
                case "P4": return new Point(1102, 536);
                case "E1": return new Point(592, 326);
                case "E2": return new Point(592, 536);
                default: return new Point(82, 326);
            }
        }

        private void UpdatePosition()
        {
            Point origin = GetPosition(_viewModel.Origin);
            Point destination = GetPosition(_viewModel.Destination);
            Canvas.SetLeft(pnlKitten, origin.X + (destination.X - origin.X) * _viewModel.Progress);
            Canvas.SetTop(pnlKitten, origin.Y + (destination.Y - origin.Y) * _viewModel.Progress);
            pnlKitten.RenderTransformOrigin = new Point(0.5, 0.5);
            pnlKitten.RenderTransform = new ScaleTransform(destination.X < origin.X ? -1 : 1, 1);
            bool firstPose = _viewModel.AlternatePose;
            ((UIElement)pnlKitten.Children[0]).Visibility = firstPose ? Visibility.Visible : Visibility.Collapsed;
            ((UIElement)pnlKitten.Children[1]).Visibility = firstPose ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
