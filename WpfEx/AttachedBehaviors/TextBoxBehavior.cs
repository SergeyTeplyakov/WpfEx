using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfEx.Utils;

namespace WpfEx.AttachedBehaviors
{
    /// <summary>
    /// <see cref="TextBox"/> attached properties with some special behavior.
    /// </summary>
    /// <remarks>
    /// Class contains attached behaivor that extends existig <see cref="TextBox"/> behavior like IsInteger -
    /// user could type only numberic string or IsDouble - user could type +, -, . and numeric values
    /// or IsDoubl
    /// </remarks>
    public static class TextBoxBehavior
    {
        //-----------------------------------------------------------------------------------------
        // IsInteger
        //-----------------------------------------------------------------------------------------
        #region IsInteger
        
        /// <summary>
        /// Attached property that restricts <see cref="TextBox"/> input only to numberics.
        /// </summary>
        /// <remarks>
        /// This attached property works only for <see cref="TextBox"/> or for its descendants.
        /// </remarks>
        public readonly static DependencyProperty IsIntegerProperty =
          DependencyProperty.RegisterAttached(
              "IsInteger", typeof(bool),
              typeof(TextBoxBehavior),
              new FrameworkPropertyMetadata(false, OnIsIntegerChanged));

        /// <summary>
        /// Gets value for <see cref="IsIntegerProperty"/> property.
        /// </summary>
        /// <remarks>
        /// Using <see cref="AttachedPropertyBrowsableForTypeAttribute"/> restricts this attached 
        /// property only to <see cref="TextBox"/> and its descendants.
        /// </remarks>
        [AttachedPropertyBrowsableForType(typeof(TextBox))]
        public static bool GetIsInteger(DependencyObject element)
        {
            return (bool)element.GetValue(IsIntegerProperty);
        }

        /// <summary>
        /// Sets value for <see cref="IsIntegerProperty"/> property.
        /// </summary>
        public static void SetIsInteger(DependencyObject element, bool value)
        {
            element.SetValue(IsIntegerProperty, value);
        }

        /// <summary>
        /// Fires when <see cref="IsIntegerProperty"/> changed in xaml
        /// </summary>
        private static void OnIsIntegerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBox = (TextBox)d;

            // To restrict text box input only to numerics we should handle two events:
            // 1. PreviewTextInput - to handle custom (keyboard) text input and
            // 2. Paste command - to handle pasting custom text from clipboard.
            // Unfortunately we don't have PrevewTextChanged event so we should implmenent this manually
            textBox.PreviewTextInput += PreviewTextInputForInteger;
            DataObject.AddPastingHandler(textBox, OnPasteHandlerForInteger);
        }

        private static void OnPasteHandlerForInteger(object sender, DataObjectPastingEventArgs e)
        {
            var isText = e.SourceDataObject.GetDataPresent(DataFormats.Text, true);
            if (!isText) return;

            // Getting data from clipboard 
            var clipboardText = (string)e.SourceDataObject.GetData(DataFormats.Text);

            bool isInteger = StringEx.IsInt32(clipboardText);

            // Canceling command if clipboard text is not an integer 
            if (!isInteger)
                e.CancelCommand();
        }

        private static void PreviewTextInputForInteger(object sender, TextCompositionEventArgs e)
        {
            // Empty text is a valid value for text box 
            if (string.IsNullOrEmpty(e.Text))
                return;

            // Now, lets try parse current text 
            bool isInteger = StringEx.IsInt32(e.Text);

            // And we'll stop text changed event if text is not an integer 
            e.Handled = !isInteger;
        }

        #endregion

        //-----------------------------------------------------------------------------------------
        // IsDouble
        //-----------------------------------------------------------------------------------------
        #region IsDouble

        /// <summary>
        /// Attached property that restricts <see cref="TextBox"/> input only to doubles.
        /// </summary>
        /// <remarks>
        /// This attached property works only for <see cref="TextBox"/> or for its descendants.
        /// </remarks>
        public readonly static DependencyProperty IsDoubleProperty =
          DependencyProperty.RegisterAttached(
              "IsDouble", typeof(bool),
              typeof(TextBoxBehavior),
              new FrameworkPropertyMetadata(false, OnIsDoubleChanged));

        /// <summary>
        /// Gets value for <see cref="IsDoubleProperty"/> property.
        /// </summary>
        /// <remarks>
        /// Using <see cref="AttachedPropertyBrowsableForTypeAttribute"/> restricts this attached 
        /// property only to <see cref="TextBox"/> and its descendants.
        /// </remarks>
        [AttachedPropertyBrowsableForType(typeof(TextBox))]
        public static bool GetIsDouble(DependencyObject element)
        {
            return (bool)element.GetValue(IsDoubleProperty);
        }

        /// <summary>
        /// Sets valud for <see cref="IsDoubleProperty"/> property.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        public static void SetIsDouble(DependencyObject element, bool value)
        {
            element.SetValue(IsDoubleProperty, value);
        }

        /// <summary>
        /// Fires when IsDouble attached property changed.
        /// </summary>
        private static void OnIsDoubleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBox = (TextBox)d;
            
            // As well as for IsInteger attached property we should handle 2 cases:
            // 1. User input via TextBox.PreviewTextInput event
            // 2. Pasting text from clipboard
            textBox.PreviewTextInput += PreviewTextInputForDouble;
            DataObject.AddPastingHandler(textBox, OnPasteForDouble);
        }


        private static void OnPasteForDouble(object sender, DataObjectPastingEventArgs e)
        {
            var textBox = (TextBox)sender;

            var isText = e.SourceDataObject.GetDataPresent(DataFormats.Text, true);
            if (!isText) return;

            // Getting data from clipboard 
            var clipboardText = (string)e.SourceDataObject.GetData(DataFormats.Text);

            // Lets create full text box text by current text box state and text from clipboard 
            string text = CreateFullText(textBox, clipboardText);

            bool isValid = TextBoxDoubleValidator.IsValid(text);

            // If text is invalid we should cancel current command 
            if (!isValid)
                e.CancelCommand();
        }

        private static void PreviewTextInputForDouble(object sender, TextCompositionEventArgs e)
        {
            var textBox = (TextBox)sender;

            // Unforortunately TextBox doesn't provide PreviewTextChanged event where we can get 
            // full string that will be send to TextChanged event. 
            // PreviewTextInput contains only new input in e.Text property.

            // But in some cases for validation we need full text but not only 
            // new input, in this case we should create full text by hand.

            // Full text contains TextBox.Text WITHOUT selected text with e.Text 
            string fullText = CreateFullText(textBox, e.Text);

            // Now fullText contains TextBox.Text that we'll have in TextChanged event 
            bool isTextValid = TextBoxDoubleValidator.IsValid(fullText);

            // We'll stop text changed event if text is not a valid double 
            e.Handled = !isTextValid;
        }

        /// <summary> 
        /// Creates full text box text by current text box state and new text, typed into text box 
        /// </summary> 
        private static string CreateFullText(TextBox textBox, string inputText)
        {
            string text;
            if (textBox.SelectionLength > 0)
            {
                // Replacing text box's selected text with text from clipboard 
                text = textBox.Text.Replace(textBox.SelectedText, inputText);
            }
            else
            {
                // If we don't have selected text we should insert clipboard text 
                // into the caret index. 
                text = textBox.Text.Insert(textBox.CaretIndex, inputText);
            }

            return text;
        }

        #endregion


    }
}