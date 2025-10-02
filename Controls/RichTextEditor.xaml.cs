using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using System.Linq;
using Windows.UI;

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
            UpdateDocumentStats();
            UpdateCursorPosition();
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

        private void TableButton_Click(object sender, RoutedEventArgs e)
        {
            var tableTemplate = @"| Header 1 | Header 2 | Header 3 |
|----------|----------|----------|
| Row 1 Col 1 | Row 1 Col 2 | Row 1 Col 3 |
| Row 2 Col 1 | Row 2 Col 2 | Row 2 Col 3 |";

            InsertText(tableTemplate);
        }

        private void EmojiButton_Click(object sender, RoutedEventArgs e)
        {
            var commonEmojis = new[]
            {
                "😀", "😃", "😄", "😁", "😆", "😅", "😂", "🤣",
                "😊", "😇", "🙂", "🙃", "😉", "😌", "😍", "🥰",
                "😘", "😗", "😙", "😚", "😋", "😛", "😝", "😜",
                "🤪", "🤨", "🧐", "🤓", "😎", "🤩", "🥳", "😏",
                "👍", "👎", "👌", "🤞", "✌️", "🤟", "🤘", "👊",
                "✊", "🤝", "👏", "🙌", "👐", "🤲", "🤝", "💪",
                "🎉", "🎊", "🎈", "🎁", "🏆", "🥇", "🥈", "🥉"
            };

            // Create a simple emoji picker menu
            var menuFlyout = new MenuFlyout();
            
            foreach (var emoji in commonEmojis)
            {
                var menuItem = new MenuFlyoutItem
                {
                    Text = emoji
                };
                menuItem.Click += (s, args) => InsertText(emoji);
                menuFlyout.Items.Add(menuItem);
            }

            menuFlyout.ShowAt(EmojiButton);
        }

        private void MathButton_Click(object sender, RoutedEventArgs e)
        {
            var mathTemplate = @"$$
\begin{align}
f(x) &= ax^2 + bx + c \\
x &= \frac{-b \pm \sqrt{b^2 - 4ac}}{2a}
\end{align}
$$";

            InsertText(mathTemplate);
        }

        private void DiagramButton_Click(object sender, RoutedEventArgs e)
        {
            var diagramTemplate = @"```mermaid
graph TD
    A[Start] --> B{Decision}
    B -->|Yes| C[Process 1]
    B -->|No| D[Process 2]
    C --> E[End]
    D --> E
```";

            InsertText(diagramTemplate);
        }

        private void StatsButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateDocumentStats();
            ShowStatsDialog();
        }

        private void FindReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            FindReplacePanel.Visibility = FindReplacePanel.Visibility == Visibility.Visible 
                ? Visibility.Collapsed 
                : Visibility.Visible;
            
            if (FindReplacePanel.Visibility == Visibility.Visible)
            {
                FindTextBox.Focus(FocusState.Programmatic);
            }
        }

        private void FindNextButton_Click(object sender, RoutedEventArgs e)
        {
            var searchText = FindTextBox.Text;
            if (string.IsNullOrEmpty(searchText)) return;

            var content = ContentTextBox.Text;
            var currentPosition = ContentTextBox.SelectionStart + ContentTextBox.SelectionLength;
            
            var index = content.IndexOf(searchText, currentPosition, StringComparison.OrdinalIgnoreCase);
            if (index == -1)
            {
                // Search from beginning
                index = content.IndexOf(searchText, 0, StringComparison.OrdinalIgnoreCase);
            }

            if (index != -1)
            {
                ContentTextBox.SelectionStart = index;
                ContentTextBox.SelectionLength = searchText.Length;
                ContentTextBox.Focus(FocusState.Programmatic);
            }
        }

        private void ReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            var searchText = FindTextBox.Text;
            var replaceText = ReplaceTextBox.Text;
            
            if (string.IsNullOrEmpty(searchText)) return;

            if (ContentTextBox.SelectedText.Equals(searchText, StringComparison.OrdinalIgnoreCase))
            {
                ContentTextBox.SelectedText = replaceText;
                FindNextButton_Click(sender, e); // Find next occurrence
            }
            else
            {
                FindNextButton_Click(sender, e); // Find first occurrence
            }
        }

        private void ReplaceAllButton_Click(object sender, RoutedEventArgs e)
        {
            var searchText = FindTextBox.Text;
            var replaceText = ReplaceTextBox.Text;
            
            if (string.IsNullOrEmpty(searchText)) return;

            var content = ContentTextBox.Text;
            var newContent = content.Replace(searchText, replaceText, StringComparison.OrdinalIgnoreCase);
            ContentTextBox.Text = newContent;
        }

        private void CloseFindButton_Click(object sender, RoutedEventArgs e)
        {
            FindReplacePanel.Visibility = Visibility.Collapsed;
            ContentTextBox.Focus(FocusState.Programmatic);
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
                    case Windows.System.VirtualKey.F:
                        if (FindReplacePanel.Visibility == Visibility.Collapsed)
                        {
                            FindReplaceButton_Click(sender, new RoutedEventArgs());
                        }
                        e.Handled = true;
                        break;
                    case Windows.System.VirtualKey.H:
                        FindReplaceButton_Click(sender, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                }
            }
            
            // Update cursor position on any key press
            UpdateCursorPosition();
        }

        private void UpdateDocumentStats()
        {
            var text = ContentTextBox.Text;
            
            // Word count
            var wordCount = string.IsNullOrWhiteSpace(text) ? 0 : 
                text.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
            
            // Character count
            var charCount = text.Length;
            var charCountNoSpaces = text.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "").Length;
            
            // Reading time (average 200 words per minute)
            var readingTimeMinutes = Math.Max(1, (int)Math.Ceiling(wordCount / 200.0));
            
            // Update UI
            WordCountText.Text = $"Words: {wordCount:N0}";
            CharCountText.Text = $"Characters: {charCount:N0} ({charCountNoSpaces:N0} no spaces)";
            ReadingTimeText.Text = $"Reading time: {readingTimeMinutes} min";
        }

        private void UpdateCursorPosition()
        {
            var text = ContentTextBox.Text;
            var selectionStart = ContentTextBox.SelectionStart;
            
            if (string.IsNullOrEmpty(text))
            {
                CursorPositionText.Text = "Line 1, Column 1";
                return;
            }
            
            var lines = text.Substring(0, Math.Min(selectionStart, text.Length)).Split('\n');
            var lineNumber = lines.Length;
            var columnNumber = lines[lines.Length - 1].Length + 1;
            
            CursorPositionText.Text = $"Line {lineNumber}, Column {columnNumber}";
        }

        private async void ShowStatsDialog()
        {
            var text = ContentTextBox.Text;
            
            // Calculate detailed statistics
            var stats = CalculateDetailedStats(text);
            
            var dialog = new ContentDialog
            {
                Title = "Document Statistics",
                Content = CreateStatsContent(stats),
                CloseButtonText = "Close",
                DefaultButton = ContentDialogButton.Close
            };

            // Set XamlRoot for WinUI 3
            dialog.XamlRoot = this.XamlRoot;
            
            await dialog.ShowAsync();
        }

        private DocumentStats CalculateDetailedStats(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new DocumentStats();
            }

            var lines = text.Split('\n');
            var words = text.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var paragraphs = text.Split(new string[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            // Count sentences (approximate)
            var sentences = text.Split(new char[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(s => !string.IsNullOrWhiteSpace(s)).Count();
            
            // Count headers
            var headers = lines.Count(line => line.TrimStart().StartsWith("#"));
            
            // Count code blocks
            var codeBlocks = System.Text.RegularExpressions.Regex.Matches(text, @"```").Count / 2;
            
            // Count links
            var links = System.Text.RegularExpressions.Regex.Matches(text, @"\[.*?\]\(.*?\)").Count;
            
            // Count images
            var images = System.Text.RegularExpressions.Regex.Matches(text, @"!\[.*?\]\(.*?\)").Count;

            return new DocumentStats
            {
                Characters = text.Length,
                CharactersNoSpaces = text.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "").Length,
                Words = words.Length,
                Sentences = sentences,
                Paragraphs = paragraphs.Length,
                Lines = lines.Length,
                Headers = headers,
                CodeBlocks = codeBlocks,
                Links = links,
                Images = images,
                ReadingTimeMinutes = Math.Max(1, (int)Math.Ceiling(words.Length / 200.0))
            };
        }

        private StackPanel CreateStatsContent(DocumentStats stats)
        {
            var panel = new StackPanel { Spacing = 8 };
            
            // Basic stats
            panel.Children.Add(CreateStatItem("📝 Characters:", $"{stats.Characters:N0} ({stats.CharactersNoSpaces:N0} without spaces)"));
            panel.Children.Add(CreateStatItem("📖 Words:", $"{stats.Words:N0}"));
            panel.Children.Add(CreateStatItem("💬 Sentences:", $"{stats.Sentences:N0}"));
            panel.Children.Add(CreateStatItem("📄 Paragraphs:", $"{stats.Paragraphs:N0}"));
            panel.Children.Add(CreateStatItem("📏 Lines:", $"{stats.Lines:N0}"));
            
            // Separator
            panel.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromArgb(50, 128, 128, 128)), Margin = new Thickness(0, 8, 0, 8) });
            
            // Markdown elements
            panel.Children.Add(CreateStatItem("🔤 Headers:", $"{stats.Headers:N0}"));
            panel.Children.Add(CreateStatItem("💻 Code Blocks:", $"{stats.CodeBlocks:N0}"));
            panel.Children.Add(CreateStatItem("🔗 Links:", $"{stats.Links:N0}"));
            panel.Children.Add(CreateStatItem("🖼️ Images:", $"{stats.Images:N0}"));
            
            // Separator
            panel.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromArgb(50, 128, 128, 128)), Margin = new Thickness(0, 8, 0, 8) });
            
            // Reading info
            panel.Children.Add(CreateStatItem("⏱️ Reading Time:", $"{stats.ReadingTimeMinutes} minute(s)"));
            panel.Children.Add(CreateStatItem("📊 Avg. Words/Sentence:", $"{(stats.Sentences > 0 ? stats.Words / (double)stats.Sentences : 0):F1}"));
            
            return panel;
        }

        private Grid CreateStatItem(string label, string value)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            var labelBlock = new TextBlock
            {
                Text = label,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };
            Grid.SetColumn(labelBlock, 0);
            
            var valueBlock = new TextBlock
            {
                Text = value,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(valueBlock, 1);
            
            grid.Children.Add(labelBlock);
            grid.Children.Add(valueBlock);
            
            return grid;
        }

        public class DocumentStats
        {
            public int Characters { get; set; }
            public int CharactersNoSpaces { get; set; }
            public int Words { get; set; }
            public int Sentences { get; set; }
            public int Paragraphs { get; set; }
            public int Lines { get; set; }
            public int Headers { get; set; }
            public int CodeBlocks { get; set; }
            public int Links { get; set; }
            public int Images { get; set; }
            public int ReadingTimeMinutes { get; set; }
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