using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jot.Models;
using Jot.Services;
using System.Linq;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Jot.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DocumentService _documentService;
        private readonly ChatbotService _chatbotService;
        private readonly PdfExportService _pdfExportService;
        private readonly DrawingService _drawingService;
        
        [ObservableProperty]
        private ObservableCollection<Document> documents = new();

        [ObservableProperty]
        private Document? selectedDocument;

        [ObservableProperty]
        private string searchText = "";

        [ObservableProperty]
        private bool isPaneOpen = true;

        [ObservableProperty]
        private bool isChatbotOpen = false;

        [ObservableProperty]
        private ViewMode currentViewMode = ViewMode.Edit;

        [ObservableProperty]
        private bool isExportingPdf = false;

        [ObservableProperty]
        private bool isDrawingModeEnabled = false;

        [ObservableProperty]
        private DrawingData? currentDrawing;

        [ObservableProperty]
        private string selectedDrawingTool = "FreeDrawing";

        [ObservableProperty]
        private string selectedColor = "#000000";

        [ObservableProperty]
        private double strokeWidth = 2.0;

        private ObservableCollection<Document> _allDocuments = new();

        public ChatbotService ChatbotService => _chatbotService;

        public MainViewModel()
        {
            _documentService = new DocumentService();
            _chatbotService = new ChatbotService(_documentService);
            _pdfExportService = new PdfExportService();
            _drawingService = new DrawingService();
            LoadDocuments();
        }

        [RelayCommand]
        private async Task CreateNewDocument()
        {
            var newDoc = new Document
            {
                Id = Guid.NewGuid(),
                Title = $"New Document {Documents.Count + 1}",
                Content = "# New Document\n\nStart writing here...",
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now
            };

            try
            {
                await _documentService.SaveDocumentAsync(newDoc);
                Documents.Add(newDoc);
                _allDocuments.Add(newDoc);
                SelectedDocument = newDoc;
            }
            catch (Exception ex)
            {
                // Handle error - in a real app you'd show a message to the user
                System.Diagnostics.Debug.WriteLine($"Error creating document: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task SaveCurrentDocument()
        {
            if (SelectedDocument != null)
            {
                try
                {
                    SelectedDocument.ModifiedAt = DateTime.Now;
                    await _documentService.SaveDocumentAsync(SelectedDocument);
                }
                catch (Exception ex)
                {
                    // Handle error
                    System.Diagnostics.Debug.WriteLine($"Error saving document: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task DeleteDocument(Document document)
        {
            if (document != null)
            {
                try
                {
                    await _documentService.DeleteDocumentAsync(document.Id);
                    Documents.Remove(document);
                    _allDocuments.Remove(document);
                    
                    if (SelectedDocument == document)
                    {
                        SelectedDocument = Documents.Count > 0 ? Documents[0] : null;
                    }
                }
                catch (Exception ex)
                {
                    // Handle error
                    System.Diagnostics.Debug.WriteLine($"Error deleting document: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private void ToggleSidebar()
        {
            IsPaneOpen = !IsPaneOpen;
        }

        [RelayCommand]
        private void ToggleChatbot()
        {
            IsChatbotOpen = !IsChatbotOpen;
        }

        [RelayCommand]
        private void SetViewMode(ViewMode mode)
        {
            CurrentViewMode = mode;
        }

        [RelayCommand]
        private void SearchDocuments()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // Show all documents
                Documents.Clear();
                foreach (var doc in _allDocuments)
                {
                    Documents.Add(doc);
                }
            }
            else
            {
                // Filter documents
                var filteredDocs = _allDocuments.Where(d => 
                    d.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    d.Content.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                Documents.Clear();
                foreach (var doc in filteredDocs)
                {
                    Documents.Add(doc);
                }
            }
        }

        [RelayCommand]
        private void OpenDocumentFromChat(string documentTitle)
        {
            var document = _allDocuments.FirstOrDefault(d => 
                d.Title.Equals(documentTitle, StringComparison.OrdinalIgnoreCase));
            
            if (document != null)
            {
                SelectedDocument = document;
                IsChatbotOpen = false; // Close chatbot to focus on document
            }
        }

        public async Task<string> AskChatbotQuestion(string question)
        {
            try
            {
                var response = await _chatbotService.AskQuestionAsync(question, _allDocuments.ToList());
                return response.Answer;
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public List<Document> GetAllDocuments()
        {
            return _allDocuments.ToList();
        }

        private async void LoadDocuments()
        {
            try
            {
                var docs = await _documentService.LoadAllDocumentsAsync();
                Documents.Clear();
                _allDocuments.Clear();
                
                foreach (var doc in docs)
                {
                    Documents.Add(doc);
                    _allDocuments.Add(doc);
                }

                if (Documents.Count > 0)
                {
                    SelectedDocument = Documents[0];
                }
            }
            catch (Exception ex)
            {
                // Handle error
                System.Diagnostics.Debug.WriteLine($"Error loading documents: {ex.Message}");
            }
        }

        partial void OnSelectedDocumentChanged(Document? value)
        {
            // Auto-save previous document when switching
            // This could be implemented with a timer or immediate save
            if (value != null)
            {
                // Trigger property change notifications for UI updates
                OnPropertyChanged(nameof(SelectedDocument));
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            // Automatically search when text changes
            SearchDocuments();
        }

        [RelayCommand]
        private async Task OpenChatbot()
        {
            try
            {
                var chatbotDialog = new Jot.Dialogs.ChatbotDialog(_chatbotService, _allDocuments.ToList());
                if (App.MainWindow?.Content?.XamlRoot != null)
                {
                    chatbotDialog.XamlRoot = App.MainWindow.Content.XamlRoot;
                    await chatbotDialog.ShowAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Cannot show chatbot: XamlRoot is null");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening chatbot: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task ExportCurrentDocumentToPdf()
        {
            if (SelectedDocument == null)
                return;

            try
            {
                IsExportingPdf = true;
                
                // Save current document first to ensure latest changes are included
                await SaveCurrentDocument();
                
                System.Diagnostics.Debug.WriteLine("📸 Iniciando exportación PDF con captura de Preview...");
                
                // Export to PDF usando captura de Preview
                var success = await _pdfExportService.ExportDocumentToPdfAsync(SelectedDocument);
                
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Successfully exported '{SelectedDocument.Title}' to PDF with Preview capture");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ PDF export was cancelled or failed");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error exporting to PDF: {ex.Message}");
            }
            finally
            {
                IsExportingPdf = false;
            }
        }

        [RelayCommand]
        private async Task ExportDocumentToPdf(Document document)
        {
            if (document == null)
                return;

            try
            {
                IsExportingPdf = true;
                
                System.Diagnostics.Debug.WriteLine($"📸 Iniciando exportación PDF con captura para '{document.Title}'...");
                
                // Export the specified document to PDF usando captura de Preview
                var success = await _pdfExportService.ExportDocumentToPdfAsync(document);
                
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Successfully exported '{document.Title}' to PDF with Preview capture");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ PDF export was cancelled or failed");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error exporting to PDF: {ex.Message}");
            }
            finally
            {
                IsExportingPdf = false;
            }
        }

        [RelayCommand]
        private void ApplyTextColor(string colorCode)
        {
            // This command can be used to apply text color formatting
            // The actual implementation is handled in the RichTextEditor control
            System.Diagnostics.Debug.WriteLine($"Text color applied: {colorCode}");
        }

        [RelayCommand]
        private async Task OpenPaintCanvas()
        {
            if (SelectedDocument == null)
                return;

            try
            {
                var dialog = new ContentDialog
                {
                    Title = "🎨 Paint Canvas - Free Drawing",
                    PrimaryButtonText = "Insert Drawing",
                    CloseButtonText = "Cancel",
                    XamlRoot = App.MainWindow?.Content?.XamlRoot
                };

                // Create the drawing interface
                var mainPanel = new StackPanel { Spacing = 10, Margin = new Thickness(10) };
                
                // Title
                mainPanel.Children.Add(new TextBlock
                {
                    Text = "🖌️ Free Drawing Canvas",
                    FontSize = 18,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                // Toolbar
                var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0, 10, 0, 10) };
                
                // Color picker
                toolbar.Children.Add(new TextBlock { Text = "Color:", VerticalAlignment = VerticalAlignment.Center });
                
                var colorButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
                var colors = new[] { 
                    ("Black", Windows.UI.Color.FromArgb(255, 0, 0, 0)),
                    ("Red", Windows.UI.Color.FromArgb(255, 255, 0, 0)),
                    ("Blue", Windows.UI.Color.FromArgb(255, 0, 0, 255)),
                    ("Green", Windows.UI.Color.FromArgb(255, 0, 128, 0)),
                    ("Yellow", Windows.UI.Color.FromArgb(255, 255, 255, 0))
                };

                Button? selectedColorButton = null;
                Windows.UI.Color currentColor = Windows.UI.Color.FromArgb(255, 0, 0, 0);

                foreach (var (name, color) in colors)
                {
                    var colorBtn = new Button
                    {
                        Background = new SolidColorBrush(color),
                        Width = 30,
                        Height = 30,
                        CornerRadius = new CornerRadius(15),
                        BorderThickness = new Thickness(2),
                        BorderBrush = name == "Black" ? new SolidColorBrush(Microsoft.UI.Colors.Gray) : new SolidColorBrush(Microsoft.UI.Colors.Transparent)
                    };
                    
                    if (name == "Black") selectedColorButton = colorBtn;
                    
                    colorBtn.Click += (s, e) => {
                        if (selectedColorButton != null)
                            selectedColorButton.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                        
                        colorBtn.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray);
                        selectedColorButton = colorBtn;
                        currentColor = color;
                    };
                    
                    colorButtons.Children.Add(colorBtn);
                }
                
                toolbar.Children.Add(colorButtons);
                
                // Brush size
                toolbar.Children.Add(new TextBlock { Text = "Size:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(20, 0, 0, 0) });
                var sizeSlider = new Slider { Minimum = 1, Maximum = 10, Value = 3, Width = 100 };
                var sizeText = new TextBlock { Text = "3", VerticalAlignment = VerticalAlignment.Center, MinWidth = 20 };
                sizeSlider.ValueChanged += (s, e) => sizeText.Text = ((int)e.NewValue).ToString();
                toolbar.Children.Add(sizeSlider);
                toolbar.Children.Add(sizeText);
                
                mainPanel.Children.Add(toolbar);

                // Drawing canvas
                var canvasBorder = new Border
                {
                    Background = new SolidColorBrush(Microsoft.UI.Colors.White),
                    BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                    BorderThickness = new Thickness(2),
                    Width = 600,
                    Height = 400,
                    CornerRadius = new CornerRadius(5)
                };

                var drawingCanvas = new Canvas { Background = new SolidColorBrush(Microsoft.UI.Colors.White) };
                canvasBorder.Child = drawingCanvas;
                
                // Drawing logic
                bool isDrawing = false;
                Polyline? currentStroke = null;
                var allStrokes = new List<Polyline>();

                drawingCanvas.PointerPressed += (s, e) => {
                    isDrawing = true;
                    var position = e.GetCurrentPoint(drawingCanvas).Position;
                    
                    currentStroke = new Polyline
                    {
                        Stroke = new SolidColorBrush(currentColor),
                        StrokeThickness = sizeSlider.Value,
                        StrokeLineJoin = PenLineJoin.Round,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round
                    };
                    
                    currentStroke.Points.Add(position);
                    drawingCanvas.Children.Add(currentStroke);
                    drawingCanvas.CapturePointer(e.Pointer);
                };

                drawingCanvas.PointerMoved += (s, e) => {
                    if (isDrawing && currentStroke != null)
                    {
                        var position = e.GetCurrentPoint(drawingCanvas).Position;
                        currentStroke.Points.Add(position);
                    }
                };

                drawingCanvas.PointerReleased += (s, e) => {
                    if (isDrawing && currentStroke != null)
                    {
                        allStrokes.Add(currentStroke);
                        isDrawing = false;
                        drawingCanvas.ReleasePointerCapture(e.Pointer);
                    }
                };

                mainPanel.Children.Add(canvasBorder);

                // Action buttons
                var actionPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
                
                var undoBtn = new Button { Content = "↶ Undo" };
                undoBtn.Click += (s, e) => {
                    if (allStrokes.Count > 0)
                    {
                        var lastStroke = allStrokes[allStrokes.Count - 1];
                        drawingCanvas.Children.Remove(lastStroke);
                        allStrokes.RemoveAt(allStrokes.Count - 1);
                    }
                };
                
                var clearBtn = new Button { Content = "🗑️ Clear" };
                clearBtn.Click += (s, e) => {
                    drawingCanvas.Children.Clear();
                    allStrokes.Clear();
                };
                
                actionPanel.Children.Add(undoBtn);
                actionPanel.Children.Add(clearBtn);
                mainPanel.Children.Add(actionPanel);

                dialog.Content = mainPanel;

                var result = await dialog.ShowAsync();
                
                if (result == ContentDialogResult.Primary && allStrokes.Count > 0)
                {
                    // Create drawing data
                    CurrentDrawing = new DrawingData
                    {
                        Title = $"Paint Drawing {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                        CanvasWidth = 600,
                        CanvasHeight = 400
                    };

                    // Convert strokes to drawing elements
                    foreach (var stroke in allStrokes)
                    {
                        var points = stroke.Points.Select(p => new Models.Point(p.X, p.Y)).ToList();
                        var element = new DrawingElement
                        {
                            Type = DrawingElementType.FreeDrawing,
                            Points = points,
                            Color = ColorToHex(((SolidColorBrush)stroke.Stroke).Color),
                            StrokeWidth = stroke.StrokeThickness
                        };
                        CurrentDrawing.Elements.Add(element);
                    }

                    CurrentDrawing.ModifiedAt = DateTime.Now;
                    await _drawingService.SaveDrawingAsync(CurrentDrawing);
                    
                    // Insert into document
                    var drawingContent = $@"

## 🎨 Paint Drawing - {DateTime.Now:yyyy-MM-dd HH:mm:ss}

![Drawing: {CurrentDrawing.Title}](drawing:{CurrentDrawing.Id})

*Free drawing created with Paint Canvas - {CurrentDrawing.Elements.Count} strokes*

---
";
                    
                    SelectedDocument.Content += drawingContent;
                    SelectedDocument.ModifiedAt = DateTime.Now;
                    await SaveCurrentDocument();
                    
                    System.Diagnostics.Debug.WriteLine($"Paint drawing completed with {CurrentDrawing.Elements.Count} strokes");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening paint canvas: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task OpenMathFormulaDialog()
        {
            if (SelectedDocument == null)
                return;

            try
            {
                var dialog = new ContentDialog
                {
                    Title = "🧮 Mathematical Formula Editor",
                    PrimaryButtonText = "Insert Formula",
                    CloseButtonText = "Cancel",
                    XamlRoot = App.MainWindow?.Content?.XamlRoot
                };

                // Create the formula editor interface
                var mainPanel = new StackPanel { Spacing = 15, Margin = new Thickness(20) };
                
                // Title
                mainPanel.Children.Add(new TextBlock
                {
                    Text = "📐 LaTeX Formula Editor",
                    FontSize = 18,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                // Instructions
                mainPanel.Children.Add(new TextBlock
                {
                    Text = "Enter your mathematical formula using LaTeX syntax. The preview will update as you type.",
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Opacity = 0.8,
                    Margin = new Thickness(0, 0, 0, 10)
                });

                // Formula input
                var inputLabel = new TextBlock
                {
                    Text = "LaTeX Formula:",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                mainPanel.Children.Add(inputLabel);

                var formulaTextBox = new TextBox
                {
                    Height = 100,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                    FontSize = 12,
                    PlaceholderText = "Enter LaTeX formula, e.g.:\n\n$$x = \\frac{-b \\pm \\sqrt{b^2 - 4ac}}{2a}$$\n\n$$\\sum_{i=1}^{n} x_i = x_1 + x_2 + ... + x_n$$\n\n$$\\int_{a}^{b} f(x) dx$$"
                };
                mainPanel.Children.Add(formulaTextBox);

                // Quick templates
                mainPanel.Children.Add(new TextBlock 
                { 
                    Text = "🔧 Quick Templates:", 
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Margin = new Thickness(0, 10, 0, 5)
                });

                var templatesPanel = new StackPanel { Spacing = 8 };
                
                var templates = new[]
                {
                    ("Quadratic Formula", "$$x = \\frac{-b \\pm \\sqrt{b^2 - 4ac}}{2a}$$"),
                    ("Integral", "$$\\int_{a}^{b} f(x) dx = F(b) - F(a)$$"),
                    ("Summation", "$$\\sum_{i=1}^{n} x_i = x_1 + x_2 + \\cdots + x_n$$"),
                    ("Derivative", "$$\\frac{d}{dx}f(x) = \\lim_{h \\to 0} \\frac{f(x+h) - f(x)}{h}$$"),
                    ("Matrix", "$$\\begin{pmatrix} a & b \\\\ c & d \\end{pmatrix}$$"),
                    ("Fraction", "$$\\frac{numerator}{denominator}$$"),
                    ("Square Root", "$$\\sqrt{x^2 + y^2}$$"),
                    ("Greek Letters", "$$\\alpha + \\beta = \\gamma$$"),
                    ("Limits", "$$\\lim_{x \\to \\infty} f(x) = L$$"),
                    ("Probability", "$$P(A \\cap B) = P(A) \\cdot P(B|A)$$")
                };

                var templateGrid = new Grid();
                templateGrid.ColumnDefinitions.Add(new ColumnDefinition());
                templateGrid.ColumnDefinitions.Add(new ColumnDefinition());
                
                for (int i = 0; i < templates.Length; i++)
                {
                    var (name, template) = templates[i];
                    templateGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    
                    var btn = new Button
                    {
                        Content = name,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Margin = new Thickness(2),
                        FontSize = 11
                    };
                    
                    btn.Click += (s, e) => {
                        formulaTextBox.Text = template;
                        formulaTextBox.Focus(FocusState.Programmatic);
                    };
                    
                    Grid.SetRow(btn, i / 2);
                    Grid.SetColumn(btn, i % 2);
                    templateGrid.Children.Add(btn);
                }

                templatesPanel.Children.Add(templateGrid);
                mainPanel.Children.Add(templatesPanel);

                // LaTeX reference
                var referenceExpander = new Expander
                {
                    Header = "📚 LaTeX Reference",
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                var referenceContent = new TextBlock
                {
                    Text = @"Common LaTeX Commands:

• Greek Letters: \alpha, \beta, \gamma, \pi, \theta, \omega
• Operators: \pm (±), \times (×), \div (÷), \cdot (·)
• Relations: \leq (≤), \geq (≥), \neq (≠), \approx (≈)
• Functions: \sin, \cos, \tan, \log, \ln, \exp
• Calculus: \int (integral), \sum (summation), \partial (∂)
• Arrows: \rightarrow (→), \Rightarrow (⇒)
• Set Theory: \in (∈), \subset (⊂), \cup (∪), \cap (∩)
• Roots: \sqrt{x} (√x), \sqrt[3]{x} (∛x)
• Fractions: \frac{num}{den}
• Superscript: x^2, Subscript: x_1
• Brackets: Use { } for grouping",
                    FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                    FontSize = 10,
                    TextWrapping = TextWrapping.Wrap,
                    IsTextSelectionEnabled = true
                };

                referenceExpander.Content = referenceContent;
                mainPanel.Children.Add(referenceExpander);

                dialog.Content = mainPanel;
                var result = await dialog.ShowAsync();
                
                if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(formulaTextBox.Text))
                {
                    var formulaContent = formulaTextBox.Text.Trim();
                    
                    // Ensure formula is wrapped in $$ if not already
                    if (!formulaContent.StartsWith("$$"))
                    {
                        formulaContent = "$$" + formulaContent + "$$";
                    }
                    
                    var insertContent = $"\n\n{formulaContent}\n\n";
                    
                    SelectedDocument.Content += insertContent;
                    SelectedDocument.ModifiedAt = DateTime.Now;
                    await SaveCurrentDocument();
                    
                    System.Diagnostics.Debug.WriteLine($"Mathematical formula inserted: {formulaContent}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening math formula dialog: {ex.Message}");
            }
        }

        private string ColorToHex(Windows.UI.Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        public DrawingElement CreateDrawingElement(DrawingElementType type, List<Models.Point> points)
        {
            return new DrawingElement
            {
                Type = type,
                Points = points,
                Color = SelectedColor,
                StrokeWidth = StrokeWidth
            };
        }
    }
}