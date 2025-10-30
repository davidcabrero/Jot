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
using Microsoft.UI.Dispatching;
using System.Text;

namespace Jot.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DocumentService _documentService;
        private readonly ChatbotService _chatbotService;
        private readonly HtmlExportService _htmlExportService;
        private readonly DrawingService _drawingService;
        private readonly PythonExecutionService _pythonService;
        private readonly GitHubUploadService _gitHubService;
  
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
        private bool isExportingHtml = false;

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

     [ObservableProperty]
    private bool isGitHubConnected = false;

      private ObservableCollection<Document> _allDocuments = new();

     public ChatbotService ChatbotService => _chatbotService;
        public PythonExecutionService PythonService => _pythonService;
        public GitHubUploadService GitHubService => _gitHubService;

 public MainViewModel()
        {
   _documentService = new DocumentService();
 _chatbotService = new ChatbotService(_documentService);
      _htmlExportService = new HtmlExportService();
    _drawingService = new DrawingService();
       _pythonService = new PythonExecutionService();
        _gitHubService = new GitHubUploadService();
     
            // Initialize Python interpreters
       _ = Task.Run(async () => await _pythonService.DiscoverPythonInterpretersAsync());
  
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
  private async Task ExportCurrentDocumentToHtml()
        {
  if (SelectedDocument == null)
         return;

        try
 {
          IsExportingHtml = true;
          
         // Save current document first to ensure latest changes are included
   await SaveCurrentDocument();
      
     System.Diagnostics.Debug.WriteLine("🌐 Starting HTML export with preview styling...");
     
           // Export to HTML that matches preview exactly
 var success = await _htmlExportService.ExportDocumentToHtmlAsync(SelectedDocument);
          
     if (success)
           {
   System.Diagnostics.Debug.WriteLine($"✅ Successfully exported '{SelectedDocument.Title}' to HTML");
         }
     else
            {
 System.Diagnostics.Debug.WriteLine("❌ HTML export was cancelled or failed");
            }
}
            catch (Exception ex)
     {
System.Diagnostics.Debug.WriteLine($"❌ Error exporting to HTML: {ex.Message}");
        }
            finally
      {
    IsExportingHtml = false;
  }
        }

        [RelayCommand]
        private async Task ExportDocumentToHtml(Document document)
        {
            if (document == null)
      return;

   try
      {
        IsExportingHtml = true;
     
                System.Diagnostics.Debug.WriteLine($"🌐 Starting HTML export for '{document.Title}'...");
            
      // Export the specified document to HTML
         var success = await _htmlExportService.ExportDocumentToHtmlAsync(document);
         
  if (success)
    {
     System.Diagnostics.Debug.WriteLine($"✅ Successfully exported '{document.Title}' to HTML");
                }
      else
     {
   System.Diagnostics.Debug.WriteLine("❌ HTML export was cancelled or failed");
                }
            }
      catch (Exception ex)
       {
          System.Diagnostics.Debug.WriteLine($"❌ Error exporting to HTML: {ex.Message}");
  }
            finally
    {
     IsExportingHtml = false;
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

        [RelayCommand]
        private async Task OpenPythonDialog()
   {
   if (SelectedDocument == null)
  return;

 try
         {
    var dialog = new ContentDialog
     {
    Title = "🐍 Python Code Execution",
   PrimaryButtonText = "Add Cell to Document",
          CloseButtonText = "Cancel",
       XamlRoot = App.MainWindow?.Content?.XamlRoot,
      DefaultButton = ContentDialogButton.Primary
     };

   var mainPanel = new StackPanel { Spacing = 16, Width = 700 };

          // Header with interpreter selection
  var headerPanel = new StackPanel { Spacing = 12 };
   
                headerPanel.Children.Add(new TextBlock
              {
        Text = "Python Code Cell Editor",
       FontSize = 18,
   FontWeight = Microsoft.UI.Text.FontWeights.Bold,
     HorizontalAlignment = HorizontalAlignment.Center
                });

            // Interpreter Selection Section
   var interpreterSection = new StackPanel { Spacing = 8 };
           
     interpreterSection.Children.Add(new TextBlock
 {
              Text = "🔧 Select Python Interpreter:",
        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
       });

    var interpreterComboBox = new ComboBox
          {
         PlaceholderText = "🔍 Detecting Python interpreters...",
         HorizontalAlignment = HorizontalAlignment.Stretch,
         MinWidth = 400
      };

      var refreshButton = new Button
        {
          Content = "🔄 Refresh Interpreters",
         HorizontalAlignment = HorizontalAlignment.Left,
          Margin = new Thickness(0, 4, 0, 0)
    };

       interpreterSection.Children.Add(interpreterComboBox);
      interpreterSection.Children.Add(refreshButton);
         headerPanel.Children.Add(interpreterSection);
    mainPanel.Children.Add(headerPanel);

       // Code Editor Section
        var codeSection = new StackPanel { Spacing = 8 };
  
    codeSection.Children.Add(new TextBlock
      {
    Text = "💻 Python Code:",
      FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
             });

       var codeEditor = new TextBox
        {
        AcceptsReturn = true,
     Height = 200,
   TextWrapping = TextWrapping.NoWrap,
         FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Cascadia Code, Consolas, Courier New, monospace"),
          FontSize = 14,
   PlaceholderText = "# Write your Python code here\nprint('Hello from Jot!')\n\n# Example: Simple calculation\nimport math\nresult = math.sqrt(16)\nprint(f'Square root of 16 = {result}')\n\n# Example: Data processing\nnumbers = [1, 2, 3, 4, 5]\nsum_numbers = sum(numbers)\nprint(f'Sum: {sum_numbers}')"
        };

                // Set scroll properties after creation
    codeEditor.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
   codeEditor.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);

                codeSection.Children.Add(codeEditor);
     mainPanel.Children.Add(codeSection);

      // Execute Section
        var executeSection = new StackPanel { Spacing = 8 };
    
            var executeButton = new Button
     {
  Content = "▶️ Execute Code",
           Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 76, 175, 80)),
      Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
     HorizontalAlignment = HorizontalAlignment.Left,
 Padding = new Thickness(16, 8, 16, 8),
         FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
          };

     executeSection.Children.Add(executeButton);
          mainPanel.Children.Add(executeSection);

                // Output Section
    var outputSection = new StackPanel { Spacing = 8, Visibility = Visibility.Collapsed };
                
     outputSection.Children.Add(new TextBlock
      {
     Text = "📤 Output:",
        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
           });

    var outputTextBox = new TextBox
                {
   IsReadOnly = true,
    Height = 150,
             TextWrapping = TextWrapping.Wrap,
   FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Cascadia Code, Consolas, Courier New, monospace"),
         FontSize = 12,
    Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 248, 249, 250))
     };

     // Set scroll properties after creation
  outputTextBox.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);

     outputSection.Children.Add(outputTextBox);
   mainPanel.Children.Add(outputSection);

   // Status Section
           var statusTextBlock = new TextBlock
      {
     Visibility = Visibility.Collapsed,
         TextWrapping = TextWrapping.Wrap,
               FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
              mainPanel.Children.Add(statusTextBlock);

        dialog.Content = mainPanel;

        // Variables for execution
      string lastExecutedCode = "";
      string lastOutput = "";
   List<string> lastPlotFiles = new();
 bool hasExecuted = false;

       // Load Python interpreters
       async Task LoadInterpreters()
        {
                try
            {
            interpreterComboBox.PlaceholderText = "🔍 Searching for Python interpreters...";
       var interpreters = await _pythonService.DiscoverPythonInterpretersAsync();
        
               interpreterComboBox.ItemsSource = interpreters;
         interpreterComboBox.DisplayMemberPath = "Name";
      
             if (interpreters.Count > 0)
     {
     interpreterComboBox.SelectedIndex = 0;
      _pythonService.SetSelectedInterpreter(interpreters[0]);
    interpreterComboBox.PlaceholderText = "";
     
    statusTextBlock.Text = $"✅ Found {interpreters.Count} Python interpreter(s)";
       statusTextBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green);
         statusTextBlock.Visibility = Visibility.Visible;
 }
           else
  {
                interpreterComboBox.PlaceholderText = "❌ No Python interpreters found";
    statusTextBlock.Text = "❌ No Python interpreters found. Please install Python and try again.";
           statusTextBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
      statusTextBlock.Visibility = Visibility.Visible;
     }
           }
     catch (Exception ex)
   {
              interpreterComboBox.PlaceholderText = "❌ Error detecting interpreters";
            statusTextBlock.Text = $"❌ Error: {ex.Message}";
         statusTextBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
             statusTextBlock.Visibility = Visibility.Visible;
  }
     }

           // Initial load
       await LoadInterpreters();

         // Event handlers
     interpreterComboBox.SelectionChanged += (s, e) =>
       {
 if (interpreterComboBox.SelectedItem is PythonInterpreter interpreter)
  {
                _pythonService.SetSelectedInterpreter(interpreter);
    statusTextBlock.Text = $"🐍 Selected: {interpreter.Name}";
  statusTextBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Blue);
    statusTextBlock.Visibility = Visibility.Visible;
  }
       };

        refreshButton.Click += async (s, e) =>
    {
  refreshButton.IsEnabled = false;
          refreshButton.Content = "🔄 Refreshing...";
     
 await LoadInterpreters();
        
             refreshButton.Content = "🔄 Refresh Interpreters";
       refreshButton.IsEnabled = true;
  };

    executeButton.Click += async (s, e) =>
      {
        if (string.IsNullOrWhiteSpace(codeEditor.Text))
     {
   statusTextBlock.Text = "⚠️ Please enter some Python code first";
        statusTextBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Orange);
           statusTextBlock.Visibility = Visibility.Visible;
  return;
          }

         if (_pythonService.SelectedInterpreter == null)
          {
    statusTextBlock.Text = "⚠️ Please select a Python interpreter first";
        statusTextBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Orange);
    statusTextBlock.Visibility = Visibility.Visible;
    return;
   }

   try
    {
          executeButton.IsEnabled = false;
   executeButton.Content = "🔄 Executing...";
     
    statusTextBlock.Text = "🚀 Executing Python code...";
       statusTextBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Blue);
   statusTextBlock.Visibility = Visibility.Visible;
     
        outputSection.Visibility = Visibility.Visible;
          outputTextBox.Text = "Executing...";

           // Execute the code
           var result = await _pythonService.ExecuteCodeAsync(codeEditor.Text);
       
  if (result.Success)
        {
       var output = string.IsNullOrWhiteSpace(result.Output) ? "(No output)" : result.Output;
       
     if (!string.IsNullOrWhiteSpace(result.Error))
      {
     output += "\n\n--- Warnings/Errors ---\n" + result.Error;
     }
   
       outputTextBox.Text = output;
       lastExecutedCode = codeEditor.Text;
       lastOutput = output;
     lastPlotFiles = result.PlotFiles;
     hasExecuted = true;
   
    var plotInfo = result.PlotFiles.Count > 0 ? $" | {result.PlotFiles.Count} plot(s) generated" : "";
  statusTextBlock.Text = $"✅ Executed successfully in {result.ExecutionTime.TotalMilliseconds:F0}ms{plotInfo}";
   statusTextBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green);
 
       dialog.IsPrimaryButtonEnabled = true;
            }
  else
        {
  outputTextBox.Text = "❌ Execution Failed:\n" + result.Error;
   
  statusTextBlock.Text = "❌ Execution failed";
statusTextBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
         }
    }
