using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Text.RegularExpressions;
using Windows.UI;
using Microsoft.UI.Xaml.Documents;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using Windows.Foundation;

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

        // Annotation mode properties
        private bool _isAnnotationMode = false;
        private bool _isAnnotating = false;
        private Polyline? _currentAnnotation;
        private Color _annotationColor = Color.FromArgb(255, 255, 0, 0); // Red
        private double _annotationSize = 2.0;
        private readonly List<UIElement> _annotations = new();

        public event EventHandler<bool>? AnnotationModeChanged;

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
            InitializeAnnotationMode();
        }

        private void InitializeAnnotationMode()
        {
            // Set default annotation color (select red by default)
            RedAnnotationBtn.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
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
                    
                    // Check for table rows
                    if (trimmedLine.Contains("|") && !trimmedLine.StartsWith("```"))
                    {
                        // Check if this is part of a table
                        if (IsTableRow(trimmedLine) || IsTableHeader(trimmedLine, i, lines))
                        {
                            var tableLines = ExtractTableLines(lines, ref i);
                            AddTable(tableLines);
                            continue;
                        }
                    }
                    
                    // Check for math formulas
                    if (trimmedLine.StartsWith("$$") || trimmedLine.Contains("$$"))
                    {
                        var mathContent = ExtractMathContent(lines, ref i);
                        AddMathFormula(mathContent);
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
            // Special handling for Mermaid diagrams
            if (language.ToLower() == "mermaid")
            {
                AddMermaidDiagram(code);
                return;
            }

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

        private void AddMermaidDiagram(string diagramContent)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(30, 75, 181, 67)), // Green background
                BorderBrush = new SolidColorBrush(Color.FromArgb(150, 75, 181, 67)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 16, 0, 16)
            };

            var stackPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Diagram icon and title
            var titlePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            };

            var diagramIcon = new FontIcon
            {
                Glyph = "\uE8B2", // Diagram icon
                FontSize = 24,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 75, 181, 67)),
                Margin = new Thickness(0, 0, 8, 0)
            };
            titlePanel.Children.Add(diagramIcon);

            var titleText = new TextBlock
            {
                Text = "Mermaid Diagram",
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            titlePanel.Children.Add(titleText);
            stackPanel.Children.Add(titlePanel);

            // Simplified diagram representation
            var diagramType = DetectMermaidDiagramType(diagramContent);
            var visualRepresentation = CreateSimplifiedDiagram(diagramContent, diagramType);
            stackPanel.Children.Add(visualRepresentation);

            // Code preview
            var codeExpander = new Expander
            {
                Header = "View Diagram Code",
                Margin = new Thickness(0, 12, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var codeBlock = new TextBlock
            {
                Text = diagramContent,
                FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New, monospace"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                IsTextSelectionEnabled = true
            };

            codeExpander.Content = codeBlock;
            stackPanel.Children.Add(codeExpander);

            border.Child = stackPanel;
            ContentPanel.Children.Add(border);
        }

        private string DetectMermaidDiagramType(string content)
        {
            var firstLine = content.Split('\n')[0].Trim();
            
            if (firstLine.StartsWith("graph"))
                return "flowchart";
            else if (firstLine.StartsWith("sequenceDiagram"))
                return "sequence";
            else if (firstLine.StartsWith("gantt"))
                return "gantt";
            else if (firstLine.StartsWith("pie"))
                return "pie";
            else if (firstLine.StartsWith("classDiagram"))
                return "class";
            else
                return "generic";
        }

        private FrameworkElement CreateSimplifiedDiagram(string content, string type)
        {
            var container = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(20),
                MinHeight = 150,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var grid = new Grid();
            
            switch (type)
            {
                case "flowchart":
                    container.Child = CreateFlowchartPreview(content);
                    break;
                case "sequence":
                    container.Child = CreateSequencePreview();
                    break;
                case "gantt":
                    container.Child = CreateGanttPreview();
                    break;
                case "pie":
                    container.Child = CreatePiePreview();
                    break;
                default:
                    container.Child = CreateGenericDiagramPreview(type);
                    break;
            }

            return container;
        }

        private StackPanel CreateFlowchartPreview(string content)
        {
            var panel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Simple flowchart representation
            var elements = new[]
            {
                "📋 Start",
                "⬇️",
                "⚙️ Process",
                "⬇️", 
                "❓ Decision",
                "⬇️",
                "✅ End"
            };

            foreach (var element in elements)
            {
                var block = new TextBlock
                {
                    Text = element,
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                panel.Children.Add(block);
            }

            return panel;
        }

        private StackPanel CreateSequencePreview()
        {
            var panel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            panel.Children.Add(new TextBlock { Text = "👤 Actor A", FontSize = 14, HorizontalAlignment = HorizontalAlignment.Center });
            panel.Children.Add(new TextBlock { Text = "⬇️ Message", FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) });
            panel.Children.Add(new TextBlock { Text = "👤 Actor B", FontSize = 14, HorizontalAlignment = HorizontalAlignment.Center });
            panel.Children.Add(new TextBlock { Text = "⬆️ Response", FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) });

            return panel;
        }

        private StackPanel CreateGanttPreview()
        {
            var panel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            panel.Children.Add(new TextBlock { Text = "📊 Gantt Chart", FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center });
            panel.Children.Add(new TextBlock { Text = "📅 Timeline visualization", FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) });
            panel.Children.Add(new TextBlock { Text = "▬▬▬ Task 1", FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 0) });
            panel.Children.Add(new TextBlock { Text = "  ▬▬▬ Task 2", FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 0) });

            return panel;
        }

        private StackPanel CreatePiePreview()
        {
            var panel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            panel.Children.Add(new TextBlock { Text = "🥧 Pie Chart", FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center });
            panel.Children.Add(new TextBlock { Text = "📊 Data visualization", FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0) });

            return panel;
        }

        private StackPanel CreateGenericDiagramPreview(string type)
        {
            var panel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            panel.Children.Add(new TextBlock { Text = "📈 Diagram", FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center });
            panel.Children.Add(new TextBlock { Text = $"Type: {type}", FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) });

            return panel;
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
                    (@"<span style=""color:\s*([^""]+)"">([^<]+)</span>", "color"), // <span style="color: #color">text</span>
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
                    case "color":
                        // Handle colored text
                        var colorValue = earliestMatch.Groups[1].Value.Trim();
                        matchedText = earliestMatch.Groups[2].Value;
                        run.Text = matchedText;
                        
                        // Parse color value
                        if (TryParseColor(colorValue, out Color color))
                        {
                            run.Foreground = new SolidColorBrush(color);
                        }
                        break;
                        
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

        private bool TryParseColor(string colorString, out Color color)
        {
            color = Color.FromArgb(255, 0, 0, 0); // Black
            
            try
            {
                colorString = colorString.Trim().ToLowerInvariant();
                
                // Handle hex colors
                if (colorString.StartsWith("#"))
                {
                    var hex = colorString.Substring(1);
                    if (hex.Length == 6)
                    {
                        var r = Convert.ToByte(hex.Substring(0, 2), 16);
                        var g = Convert.ToByte(hex.Substring(2, 2), 16);
                        var b = Convert.ToByte(hex.Substring(4, 2), 16);
                        color = Color.FromArgb(255, r, g, b);
                        return true;
                    }
                    else if (hex.Length == 3)
                    {
                        var r = Convert.ToByte(hex.Substring(0, 1) + hex.Substring(0, 1), 16);
                        var g = Convert.ToByte(hex.Substring(1, 1) + hex.Substring(1, 1), 16);
                        var b = Convert.ToByte(hex.Substring(2, 1) + hex.Substring(2, 1), 16);
                        color = Color.FromArgb(255, r, g, b);
                        return true;
                    }
                }
                
                // Handle RGB colors
                if (colorString.StartsWith("rgb("))
                {
                    var values = colorString.Substring(4, colorString.Length - 5).Split(',');
                    if (values.Length == 3)
                    {
                        var r = byte.Parse(values[0].Trim());
                        var g = byte.Parse(values[1].Trim());
                        var b = byte.Parse(values[2].Trim());
                        color = Color.FromArgb(255, r, g, b);
                        return true;
                    }
                }
                
                // Handle named colors
                var namedColors = new Dictionary<string, Color>
                {
                    {"black", Color.FromArgb(255, 0, 0, 0)},
                    {"white", Color.FromArgb(255, 255, 255, 255)},
                    {"red", Color.FromArgb(255, 255, 0, 0)},
                    {"green", Color.FromArgb(255, 0, 128, 0)},
                    {"blue", Color.FromArgb(255, 0, 0, 255)},
                    {"yellow", Color.FromArgb(255, 255, 255, 0)},
                    {"orange", Color.FromArgb(255, 255, 165, 0)},
                    {"purple", Color.FromArgb(255, 128, 0, 128)},
                    {"pink", Color.FromArgb(255, 255, 192, 203)},
                    {"brown", Color.FromArgb(255, 139, 69, 19)},
                    {"gray", Color.FromArgb(255, 128, 128, 128)},
                    {"grey", Color.FromArgb(255, 128, 128, 128)},
                    {"darkred", Color.FromArgb(255, 139, 0, 0)},
                    {"darkgreen", Color.FromArgb(255, 0, 100, 0)},
                    {"darkblue", Color.FromArgb(255, 0, 0, 139)},
                    {"gold", Color.FromArgb(255, 255, 215, 0)},
                    {"silver", Color.FromArgb(255, 192, 192, 192)},
                    {"maroon", Color.FromArgb(255, 128, 0, 0)},
                    {"navy", Color.FromArgb(255, 0, 0, 128)},
                    {"teal", Color.FromArgb(255, 0, 128, 128)}
                };
                
                if (namedColors.TryGetValue(colorString, out color))
                {
                    return true;
                }
            }
            catch
            {
                // If parsing fails, fall back to black
            }
            
            return false;
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

        private bool IsTableRow(string line)
        {
            var trimmed = line.Trim();
            return trimmed.StartsWith("|") && trimmed.EndsWith("|") && trimmed.Count(c => c == '|') >= 2;
        }

        private bool IsTableHeader(string line, int currentIndex, string[] lines)
        {
            if (currentIndex + 1 >= lines.Length) return false;
            var nextLine = lines[currentIndex + 1].Trim();
            return IsTableRow(line) && Regex.IsMatch(nextLine, @"^\|[\s\-\|:]+\|$");
        }

        private List<string> ExtractTableLines(string[] lines, ref int currentIndex)
        {
            var tableLines = new List<string>();
            
            // Get all consecutive table lines
            while (currentIndex < lines.Length && IsTableRow(lines[currentIndex].Trim()))
            {
                tableLines.Add(lines[currentIndex]);
                currentIndex++;
            }
            currentIndex--; // Adjust because the loop will increment
            
            return tableLines;
        }

        private string ExtractMathContent(string[] lines, ref int currentIndex)
        {
            var mathContent = new StringBuilder();
            var line = lines[currentIndex];
            
            if (line.Trim().StartsWith("$$") && line.Trim().EndsWith("$$") && line.Trim().Length > 4)
            {
                // Single line math
                return line.Trim();
            }
            
            // Multi-line math
            mathContent.AppendLine(line);
            currentIndex++;
            
            while (currentIndex < lines.Length)
            {
                line = lines[currentIndex];
                mathContent.AppendLine(line);
                
                if (line.Trim().EndsWith("$$"))
                {
                    break;
                }
                currentIndex++;
            }
            
            return mathContent.ToString();
        }

        private void AddTable(List<string> tableLines)
        {
            if (tableLines.Count < 2) return;

            var border = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromArgb(100, 128, 128, 128)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 12, 0, 12)
            };

            var grid = new Grid();
            
            // Parse header
            var headerCells = ParseTableRow(tableLines[0]);
            
            // Setup columns
            for (int i = 0; i < headerCells.Count; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            
            // Setup rows - count all rows except separator
            var dataRowCount = tableLines.Count - 1; // Subtract header and separator rows
            for (int i = 0; i < dataRowCount; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            int rowIndex = 0;
            
            // Add header
            for (int col = 0; col < headerCells.Count; col++)
            {
                var cell = CreateTableCell(headerCells[col], true);
                Grid.SetRow(cell, rowIndex);
                Grid.SetColumn(cell, col);
                grid.Children.Add(cell);
            }
            rowIndex++;

            // Add data rows (skip header and separator at index 1)
            for (int row = 0; row < tableLines.Count; row++)
            {
                if (row == 0 || row == 1) continue; // Skip header (0) and separator (1)
                
                var cells = ParseTableRow(tableLines[row]);
                for (int col = 0; col < Math.Min(cells.Count, headerCells.Count); col++)
                {
                    var cell = CreateTableCell(cells[col], false);
                    Grid.SetRow(cell, rowIndex);
                    Grid.SetColumn(cell, col);
                    grid.Children.Add(cell);
                }
                rowIndex++;
            }

            border.Child = grid;
            ContentPanel.Children.Add(border);
        }

        private List<string> ParseTableRow(string row)
        {
            var cells = row.Split('|')
                .Skip(1) // Skip first empty element (before first |)
                .Take(row.Split('|').Length - 2) // Skip last empty element (after last |)
                .Select(cell => cell.Trim())
                .ToList();
            return cells;
        }

        private Border CreateTableCell(string content, bool isHeader)
        {
            var cell = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromArgb(50, 128, 128, 128)),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Padding = new Thickness(8, 6, 8, 6),
                Background = isHeader ? 
                    new SolidColorBrush(Color.FromArgb(30, 100, 149, 237)) : 
                    new SolidColorBrush(Color.FromArgb(10, 128, 128, 128))
            };

            var textBlock = new TextBlock
            {
                FontWeight = isHeader ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal,
                TextWrapping = TextWrapping.Wrap
            };

            ProcessInlineFormattingToTextBlock(textBlock, content);
            cell.Child = textBlock;
            
            return cell;
        }

        private void AddMathFormula(string mathContent)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, 100, 149, 237)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(100, 100, 149, 237)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 12, 0, 12),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var stackPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Math icon
            var mathIcon = new FontIcon
            {
                Glyph = "\uE8EF", // Calculator icon
                FontSize = 20,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 149, 237)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            stackPanel.Children.Add(mathIcon);

            // Process and clean the formula text
            var formulaText = ProcessMathFormula(mathContent);
            var textBlock = new TextBlock
            {
                Text = formulaText,
                FontFamily = new FontFamily("Cambria Math, Times New Roman, serif"),
                FontSize = 14,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                LineHeight = 20,
                IsTextSelectionEnabled = true
            };
            stackPanel.Children.Add(textBlock);

            // Add note
            var noteBlock = new TextBlock
            {
                Text = "📐 Mathematical Formula (LaTeX)",
                FontSize = 12,
                Opacity = 0.7,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            };
            stackPanel.Children.Add(noteBlock);

            border.Child = stackPanel;
            ContentPanel.Children.Add(border);
        }

        private string ProcessMathFormula(string mathContent)
        {
            // Clean up the LaTeX code for better readability
            var processed = mathContent.Replace("$$", "").Trim();
            
            // Replace common LaTeX commands with more readable text
            processed = processed.Replace("\\begin{align}", "");
            processed = processed.Replace("\\end{align}", "");
            processed = processed.Replace("\\\\", "\n");
            processed = processed.Replace("&=", " = ");
            processed = processed.Replace("\\pm", "±");
            processed = processed.Replace("\\sqrt{", "√(");
            processed = processed.Replace("\\frac{", "");
            processed = Regex.Replace(processed, @"}\{", " / (");
            processed = processed.Replace("}{", ") / (");
            processed = processed.Replace("{", "(");
            processed = processed.Replace("}", ")");
            processed = processed.Replace("^2", "²");
            processed = processed.Replace("^", "^");
            
            // Clean up extra spaces and line breaks
            processed = Regex.Replace(processed, @"\s+", " ");
            processed = processed.Replace(" \n ", "\n");
            processed = processed.Trim();
            
            return processed;
        }

        // Annotation Mode Methods
        private void AnnotationModeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleAnnotationMode();
        }

        private void ToggleAnnotationMode()
        {
            _isAnnotationMode = !_isAnnotationMode;
            
            if (_isAnnotationMode)
            {
                // Enable annotation mode
                AnnotationCanvas.Visibility = Visibility.Visible;
                AnnotationCanvas.IsHitTestVisible = true;
                AnnotationToolbar.Visibility = Visibility.Visible;
                
                // Change button appearance
                AnnotationModeButton.Background = new SolidColorBrush(Color.FromArgb(50, 255, 165, 0));
                
                System.Diagnostics.Debug.WriteLine("Annotation mode enabled on Preview - You can now draw on the document");
            }
            else
            {
                // Disable annotation mode
                AnnotationCanvas.Visibility = Visibility.Collapsed;
                AnnotationCanvas.IsHitTestVisible = false;
                AnnotationToolbar.Visibility = Visibility.Collapsed;
                
                // Reset button appearance
                AnnotationModeButton.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)); // Transparent
                
                System.Diagnostics.Debug.WriteLine("Annotation mode disabled");
            }

            // Notify about the mode change
            AnnotationModeChanged?.Invoke(this, _isAnnotationMode);
        }

        private void AnnotationCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!_isAnnotationMode) return;

            _isAnnotating = true;
            var position = e.GetCurrentPoint(AnnotationCanvas).Position;
            
            _currentAnnotation = new Polyline
            {
                Stroke = new SolidColorBrush(_annotationColor),
                StrokeThickness = _annotationSize,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Opacity = 0.8
            };

            _currentAnnotation.Points.Add(position);
            AnnotationCanvas.Children.Add(_currentAnnotation);
            AnnotationCanvas.CapturePointer(e.Pointer);
        }

        private void AnnotationCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isAnnotationMode || !_isAnnotating || _currentAnnotation == null) return;

            var position = e.GetCurrentPoint(AnnotationCanvas).Position;
            _currentAnnotation.Points.Add(position);
        }

        private void AnnotationCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!_isAnnotationMode || !_isAnnotating) return;

            if (_currentAnnotation != null && _currentAnnotation.Points.Count > 0)
            {
                _annotations.Add(_currentAnnotation);
                System.Diagnostics.Debug.WriteLine($"Annotation completed with {_currentAnnotation.Points.Count} points");
            }

            _isAnnotating = false;
            _currentAnnotation = null;
            AnnotationCanvas.ReleasePointerCapture(e.Pointer);
        }

        private void AnnotationCanvas_PointerCaptureLost(object sender, object e)
        {
            if (_isAnnotating && _currentAnnotation != null)
            {
                _annotations.Add(_currentAnnotation);
            }
            
            _isAnnotating = false;
            _currentAnnotation = null;
        }

        private void AnnotationColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string colorName)
            {
                // Reset all borders
                RedAnnotationBtn.BorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                BlueAnnotationBtn.BorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                GreenAnnotationBtn.BorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                YellowAnnotationBtn.BorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

                // Set new color and highlight selected button
                switch (colorName)
                {
                    case "Red":
                        button.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        _annotationColor = Color.FromArgb(255, 255, 0, 0);
                        break;
                    case "Blue":
                        button.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        _annotationColor = Color.FromArgb(255, 0, 0, 255);
                        break;
                    case "Green":
                        button.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                        _annotationColor = Color.FromArgb(255, 0, 128, 0);
                        break;
                    case "Yellow":
                        button.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
                        _annotationColor = Color.FromArgb(255, 255, 165, 0); // Orange for better visibility
                        break;
                    default:
                        _annotationColor = Color.FromArgb(255, 255, 0, 0);
                        break;
                }

                System.Diagnostics.Debug.WriteLine($"Annotation color changed to: {colorName}");
            }
        }

        private void AnnotationSizeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            _annotationSize = e.NewValue;
            System.Diagnostics.Debug.WriteLine($"Annotation size changed to: {_annotationSize}");
        }

        private void UndoAnnotation_Click(object sender, RoutedEventArgs e)
        {
            if (_annotations.Count > 0)
            {
                var lastAnnotation = _annotations[_annotations.Count - 1];
                AnnotationCanvas.Children.Remove(lastAnnotation);
                _annotations.RemoveAt(_annotations.Count - 1);
                
                System.Diagnostics.Debug.WriteLine("Last annotation undone");
            }
        }

        private void ClearAnnotations_Click(object sender, RoutedEventArgs e)
        {
            AnnotationCanvas.Children.Clear();
            _annotations.Clear();
            
            System.Diagnostics.Debug.WriteLine("All annotations cleared");
        }

        private void ExitAnnotationMode_Click(object sender, RoutedEventArgs e)
        {
            ToggleAnnotationMode();
        }

        // Public methods for external control
        public void SetAnnotationMode(bool enabled)
        {
            if (_isAnnotationMode != enabled)
            {
                ToggleAnnotationMode();
            }
        }

        public bool IsAnnotationModeActive => _isAnnotationMode;

        public void SaveAnnotations()
        {
            // TODO: Implement saving annotations to document metadata
            System.Diagnostics.Debug.WriteLine($"Saved {_annotations.Count} annotations");
        }

        public void LoadAnnotations()
        {
            // TODO: Implement loading annotations from document metadata
            System.Diagnostics.Debug.WriteLine("Loading annotations...");
        }
    }
}