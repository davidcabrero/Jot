using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.RegularExpressions;
using Windows.UI;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Jot.Controls
{
    public sealed partial class MarkdownPreview : UserControl
    {
        public static readonly DependencyProperty MarkdownTextProperty =
            DependencyProperty.Register(nameof(MarkdownText), typeof(string), typeof(MarkdownPreview),
                new PropertyMetadata(string.Empty, OnMarkdownTextChanged));

        public string MarkdownText
        {
            get => (string)GetValue(MarkdownTextProperty);
            set => SetValue(MarkdownTextProperty, value);
        }

        private static void OnMarkdownTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MarkdownPreview preview && e.NewValue is string markdownText)
            {
                preview.RenderMarkdown(markdownText);
            }
        }

        public MarkdownPreview()
        {
            this.InitializeComponent();
        }

        private void RenderMarkdown(string markdown)
        {
            try
            {
                ContentPanel.Children.Clear();

                if (string.IsNullOrEmpty(markdown))
                    return;

                var lines = markdown.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
                bool inCodeBlock = false;
                bool inDetailsBlock = false;
                bool inQuizBlock = false;
                var codeBlockContent = new List<string>();
                var detailsContent = new List<string>();
                var quizContent = new List<string>();
                string codeBlockLanguage = "";
                string detailsSummary = "";
                
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var trimmedLine = line.Trim();
                    
                    // Handle code blocks first
                    if (trimmedLine.StartsWith("```"))
                    {
                        if (!inCodeBlock)
                        {
                            // Starting a code block
                            inCodeBlock = true;
                            codeBlockLanguage = trimmedLine.Length > 3 ? trimmedLine.Substring(3).Trim() : "";
                            codeBlockContent.Clear();
                        }
                        else
                        {
                            // Ending a code block
                            inCodeBlock = false;
                            AddCodeBlock(string.Join("\n", codeBlockContent), codeBlockLanguage);
                            codeBlockContent.Clear();
                        }
                        continue;
                    }
                    
                    if (inCodeBlock)
                    {
                        codeBlockContent.Add(line); // Keep original indentation for code
                        continue;
                    }
                    
                    // Handle details/summary blocks (collapsible sections)
                    if (trimmedLine.StartsWith("<details>"))
                    {
                        inDetailsBlock = true;
                        detailsContent.Clear();
                        detailsSummary = "";
                        continue;
                    }
                    
                    if (inDetailsBlock && trimmedLine.StartsWith("<summary>"))
                    {
                        // Extract summary text
                        var summaryMatch = Regex.Match(trimmedLine, @"<summary>(.*?)</summary>");
                        if (summaryMatch.Success)
                        {
                            detailsSummary = summaryMatch.Groups[1].Value.Trim();
                        }
                        continue;
                    }
                    
                    if (inDetailsBlock && trimmedLine.StartsWith("</details>"))
                    {
                        inDetailsBlock = false;
                        AddCollapsibleSection(detailsSummary, string.Join("\n", detailsContent));
                        detailsContent.Clear();
                        continue;
                    }
                    
                    if (inDetailsBlock)
                    {
                        detailsContent.Add(line);
                        continue;
                    }
                    
                    // Handle quiz blocks
                    if (trimmedLine.StartsWith("## Quiz Question") || 
                        (trimmedLine.StartsWith("##") && trimmedLine.Contains("Quiz")))
                    {
                        inQuizBlock = true;
                        quizContent.Clear();
                        quizContent.Add(line);
                        continue;
                    }
                    
                    if (inQuizBlock)
                    {
                        quizContent.Add(line);
                        
                        // Check if we've reached the end of the quiz (empty line after answer details)
                        if (string.IsNullOrEmpty(trimmedLine) && 
                            quizContent.Count > 5 && 
                            quizContent[quizContent.Count - 2].Trim().StartsWith("</details>"))
                        {
                            inQuizBlock = false;
                            AddQuizBlock(string.Join("\n", quizContent));
                            quizContent.Clear();
                        }
                        continue;
                    }
                    
                    if (string.IsNullOrEmpty(trimmedLine))
                    {
                        // Empty line - add some spacing
                        AddSpacing();
                        continue;
                    }

                    // Check for images ![alt](src)
                    var imageMatch = Regex.Match(trimmedLine, @"!\[([^\]]*)\]\(([^)]+)\)");
                    if (imageMatch.Success)
                    {
                        string altText = imageMatch.Groups[1].Value;
                        string imageSrc = imageMatch.Groups[2].Value;
                        AddImage(altText, imageSrc);
                        continue;
                    }

                    // Check for headers (must start with # and have space after)
                    if (trimmedLine.StartsWith("#"))
                    {
                        var headerMatch = Regex.Match(trimmedLine, @"^(#{1,6})\s+(.+)$");
                        if (headerMatch.Success)
                        {
                            var level = headerMatch.Groups[1].Value.Length;
                            var text = headerMatch.Groups[2].Value.Trim();
                            AddHeader(text, level);
                            continue;
                        }
                    }
                    
                    // Check for quotes
                    if (trimmedLine.StartsWith("> "))
                    {
                        AddQuote(trimmedLine.Substring(2));
                        continue;
                    }
                    
                    // Check for task lists first (more specific than bullet lists)
                    var taskMatch = Regex.Match(trimmedLine, @"^[-*]\s+\[([ xX])\]\s+(.+)$");
                    if (taskMatch.Success)
                    {
                        bool isChecked = taskMatch.Groups[1].Value.ToLower() == "x";
                        string content = taskMatch.Groups[2].Value;
                        AddCheckboxListItem(content, isChecked);
                        continue;
                    }
                    
                    // Check for bullet lists
                    if (Regex.IsMatch(trimmedLine, @"^[-*+]\s+.+"))
                    {
                        var content = Regex.Replace(trimmedLine, @"^[-*+]\s+", "");
                        AddBulletListItem(content);
                        continue;
                    }
                    
                    // Check for numbered lists
                    var numberedMatch = Regex.Match(trimmedLine, @"^(\d+)\.\s+(.+)$");
                    if (numberedMatch.Success)
                    {
                        AddNumberedListItem(numberedMatch.Groups[1].Value, numberedMatch.Groups[2].Value);
                        continue;
                    }
                    
                    // Check for horizontal rules
                    if (Regex.IsMatch(trimmedLine, @"^(---+|___+|\*\*\*+)$"))
                    {
                        AddHorizontalRule();
                        continue;
                    }
                    
                    // Everything else is a paragraph
                    AddFormattedParagraph(trimmedLine);
                }
                
                // Handle unclosed blocks
                if (inCodeBlock && codeBlockContent.Count > 0)
                {
                    AddCodeBlock(string.Join("\n", codeBlockContent), codeBlockLanguage);
                }
                
                if (inDetailsBlock && detailsContent.Count > 0)
                {
                    AddCollapsibleSection(detailsSummary, string.Join("\n", detailsContent));
                }
                
                if (inQuizBlock && quizContent.Count > 0)
                {
                    AddQuizBlock(string.Join("\n", quizContent));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error rendering markdown: {ex.Message}");
                // Show error message instead of crashing
                ContentPanel.Children.Clear();
                ContentPanel.Children.Add(new TextBlock 
                { 
                    Text = $"Error rendering markdown: {ex.Message}",
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)),
                    Margin = new Thickness(16)
                });
            }
        }

        private void AddSpacing()
        {
            ContentPanel.Children.Add(new Border { Height = 8 });
        }

        private void AddHeader(string text, int level)
        {
            double fontSize = level switch
            {
                1 => 32,
                2 => 26,
                3 => 22,
                4 => 18,
                5 => 16,
                6 => 14,
                _ => 16
            };

            var fontWeight = level <= 2 ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.SemiBold;
            
            var textBlock = new TextBlock
            {
                FontSize = fontSize,
                FontWeight = fontWeight,
                Margin = new Thickness(0, level == 1 ? 24 : 16, 0, 8),
                TextWrapping = TextWrapping.Wrap
            };
            
            // Process inline formatting for headers too
            ProcessInlineFormattingToTextBlock(textBlock, text);
            ContentPanel.Children.Add(textBlock);
        }

        private void AddQuote(string text)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, 100, 149, 237)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(180, 100, 149, 237)),
                BorderThickness = new Thickness(4, 0, 0, 0),
                Padding = new Thickness(16, 12, 12, 12),
                Margin = new Thickness(0, 8, 0, 8),
                CornerRadius = new CornerRadius(0, 4, 4, 0)
            };
            
            var textBlock = new TextBlock
            {
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                Opacity = 0.9,
                TextWrapping = TextWrapping.Wrap
            };
            
            ProcessInlineFormattingToTextBlock(textBlock, text);
            border.Child = textBlock;
            ContentPanel.Children.Add(border);
        }

        private void AddBulletListItem(string text)
        {
            var stackPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                Margin = new Thickness(0, 4, 0, 4) 
            };
            
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = "•", 
                Margin = new Thickness(16, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Top,
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            });
            
            var contentBlock = new TextBlock
            {
                Margin = new Thickness(0),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top
            };
            
            ProcessInlineFormattingToTextBlock(contentBlock, text);
            stackPanel.Children.Add(contentBlock);
            
            ContentPanel.Children.Add(stackPanel);
        }

        private void AddNumberedListItem(string number, string text)
        {
            var stackPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                Margin = new Thickness(0, 4, 0, 4) 
            };
            
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = number + ".", 
                Margin = new Thickness(16, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Top,
                FontSize = 14,
                MinWidth = 32,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            
            var contentBlock = new TextBlock
            {
                Margin = new Thickness(0),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top
            };
            
            ProcessInlineFormattingToTextBlock(contentBlock, text);
            stackPanel.Children.Add(contentBlock);
            
            ContentPanel.Children.Add(stackPanel);
        }

        private void AddCheckboxListItem(string text, bool isChecked)
        {
            var stackPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                Margin = new Thickness(0, 4, 0, 4) 
            };
            
            var checkbox = new CheckBox
            {
                IsChecked = isChecked,
                Margin = new Thickness(16, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Top,
                IsEnabled = false
            };
            stackPanel.Children.Add(checkbox);
            
            var contentBlock = new TextBlock
            {
                Margin = new Thickness(0),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top
            };
            
            if (isChecked)
            {
                contentBlock.Opacity = 0.7;
                contentBlock.TextDecorations = Windows.UI.Text.TextDecorations.Strikethrough;
            }
            
            ProcessInlineFormattingToTextBlock(contentBlock, text);
            stackPanel.Children.Add(contentBlock);
            
            ContentPanel.Children.Add(stackPanel);
        }

        private void AddCodeBlock(string code, string language)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(60, 128, 128, 128)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 12, 0, 12)
            };
            
            var stackPanel = new StackPanel();
            
            // Language label if specified
            if (!string.IsNullOrEmpty(language))
            {
                var languageLabel = new TextBlock
                {
                    Text = language,
                    FontSize = 12,
                    Opacity = 0.7,
                    Margin = new Thickness(0, 0, 0, 8),
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                };
                stackPanel.Children.Add(languageLabel);
            }
            
            var textBlock = new TextBlock
            {
                Text = code,
                FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New, monospace"),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                IsTextSelectionEnabled = true,
                LineHeight = 18
            };
            
            stackPanel.Children.Add(textBlock);
            border.Child = stackPanel;
            ContentPanel.Children.Add(border);
        }

        private void AddHorizontalRule()
        {
            var border = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromArgb(100, 128, 128, 128)),
                Margin = new Thickness(0, 16, 0, 16)
            };
            ContentPanel.Children.Add(border);
        }

        private void AddFormattedParagraph(string text)
        {
            var textBlock = new TextBlock 
            { 
                Margin = new Thickness(0, 6, 0, 6),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 24
            };
            
            ProcessInlineFormattingToTextBlock(textBlock, text);
            ContentPanel.Children.Add(textBlock);
        }

        private void ProcessInlineFormattingToTextBlock(TextBlock textBlock, string text)
        {
            textBlock.Inlines.Clear();
            
            if (string.IsNullOrEmpty(text))
            {
                textBlock.Inlines.Add(new Run { Text = "" });
                return;
            }

            var parts = ParseInlineFormatting(text);
            foreach (var part in parts)
            {
                textBlock.Inlines.Add(part);
            }
        }

        private List<Run> ParseInlineFormatting(string text)
        {
            var runs = new List<Run>();
            var remaining = text;
            
            while (!string.IsNullOrEmpty(remaining))
            {
                // Find all potential matches
                var patterns = new[]
                {
                    (@"\*\*(.+?)\*\*", "bold"),          // **bold**
                    (@"__(.+?)__", "bold"),              // __bold__
                    (@"\*(.+?)\*", "italic"),            // *italic*
                    (@"_(.+?)_", "italic"),              // _italic_
                    (@"`(.+?)`", "code"),                // `code`
                    (@"~~(.+?)~~", "strikethrough"),     // ~~strikethrough~~
                    (@"==(.+?)==", "highlight"),         // ==highlight==
                    (@"\[([^\]]+)\]\(([^)]+)\)", "link") // [text](url)
                };

                Match earliestMatch = null;
                string earliestType = null;
                int earliestIndex = int.MaxValue;

                // Find the earliest match
                foreach (var (pattern, type) in patterns)
                {
                    var match = Regex.Match(remaining, pattern);
                    if (match.Success && match.Index < earliestIndex)
                    {
                        earliestMatch = match;
                        earliestType = type;
                        earliestIndex = match.Index;
                    }
                }

                if (earliestMatch == null)
                {
                    // No more formatting, add the rest as plain text
                    if (!string.IsNullOrEmpty(remaining))
                    {
                        runs.Add(new Run { Text = remaining });
                    }
                    break;
                }

                // Add text before the match
                if (earliestMatch.Index > 0)
                {
                    var beforeText = remaining.Substring(0, earliestMatch.Index);
                    runs.Add(new Run { Text = beforeText });
                }

                // Add the formatted text
                string matchedText;
                var run = new Run();

                switch (earliestType)
                {
                    case "link":
                        // For links, show the link text with special formatting
                        matchedText = earliestMatch.Groups[1].Value; // Link text
                        var linkUrl = earliestMatch.Groups[2].Value; // URL
                        run.Text = $"{matchedText} 🔗";
                        run.Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 149, 237));
                        run.TextDecorations = Windows.UI.Text.TextDecorations.Underline;
                        break;
                    default:
                        matchedText = earliestMatch.Groups[1].Value;
                        run.Text = matchedText;
                        
                        switch (earliestType)
                        {
                            case "bold":
                                run.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
                                break;
                            case "italic":
                                run.FontStyle = Windows.UI.Text.FontStyle.Italic;
                                break;
                            case "code":
                                run.FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New, monospace");
                                run.Foreground = new SolidColorBrush(Color.FromArgb(255, 214, 157, 133));
                                run.FontSize = 13;
                                break;
                            case "strikethrough":
                                run.TextDecorations = Windows.UI.Text.TextDecorations.Strikethrough;
                                break;
                            case "highlight":
                                run.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
                                run.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
                                break;
                        }
                        break;
                }

                runs.Add(run);

                // Continue with the remaining text
                remaining = remaining.Substring(earliestMatch.Index + earliestMatch.Length);
            }

            return runs;
        }

        private void AddImage(string altText, string imageSrc)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 128, 128, 128)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 12, 0, 12),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var stackPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Image icon
            var imageIcon = new FontIcon
            {
                Glyph = "\uEB9F", // Image icon
                FontSize = 48,
                Foreground = new SolidColorBrush(Color.FromArgb(180, 100, 149, 237)),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stackPanel.Children.Add(imageIcon);

            // Alt text
            if (!string.IsNullOrEmpty(altText))
            {
                var altTextBlock = new TextBlock
                {
                    Text = altText,
                    FontSize = 14,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 8, 0, 4)
                };
                stackPanel.Children.Add(altTextBlock);
            }

            // Image source
            var sourceTextBlock = new TextBlock
            {
                Text = $"Image: {imageSrc}",
                FontSize = 12,
                Opacity = 0.7,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(sourceTextBlock);

            border.Child = stackPanel;
            ContentPanel.Children.Add(border);
        }

        private void AddCollapsibleSection(string summary, string content)
        {
            var expander = new Expander
            {
                Header = summary,
                Margin = new Thickness(0, 8, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var contentPanel = new StackPanel();
            
            // Process the content as markdown
            var contentLines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            foreach (var line in contentLines)
            {
                var trimmedLine = line.Trim();
                if (!string.IsNullOrEmpty(trimmedLine))
                {
                    var textBlock = new TextBlock
                    {
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 4, 0, 4)
                    };
                    ProcessInlineFormattingToTextBlock(textBlock, trimmedLine);
                    contentPanel.Children.Add(textBlock);
                }
                else
                {
                    contentPanel.Children.Add(new Border { Height = 8 });
                }
            }

            expander.Content = contentPanel;
            ContentPanel.Children.Add(expander);
        }

        private void AddQuizBlock(string quizContent)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 193, 7)), // Yellow background
                BorderBrush = new SolidColorBrush(Color.FromArgb(180, 255, 193, 7)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 16, 0, 16)
            };

            var mainPanel = new StackPanel();

            // Quiz icon and title
            var titlePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 12)
            };

            var quizIcon = new FontIcon
            {
                Glyph = "\uE8EB", // Quiz icon
                FontSize = 20,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 193, 7)),
                Margin = new Thickness(0, 0, 8, 0)
            };
            titlePanel.Children.Add(quizIcon);

            var titleText = new TextBlock
            {
                Text = "Quiz Question",
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            titlePanel.Children.Add(titleText);
            mainPanel.Children.Add(titlePanel);

            // Parse quiz content
            var lines = quizContent.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            string question = "";
            var options = new List<string>();
            string answer = "";
            string explanation = "";
            bool inAnswer = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                if (trimmedLine.StartsWith("**Question:**"))
                {
                    question = trimmedLine.Replace("**Question:**", "").Trim();
                }
                else if (Regex.IsMatch(trimmedLine, @"^[A-D]\)"))
                {
                    options.Add(trimmedLine);
                }
                else if (trimmedLine.StartsWith("Correct answer:"))
                {
                    answer = trimmedLine.Replace("Correct answer:", "").Trim();
                    inAnswer = true;
                }
                else if (trimmedLine.StartsWith("Explanation:"))
                {
                    explanation = trimmedLine.Replace("Explanation:", "").Trim();
                }
                else if (inAnswer && !string.IsNullOrEmpty(trimmedLine) && 
                         !trimmedLine.StartsWith("<") && !trimmedLine.StartsWith("Explanation:"))
                {
                    explanation += " " + trimmedLine;
                }
            }

            // Add question
            if (!string.IsNullOrEmpty(question))
            {
                var questionBlock = new TextBlock
                {
                    Text = question,
                    FontSize = 14,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 12),
                    TextWrapping = TextWrapping.Wrap
                };
                mainPanel.Children.Add(questionBlock);
            }

            // Add options
            foreach (var option in options)
            {
                var optionBlock = new TextBlock
                {
                    Text = option,
                    FontSize = 13,
                    Margin = new Thickness(0, 4, 0, 4),
                    TextWrapping = TextWrapping.Wrap
                };
                mainPanel.Children.Add(optionBlock);
            }

            // Add answer section
            if (!string.IsNullOrEmpty(answer) || !string.IsNullOrEmpty(explanation))
            {
                var answerExpander = new Expander
                {
                    Header = "Show Answer",
                    Margin = new Thickness(0, 12, 0, 0)
                };

                var answerPanel = new StackPanel();

                if (!string.IsNullOrEmpty(answer))
                {
                    var answerBlock = new TextBlock
                    {
                        Text = $"Correct answer: {answer}",
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 128, 0)),
                        Margin = new Thickness(0, 0, 0, 8)
                    };
                    answerPanel.Children.Add(answerBlock);
                }

                if (!string.IsNullOrEmpty(explanation))
                {
                    var explanationBlock = new TextBlock
                    {
                        Text = $"Explanation: {explanation}",
                        FontSize = 13,
                        TextWrapping = TextWrapping.Wrap
                    };
                    answerPanel.Children.Add(explanationBlock);
                }

                answerExpander.Content = answerPanel;
                mainPanel.Children.Add(answerExpander);
            }

            border.Child = mainPanel;
            ContentPanel.Children.Add(border);
        }
    }
}