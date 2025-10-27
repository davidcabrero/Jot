using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Jot.ViewModels;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Jot
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }
        private bool _isUpdatingContent = false;
        private ObservableCollection<DocumentHeading> _documentHeadings = new();

        public MainWindow()
        {
            this.InitializeComponent();
            
            // Set up data context
            ViewModel = new MainViewModel();
            
            // Initialize view mode buttons
            SetupViewModeButtons();
            
            // Set window properties
            Title = "Jot - Modern Note Taking";
            
            // Set minimum window size
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1000, 700));
            
            // Initialize the loading overlay to be hidden initially
            LoadingOverlay.Visibility = Visibility.Collapsed;
            
            // Setup bindings after initialization
            SetupBindings();
        }

        private void SetupViewModeButtons()
        {
            // Setup view mode button events
            EditModeButton.Click += (s, e) => ViewModel.SetViewModeCommand.Execute(ViewMode.Edit);
            PreviewModeButton.Click += (s, e) => ViewModel.SetViewModeCommand.Execute(ViewMode.Preview);
            SplitModeButton.Click += (s, e) => ViewModel.SetViewModeCommand.Execute(ViewMode.Split);
        }

        private void SetupBindings()
        {
            // Setup simple bindings
            DocumentsList.ItemsSource = ViewModel.Documents;
            SearchBox.Text = ViewModel.SearchText;
            DocumentIndexList.ItemsSource = _documentHeadings;
            
            // Bind document list selection
            DocumentsList.SelectionChanged += DocumentsList_SelectionChanged;
            
            // Setup event handlers for buttons
            TogglePaneButton.Click += (s, e) => ViewModel.ToggleSidebarCommand.Execute(null);
            
            // Setup text binding for text editors
            TextEditor.TextChanged += TextEditor_TextChanged;
            TextEditor.DrawingModeChanged += TextEditor_DrawingModeChanged;
            TextEditor.RequestFreeDrawing += TextEditor_RequestFreeDrawing;
            
            // Setup search box
            SearchBox.TextChanged += SearchBox_TextChanged;
            
            // Setup view mode changes
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            
            // Setup initial state
            UpdateViewMode();
            UpdateSelectedDocument();
            UpdateDocumentIndex();
        }

        private void CreateNewDocument_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CreateNewDocumentCommand.Execute(null);
        }

        private void SaveDocument_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SaveCurrentDocumentCommand.Execute(null);
        }

        private void DeleteDocument_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && menuItem.CommandParameter is Models.Document document)
            {
                ViewModel.DeleteDocumentCommand.Execute(document);
            }
        }

        private void ToggleChatbot_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenChatbotCommand.Execute(null);
        }

        private void ExportToPdf_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ExportCurrentDocumentToPdfCommand.Execute(null);
        }

        private void ExportDocumentToPdf_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && menuItem.CommandParameter is Models.Document document)
            {
                ViewModel.ExportDocumentToPdfCommand.Execute(document);
            }
        }

        private void ToggleDrawingMode_Click(object sender, RoutedEventArgs e)
        {
            // Open the real drawing canvas
            OpenFreeDrawingCanvas();
        }

        private void OpenFreeDrawingCanvas()
        {
            // Use the enhanced ASCII drawing dialog as the main drawing interface
            ShowSimpleDrawingDialog();
        }

        private void ShowFreeDrawingCanvas(object panel)
        {
            // Not needed for ASCII implementation
        }

        private void CloseFreeDrawingCanvas()
        {
            // Not needed for ASCII implementation
        }

        private async void ShowSimpleDrawingDialog()
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = "🎨 Rotulador Libre - Dibujo ASCII",
                    PrimaryButtonText = "💾 Guardar Dibujo",
                    SecondaryButtonText = "🗑️ Limpiar",
                    CloseButtonText = "❌ Cancelar",
                    XamlRoot = this.Content.XamlRoot
                };

                var mainPanel = new StackPanel { Spacing = 16, Margin = new Thickness(20, 20, 20, 20) };
                
                // Title and instructions
                mainPanel.Children.Add(new TextBlock
                {
                    Text = "🖌️ Canvas de Dibujo Libre",
                    FontSize = 20,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                mainPanel.Children.Add(new TextBlock
                {
                    Text = "Usa caracteres ASCII para crear dibujos libremente, como si fuera un rotulador digital.",
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 12)
                });

                // Drawing area with better styling
                var drawingTextBox = new TextBox
                {
                    Height = 380,
                    AcceptsReturn = true,
                    FontFamily = new FontFamily("Cascadia Code, Consolas, monospace"),
                    FontSize = 14,
                    PlaceholderText = "🎨 Dibuja aquí usando el teclado como rotulador...\n\n" +
                                     "Tips para dibujar:\n" +
                                     "• Usa ─ │ ┌ ┐ └ ┘ para marcos\n" +
                                     "• Usa ○ ● □ ■ △ ▲ para formas\n" +
                                     "• Usa → ← ↑ ↓ para flechas\n" +
                                     "• Usa ★ ♥ ♦ ♣ para decoraciones\n\n" +
                                     "Ejemplo:\n" +
                                     "   ★ Mi Dibujo ★\n" +
                                     "  ╔═══════════╗\n" +
                                     "  ║     ♥     ║\n" +
                                     "  ║  ○ → □    ║\n" +
                                     "  ╚═══════════╝",
                    Background = new SolidColorBrush(Microsoft.UI.Colors.White),
                    BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                    BorderThickness = new Thickness(2)
                };
                
                ScrollViewer.SetVerticalScrollBarVisibility(drawingTextBox, ScrollBarVisibility.Auto);
                ScrollViewer.SetHorizontalScrollBarVisibility(drawingTextBox, ScrollBarVisibility.Auto);

                mainPanel.Children.Add(drawingTextBox);

                // Enhanced tool palette
                mainPanel.Children.Add(new TextBlock 
                { 
                    Text = "🛠️ Paleta de Herramientas:", 
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Margin = new Thickness(0, 8, 0, 4)
                });

                // Create a comprehensive tool grid
                var toolsGrid = new Grid();
                toolsGrid.ColumnDefinitions.Add(new ColumnDefinition());
                toolsGrid.ColumnDefinitions.Add(new ColumnDefinition());
                toolsGrid.ColumnDefinitions.Add(new ColumnDefinition());
                toolsGrid.ColumnDefinitions.Add(new ColumnDefinition());

                var toolCategories = new[]
                {
                    ("Líneas", new[] { "─", "│", "┌", "┐", "└", "┘", "├", "┤", "┬", "┴", "┼" }),
                    ("Formas", new[] { "○", "●", "□", "■", "△", "▲", "◇", "♦", "◯", "◉", "▢" }),
                    ("Flechas", new[] { "→", "←", "↑", "↓", "↗", "↘", "↙", "↖", "▶", "◀", "▲" }),
                    ("Decoración", new[] { "★", "☆", "♥", "♦", "♣", "♠", "※", "⚡", "✦", "❀", "⭐" })
                };

                for (int col = 0; col < toolCategories.Length; col++)
                {
                    var (categoryName, symbols) = toolCategories[col];
                    
                    var categoryPanel = new StackPanel { Spacing = 4, Margin = new Thickness(4) };
                    
                    categoryPanel.Children.Add(new TextBlock 
                    { 
                        Text = categoryName, 
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Center
                    });

                    var symbolsPanel = new StackPanel { Orientation = Orientation.Horizontal };
                    foreach (var symbol in symbols)
                    {
                        var btn = new Button
                        {
                            Content = symbol,
                            Width = 32,
                            Height = 32,
                            FontFamily = new FontFamily("Segoe UI Symbol, Consolas"),
                            FontSize = 16,
                            Margin = new Thickness(1),
                            Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray)
                        };
                        
                        ToolTipService.SetToolTip(btn, $"Insertar: {symbol}");
                        
                        btn.Click += (s, e) => {
                            var pos = drawingTextBox.SelectionStart;
                            drawingTextBox.Text = drawingTextBox.Text.Insert(pos, symbol);
                            drawingTextBox.SelectionStart = pos + symbol.Length;
                            drawingTextBox.Focus(FocusState.Programmatic);
                        };
                        
                        symbolsPanel.Children.Add(btn);
                    }
                    
                    categoryPanel.Children.Add(symbolsPanel);
                    Grid.SetColumn(categoryPanel, col);
                    toolsGrid.Children.Add(categoryPanel);
                }

                mainPanel.Children.Add(toolsGrid);

                // Quick templates
                mainPanel.Children.Add(new TextBlock 
                { 
                    Text = "📋 Plantillas Rápidas:", 
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Margin = new Thickness(0, 8, 0, 4)
                });

                var templatesPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                
                var templates = new[]
                {
                    ("Marco Simple", "┌─────────────┐\n│             │\n│             │\n└─────────────┘"),
                    ("Marco Doble", "╔═════════════╗\n║             ║\n║             ║\n╚═════════════╝"),
                    ("Flecha Simple", "     ↑\n     │\n○ ──→ □ ──→ △\n     │\n     ▼"),
                    ("Diagrama Básico", "  [INICIO]\n      │\n      ▼\n  ┌───────┐\n  │PROCESO│\n  └───────┘\n      │\n      ▼\n   [FIN]")
                };

                foreach (var (name, template) in templates)
                {
                    var btn = new Button
                    {
                        Content = name,
                        FontSize = 11,
                        Padding = new Thickness(8, 4, 8, 4)
                    };
                    
                    btn.Click += (s, e) => {
                        var pos = drawingTextBox.SelectionStart;
                        var insertion = $"\n{template}\n";
                        drawingTextBox.Text = drawingTextBox.Text.Insert(pos, insertion);
                        drawingTextBox.SelectionStart = pos + insertion.Length;
                        drawingTextBox.Focus(FocusState.Programmatic);
                    };
                    
                    templatesPanel.Children.Add(btn);
                }

                mainPanel.Children.Add(templatesPanel);

                dialog.Content = mainPanel;

                var result = await dialog.ShowAsync();
                
                if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(drawingTextBox.Text))
                {
                    var drawingContent = $@"
## 🎨 Dibujo Libre con Rotulador - {DateTime.Now:HH:mm:ss}

```ascii-art
{drawingTextBox.Text}
```

*Creado con rotulador ASCII libre*
";
                    
                    if (ViewModel.SelectedDocument != null)
                    {
                        ViewModel.SelectedDocument.Content += drawingContent;
                        ViewModel.SelectedDocument.ModifiedAt = DateTime.Now;
                        
                        // Update UI
                        UpdateDocumentContent();
                        UpdatePreviewContent();
                    }
                }
                else if (result == ContentDialogResult.Secondary)
                {
                    // Clear and restart dialog  
                    drawingTextBox.Text = "";
                    // Don't call ShowSimpleDrawingDialog again to avoid infinite recursion
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing drawing dialog: {ex.Message}");
            }
        }

        private void TextEditor_DrawingModeChanged(object? sender, bool isDrawingMode)
        {
            // Just log for now since we removed the ToggleDrawingModeCommand
            System.Diagnostics.Debug.WriteLine($"Drawing mode changed: {isDrawingMode}");
        }

        private void TextEditor_RequestFreeDrawing(object? sender, EventArgs e)
        {
            // Open free drawing canvas when requested from the text editor
            OpenFreeDrawingCanvas();
        }

        private async void ShowDrawingModeInstructions()
        {
            try
            {
                var instructionsDialog = new ContentDialog
                {
                    Title = "🎨 Modo de Dibujo Activado",
                    Content = CreateDrawingInstructionsContent(),
                    CloseButtonText = "Entendido",
                    XamlRoot = this.Content.XamlRoot
                };

                await instructionsDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing drawing instructions: {ex.Message}");
            }
        }

        private StackPanel CreateDrawingInstructionsContent()
        {
            var panel = new StackPanel { Spacing = 12, MaxWidth = 500 };

            panel.Children.Add(new TextBlock
            {
                Text = "El modo de dibujo está ahora activo. Puedes crear diagramas y esquemas usando:",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14
            });

            panel.Children.Add(new TextBlock
            {
                Text = "🖌️ Botón de Dibujo en la barra de herramientas",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });

            panel.Children.Add(new TextBlock
            {
                Text = "📊 Botón de Esquemas para plantillas predefinidas",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });

            panel.Children.Add(new TextBlock
            {
                Text = "🔢 Botón de Figuras Geométricas para formas rápidas",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });

            panel.Children.Add(new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(30, 0, 120, 215)),
                Padding = new Thickness(12),
                CornerRadius = new CornerRadius(4),
                Child = new TextBlock
                {
                    Text = "💡 Consejo: Cambia a modo Preview o Split para ver tus diagramas renderizados en tiempo real.",
                    TextWrapping = TextWrapping.Wrap,
                    FontStyle = Windows.UI.Text.FontStyle.Italic
                }
            });

            return panel;
        }
        
        private void DocumentsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DocumentsList.SelectedItem is Models.Document selectedDoc)
            {
                ViewModel.SelectedDocument = selectedDoc;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.SearchText = SearchBox.Text;
            ViewModel.SearchDocumentsCommand.Execute(null);
        }

        private void TextEditor_TextChanged(object? sender, string newText)
        {
            if (!_isUpdatingContent && ViewModel.SelectedDocument != null)
            {
                ViewModel.SelectedDocument.Content = newText;
                ViewModel.SelectedDocument.ModifiedAt = DateTime.Now;
                
                // Update the split mode editor and preview
                UpdateSplitModeContent();
                UpdatePreviewContent();
                
                // Update document index immediately when content changes
                UpdateDocumentIndex();
                
                // Auto-save after a short delay (could be implemented with a timer)
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ViewModel.CurrentViewMode):
                    UpdateViewMode();
                    break;
                case nameof(ViewModel.SelectedDocument):
                    UpdateSelectedDocument();
                    UpdateDocumentContent();
                    UpdateDocumentIndex();
                    break;
                case nameof(ViewModel.IsPaneOpen):
                    MainNavigationView.IsPaneOpen = ViewModel.IsPaneOpen;
                    break;
                case nameof(ViewModel.IsExportingPdf):
                    UpdateLoadingOverlayVisibility();
                    break;
            }
        }

        private void UpdateLoadingOverlayVisibility()
        {
            if (LoadingOverlay != null)
            {
                LoadingOverlay.Visibility = ViewModel.IsExportingPdf ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdateDocumentContent()
        {
            _isUpdatingContent = true;
            
            if (ViewModel.SelectedDocument != null)
            {
                // Update title
                if (TitleTextBox != null)
                {
                    TitleTextBox.Text = ViewModel.SelectedDocument.Title ?? "";
                }
                
                // Update content in all editors
                TextEditor.Text = ViewModel.SelectedDocument.Content ?? "";
                
                // Update split mode editor specifically
                if (SplitModeEditor != null)
                {
                    SplitModeEditor.Text = ViewModel.SelectedDocument.Content ?? "";
                    // Remove previous event handler to avoid double subscription
                    SplitModeEditor.TextChanged -= SplitEditor_TextChanged;
                    SplitModeEditor.DrawingModeChanged -= TextEditor_DrawingModeChanged;
                    SplitModeEditor.RequestFreeDrawing -= TextEditor_RequestFreeDrawing;
                    
                    // Add event handlers
                    SplitModeEditor.TextChanged += SplitEditor_TextChanged;
                    SplitModeEditor.DrawingModeChanged += TextEditor_DrawingModeChanged;
                    SplitModeEditor.RequestFreeDrawing += TextEditor_RequestFreeDrawing;
                }
                
                // Update previews
                UpdatePreviewContent();
            }
            else
            {
                // Clear content when no document is selected
                if (TitleTextBox != null)
                {
                    TitleTextBox.Text = "";
                }
                TextEditor.Text = "";
                if (SplitModeEditor != null)
                {
                    SplitModeEditor.Text = "";
                }
                UpdatePreviewContent();
            }
            
            _isUpdatingContent = false;
        }

        private void SplitEditor_TextChanged(object? sender, string newText)
        {
            if (!_isUpdatingContent && ViewModel.SelectedDocument != null)
            {
                ViewModel.SelectedDocument.Content = newText;
                ViewModel.SelectedDocument.ModifiedAt = DateTime.Now;
                
                // Update the main editor and preview
                _isUpdatingContent = true;
                TextEditor.Text = newText;
                _isUpdatingContent = false;
                
                UpdatePreviewContent();
                UpdateDocumentIndex();
            }
        }

        private void UpdatePreviewContent()
        {
            var content = ViewModel.SelectedDocument?.Content ?? "";
            
            // Update main preview
            MarkdownPreview.MarkdownText = content;
            
            // Update split mode preview specifically
            if (SplitModePreview != null)
            {
                SplitModePreview.MarkdownText = content;
            }
            
            System.Diagnostics.Debug.WriteLine($"Updated preview content: {content.Length} characters");
        }

        private void UpdateSplitModeContent()
        {
            if (SplitModePreview != null && ViewModel.SelectedDocument != null)
            {
                SplitModePreview.MarkdownText = ViewModel.SelectedDocument.Content ?? "";
            }
        }

        private (Controls.RichTextEditor? editor, Controls.MarkdownPreview? preview) GetSplitModeEditors()
        {
            return (SplitModeEditor, SplitModePreview);
        }

        private void UpdateViewMode()
        {
            // Reset all toggle buttons
            EditModeButton.IsChecked = false;
            PreviewModeButton.IsChecked = false;
            SplitModeButton.IsChecked = false;
            
            // Hide all content panels
            TextEditor.Visibility = Visibility.Collapsed;
            MarkdownPreview.Visibility = Visibility.Collapsed;
            SplitModeGrid.Visibility = Visibility.Collapsed;
            
            // Show appropriate content based on view mode
            switch (ViewModel.CurrentViewMode)
            {
                case ViewMode.Edit:
                    EditModeButton.IsChecked = true;
                    TextEditor.Visibility = Visibility.Visible;
                    break;
                case ViewMode.Preview:
                    PreviewModeButton.IsChecked = true;
                    MarkdownPreview.Visibility = Visibility.Visible;
                    UpdatePreviewContent();
                    break;
                case ViewMode.Split:
                    SplitModeButton.IsChecked = true;
                    SplitModeGrid.Visibility = Visibility.Visible;
                    UpdateSplitModeContent();
                    break;
            }
        }

        private void UpdateSelectedDocument()
        {
            bool hasDocument = ViewModel.SelectedDocument != null;
            
            // Update visibility of empty state vs document editor
            EmptyStateGrid.Visibility = hasDocument ? Visibility.Collapsed : Visibility.Visible;
            
            // Update the document header visibility
            if (DocumentHeaderBorder != null)
            {
                DocumentHeaderBorder.Visibility = hasDocument ? Visibility.Visible : Visibility.Collapsed;
            }
            
            // Update the editor container visibility
            if (EditorContainer != null)
            {
                EditorContainer.Visibility = hasDocument ? Visibility.Visible : Visibility.Collapsed;
            }
            
            // Update the document list selection
            if (hasDocument && DocumentsList.SelectedItem != ViewModel.SelectedDocument)
            {
                DocumentsList.SelectedItem = ViewModel.SelectedDocument;
            }
        }

        private void UpdateDocumentIndex()
        {
            try
            {
                _documentHeadings.Clear();
                
                if (ViewModel.SelectedDocument == null)
                    return;

                var content = ViewModel.SelectedDocument.Content ?? "";
                var headings = ExtractHeadings(content);
                
                foreach (var heading in headings)
                {
                    _documentHeadings.Add(heading);
                }
                
                System.Diagnostics.Debug.WriteLine($"Document index updated with {headings.Count} headings");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating document index: {ex.Message}");
            }
        }

        private List<DocumentHeading> ExtractHeadings(string content)
        {
            var headings = new List<DocumentHeading>();
            
            if (string.IsNullOrEmpty(content))
                return headings;
                
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            int lineNumber = 0;

            foreach (var line in lines)
            {
                lineNumber++;
                var trimmedLine = line.Trim();
                
                // Match headers using regex - more precise than simple StartsWith
                var headerMatch = Regex.Match(trimmedLine, @"^(#{1,6})\s+(.+)$");
                if (headerMatch.Success)
                {
                    var level = headerMatch.Groups[1].Value.Length;
                    var title = headerMatch.Groups[2].Value.Trim();
                    
                    if (!string.IsNullOrEmpty(title))
                    {
                        headings.Add(new DocumentHeading
                        {
                            Title = title,
                            Level = level,
                            LineNumber = lineNumber
                        });
                        
                        System.Diagnostics.Debug.WriteLine($"Found heading: Level {level}, Title: '{title}', Line: {lineNumber}");
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"Extracted {headings.Count} headings from content");
            return headings;
        }

        private void TitleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isUpdatingContent && ViewModel.SelectedDocument != null)
            {
                ViewModel.SelectedDocument.Title = ((TextBox)sender).Text;
                ViewModel.SelectedDocument.ModifiedAt = DateTime.Now;
            }
        }

        private void DocumentIndex_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DocumentIndexList.SelectedItem is DocumentHeading heading)
            {
                // Scroll to the heading in the text editor
                ScrollToLineInEditor(heading.LineNumber);
            }
        }

        private void ScrollToLineInEditor(int lineNumber)
        {
            // This is a simplified implementation
            // In a real app, you would calculate the actual position and scroll to it
            TextEditor.Focus(FocusState.Programmatic);
            
            // Try to calculate approximate position and scroll
            try
            {
                var content = ViewModel.SelectedDocument?.Content ?? "";
                var lines = content.Split('\n');
                if (lineNumber > 0 && lineNumber <= lines.Length)
                {
                    // Calculate character position
                    int charPosition = 0;
                    for (int i = 0; i < lineNumber - 1 && i < lines.Length; i++)
                    {
                        charPosition += lines[i].Length + 1; // +1 for newline
                    }
                    
                    // Set cursor position (this is a basic implementation)
                    // In a real implementation, you'd want to scroll the view as well
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error scrolling to line: {ex.Message}");
            }
        }
    }

    public class DocumentHeading
    {
        public string Title { get; set; } = "";
        public int Level { get; set; }
        public int LineNumber { get; set; }
        public string IndentedTitle => new string(' ', (Level - 1) * 2) + Title;
    }
}
