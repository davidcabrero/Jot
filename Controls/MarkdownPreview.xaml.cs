using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Text.RegularExpressions;
using Windows.UI;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

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
            ContentPanel.Children.Clear();

            if (string.IsNullOrEmpty(markdown))
                return;

            var lines = markdown.Split('\n');
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                if (string.IsNullOrEmpty(trimmedLine))
                {
                    // Empty line
                    ContentPanel.Children.Add(new Border { Height = 8 });
                    continue;
                }

                if (trimmedLine.StartsWith("# "))
                {
                    // Header 1
                    var textBlock = new TextBlock
                    {
                        Text = trimmedLine.Substring(2),
                        FontSize = 24,
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                        Margin = new Thickness(0, 16, 0, 8)
                    };
                    ContentPanel.Children.Add(textBlock);
                }
                else if (trimmedLine.StartsWith("## "))
                {
                    // Header 2
                    var textBlock = new TextBlock
                    {
                        Text = trimmedLine.Substring(3),
                        FontSize = 20,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        Margin = new Thickness(0, 12, 0, 6)
                    };
                    ContentPanel.Children.Add(textBlock);
                }
                else if (trimmedLine.StartsWith("### "))
                {
                    // Header 3
                    var textBlock = new TextBlock
                    {
                        Text = trimmedLine.Substring(4),
                        FontSize = 16,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        Margin = new Thickness(0, 8, 0, 4)
                    };
                    ContentPanel.Children.Add(textBlock);
                }
                else if (trimmedLine.StartsWith("> "))
                {
                    // Quote
                    var border = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(20, 128, 128, 128)),
                        BorderBrush = new SolidColorBrush(Color.FromArgb(100, 128, 128, 128)),
                        BorderThickness = new Thickness(4, 0, 0, 0),
                        Padding = new Thickness(12, 8, 8, 8),
                        Margin = new Thickness(0, 4, 0, 4)
                    };
                    
                    var textBlock = new TextBlock
                    {
                        Text = trimmedLine.Substring(2),
                        FontStyle = Windows.UI.Text.FontStyle.Italic,
                        TextWrapping = TextWrapping.Wrap
                    };
                    
                    border.Child = textBlock;
                    ContentPanel.Children.Add(border);
                }
                else if (trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("* "))
                {
                    // Bullet list
                    var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
                    stackPanel.Children.Add(new TextBlock 
                    { 
                        Text = "•", 
                        Margin = new Thickness(0, 0, 8, 0),
                        VerticalAlignment = VerticalAlignment.Top
                    });
                    stackPanel.Children.Add(new TextBlock 
                    { 
                        Text = trimmedLine.Substring(2),
                        TextWrapping = TextWrapping.Wrap
                    });
                    
                    ContentPanel.Children.Add(stackPanel);
                }
                else if (Regex.IsMatch(trimmedLine, @"^\d+\. "))
                {
                    // Numbered list
                    var match = Regex.Match(trimmedLine, @"^(\d+)\. (.*)");
                    if (match.Success)
                    {
                        var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
                        stackPanel.Children.Add(new TextBlock 
                        { 
                            Text = match.Groups[1].Value + ".", 
                            Margin = new Thickness(0, 0, 8, 0),
                            VerticalAlignment = VerticalAlignment.Top
                        });
                        stackPanel.Children.Add(new TextBlock 
                        { 
                            Text = match.Groups[2].Value,
                            TextWrapping = TextWrapping.Wrap
                        });
                        
                        ContentPanel.Children.Add(stackPanel);
                    }
                }
                else if (trimmedLine.StartsWith("```"))
                {
                    // Code block (simplified)
                    var border = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(30, 128, 128, 128)),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(12),
                        Margin = new Thickness(0, 8, 0, 8)
                    };
                    
                    var textBlock = new TextBlock
                    {
                        Text = "Code block", // Simplified - would need proper parsing
                        FontFamily = new FontFamily("Consolas"),
                        TextWrapping = TextWrapping.Wrap
                    };
                    
                    border.Child = textBlock;
                    ContentPanel.Children.Add(border);
                }
                else
                {
                    // Regular paragraph with inline formatting
                    var textBlock = CreateFormattedTextBlock(trimmedLine);
                    textBlock.Margin = new Thickness(0, 4, 0, 4);
                    ContentPanel.Children.Add(textBlock);
                }
            }
        }

        private TextBlock CreateFormattedTextBlock(string text)
        {
            var textBlock = new TextBlock { TextWrapping = TextWrapping.Wrap };
            
            // Simple inline formatting (would need more robust parsing for full markdown)
            // For now, just handle basic bold and italic
            
            // Replace **bold** with bold formatting
            text = Regex.Replace(text, @"\*\*(.*?)\*\*", "<Bold>$1</Bold>");
            
            // Replace *italic* with italic formatting
            text = Regex.Replace(text, @"\*(.*?)\*", "<Italic>$1</Italic>");
            
            // Replace `code` with code formatting
            text = Regex.Replace(text, @"`(.*?)`", "<Code>$1</Code>");
            
            // For this simplified version, just set the text directly
            // A full implementation would parse the formatted text properly
            textBlock.Text = Regex.Replace(text, @"<[^>]+>", "");
            
            return textBlock;
        }
    }
}