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
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Jot.ViewModels;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.UI;
using Windows.ApplicationModel.DataTransfer;
using System.Threading.Tasks;

namespace Jot
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }
        private bool _isUpdatingContent = false;
   private ObservableCollection<DocumentHeading> _documentHeadings = new();
        
     // Nuevas funcionalidades de edición
        private DispatcherTimer _autoSaveTimer;
        private Stack<string> _undoStack = new();
        private Stack<string> _redoStack = new();
    private readonly Dictionary<string, string> _textSnippets = new();
        private readonly Dictionary<string, Action> _customShortcuts = new();
        private string _lastSavedContent = "";
        private DateTime _lastTypingTime;
        private DispatcherTimer _typingTimer;

        public MainWindow()
        {
      this.InitializeComponent();
    
     ViewModel = new MainViewModel();
 
            SetupViewModeButtons();
   Title = "Jot - Modern Note Taking";
    this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1000, 700));
        LoadingOverlay.Visibility = Visibility.Collapsed;
            SetupBindings();
     
            // Configurar nuevas funcionalidades
            SetupAutoSave();
       SetupTextSnippets();
            SetupCustomShortcuts();
            SetupTypingTimer();
      }

        #region Nuevas Funcionalidades de Edición

    private void SetupAutoSave()
 {
            _autoSaveTimer = new DispatcherTimer();
     _autoSaveTimer.Interval = TimeSpan.FromMinutes(2); // Auto-guardar cada 2 minutos
          _autoSaveTimer.Tick += AutoSave_Tick;
            _autoSaveTimer.Start();
      }

      private void SetupTextSnippets()
        {
            // Fragmentos de texto predefinidos para inserción rápida
            _textSnippets["//date"] = DateTime.Now.ToString("yyyy-MM-dd");
            _textSnippets["//time"] = DateTime.Now.ToString("HH:mm");
            _textSnippets["//datetime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
       _textSnippets["//meeting"] = $"# Meeting Notes - {DateTime.Now:yyyy-MM-dd}\n\n## Attendees\n- \n\n## Agenda\n1. \n\n## Action Items\n- [ ] ";
    _textSnippets["//todo"] = "## TODO List\n\n- [ ] Task 1\n- [ ] Task 2\n- [ ] Task 3";
       _textSnippets["//code"] = "```\n// Your code here\n```";
   _textSnippets["//table"] = "| Column 1 | Column 2 | Column 3 |\n|----------|----------|----------|\n| Data 1   | Data 2   | Data 3   |";
   _textSnippets["//quote"] = "> This is a quote\n> \n> *Author*";
        }

  private void SetupCustomShortcuts()
        {
            // Atajos personalizados
            _customShortcuts["Ctrl+Shift+D"] = () => InsertDateTime();
 _customShortcuts["Ctrl+Shift+T"] = () => InsertTodoTemplate();
_customShortcuts["Ctrl+Shift+M"] = () => InsertMeetingTemplate();
          _customShortcuts["Ctrl+Shift+C"] = () => InsertCodeBlock();
            _customShortcuts["Ctrl+H"] = () => ShowFindReplaceDialog();
    _customShortcuts["F3"] = () => FindNext();
        _customShortcuts["Ctrl+L"] = () => SelectCurrentLine();
         _customShortcuts["Ctrl+D"] = () => DuplicateCurrentLine();
        }

        private void SetupTypingTimer()
        {
      _typingTimer = new DispatcherTimer();
_typingTimer.Interval = TimeSpan.FromMilliseconds(500);
  _typingTimer.Tick += TypingTimer_Tick;
  }

        private void AutoSave_Tick(object sender, object e)
        {
            if (ViewModel.SelectedDocument != null && 
          ViewModel.SelectedDocument.Content != _lastSavedContent)
        {
       ViewModel.SaveCurrentDocumentCommand.Execute(null);
     _lastSavedContent = ViewModel.SelectedDocument.Content ?? "";
   ShowAutoSaveNotification();
            }
        }

private void TypingTimer_Tick(object sender, object e)
        {
            _typingTimer.Stop();
            // Aquí se puede añadiar funcionalidad para formateo automático después de pausar
            AutoFormatContent();
        }

        private void ShowAutoSaveNotification()
        {
          // Mostrar una pequeña notificación de auto-guardado
   System.Diagnostics.Debug.WriteLine("Document auto-saved");
        }

        private void AutoFormatContent()
        {
            if (ViewModel.SelectedDocument == null) return;

          var content = ViewModel.SelectedDocument.Content ?? "";
            var formatted = content;

            // Auto-formato básico
     // Corregir espaciado en listas
            formatted = Regex.Replace(formatted, @"^(\s*)-\s*([^\s])", "$1- $2", RegexOptions.Multiline);
            
      // Asegurar línea vacía después de headers
        formatted = Regex.Replace(formatted, @"(^#{1,6}.*)\n([^#\n])", "$1\n\n$2", RegexOptions.Multiline);

     if (formatted != content)
     {
            _isUpdatingContent = true;
    ViewModel.SelectedDocument.Content = formatted;
    TextEditor.Text = formatted;
          _isUpdatingContent = false;
   }
 }

// Funciones de Deshacer/Rehacer
        private void SaveToUndoStack()
        {
       if (ViewModel.SelectedDocument?.Content != null)
            {
     _undoStack.Push(ViewModel.SelectedDocument.Content);
    _redoStack.Clear(); // Limpiar redo stack cuando se hace una nueva acción
     
        // Limitar el tamaño del stack
                if (_undoStack.Count > 50)
  {
        var tempStack = new Stack<string>();
           for (int i = 0; i < 49; i++)
       {
     tempStack.Push(_undoStack.Pop());
   }
      _undoStack.Clear();
     while (tempStack.Count > 0)
     {
   _undoStack.Push(tempStack.Pop());
  }
    }
       }
        }

        private void Undo()
        {
      if (_undoStack.Count > 0 && ViewModel.SelectedDocument != null)
            {
       _redoStack.Push(ViewModel.SelectedDocument.Content ?? "");
    var previousContent = _undoStack.Pop();
 
     _isUpdatingContent = true;
      ViewModel.SelectedDocument.Content = previousContent;
      TextEditor.Text = previousContent;
    UpdatePreviewContentImmediate(previousContent);
  _isUpdatingContent = false;
        }
 }

        private void Redo()
        {
    if (_redoStack.Count > 0 && ViewModel.SelectedDocument != null)
       {
 _undoStack.Push(ViewModel.SelectedDocument.Content ?? "");
                var nextContent = _redoStack.Pop();
          
      _isUpdatingContent = true;
        ViewModel.SelectedDocument.Content = nextContent;
     TextEditor.Text = nextContent;
           UpdatePreviewContentImmediate(nextContent);
       _isUpdatingContent = false;
            }
        }

        // Inserción de plantillas
        private void InsertDateTime()
        {
         InsertTextAtCursor(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
        }

        private void InsertTodoTemplate()
        {
            InsertTextAtCursor(_textSnippets["//todo"]);
        }

      private void InsertMeetingTemplate()
        {
        InsertTextAtCursor(_textSnippets["//meeting"]);
        }

        private void InsertCodeBlock()
        {
            InsertTextAtCursor(_textSnippets["//code"]);
    }

        private void InsertTextAtCursor(string text)
    {
            if (ViewModel.SelectedDocument == null) return;

      SaveToUndoStack();
     
    // Obtener posición del cursor (simplificado - en una implementación real se obtendría del editor)
 var currentContent = ViewModel.SelectedDocument.Content ?? "";
       var newContent = currentContent + text;
         
   _isUpdatingContent = true;
            ViewModel.SelectedDocument.Content = newContent;
            TextEditor.Text = newContent;
     UpdatePreviewContentImmediate(newContent);
     _isUpdatingContent = false;
        }

        // Búsqueda y reemplazo
        private async void ShowFindReplaceDialog()
        {
            var dialog = new ContentDialog
  {
      Title = "Find and Replace",
                PrimaryButtonText = "Replace All",
    SecondaryButtonText = "Find Next",
       CloseButtonText = "Close",
        XamlRoot = this.Content.XamlRoot
         };

    var stackPanel = new StackPanel { Spacing = 12 };
 
   var findBox = new TextBox
      {
  PlaceholderText = "Find...",
  Header = "Find:"
            };
 
            var replaceBox = new TextBox
  {
       PlaceholderText = "Replace with...",
        Header = "Replace with:"
            };

      var caseSensitiveCheck = new CheckBox
       {
       Content = "Case sensitive",
      IsChecked = false
   };

            stackPanel.Children.Add(findBox);
        stackPanel.Children.Add(replaceBox);
    stackPanel.Children.Add(caseSensitiveCheck);
         
            dialog.Content = stackPanel;

 var result = await dialog.ShowAsync();
            
  if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(findBox.Text))
  {
    ReplaceAll(findBox.Text, replaceBox.Text, caseSensitiveCheck.IsChecked ?? false);
            }
            else if (result == ContentDialogResult.Secondary && !string.IsNullOrEmpty(findBox.Text))
       {
  FindNext(findBox.Text, caseSensitiveCheck.IsChecked ?? false);
    }
     }

        private void FindNext(string searchText = "", bool caseSensitive = false)
        {
  if (ViewModel.SelectedDocument == null || string.IsNullOrEmpty(searchText)) return;

   var content = ViewModel.SelectedDocument.Content ?? "";
  var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
    var index = content.IndexOf(searchText, comparison);
            
        if (index >= 0)
{
                // En una implementación real, se destacaría el texto encontrado en el editor
            System.Diagnostics.Debug.WriteLine($"Found '{searchText}' at position {index}");
  }
        }

    private void ReplaceAll(string findText, string replaceText, bool caseSensitive)
        {
   if (ViewModel.SelectedDocument == null || string.IsNullOrEmpty(findText)) return;

            SaveToUndoStack();
    
          var content = ViewModel.SelectedDocument.Content ?? "";
            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            
    // Usar Regex para reemplazo si no es case sensitive
         string newContent;
            if (caseSensitive)
     {
                newContent = content.Replace(findText, replaceText);
     }
            else
            {
 newContent = Regex.Replace(content, Regex.Escape(findText), replaceText, RegexOptions.IgnoreCase);
  }

         _isUpdatingContent = true;
       ViewModel.SelectedDocument.Content = newContent;
        TextEditor.Text = newContent;
   UpdatePreviewContentImmediate(newContent);
            _isUpdatingContent = false;
        }

        // Funciones de línea
        private void SelectCurrentLine()
    {
            // En una implementación real, esto seleccionaría la línea actual en el editor
        System.Diagnostics.Debug.WriteLine("Select current line");
        }

        private void DuplicateCurrentLine()
        {
  if (ViewModel.SelectedDocument == null) return;

  SaveToUndoStack();
 
            // Implementación simplificada - duplicar todo el contenido
            var content = ViewModel.SelectedDocument.Content ?? "";
            var newContent = content + "\n" + content;
        
            _isUpdatingContent = true;
            ViewModel.SelectedDocument.Content = newContent;
            TextEditor.Text = newContent;
            UpdatePreviewContentImmediate(newContent);
            _isUpdatingContent = false;
 }

        // Formateo de texto
   private void FormatSelectedText(string beforeText, string afterText)
        {
            if (ViewModel.SelectedDocument == null) return;

    SaveToUndoStack();
    
            // Implementación simplificada - agregar formato al final
   var content = ViewModel.SelectedDocument.Content ?? "";
  var newContent = content + beforeText + "selected text" + afterText;
     
            _isUpdatingContent = true;
            ViewModel.SelectedDocument.Content = newContent;
            TextEditor.Text = newContent;
            UpdatePreviewContentImmediate(newContent);
     _isUpdatingContent = false;
        }

        // Procesamiento de fragmentos de texto
        private void ProcessTextSnippets(string text)
    {
          foreach (var snippet in _textSnippets)
            {
    if (text.Contains(snippet.Key))
    {
         var newContent = text.Replace(snippet.Key, snippet.Value);
        if (newContent != text)
 {
         _isUpdatingContent = true;
               ViewModel.SelectedDocument.Content = newContent;
  TextEditor.Text = newContent;
    UpdatePreviewContentImmediate(newContent);
  _isUpdatingContent = false;
              break;
          }
    }
   }
        }

    // Estadísticas del documento
     private async void ShowDocumentStats()
        {
          if (ViewModel.SelectedDocument == null) return;

 var content = ViewModel.SelectedDocument.Content ?? "";
            var words = content.Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
      var characters = content.Length;
            var charactersNoSpaces = content.Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", "").Length;
            var lines = content.Split('\n').Length;
    var paragraphs = content.Split(new string[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries).Length;

         var statsText = $"Words: {words}\n" +
      $"Characters: {characters}\n" +
     $"Characters (no spaces): {charactersNoSpaces}\n" +
        $"Lines: {lines}\n" +
           $"Paragraphs: {paragraphs}\n" +
      $"Reading time: ~{Math.Ceiling(words / 200.0)} minutes";

    var dialog = new ContentDialog
          {
      Title = "Document Statistics",
    Content = statsText,
            CloseButtonText = "Close",
      XamlRoot = this.Content.XamlRoot
        };

            await dialog.ShowAsync();
        }

        #endregion

        // Métodos existentes con mejoras
     private void SetupViewModeButtons()
        {
    EditModeButton.Click += (s, e) => ViewModel.SetViewModeCommand.Execute(ViewMode.Edit);
            PreviewModeButton.Click += (s, e) => ViewModel.SetViewModeCommand.Execute(ViewMode.Preview);
          SplitModeButton.Click += (s, e) => ViewModel.SetViewModeCommand.Execute(ViewMode.Split);
  }

      private void SetupBindings()
      {
            DocumentsList.ItemsSource = ViewModel.Documents;
        SearchBox.Text = ViewModel.SearchText;
     DocumentIndexList.ItemsSource = _documentHeadings;
       
DocumentsList.SelectionChanged += DocumentsList_SelectionChanged;
   TogglePaneButton.Click += (s, e) => ViewModel.ToggleSidebarCommand.Execute(null);
   
            TextEditor.TextChanged += TextEditor_TextChanged;
       TextEditor.DrawingModeChanged += TextEditor_DrawingModeChanged;
            TextEditor.RequestFreeDrawing += TextEditor_RequestFreeDrawing;
       
            SearchBox.TextChanged += SearchBox_TextChanged;
       ViewModel.PropertyChanged += ViewModel_PropertyChanged;
      
     // Configurar eventos de teclado para atajos en el Grid raíz
            RootGrid.KeyDown += MainWindow_KeyDown;
   RootGrid.Focus(FocusState.Programmatic);
       
    UpdateViewMode();
            UpdateSelectedDocument();
        UpdateDocumentIndex();
            UpdateGitHubConnectionStatus();
         UpdateGitHubButtonStates();
   }

    private void MainWindow_KeyDown(object sender, KeyRoutedEventArgs e)
     {
            var key = e.Key.ToString();
   var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
            var shift = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            string shortcut = "";
            if (ctrl && shift) shortcut = $"Ctrl+Shift+{key}";
 else if (ctrl) shortcut = $"Ctrl+{key}";
            else if (key == "F3") shortcut = "F3";

        if (_customShortcuts.ContainsKey(shortcut))
  {
    _customShortcuts[shortcut].Invoke();
          e.Handled = true;
            }
       
   // Atajos adicionales integrados
            if (ctrl && key == "Z")
         {
    Undo();
           e.Handled = true;
        }
            else if (ctrl && key == "Y")
            {
     Redo();
   e.Handled = true;
            }
    else if (ctrl && shift && key == "S")
            {
         ShowDocumentStats();
    e.Handled = true;
 }
        }

        // Resto de métodos existentes...
        #region Button Events
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

    private void ExportToHtml_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ExportCurrentDocumentToHtmlCommand.Execute(null);
        }

        private void ExportDocumentToHtml_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && menuItem.CommandParameter is Models.Document document)
         {
      ViewModel.ExportDocumentToHtmlCommand.Execute(document);
            }
   }

        private void UploadDocumentToGitHub_Click(object sender, RoutedEventArgs e)
        {
if (sender is MenuFlyoutItem menuItem && menuItem.CommandParameter is Models.Document document)
{
       ViewModel.UploadToGitHubCommand.Execute(document);
            }
        }

    private void OpenPythonDialog_Click(object sender, RoutedEventArgs e)
        {
       ViewModel.OpenPythonDialogCommand.Execute(null);
      }

        private void SetupGitHub_Click(object sender, RoutedEventArgs e)
        {
     ViewModel.SetupGitHubCommand.Execute(null);
   }

        private void ToggleGitHubPanel_Click(object sender, RoutedEventArgs e)
        {
            // Toggle GitHub panel expansion
            if (GitHubActionsPanel?.Visibility == Visibility.Visible)
       {
     GitHubActionsPanel.Visibility = Visibility.Collapsed;
                if (GitHubToggleButton != null)
        GitHubToggleButton.Content = new FontIcon { Glyph = "\uE70D", FontSize = 12 }; // Down arrow
            }
     else
            {
                if (GitHubActionsPanel != null)
          GitHubActionsPanel.Visibility = Visibility.Visible;
       if (GitHubToggleButton != null)
         GitHubToggleButton.Content = new FontIcon { Glyph = "\uE70E", FontSize = 12 }; // Up arrow
    }
        }

   private void UploadCurrentDocument_Click(object sender, RoutedEventArgs e)
        {
         if (ViewModel.SelectedDocument != null)
        {
    ViewModel.UploadToGitHubCommand.Execute(null);
     }
    }

        private void QuickUploadToGitHub_Click(object sender, RoutedEventArgs e)
        {
       if (ViewModel.SelectedDocument != null)
          {
        ViewModel.UploadToGitHubCommand.Execute(null);
      }
  else
            {
        // Show message that no document is selected
   ShowNoDocumentSelectedMessage();
        }
        }

        private async void ShowNoDocumentSelectedMessage()
        {
            try
          {
      var dialog = new ContentDialog
     {
     Title = "No Document Selected",
Content = "Please select or create a document first before uploading to GitHub.",
         CloseButtonText = "OK",
        XamlRoot = this.Content.XamlRoot
   };
     await dialog.ShowAsync();
            }
  catch (Exception ex)
            {
        System.Diagnostics.Debug.WriteLine($"Error showing dialog: {ex.Message}");
            }
        }

        private void ManageRepositories_Click(object sender, RoutedEventArgs e)
        {
            // Open repository management dialog
       ViewModel.SetupGitHubCommand.Execute(null);
        }

        private void DisconnectGitHub_Click(object sender, RoutedEventArgs e)
        {
         ViewModel.DisconnectGitHubCommand.Execute(null);
        }
      #endregion

        #region Event Handlers
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
       // Guardar en stack de deshacer antes de cambios significativos
        var timeSinceLastTyping = DateTime.Now - _lastTypingTime;
  if (timeSinceLastTyping.TotalSeconds > 2)
           {
       SaveToUndoStack();
 }
       _lastTypingTime = DateTime.Now;

    ViewModel.SelectedDocument.Content = newText;
             ViewModel.SelectedDocument.ModifiedAt = DateTime.Now;
       
       UpdatePreviewContentImmediate(newText);
         UpdateSplitModeContentImmediate(newText);
     UpdateDocumentIndex();

     // Procesar fragmentos de texto
      ProcessTextSnippets(newText);

     // Reiniciar timer de tipeo para auto-formato
        _typingTimer.Stop();
                _typingTimer.Start();
    }
        }

        private void TextEditor_DrawingModeChanged(object? sender, bool isDrawingMode)
        {
       System.Diagnostics.Debug.WriteLine($"Drawing mode changed: {isDrawingMode}");
        }

        private void TextEditor_RequestFreeDrawing(object? sender, EventArgs e)
     {
            // Open free drawing canvas when requested from the text editor
            System.Diagnostics.Debug.WriteLine("Free drawing requested");
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
                  UpdateGitHubButtonStates();
         
    // Limpiar stacks de deshacer/rehacer al cambiar documento
         _undoStack.Clear();
     _redoStack.Clear();
        _lastSavedContent = ViewModel.SelectedDocument?.Content ?? "";
               break;
           case nameof(ViewModel.IsPaneOpen):
 MainNavigationView.IsPaneOpen = ViewModel.IsPaneOpen;
        break;
  case nameof(ViewModel.IsExportingHtml):
    UpdateLoadingOverlayVisibility();
         break;
         case nameof(ViewModel.IsGitHubConnected):
         UpdateGitHubConnectionStatus();
   UpdateGitHubButtonStates();
          break;
            }
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
       ScrollToLineInEditor(heading.LineNumber);
          }
    }
        #endregion

        // Resto de métodos existentes...
        #region UI Updates
  private void UpdateLoadingOverlayVisibility()
        {
 if (LoadingOverlay != null)
 {
           LoadingOverlay.Visibility = ViewModel.IsExportingHtml ? Visibility.Visible : Visibility.Collapsed;
          }
        }

   private void UpdateDocumentContent()
        {
      _isUpdatingContent = true;

    if (ViewModel.SelectedDocument != null)
  {
      if (TitleTextBox != null)
     {
         TitleTextBox.Text = ViewModel.SelectedDocument.Title ?? "";
                }
  
             TextEditor.Text = ViewModel.SelectedDocument.Content ?? "";
   
    if (SplitModeEditor != null)
          {
             SplitModeEditor.Text = ViewModel.SelectedDocument.Content ?? "";
     SplitModeEditor.TextChanged -= SplitEditor_TextChanged;
     SplitModeEditor.DrawingModeChanged -= TextEditor_DrawingModeChanged;
  SplitModeEditor.RequestFreeDrawing -= TextEditor_RequestFreeDrawing;
         
    SplitModeEditor.TextChanged += SplitEditor_TextChanged;
         SplitModeEditor.DrawingModeChanged += TextEditor_DrawingModeChanged;
   SplitModeEditor.RequestFreeDrawing += TextEditor_RequestFreeDrawing;
        }
    
       UpdatePreviewContent();
            }
       else
            {
  if (TitleTextBox != null) TitleTextBox.Text = "";
        TextEditor.Text = "";
 if (SplitModeEditor != null) SplitModeEditor.Text = "";
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
           
       _isUpdatingContent = true;
      TextEditor.Text = newText;
                _isUpdatingContent = false;
       
   UpdatePreviewContentImmediate(newText);
                UpdateDocumentIndex();
  }
        }

  private void UpdatePreviewContent()
        {
    var content = ViewModel.SelectedDocument?.Content ?? "";
            UpdatePreviewContentImmediate(content);
        }

        private void UpdatePreviewContentImmediate(string content)
        {
 if (MarkdownPreview != null)
            {
MarkdownPreview.MarkdownText = content;
            }
 
 if (SplitModePreview != null)
            {
 SplitModePreview.MarkdownText = content;
            }
        }

   private void UpdateSplitModeContentImmediate(string content)
        {
          if (SplitModeEditor != null && !_isUpdatingContent)
       {
                _isUpdatingContent = true;
      SplitModeEditor.Text = content;
    _isUpdatingContent = false;
    }
   
    if (SplitModePreview != null)
     {
         SplitModePreview.MarkdownText = content;
            }
        }

        private void UpdateViewMode()
        {
    EditModeButton.IsChecked = false;
      PreviewModeButton.IsChecked = false;
     SplitModeButton.IsChecked = false;
 
    TextEditor.Visibility = Visibility.Collapsed;
MarkdownPreview.Visibility = Visibility.Collapsed;
  SplitModeGrid.Visibility = Visibility.Collapsed;
  
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
           break;
    }
      }

     private void UpdateSelectedDocument()
        {
      bool hasDocument = ViewModel.SelectedDocument != null;

     EmptyStateGrid.Visibility = hasDocument ? Visibility.Collapsed : Visibility.Visible;
 
   if (DocumentHeaderBorder != null)
     {
           DocumentHeaderBorder.Visibility = hasDocument ? Visibility.Visible : Visibility.Collapsed;
    }
   
     if (EditorContainer != null)
    {
        EditorContainer.Visibility = hasDocument ? Visibility.Visible : Visibility.Collapsed;
       }
  
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
        }
       catch (Exception ex)
            {
       System.Diagnostics.Debug.WriteLine($"Error updating document index: {ex.Message}");
     }
 }

        private void UpdateGitHubConnectionStatus()
      {
            try
            {
            if (ViewModel.IsGitHubConnected)
        {
  // Connected state
           if (GitHubStatusIndicator != null)
  GitHubStatusIndicator.Fill = new SolidColorBrush(Microsoft.UI.Colors.Green);
   if (GitHubStatusText != null)
    GitHubStatusText.Text = "Connected";
 if (SetupGitHubButton != null)
    SetupGitHubButton.Visibility = Visibility.Collapsed;
  if (GitHubActionsPanel != null)
GitHubActionsPanel.Visibility = Visibility.Visible;
      
         // Update quick upload button in toolbar
    if (QuickGitHubUploadButton != null)
  {
     QuickGitHubUploadButton.Opacity = 1.0;
     QuickGitHubUploadButton.IsEnabled = true;
         }
    }
                else
     {
      // Disconnected state
      if (GitHubStatusIndicator != null)
        GitHubStatusIndicator.Fill = new SolidColorBrush(Microsoft.UI.Colors.Red);
     if (GitHubStatusText != null)
            GitHubStatusText.Text = "Not Connected";
      if (SetupGitHubButton != null)
    SetupGitHubButton.Visibility = Visibility.Visible;
    if (GitHubActionsPanel != null)
   GitHubActionsPanel.Visibility = Visibility.Collapsed;
    
    // Update quick upload button in toolbar
   if (QuickGitHubUploadButton != null)
     {
          QuickGitHubUploadButton.Opacity = 0.5;
  QuickGitHubUploadButton.IsEnabled = false;
             }
     }
  }
      catch (Exception ex)
     {
                System.Diagnostics.Debug.WriteLine($"Error updating GitHub status: {ex.Message}");
     }
        }

        private void UpdateGitHubButtonStates()
        {
        try
         {
        bool hasDocument = ViewModel.SelectedDocument != null;
    bool isConnected = ViewModel.IsGitHubConnected;
    
            // Enable/disable upload buttons based on document selection and connection
                if (UploadCurrentDocButton != null)
     {
 UploadCurrentDocButton.IsEnabled = hasDocument && isConnected;
   UploadCurrentDocButton.Opacity = (hasDocument && isConnected) ? 1.0 : 0.5;
      }
      
      if (QuickGitHubUploadButton != null)
         {
            QuickGitHubUploadButton.IsEnabled = hasDocument && isConnected;
          QuickGitHubUploadButton.Opacity = (hasDocument && isConnected) ? 1.0 : 0.5;
   }
     }
            catch (Exception ex)
            {
     System.Diagnostics.Debug.WriteLine($"Error updating GitHub button states: {ex.Message}");
            }
        }
        #endregion

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
  }
      }
            }

     return headings;
        }

    private void ScrollToLineInEditor(int lineNumber)
        {
            TextEditor.Focus(FocusState.Programmatic);
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