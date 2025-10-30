using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using System.Linq;
using Windows.UI;
using System.Collections.Generic;
using Windows.Foundation;

namespace Jot.Controls
{
    public sealed partial class RichTextEditor : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(RichTextEditor),
                new PropertyMetadata(string.Empty, OnTextChanged));

        public static readonly DependencyProperty CurrentTextColorProperty =
            DependencyProperty.Register(nameof(CurrentTextColor), typeof(SolidColorBrush), typeof(RichTextEditor),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 0, 0, 0))));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public SolidColorBrush CurrentTextColor
        {
            get => (SolidColorBrush)GetValue(CurrentTextColorProperty);
            set => SetValue(CurrentTextColorProperty, value);
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

        private readonly Dictionary<string, Color> _predefinedColors = new()
        {
            { "Black", Color.FromArgb(255, 0, 0, 0) },
            { "Red", Color.FromArgb(255, 255, 0, 0) },
            { "Green", Color.FromArgb(255, 0, 128, 0) },
            { "Blue", Color.FromArgb(255, 0, 0, 255) },
            { "Orange", Color.FromArgb(255, 255, 165, 0) },
            { "Purple", Color.FromArgb(255, 128, 0, 128) },
            { "Brown", Color.FromArgb(255, 139, 69, 19) },
            { "Pink", Color.FromArgb(255, 255, 192, 203) },
            { "Gray", Color.FromArgb(255, 128, 128, 128) },
            { "Dark Red", Color.FromArgb(255, 139, 0, 0) },
            { "Dark Green", Color.FromArgb(255, 0, 100, 0) },
            { "Dark Blue", Color.FromArgb(255, 0, 0, 139) },
            { "Gold", Color.FromArgb(255, 255, 215, 0) },
            { "Indigo", Color.FromArgb(255, 75, 0, 130) },
            { "Maroon", Color.FromArgb(255, 128, 0, 0) },
            { "Teal", Color.FromArgb(255, 0, 128, 128) }
        };

        public event EventHandler<string>? TextChanged;
        public event EventHandler<bool>? DrawingModeChanged;
        public event EventHandler? RequestFreeDrawing;

        public RichTextEditor()
        {
            this.InitializeComponent();
            InitializeColorPicker();
        }

        private void InitializeColorPicker()
        {
            // Add quick color buttons to GridView
            var colorButtons = new List<Button>();
            
            foreach (var colorPair in _predefinedColors)
            {
                var colorButton = new Button
                {
                    Width = 20,
                    Height = 20,
                    Background = new SolidColorBrush(colorPair.Value),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(2),
                    Margin = new Thickness(1)
                };

                // Set tooltip
                ToolTipService.SetToolTip(colorButton, colorPair.Key);

                colorButton.Click += (sender, e) =>
                {
                    CurrentTextColor = new SolidColorBrush(colorPair.Value);
                    ApplyTextColor_Click(sender, e);
                };

                colorButtons.Add(colorButton);
            }

            QuickColorsGrid.ItemsSource = colorButtons;
        }

        private void CustomColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            CurrentTextColor = new SolidColorBrush(args.NewColor);
        }

        private void ApplyTextColor_Click(object sender, RoutedEventArgs e)
        {
            var color = CurrentTextColor.Color;
            var hexColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            
            InsertMarkdown($"<span style=\"color: {hexColor}\">", "</span>");
            ColorPickerFlyout.Hide();
        }

        private void ResetTextColor_Click(object sender, RoutedEventArgs e)
        {
            CurrentTextColor = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
            CustomColorPicker.Color = Color.FromArgb(255, 0, 0, 0);
        }

        private async void DrawingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Show options for different drawing modes
                var drawingOptionsDialog = new ContentDialog
                {
                    Title = "🎨 Modo de Dibujo",
                    PrimaryButtonText = "Dibujo Libre",
                    SecondaryButtonText = "Plantillas",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                var optionsPanel = new StackPanel { Spacing = 16, Margin = new Thickness(20, 20, 20, 20) };
                
                optionsPanel.Children.Add(new TextBlock
                {
                    Text = "🖌️ Dibujo Libre con Rotulador",
                    FontSize = 16,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                });
                
                optionsPanel.Children.Add(new TextBlock
                {
                    Text = "Dibuja libremente con un rotulador digital. Cambia colores, grosor y herramientas.",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 12)
                });

                optionsPanel.Children.Add(new TextBlock
                {
                    Text = "📐 Plantillas y Esquemas",
                    FontSize = 16,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                });
                
                optionsPanel.Children.Add(new TextBlock
                {
                    Text = "Usa plantillas predefinidas para diagramas, organigramas y esquemas.",
                    TextWrapping = TextWrapping.Wrap
                });

                drawingOptionsDialog.Content = optionsPanel;

                var result = await drawingOptionsDialog.ShowAsync();
                
                if (result == ContentDialogResult.Primary)
                {
                    // Trigger free drawing mode - this event will be handled by MainWindow
                    RequestFreeDrawing?.Invoke(this, EventArgs.Empty);
                }
                else if (result == ContentDialogResult.Secondary)
                {
                    // Show template options
                    await ShowDrawingTemplateDialog();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening drawing dialog: {ex.Message}");
                
                // Fallback to simple menu
                ShowSimpleDrawingMenu(sender);
            }
        }

        private async System.Threading.Tasks.Task ShowDrawingTemplateDialog()
        {
            try
            {
                // Create an interactive drawing dialog
                var dialog = new ContentDialog
                {
                    Title = "📐 Plantillas de Dibujo",
                    PrimaryButtonText = "Insertar",
                    SecondaryButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                // Create drawing options panel
                var drawingPanel = CreateDrawingOptionsPanel();
                dialog.Content = drawingPanel;

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    // Get selected drawing from the panel
                    var selectedDrawing = GetSelectedDrawingFromPanel(drawingPanel);
                    if (!string.IsNullOrEmpty(selectedDrawing))
                    {
                        InsertText(selectedDrawing);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing template dialog: {ex.Message}");
            }
        }

        // Continue with all other methods without duplication...
        private StackPanel CreateDrawingOptionsPanel()
        {
            var panel = new StackPanel { Spacing = 16, Margin = new Thickness(20, 20, 20, 20) };
            
            // Add basic drawing options
            panel.Children.Add(new TextBlock 
            { 
                Text = "⚡ Herramientas de Dibujo", 
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                FontSize = 16
            });
            
            var freeDrawingButton = new Button
            {
                Content = "🖌️ Rotulador Libre",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 120, 215)),
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
                Margin = new Thickness(0, 8, 0, 0)
            };

            freeDrawingButton.Click += (s, e) =>
            {
                RequestFreeDrawing?.Invoke(this, EventArgs.Empty);
            };

            panel.Children.Add(freeDrawingButton);
            
            return panel;
        }

        private void ShowSimpleDrawingMenu(object sender)
        {
            var menuFlyout = new MenuFlyout();
            
            var freeDrawingItem = new MenuFlyoutItem 
            { 
                Text = "🖌️ Dibujo Libre (Rotulador)",
                Icon = new FontIcon { Glyph = "&#xEDC6;" }
            };
            freeDrawingItem.Click += (s, args) => RequestFreeDrawing?.Invoke(this, EventArgs.Empty);
            menuFlyout.Items.Add(freeDrawingItem);

            menuFlyout.Items.Add(new MenuFlyoutSeparator());

            // ASCII art option as fallback
            var asciiDrawingItem = new MenuFlyoutItem 
            { 
                Text = "📝 Dibujo ASCII",
                Icon = new FontIcon { Glyph = "&#xE8A5;" }
            };
            asciiDrawingItem.Click += (s, args) => {
                // Simple ASCII art insertion
                var asciiArt = @"
```
┌─────────────────┐
│  Tu dibujo aquí │
│                 │
│   ○ → □ → △     │
│                 │
└─────────────────┘
```
";
                InsertText(asciiArt);
            };
            menuFlyout.Items.Add(asciiDrawingItem);

            menuFlyout.ShowAt(sender as FrameworkElement);
        }

        private string GetSelectedDrawingFromPanel(StackPanel panel)
        {
            return ""; // Simplified for now
        }

        // All other essential methods continue here...
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

        // Event handlers for toolbar buttons
        private void ContentTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                Text = textBox.Text;
                TextChanged?.Invoke(this, textBox.Text);
            }
        }

        private void ContentTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Handle keyboard shortcuts
            if (e.Key == Windows.System.VirtualKey.B && 
                (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control) & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
            {
                BoldButton_Click(sender, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Windows.System.VirtualKey.I && 
                     (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control) & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
            {
                ItalicButton_Click(sender, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void BoldButton_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("**", "**");
        }

        private void ItalicButton_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("*", "*");
        }

        private void StrikethroughButton_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("~~", "~~");
        }

        private void HighlightButton_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("==", "==");
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
            var menuFlyout = new MenuFlyout();
            
            for (int i = 1; i <= 6; i++)
            {
                var headerLevel = i;
                var menuItem = new MenuFlyoutItem
                {
                    Text = $"Header {headerLevel}",
                    Icon = new FontIcon { Glyph = "&#xE8C4;" }
                };
                menuItem.Click += (s, args) => InsertMarkdown($"\n{new string('#', headerLevel)} ", "\n");
                menuFlyout.Items.Add(menuItem);
            }

            menuFlyout.ShowAt(sender as FrameworkElement);
        }

        private void QuoteButton_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("\n> ", "\n");
        }

        private void ListButton_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("\n- ", "\n");
        }

        private void NumberedListButton_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("\n1. ", "\n");
        }

        private void CheckboxButton_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("\n- [ ] ", "\n");
        }

        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            ShowImageDialog();
     }

        private async void ShowImageDialog()
      {
   try
      {
     var dialog = new ContentDialog
    {
           Title = "🖼️ Insert Image",
     PrimaryButtonText = "Insert",
         SecondaryButtonText = "Browse File",
       CloseButtonText = "Cancel",
  XamlRoot = this.XamlRoot
         };

       var panel = new StackPanel { Spacing = 12, Width = 400 };

        // URL input
                var urlTextBox = new TextBox
       {
           Header = "Image URL",
      PlaceholderText = "https://example.com/image.jpg"
        };
            panel.Children.Add(urlTextBox);

  // Alt text
 var altTextBox = new TextBox
     {
    Header = "Alt text (description)",
  PlaceholderText = "Description of the image"
  };
                panel.Children.Add(altTextBox);

             // Preview area
          var previewBorder = new Border
          {
             Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 248, 248, 248)),
     BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 200, 200, 200)),
          BorderThickness = new Thickness(1),
       CornerRadius = new CornerRadius(4),
    Height = 150,
  Margin = new Thickness(0, 8, 0, 0)
      };

                var previewImage = new Image
  {
        Stretch = Stretch.Uniform,
              HorizontalAlignment = HorizontalAlignment.Center,
  VerticalAlignment = VerticalAlignment.Center
    };

   var previewText = new TextBlock
       {
           Text = "Image preview will appear here",
      HorizontalAlignment = HorizontalAlignment.Center,
    VerticalAlignment = VerticalAlignment.Center,
                  Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 128, 128, 128))
         };

         var previewGrid = new Grid();
    previewGrid.Children.Add(previewText);
            previewGrid.Children.Add(previewImage);
                previewBorder.Child = previewGrid;
              panel.Children.Add(previewBorder);

            dialog.Content = panel;

        // URL change handler for preview
        urlTextBox.TextChanged += async (s, e) =>
    {
   if (!string.IsNullOrEmpty(urlTextBox.Text) && Uri.TryCreate(urlTextBox.Text, UriKind.Absolute, out var uri))
        {
  try
    {
  previewImage.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(uri);
       previewText.Visibility = Visibility.Collapsed;
       previewImage.Visibility = Visibility.Visible;
      }
         catch
             {
      previewImage.Visibility = Visibility.Collapsed;
     previewText.Visibility = Visibility.Visible;
             previewText.Text = "Failed to load image";
           }
 }
           else
{
  previewImage.Visibility = Visibility.Collapsed;
   previewText.Visibility = Visibility.Visible;
    previewText.Text = "Image preview will appear here";
         }
           };

 var result = await dialog.ShowAsync();

    if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(urlTextBox.Text))
     {
       var altText = string.IsNullOrEmpty(altTextBox.Text) ? "Image" : altTextBox.Text;
  var imageMarkdown = $"![{altText}]({urlTextBox.Text})";
         InsertText(imageMarkdown);
      }
         else if (result == ContentDialogResult.Secondary)
           {
   await BrowseForImageFile();
         }
   }
            catch (Exception ex)
          {
      System.Diagnostics.Debug.WriteLine($"Error showing image dialog: {ex.Message}");
     // Fallback to simple insertion
        InsertMarkdown("![Alt text](", ")");
            }
        }

        private async Task BrowseForImageFile()
    {
            try
            {
      var picker = new FileOpenPicker();
                picker.FileTypeFilter.Add(".jpg");
     picker.FileTypeFilter.Add(".jpeg");
    picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".gif");
    picker.FileTypeFilter.Add(".bmp");
                picker.FileTypeFilter.Add(".svg");
   picker.FileTypeFilter.Add(".webp");

    // Get the current window handle
      var window = App.MainWindow;
         var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

     var file = await picker.PickSingleFileAsync();
       if (file != null)
      {
        // For local files, we'll create a relative path or use file:// URL
 var filePath = file.Path;
         var fileName = file.Name;
            
        // Show dialog with file info
          var fileDialog = new ContentDialog
       {
     Title = "Selected Image File",
 PrimaryButtonText = "Insert",
 CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
  };

           var filePanel = new StackPanel { Spacing = 12 };
             
     filePanel.Children.Add(new TextBlock 
         { 
       Text = $"File: {fileName}",
        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
               });

    var altTextBox = new TextBox
         {
 Header = "Alt text (description)",
     PlaceholderText = fileName,
           Text = fileName
    };
        filePanel.Children.Add(altTextBox);

         // Show preview
         try
         {
 var previewImage = new Image
          {
        MaxHeight = 200,
 Stretch = Stretch.Uniform,
         Margin = new Thickness(0, 8, 0, 0)
    };

 var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
  using (var stream = await file.OpenAsync(FileAccessMode.Read))
    {
      await bitmap.SetSourceAsync(stream);
         }
               previewImage.Source = bitmap;
         filePanel.Children.Add(previewImage);
         }
     catch
            {
              filePanel.Children.Add(new TextBlock 
               { 
         Text = "Could not preview image",
          Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 128, 128, 128))
       });
               }

              fileDialog.Content = filePanel;

         var result = await fileDialog.ShowAsync();
    if (result == ContentDialogResult.Primary)
          {
     var altText = string.IsNullOrEmpty(altTextBox.Text) ? fileName : altTextBox.Text;
             // Use file:// URL for local files
  var imageMarkdown = $"![{altText}](file:///{filePath.Replace('\\', '/')})";
       InsertText(imageMarkdown);
                }
          }
  }
            catch (Exception ex)
       {
      System.Diagnostics.Debug.WriteLine($"Error browsing for image file: {ex.Message}");
     // Fallback to simple insertion
     InsertMarkdown("![Alt text](", ")");
            }
 }

        private void LinkButton_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("[Link text](", ")");
        }

        private void CollapsibleButton_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdown("\n<details>\n<summary>Summary</summary>\n\n", "\n\n</details>\n");
        }

        private void QuizButton_Click(object sender, RoutedEventArgs e)
        {
            var quizTemplate = @"
## Quiz Question

**Question:** Your question here?

A) Option A
B) Option B  
C) Option C
D) Option D

