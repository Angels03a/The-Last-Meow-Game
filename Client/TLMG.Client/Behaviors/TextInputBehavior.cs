using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TLMG.Client.Validation;

namespace TLMG.Client.Behaviors
{
    public static class TextInputBehavior
    {
        public static readonly DependencyProperty InputTypeProperty =
            DependencyProperty.RegisterAttached(
                "InputType",
                typeof(ValidationInputType),
                typeof(TextInputBehavior),
                new PropertyMetadata(ValidationInputType.None, OnInputTypeChanged));

        public static ValidationInputType GetInputType(DependencyObject targetObject)
        {
            ValidationInputType inputType = (ValidationInputType)targetObject.GetValue(InputTypeProperty);
            return inputType;
        }

        public static void SetInputType(DependencyObject targetObject, ValidationInputType value)
        {
            targetObject.SetValue(InputTypeProperty, value);
        }

        private static void OnInputTypeChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs eventArgs)
        {
            if (dependencyObject is TextBox textBox)
            {
                textBox.PreviewTextInput -= OnPreviewTextInput;
                textBox.PreviewKeyDown -= OnPreviewKeyDown;
                DataObject.RemovePastingHandler(textBox, OnPasting);

                ValidationInputType newInputType = (ValidationInputType)eventArgs.NewValue;

                if (newInputType != ValidationInputType.None)
                {
                    textBox.PreviewTextInput += OnPreviewTextInput;
                    textBox.PreviewKeyDown += OnPreviewKeyDown;
                    DataObject.AddPastingHandler(textBox, OnPasting);
                }
            }
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs eventArgs)
        {
            if (sender is TextBox textBox && eventArgs.Key == Key.Space)
            {
                ValidationInputType inputType = GetInputType(textBox);
                bool isSpaceBlocked = inputType == ValidationInputType.Nickname
                    || inputType == ValidationInputType.Email
                    || inputType == ValidationInputType.DigitsOnly;

                if (isSpaceBlocked)
                {
                    eventArgs.Handled = true;
                }
            }
        }

        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs eventArgs)
        {
            if (sender is TextBox textBox)
            {
                ValidationInputType inputType = GetInputType(textBox);
                string proposedText = BuildProposedText(textBox, eventArgs.Text);
                bool isAllowed = InputValidator.CanAcceptTextInput(proposedText, inputType);

                eventArgs.Handled = !isAllowed;
            }
        }

        private static void OnPasting(object sender, DataObjectPastingEventArgs eventArgs)
        {
            if (sender is TextBox textBox)
            {
                string pastedText = eventArgs.DataObject.GetData(DataFormats.UnicodeText) as string;
                bool isPastedTextValid = false;

                if (pastedText != null)
                {
                    ValidationInputType inputType = GetInputType(textBox);
                    string proposedText = BuildProposedText(textBox, pastedText);
                    isPastedTextValid = InputValidator.CanAcceptTextInput(proposedText, inputType);
                }

                if (!isPastedTextValid)
                {
                    eventArgs.CancelCommand();
                }
            }
        }

        private static string BuildProposedText(TextBox textBox, string insertedText)
        {
            string currentText = textBox.Text ?? string.Empty;
            string withoutSelection = currentText.Remove(textBox.SelectionStart, textBox.SelectionLength);
            string proposedText = withoutSelection.Insert(textBox.SelectionStart, insertedText);

            return proposedText;
        }
    }
}