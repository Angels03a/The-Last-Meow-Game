using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TLMG.Client.Views
{
    // TODO: Mover el estado visible a FugitiveKittenViewModel con CatchKittenCommand. (BL-041).
    // TODO: Reproducir la ruta de 15 s en lugar de los estados congelados. (BL-119).
    /// <summary>
    /// Pantalla del minijuego El Gatito Fugitivo (solo modalidad de 8 cartas). Hoy es una cáscara visual que
    /// muestra los seis estados fijos del prototipo; no se comunica con el servidor ni decide resultados.
    /// </summary>
    public partial class FugitiveKittenScreen : LocalizableScreen
    {
        private const int FugitiveKittenWindowSeconds = 15;
        private const int UrgentCountdownSeconds = 3;
        private const int IntroFrameElapsedMilliseconds = 750;
        private const int RunningFrameElapsedMilliseconds = 1350;
        private const int HiddenFrameElapsedMilliseconds = 4500;
        private const int CaughtFrameElapsedMilliseconds = 1350;
        private const int CaughtByOtherFrameElapsedMilliseconds = 5350;
        private const int EscapedFrameElapsedMilliseconds = 15000;

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

        // TODO: Recibir el nickname del ganador decidido por el servidor. (BL-117).
        private const string SampleWinnerNickname = "N_Stella";

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

        // TODO: Retirar las teclas temporales 1 a 6, C y Esc de esta pantalla. (BL-122).
        private static readonly Dictionary<Key, KittenSceneState> _sceneStateKeys =
            new Dictionary<Key, KittenSceneState>
            {
                { Key.D1, KittenSceneState.Intro },
                { Key.D2, KittenSceneState.Running },
                { Key.D3, KittenSceneState.Hidden },
                { Key.D4, KittenSceneState.Caught },
                { Key.D5, KittenSceneState.CaughtByOther },
                { Key.D6, KittenSceneState.Escaped },
                { Key.NumPad1, KittenSceneState.Intro },
                { Key.NumPad2, KittenSceneState.Running },
                { Key.NumPad3, KittenSceneState.Hidden },
                { Key.NumPad4, KittenSceneState.Caught },
                { Key.NumPad5, KittenSceneState.CaughtByOther },
                { Key.NumPad6, KittenSceneState.Escaped }
            };

        // TODO: Usar el color que elige el servidor en lugar del ciclo con la tecla C. (BL-117).
        private static readonly KittenColor[] _kittenColors =
            { KittenColor.Orange, KittenColor.White, KittenColor.Gray };

        private int _kittenColorIndex;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="FugitiveKittenScreen"/> en el estado Arranque.
        /// </summary>
        public FugitiveKittenScreen()
        {
            InitializeComponent();
            Loaded += OnFugitiveKittenScreenLoaded;
            KeyDown += OnFugitiveKittenScreenKeyDown;
            DrawStars();
            ApplyKittenColor(_kittenColors[_kittenColorIndex]);
            ShowSceneState(KittenSceneState.Intro);
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

            return string.Format(CultureInfo.CurrentCulture, messageFormat, SampleWinnerNickname);
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

        private void ShowNextKittenColor()
        {
            _kittenColorIndex = (_kittenColorIndex + 1) % _kittenColors.Length;
            ApplyKittenColor(_kittenColors[_kittenColorIndex]);
        }

        // TODO: Enviar el intento de atrape solo al hacer clic sobre el gatito visible. (BL-117).
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
            pnlStage.Opacity = FullStageOpacity;
            pnlKitten.Visibility = Visibility.Collapsed;
            pnlDoorBottomLeft.Visibility = Visibility.Visible;
            pnlDoorTopLeftGlow.Opacity = NoGlowLevel;
            pnlDoorTopRightGlow.Opacity = NoGlowLevel;
            pnlDoorBottomLeftGlow.Opacity = NoGlowLevel;
            pnlDoorBottomRightGlow.Opacity = NoGlowLevel;
            pnlBoxTopGlow.Opacity = NoGlowLevel;
            pnlBoxBottomGlow.Opacity = NoGlowLevel;
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
            pnlDoorTopLeftGlow.Opacity = IntroFrameGlowLevel;
            ShowCountdown(IntroFrameElapsedMilliseconds);
        }

        private void ShowRunningFrame()
        {
            pnlKitten.Visibility = Visibility.Visible;
            ShowCountdown(RunningFrameElapsedMilliseconds);
        }

        private void ShowHiddenFrame()
        {
            grdHidingMessage.Visibility = Visibility.Visible;
            pnlDoorBottomLeftGlow.Opacity = HiddenFrameGlowLevel;
            ShowCountdown(HiddenFrameElapsedMilliseconds);
        }

        private void ShowCaughtFrame()
        {
            ShowResultPanel(pnlCaughtResult);
            rectCaughtOverlay.Visibility = Visibility.Visible;
            brdResultPanel.BorderBrush = (Brush)FindResource(CaughtPanelBorderBrushKey);
            ShowCountdown(CaughtFrameElapsedMilliseconds);
        }

        private void ShowCaughtByOtherFrame()
        {
            ShowResultPanel(pnlCaughtByOtherResult);
            ShowCountdown(CaughtByOtherFrameElapsedMilliseconds);
        }

        private void ShowEscapedFrame()
        {
            ShowResultPanel(pnlEscapedResult);
            pnlDoorBottomLeft.Visibility = Visibility.Collapsed;
            pnlClosedDoor.Visibility = Visibility.Visible;
            ShowCountdown(EscapedFrameElapsedMilliseconds);
        }

        // TODO: Pasar a la partida 2.8 s después de mostrar el resultado. (BL-118).
        private void ShowResultPanel(StackPanel resultContent)
        {
            pnlStage.Opacity = DimmedStageOpacity;
            lblCatchHint.Visibility = Visibility.Collapsed;
            brdRewardPill.Visibility = Visibility.Collapsed;
            brdResultPanel.Visibility = Visibility.Visible;
            resultContent.Visibility = Visibility.Visible;
        }

        private void ShowCountdown(int elapsedMilliseconds)
        {
            TimeSpan remainingTime = TimeSpan.FromSeconds(FugitiveKittenWindowSeconds)
                - TimeSpan.FromMilliseconds(elapsedMilliseconds);
            int remainingSeconds = (int)Math.Ceiling(remainingTime.TotalSeconds);
            string countdownBrushKey = remainingSeconds <= UrgentCountdownSeconds
                ? UrgentCountdownBrushKey
                : CountdownBrushKey;

            lblCountdown.Text = remainingSeconds.ToString(CultureInfo.CurrentCulture);
            lblCountdown.Foreground = (Brush)FindResource(countdownBrushKey);
        }

        private void OnFugitiveKittenScreenLoaded(object sender, RoutedEventArgs e)
        {
            Focus();
        }

        private void OnFugitiveKittenScreenKeyDown(object sender, KeyEventArgs e)
        {
            if (_sceneStateKeys.TryGetValue(e.Key, out KittenSceneState sceneState))
            {
                ShowSceneState(sceneState);
            }
            else if (e.Key == Key.C)
            {
                ShowNextKittenColor();
            }
            else if (e.Key == Key.Escape)
            {
                ((MainWindow)Application.Current.MainWindow).GoBack();
            }
        }

        /// <summary>
        /// Estados fijos del prototipo de Figma que la pantalla puede mostrar.
        /// </summary>
        private enum KittenSceneState
        {
            Intro,
            Running,
            Hidden,
            Caught,
            CaughtByOther,
            Escaped
        }

        /// <summary>
        /// Colores del gatito; el servidor elegirá uno y todos los jugadores verán el mismo.
        /// </summary>
        private enum KittenColor
        {
            White,
            Orange,
            Gray
        }
    }
}