<details>
<summary>Show Answer</summary>

Correct answer: A

Explanation: Your explanation here.
</details>
";
            InsertText(quizTemplate);
        }

        private void TableButton_Click(object sender, RoutedEventArgs e)
        {
            var tableTemplate = @"
| Header 1 | Header 2 | Header 3 |
|----------|----------|----------|
| Cell 1   | Cell 2   | Cell 3   |
| Cell 4   | Cell 5   | Cell 6   |
";
            InsertText(tableTemplate);
        }

        private void EmojiButton_Click(object sender, RoutedEventArgs e)
        {
            var emojis = new[]
            {
                ("😀", "Smile"), ("😂", "Laugh"), ("😍", "Heart Eyes"), ("🤔", "Thinking"),
                ("👍", "Thumbs Up"), ("👎", "Thumbs Down"), ("❤️", "Heart"), ("🔥", "Fire"),
                ("⭐", "Star"), ("✅", "Check"), ("❌", "Cross"), ("⚠️", "Warning"),
                ("💡", "Idea"), ("📝", "Note"), ("📊", "Chart"), ("🎯", "Target"),
                ("🚀", "Rocket"), ("💻", "Computer"), ("📱", "Phone"), ("🌟", "Sparkle")
            };

            var menuFlyout = new MenuFlyout();
            
            foreach (var (emoji, name) in emojis)
            {
                var menuItem = new MenuFlyoutItem
                {
                    Text = $"{emoji} {name}"
                };
                menuItem.Click += (s, args) => InsertText(emoji);
                menuFlyout.Items.Add(menuItem);
            }

            menuFlyout.ShowAt(sender as FrameworkElement);
        }

        private void MathButton_Click(object sender, RoutedEventArgs e)
        {
            var mathTemplates = new[]
            {
                ("Inline Formula", "$x = \\frac{-b \\pm \\sqrt{b^2-4ac}}{2a}$"),
                ("Block Formula", "$$\\int_{-\\infty}^{\\infty} e^{-x^2} dx = \\sqrt{\\pi}$$"),
                ("Fraction", "$\\frac{numerator}{denominator}$"),
                ("Square Root", "$\\sqrt{x}$"),
                ("Summation", "$\\sum_{i=1}^{n} x_i$"),
                ("Integral", "$\\int_a^b f(x) dx$")
            };

            var menuFlyout = new MenuFlyout();
            
            foreach (var (name, formula) in mathTemplates)
            {
                var menuItem = new MenuFlyoutItem
                {
                    Text = name
                };
                menuItem.Click += (s, args) => InsertText(formula);
                menuFlyout.Items.Add(menuItem);
            }

            menuFlyout.ShowAt(sender as FrameworkElement);
        }

        private void DiagramButton_Click(object sender, RoutedEventArgs e)
        {
            // Simplified diagram insertion - just basic text
            var diagramTemplate = @"
```
┌─────────────────┐
│  Diagram Title  │
├─────────────────┤
│                 │
│  [Your diagram  │
│   content here] │
│                 │
└─────────────────┘
```
";
            InsertText(diagramTemplate);
        }

        private void StatsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var content = ContentTextBox.Text ?? "";
                var wordCount = content.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
                var charCount = content.Length;
                var charCountNoSpaces = content.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "").Length;
                var lineCount = content.Split('\n').Length;
                var readingTime = Math.Max(1, (int)Math.Ceiling(wordCount / 200.0)); // Assuming 200 words per minute

                var statsMessage = $@"Document Statistics:

