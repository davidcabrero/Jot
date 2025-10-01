using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Jot.Controls
{
    public sealed partial class RichTextEditor : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(RichTextEditor),
                new PropertyMetadata(string.Empty, OnTextChanged));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RichTextEditor editor && e.NewValue is string newText)
            {
                if (editor.ContentTextBox.Text != newText)
                {
                    editor.ContentTextBox.Text = newText;
                }
            }
        }

        public event EventHandler<string>? TextChanged;

        public RichTextEditor()
        {
            this.InitializeComponent();
        }

        private void ContentTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Text = ContentTextBox.Text;
            TextChanged?.Invoke(this, ContentTextBox.Text);
        }

        private void BoldButton_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("**", "**");
        }

        private void ItalicButton_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("*", "*");
        }

        private void CodeButton_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("`", "`");
        }

        private void CodeBlockButton_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("```\n", "\n```");
        }

        private void HeaderButton_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("# ", "");
        }

        private void QuoteButton_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("> ", "");
        }

        private void ListButton_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("- ", "");
        }

        private void NumberedListButton_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("1. ", "");
        }

        private void CheckboxButton_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("- [ ] ", "");
        }

        private async void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".gif");
            picker.FileTypeFilter.Add(".bmp");

            // Get the current window handle
            var window = WindowHelper.GetWindowForElement(this);
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                InsertMarkdown($"![{file.Name}]({file.Path})", "");
            }
        }

        private void LinkButton_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("[", "](https://example.com)");
        }

        private void HighlightButton_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("==", "==");
        }

        private void StrikethroughButton_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("~~", "~~");
        }

        private void CollapsibleButton_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("<details>\n<summary>Click to expand</summary>\n\n", "\n\n</details>");
        }

        private void QuizButton_Click(object sender, RoutedEventArgs e)
        {
            var quizTemplate = @"## Quiz Question

**Question:** Your question here?

A) Option A
B) Option B
C) Option C
D) Option D

<details>
<summary>Answer</summary>
Correct answer: A

Explanation: Your explanation here.
</details>";

            InsertText(quizTemplate);
        }

        private void InsertMarkdown(string prefix, string suffix)
        {
            var selectionStart = ContentTextBox.SelectionStart;
            var selectionLength = ContentTextBox.SelectionLength;
            var selectedText = ContentTextBox.SelectedText;

            var newText = prefix + selectedText + suffix;
            
            ContentTextBox.SelectedText = newText;
            ContentTextBox.SelectionStart = selectionStart + prefix.Length;
            ContentTextBox.SelectionLength = selectedText.Length;
            ContentTextBox.Focus(FocusState.Programmatic);
        }

        private void InsertText(string text)
        {
            var selectionStart = ContentTextBox.SelectionStart;
            ContentTextBox.SelectedText = text;
            ContentTextBox.SelectionStart = selectionStart + text.Length;
            ContentTextBox.Focus(FocusState.Programmatic);
        }

        private void ContentTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Handle keyboard shortcuts
            var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
            
            if (ctrl)
            {
                switch (e.Key)
                {
                    case Windows.System.VirtualKey.B:
                        BoldButton_Click(sender, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                    case Windows.System.VirtualKey.I:
                        ItalicButton_Click(sender, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                }
            }
        }
    }

    public static class WindowHelper
    {
        public static Window GetWindowForElement(FrameworkElement element)
        {
            // Simple fallback to the main window for WinUI 3
            return App.MainWindow;
        }
    }
}