catch (Exception ex)
   {
     outputTextBox.Text = $"❌ Error: {ex.Message}";
 statusTextBlock.Text = $"❌ Error: {ex.Message}";
 statusTextBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
   }
         finally
  {
     executeButton.IsEnabled = true;
    executeButton.Content = "▶️ Execute Code";
     }
      };

     // Initially disable primary button until code is executed
         dialog.IsPrimaryButtonEnabled = false;

  var result = await dialog.ShowAsync();

     if (result == ContentDialogResult.Primary && hasExecuted)
    {
   // Create a Python cell in the document
   var interpreterName = _pythonService.SelectedInterpreter?.Name ?? "Unknown";
  var cellId = Guid.NewGuid().ToString("N")[..8];
     var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
     
     // Build the cell content
  var cellBuilder = new StringBuilder();
 cellBuilder.AppendLine();
 cellBuilder.AppendLine("---");
  cellBuilder.AppendLine();
 cellBuilder.AppendLine($"## 🐍 Python Cell `{cellId}`");
     cellBuilder.AppendLine($"**Interpreter:** {interpreterName} | **Executed:** {timestamp}");
 cellBuilder.AppendLine();
     cellBuilder.AppendLine("### Code:");
        cellBuilder.AppendLine("```python");
   cellBuilder.AppendLine(lastExecutedCode);
     cellBuilder.AppendLine("```");
   cellBuilder.AppendLine();
  
  // Add plots if any were generated
  if (lastPlotFiles.Count > 0)
  {
       cellBuilder.AppendLine("### Generated Plots:");
   for (int i = 0; i < lastPlotFiles.Count; i++)
     {
          var plotFile = lastPlotFiles[i];
  try
       {
   // Copy plot to a permanent location in the app data
 var appDataDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jot", "Images");
        Directory.CreateDirectory(appDataDir);
   
     var fileName = $"python_plot_{cellId}_{i + 1:D2}.png";
         var destPath = System.IO.Path.Combine(appDataDir, fileName);
 File.Copy(plotFile, destPath, true);
         
     // Use relative path for the markdown
    cellBuilder.AppendLine($"![Python Plot {i + 1}](file:///{destPath.Replace('\\', '/')})");
      cellBuilder.AppendLine();
    
  // Clean up original temp file
       File.Delete(plotFile);
  }
     catch (Exception ex)
   {
         System.Diagnostics.Debug.WriteLine($"Error handling plot file {plotFile}: {ex.Message}");
   cellBuilder.AppendLine($"*Plot {i + 1}: Error saving plot*");
       }
     }
        }
   
      cellBuilder.AppendLine("### Output:");
 cellBuilder.AppendLine("```");
        cellBuilder.AppendLine(lastOutput);
cellBuilder.AppendLine("```");
     cellBuilder.AppendLine();
       cellBuilder.AppendLine("---");
 cellBuilder.AppendLine();

  var pythonCell = cellBuilder.ToString();

  // Instead of just appending, we need to insert it properly to maintain it in the editor
    SelectedDocument.Content += pythonCell;
       SelectedDocument.ModifiedAt = DateTime.Now;
   await SaveCurrentDocument();

          System.Diagnostics.Debug.WriteLine($"Python cell {cellId} with {lastPlotFiles.Count} plots added to document");
       }
    }
    catch (Exception ex)
{
      System.Diagnostics.Debug.WriteLine($"Error opening Python dialog: {ex.Message}");
     
        // Show error to user
    var errorDialog = new ContentDialog
 {
    Title = "Error",
 Content = $"Failed to open Python dialog: {ex.Message}",
 CloseButtonText = "OK",
   XamlRoot = App.MainWindow?.Content?.XamlRoot
     };
        await errorDialog.ShowAsync();
}
    }

   [RelayCommand]
        private async Task SetupGitHub()
        {
      try
       {
   var dialog = new ContentDialog
   {
           Title = "📤 GitHub Integration Setup",
  PrimaryButtonText = "Connect",
           CloseButtonText = "Cancel",
       XamlRoot = App.MainWindow?.Content?.XamlRoot
      };

      var panel = new StackPanel { Spacing = 16, Width = 500 };

   // Instructions
         panel.Children.Add(new TextBlock
      {
    Text = "To connect Jot with GitHub, you need a Personal Access Token:",
        TextWrapping = TextWrapping.Wrap,
    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
      });

panel.Children.Add(new TextBlock
   {
   Text = "1. Go to GitHub Settings > Developer settings > Personal access tokens\n2. Create a new token with 'repo' and 'user:email' permissions\n3. Copy the token and paste it below",
TextWrapping = TextWrapping.Wrap,
     Margin = new Thickness(0, 0, 0, 8)
  });

      // Create token button
  var createTokenButton = new Button
 {
        Content = "🔗 Create Token on GitHub",
         HorizontalAlignment = HorizontalAlignment.Left,
 Margin = new Thickness(0, 0, 0, 12)
    };

     createTokenButton.Click += async (s, e) =>
      {
      try
      {
 await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/settings/tokens/new?scopes=repo,user:email&description=Jot%20Note%20App"));
    }
        catch { }
  };

      panel.Children.Add(createTokenButton);

           // Token input
  var tokenPasswordBox = new PasswordBox
         {
  PlaceholderText = "ghp_xxxxxxxxxxxxxxxxxxxx",
   Header = "Personal Access Token"
       };
panel.Children.Add(tokenPasswordBox);

     // Status text
     var statusTextBlock = new TextBlock
      {
  Visibility = Visibility.Collapsed,
        TextWrapping = TextWrapping.Wrap
 };
panel.Children.Add(statusTextBlock);

    dialog.Content = panel;

     tokenPasswordBox.PasswordChanged += async (s, e) =>
    {
  var token = tokenPasswordBox.Password;
        if (string.IsNullOrWhiteSpace(token) || token.Length < 20)
          {
dialog.IsPrimaryButtonEnabled = false;
  statusTextBlock.Visibility = Visibility.Collapsed;
 return;
  }

       try
  {
           statusTextBlock.Text = "🔄 Verifying token...";
  statusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Blue);
 statusTextBlock.Visibility = Visibility.Visible;

     var success = await _gitHubService.AuthenticateAsync(token);
       if (success && _gitHubService.UserInfo != null)
    {
    var userInfo = _gitHubService.UserInfo;
  statusTextBlock.Text = $"✅ Connected as {userInfo.Name ?? userInfo.Login}";
   statusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
       dialog.IsPrimaryButtonEnabled = true;
     }
    else
   {
   statusTextBlock.Text = "❌ Authentication failed. Please check your token.";
   statusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
   dialog.IsPrimaryButtonEnabled = false;
    }
           }
   catch (Exception ex)
   {
statusTextBlock.Text = $"❌ Error: {ex.Message}";
      statusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
  dialog.IsPrimaryButtonEnabled = false;
}
   };

  dialog.IsPrimaryButtonEnabled = false;
   var result = await dialog.ShowAsync();

   if (result == ContentDialogResult.Primary)
  {
 IsGitHubConnected = _gitHubService.IsAuthenticated;
         if (IsGitHubConnected)
           {
        System.Diagnostics.Debug.WriteLine($"✅ GitHub setup completed for user: {_gitHubService.UserInfo?.Login}");
     }
       }
  }
    catch (Exception ex)
    {
             System.Diagnostics.Debug.WriteLine($"Error setting up GitHub: {ex.Message}");
    }
        }

   [RelayCommand]
        private async Task UploadToGitHub(Document? document = null)
    {
    var docToUpload = document ?? SelectedDocument;
      if (docToUpload == null)
      return;

    if (!_gitHubService.IsAuthenticated)
  {
  await SetupGitHub();
    if (!_gitHubService.IsAuthenticated)
             return;
      }

      try
    {
       var dialog = new ContentDialog
 {
        Title = "📤 Upload to GitHub",
PrimaryButtonText = "Upload",
     SecondaryButtonText = "Create Repository",
   CloseButtonText = "Cancel",
  XamlRoot = App.MainWindow?.Content?.XamlRoot
         };

       var panel = new StackPanel { Spacing = 16, Width = 500 };

    // Document info
     panel.Children.Add(new TextBlock
{
      Text = $"Document: {docToUpload.Title}",
  FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
       });

// Repository selection
       var repoComboBox = new ComboBox
 {
    Header = "Select Repository",
      PlaceholderText = "Loading repositories...",
 HorizontalAlignment = HorizontalAlignment.Stretch
    };
   panel.Children.Add(repoComboBox);

 // Path input
     var pathTextBox = new TextBox
      {
     Header = "Folder Path (optional)",
       PlaceholderText = "e.g., docs/notes/",
     Text = ""
     };
       panel.Children.Add(pathTextBox);

       // Commit message
     var commitTextBox = new TextBox
  {
   Header = "Commit Message",
   Text = $"Add '{docToUpload.Title}' from Jot"
  };
 panel.Children.Add(commitTextBox);

 // Status
     var statusText = new TextBlock
   {
  Visibility = Visibility.Collapsed,
    TextWrapping = TextWrapping.Wrap
      };
   panel.Children.Add(statusText);

       dialog.Content = panel;

         // Load repositories
         _ = Task.Run(async () =>
      {
   try
    {
   var repos = await _gitHubService.GetRepositoriesAsync();
  
   // Use MainWindow dispatcher instead
          App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
      {
      repoComboBox.ItemsSource = repos;
    repoComboBox.DisplayMemberPath = "Name";
             repoComboBox.PlaceholderText = repos.Count > 0 ? "Choose a repository..." : "No repositories found";
    dialog.IsPrimaryButtonEnabled = repos.Count > 0;
  });
   }
     catch (Exception ex)
 {
  App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
       {
  repoComboBox.PlaceholderText = $"Error loading repositories: {ex.Message}";
   });
   }
   });

    repoComboBox.SelectionChanged += (s, e) =>
  {
       dialog.IsPrimaryButtonEnabled = repoComboBox.SelectedItem != null;
 };

        var result = await dialog.ShowAsync();

           if (result == ContentDialogResult.Primary && repoComboBox.SelectedItem is GitHubRepository selectedRepo)
        {
      try
       {
     statusText.Text = "🚀 Uploading to GitHub...";
     statusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Blue);
      statusText.Visibility = Visibility.Visible;

 var uploadResult = await _gitHubService.UploadDocumentAsync(
   docToUpload, 
  selectedRepo, 
 string.IsNullOrWhiteSpace(pathTextBox.Text) ? null : pathTextBox.Text.Trim(),
     string.IsNullOrWhiteSpace(commitTextBox.Text) ? null : commitTextBox.Text.Trim());

     if (uploadResult.Success)
  {
System.Diagnostics.Debug.WriteLine($"✅ Document uploaded to GitHub: {uploadResult.FileUrl}");

        // Add reference to document
   var uploadReference = $@"

---
📤 **Uploaded to GitHub**: [{uploadResult.FileName}]({uploadResult.FileUrl})  
📅 Upload Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}  
📁 Repository: {uploadResult.Repository}

";
     docToUpload.Content += uploadReference;
      docToUpload.ModifiedAt = DateTime.Now;
  await SaveCurrentDocument();

         statusText.Text = "✅ Upload completed successfully!";
    statusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
           }
   else
 {
 statusText.Text = $"❌ Upload failed: {uploadResult.Error}";
           statusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
         }
}
          catch (Exception ex)
 {
   statusText.Text = $"❌ Error: {ex.Message}";
   statusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
 }
      }
   }
   catch (Exception ex)
       {
System.Diagnostics.Debug.WriteLine($"Error uploading to GitHub: {ex.Message}");
            }
   }

   [RelayCommand]
    private void DisconnectGitHub()
        {
  _gitHubService.Logout();
   IsGitHubConnected = false;
   System.Diagnostics.Debug.WriteLine("🔓 Disconnected from GitHub");
  }
    }
}