• Words: {wordCount:N0}
• Characters: {charCount:N0}
• Characters (no spaces): {charCountNoSpaces:N0}
• Lines: {lineCount:N0}
• Estimated reading time: {readingTime} minute{(readingTime != 1 ? "s" : "")}";

                // Show stats in a content dialog
                ShowStatsDialog(statsMessage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating statistics: {ex.Message}");
            }
        }

        private async void ShowStatsDialog(string message)
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = "Document Statistics",
                    Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                    CloseButtonText = "Close",
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing stats dialog: {ex.Message}");
            }
        }

        // Find and Replace functionality
        private void FindReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle find/replace panel visibility
            if (FindReplacePanel != null)
            {
                FindReplacePanel.Visibility = FindReplacePanel.Visibility == Visibility.Visible 
                    ? Visibility.Collapsed 
                    : Visibility.Visible;
                
                if (FindReplacePanel.Visibility == Visibility.Visible && FindTextBox != null)
                {
                    FindTextBox.Focus(FocusState.Programmatic);
                }
            }
        }

        private void FindNextButton_Click(object sender, RoutedEventArgs e)
        {
            if (FindTextBox?.Text is string searchText && !string.IsNullOrEmpty(searchText))
            {
                var content = ContentTextBox.Text ?? "";
                var currentPosition = ContentTextBox.SelectionStart + ContentTextBox.SelectionLength;
                var foundIndex = content.IndexOf(searchText, currentPosition, StringComparison.OrdinalIgnoreCase);
                
                if (foundIndex == -1)
                {
                    // Search from beginning
                    foundIndex = content.IndexOf(searchText, 0, StringComparison.OrdinalIgnoreCase);
                }
                
                if (foundIndex >= 0)
                {
                    ContentTextBox.SelectionStart = foundIndex;
                    ContentTextBox.SelectionLength = searchText.Length;
                    ContentTextBox.Focus(FocusState.Programmatic);
                }
            }
        }

        private void ReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            if (FindTextBox?.Text is string searchText && 
                ReplaceTextBox?.Text is string replaceText && 
                !string.IsNullOrEmpty(searchText) &&
                ContentTextBox.SelectedText == searchText)
            {
                ContentTextBox.SelectedText = replaceText;
                FindNextButton_Click(sender, e);
            }
        }

        private void ReplaceAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (FindTextBox?.Text is string searchText && 
                ReplaceTextBox?.Text is string replaceText && 
                !string.IsNullOrEmpty(searchText))
            {
                var content = ContentTextBox.Text ?? "";
                var newContent = content.Replace(searchText, replaceText, StringComparison.OrdinalIgnoreCase);
                ContentTextBox.Text = newContent;
            }
        }

        private void CloseFindButton_Click(object sender, RoutedEventArgs e)
        {
            if (FindReplacePanel != null)
            {
                FindReplacePanel.Visibility = Visibility.Collapsed;
            }
        }

        private void InsertGeometricFigure_Click(object sender, RoutedEventArgs e)
        {
            // Simplified geometric figures insertion
            var figureTemplates = new[]
            {
                ("Circle", "○"),
                ("Square", "□"),
                ("Triangle", "△"),
                ("Arrow Right", "→"),
                ("Arrow Left", "←"),
                ("Star", "★"),
                ("Diamond", "◆"),
                ("Heart", "♥")
            };

            var menuFlyout = new MenuFlyout();
            
            foreach (var (name, symbol) in figureTemplates)
            {
                var menuItem = new MenuFlyoutItem
                {
                    Text = $"{symbol} {name}"
                };
                menuItem.Click += (s, args) => InsertText($" {symbol} ");
                menuFlyout.Items.Add(menuItem);
            }

            menuFlyout.ShowAt(sender as FrameworkElement);
        }

        public void EnableDrawingMode(bool enabled)
        {
            // Notify about drawing mode change
            DrawingModeChanged?.Invoke(this, enabled);
            
            if (enabled)
            {
                // Could highlight drawing tools or show drawing-specific UI
                System.Diagnostics.Debug.WriteLine("Drawing mode enabled in RichTextEditor");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Drawing mode disabled in RichTextEditor");
            }
        }

        public void InsertQuickDrawing(string drawingType)
        {
            // Simple placeholder for quick drawings
            var drawing = $"🎨 {drawingType} drawing placeholder";
            InsertText(drawing);
        }
    }
}