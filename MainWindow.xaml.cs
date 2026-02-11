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
using Windows.Media.SpeechRecognition;
using Windows.Media.SpeechSynthesis;
using Windows.Storage.Streams;
using Windows.Globalization;
using Windows.ApplicationModel.Core;
using Microsoft.UI.Dispatching;
using Jot.Services;

namespace Jot
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }
        private bool _isUpdatingContent = false;
      private ObservableCollection<DocumentHeading> _documentHeadings = new();
        
        // Funcionalidades de edición existentes
private DispatcherTimer _autoSaveTimer;
        private Stack<string> _undoStack = new();
  private Stack<string> _redoStack = new();
        private readonly Dictionary<string, string> _textSnippets = new();
        private readonly Dictionary<string, Action> _customShortcuts = new();
        private string _lastSavedContent = "";
        private DateTime _lastTypingTime;
   private DispatcherTimer _typingTimer;

   // Nuevas funcionalidades avanzadas
    private SpeechRecognizer _speechRecognizer;
        private SpeechSynthesizer _speechSynthesizer;
        private bool _isListening = false;
        private bool _isSpeaking = false;
        private bool _isFocusMode = false;
        private DispatcherTimer _pomodoroTimer;
        private DispatcherTimer _wordCountTimer;
        private int _pomodoroMinutes = 25;
    private int _pomodoroSeconds = 0;
        private bool _pomodoroRunning = false;
        private int _dailyWordCount = 0;
        private int _sessionWordCount = 0;
        private Dictionary<DateTime, int> _writingStats = new();
     private DispatcherTimer _autosaveIndicatorTimer;
        private bool _darkModeEnabled = false;
        private double _originalFontSize = 14;
    private string _lastVoiceInput = "";
        private readonly List<string> _recentSearches = new();

        public MainWindow()
        {
            this.InitializeComponent();

            ViewModel = new MainViewModel();

            SetupViewModeButtons();
            Title = LocalizationService.Instance.GetString("AppTitle");
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1000, 700));
            LoadingOverlay.Visibility = Visibility.Collapsed;
            SetupBindings();
            SetupLocalization();

            // Configurar funcionalidades existentes
            SetupAutoSave();
            SetupTextSnippets();
            SetupCustomShortcuts();
            SetupTypingTimer();

            // Configurar nuevas funcionalidades avanzadas
            InitializeVoiceFeatures();
            SetupPomodoroTimer();
            SetupWordCountTimer();
            SetupWritingStats();
            LoadUserPreferences();
        }

        #region Configuración Inicial

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

        private void SetupLocalization()
        {
            // Suscribirse a cambios de idioma
            LocalizationService.Instance.PropertyChanged += LocalizationService_PropertyChanged;

            // Configurar idioma inicial
            UpdateLanguageDisplay();
            UpdateAllLocalizedTexts();
        }

        private void LocalizationService_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Cuando cambia el idioma, actualizar todos los textos
            UpdateLanguageDisplay();
            UpdateAllLocalizedTexts();
        }

        private void UpdateLanguageDisplay()
        {
            var currentLang = LocalizationService.Instance.CurrentLanguage;
            if (CurrentLanguageText != null)
                CurrentLanguageText.Text = currentLang.ToUpper();

            // Actualizar el título de la ventana
            Title = LocalizationService.Instance.GetString("AppTitle");
        }

        private void UpdateAllLocalizedTexts()
        {
            try
            {
                // Actualizar tooltips de la barra de herramientas
                UpdateToolbarTooltips();

                // Actualizar placeholders y labels
                UpdatePlaceholders();

                // Actualizar botones de modo de vista
                UpdateViewModeButtonTexts();

                // Actualizar textos del menú
                UpdateMenuTexts();

                // Actualizar textos de la barra de estado de GitHub
                UpdateGitHubConnectionStatus();

                // Notificar a todos los bindings que se refresquen
                NotifyAllBindingsChanged();

                System.Diagnostics.Debug.WriteLine($"All texts updated to: {LocalizationService.Instance.CurrentLanguageName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating localized texts: {ex.Message}");
            }
        }

        private void UpdateViewModeButtonTexts()
        {
            try
            {
                // Actualizar textos de botones de modo de vista
                if (EditModeButton != null)
                {
                    EditModeButton.Content = LocalizationService.Instance.GetString("EditMode");
                    ToolTipService.SetToolTip(EditModeButton, LocalizationService.Instance.GetString("EditMode"));
                }
                if (PreviewModeButton != null)
                {
                    PreviewModeButton.Content = LocalizationService.Instance.GetString("PreviewMode");
                    ToolTipService.SetToolTip(PreviewModeButton, LocalizationService.Instance.GetString("PreviewMode"));
                }
                if (SplitModeButton != null)
                {
                    SplitModeButton.Content = LocalizationService.Instance.GetString("SplitMode");
                    ToolTipService.SetToolTip(SplitModeButton, LocalizationService.Instance.GetString("SplitMode"));
                }

                // Actualizar otros textos de UI
                if (LastModifiedText != null)
                    LastModifiedText.Text = LocalizationService.Instance.GetString("LastModified");

                if (DocumentIndexHeader != null)
                    DocumentIndexHeader.Text = LocalizationService.Instance.GetString("DocumentIndex");

                if (WelcomeText != null)
                    WelcomeText.Text = LocalizationService.Instance.GetString("WelcomeToJot");

                if (CreateFirstDocText != null)
                    CreateFirstDocText.Text = LocalizationService.Instance.GetString("CreateFirstDocument");

                if (CreateNewDocButton != null)
                    CreateNewDocButton.Content = LocalizationService.Instance.GetString("CreateNewDocument");

                if (ExportingText != null)
                    ExportingText.Text = LocalizationService.Instance.GetString("ExportingToHtml");

                // Actualizar textos de GitHub
                UpdateGitHubTexts();

                System.Diagnostics.Debug.WriteLine("View mode button texts updated");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating view mode buttons: {ex.Message}");
            }
        }

        private void UpdateGitHubTexts()
        {
            try
            {
                if (GitHubHeaderText != null)
                    GitHubHeaderText.Text = LocalizationService.Instance.GetString("GitHub");

                if (UploadCurrentText != null)
                    UploadCurrentText.Text = LocalizationService.Instance.GetString("UploadCurrent");

                if (RepositoriesText != null)
                    RepositoriesText.Text = LocalizationService.Instance.GetString("Repositories");

                if (DisconnectText != null)
                    DisconnectText.Text = LocalizationService.Instance.GetString("Disconnect");

                if (ConnectGitHubText != null)
                    ConnectGitHubText.Text = LocalizationService.Instance.GetString("ConnectGitHub");

                if (GitHubToggleButton != null)
                    ToolTipService.SetToolTip(GitHubToggleButton, LocalizationService.Instance.GetString("ExpandGitHubOptions"));

                System.Diagnostics.Debug.WriteLine("GitHub texts updated");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating GitHub texts: {ex.Message}");
            }
        }

        private void UpdateMenuTexts()
        {
            try
            {
                // Actualizar textos de elementos de menú que sean accesibles
                // La mayoría de los menús contextuales se crean dinámicamente,
                // por lo que se actualizarán automáticamente la próxima vez que se abran
                System.Diagnostics.Debug.WriteLine("Menu texts will be updated on next open");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating menu texts: {ex.Message}");
            }
        }

        private void UpdateToolbarTooltips()
        {
            try
            {
                if (TogglePaneButton != null)
                    ToolTipService.SetToolTip(TogglePaneButton, "Toggle Sidebar"); // Mantener genérico

                // Actualizar todos los botones de la toolbar principal
                var buttons = GetAllButtons().ToList();

                foreach (var button in buttons)
                {
                    if (button is Button btn)
                    {
                        UpdateButtonTooltip(btn);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Updated {buttons.Count} button tooltips");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating toolbar tooltips: {ex.Message}");
            }
        }

        private void UpdateButtonTooltip(Button button)
        {
            try
            {
                string tooltip = null;

                // Identificar botones por nombre
                if (button.Name == "QuickGitHubUploadButton")
                {
                    tooltip = LocalizationService.Instance.GetString("QuickUploadToGitHub");
                }
                else if (button.Name == "LanguageButton")
                {
                    tooltip = LocalizationService.Instance.GetString("Language");
                }
                else if (button.Name == "TogglePaneButton")
                {
                    tooltip = LocalizationService.Instance.GetString("ToggleSidebar");
                }
                // Identificar por contenido
                else if (button.Content is FontIcon icon)
                {
                    tooltip = icon.Glyph switch
                    {
                        "\uE710" => LocalizationService.Instance.GetString("NewDocument"),
                        "\uE74E" => LocalizationService.Instance.GetString("SaveDocument"),
                        "\uE12B" => LocalizationService.Instance.GetString("ExportToHtml"),
                        "\uE943" => LocalizationService.Instance.GetString("PythonCodeExecution"),
                        "\uE8F7" => LocalizationService.Instance.GetString("GitHubSettings"),
                        "\uE8F2" => LocalizationService.Instance.GetString("AIAssistant"),
                        "\uE774" => LocalizationService.Instance.GetString("Language"),
                        _ => null
                    };
                }
                else if (button.Content is StackPanel stackPanel)
                {
                    if (button.Name == "QuickGitHubUploadButton")
                        tooltip = LocalizationService.Instance.GetString("QuickUploadToGitHub");
                }

                if (!string.IsNullOrEmpty(tooltip))
                {
                    ToolTipService.SetToolTip(button, tooltip);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating button tooltip: {ex.Message}");
            }
        }

        private string GetLocalizedTooltipForButton(Button button)
        {
            // Identificar botones por su gliph de icono o eventos
            if (button.Content is FontIcon icon)
            {
                return icon.Glyph switch
                {
                    "\uE710" => LocalizationService.Instance.GetString("NewDocument"),
                    "\uE74E" => LocalizationService.Instance.GetString("SaveDocument"),
                    "\uE12B" => LocalizationService.Instance.GetString("ExportToHtml"),
                    "\uE943" => "Python Code Execution", // Mantener en inglés por ahora
                    "\uE8F7" => "GitHub Settings",
                    "\uE8F2" => "AI Assistant",
                    "\uE774" => LocalizationService.Instance.GetString("Language"),
                    _ => null
                };
            }
            else if (button.Content is StackPanel stackPanel && stackPanel.Children.Count > 0)
            {
                // Para botones con contenido más complejo (como el de GitHub)
                if (button.Name == "QuickGitHubUploadButton")
                    return LocalizationService.Instance.GetString("UploadToGitHub");
            }

            return null;
        }

        private IEnumerable<FrameworkElement> GetAllButtons()
        {
            return GetAllChildElements(RootGrid).OfType<Button>();
        }

        private IEnumerable<FrameworkElement> GetAllChildElements(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is FrameworkElement element)
                    yield return element;

                foreach (var grandChild in GetAllChildElements(child))
                    yield return grandChild;
            }
        }

        private void UpdatePlaceholders()
        {
            try
            {
                if (SearchBox != null)
                    SearchBox.PlaceholderText = LocalizationService.Instance.GetString("SearchDocuments");

                if (DocumentsHeaderText != null)
                    DocumentsHeaderText.Text = LocalizationService.Instance.GetString("Documents");

                if (TitleTextBox != null)
                    TitleTextBox.PlaceholderText = LocalizationService.Instance.GetString("DocumentTitle");

                System.Diagnostics.Debug.WriteLine("Placeholders updated");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating placeholders: {ex.Message}");
            }
        }

        private void NotifyAllBindingsChanged()
        {
            // Forzar actualización de todos los bindings que usan el LocalizationConverter
            // Esto se podría mejorar con un sistema de binding más específico
        }

        private void SetupAutoSave()
  {
       _autoSaveTimer = new DispatcherTimer();
        _autoSaveTimer.Interval = TimeSpan.FromMinutes(2);
     _autoSaveTimer.Tick += AutoSave_Tick;
 _autoSaveTimer.Start();
        }

  private void SetupTextSnippets()
        {
       // Fragmentos existentes
            _textSnippets["//date"] = DateTime.Now.ToString("yyyy-MM-dd");
            _textSnippets["//time"] = DateTime.Now.ToString("HH:mm");
 _textSnippets["//datetime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
    _textSnippets["//meeting"] = $"# Meeting Notes - {DateTime.Now:yyyy-MM-dd}\n\n## Attendees\n- \n\n## Agenda\n1. \n\n## Action Items\n- [ ] ";
       _textSnippets["//todo"] = "## TODO List\n\n- [ ] Task 1\n- [ ] Task 2\n- [ ] Task 3";
            _textSnippets["//code"] = "```\n// Your code here\n```";
  _textSnippets["//table"] = "| Column 1 | Column 2 | Column 3 |\n|----------|----------|----------|\n| Data 1   | Data 2   | Data 3   |";
       _textSnippets["//quote"] = "> This is a quote\n> \n> *Author*";
   
            // Nuevos fragmentos
         _textSnippets["//journal"] = $"# Daily Journal - {DateTime.Now:yyyy-MM-dd}\n\n## Morning Thoughts\n\n## Goals for Today\n- [ ] \n\n## Evening Reflection\n\n";
        _textSnippets["//research"] = "## Research Notes\n\n### Source:\n\n### Key Points:\n- \n\n### Questions:\n- \n\n### Follow-up:\n- \n\n";
     _textSnippets["//outline"] = "# Document Outline\n\n## I. Introduction\n   A. \n   B. \n\n## II. Main Content\n   A. \n   B. \n\n## III. Conclusion\n   A. \n   B. \n\n";
 }

        private void SetupCustomShortcuts()
    {
            // Atajos existentes
          _customShortcuts["Ctrl+Shift+D"] = () => InsertDateTime();
  _customShortcuts["Ctrl+Shift+T"] = () => InsertTodoTemplate();
  _customShortcuts["Ctrl+Shift+M"] = () => InsertMeetingTemplate();
   _customShortcuts["Ctrl+Shift+C"] = () => InsertCodeBlock();
            _customShortcuts["Ctrl+H"] = () => ShowFindReplaceDialog();
        _customShortcuts["F3"] = () => FindNext();
            _customShortcuts["Ctrl+L"] = () => SelectCurrentLine();
            _customShortcuts["Ctrl+D"] = () => DuplicateCurrentLine();
 
        // Configurar atajos avanzados
            SetupAdvancedShortcuts();
      }

     private void SetupAdvancedShortcuts()
        {
            // Añadir más atajos personalizados
     _customShortcuts["Ctrl+Shift+V"] = () => StartVoiceRecognition();
   _customShortcuts["Ctrl+Shift+R"] = () => ReadSelectedText();
    _customShortcuts["F11"] = () => ToggleFocusMode();
            _customShortcuts["Ctrl+Shift+P"] = () => StartPomodoroSession();
      _customShortcuts["Ctrl+Shift+F"] = () => ShowAdvancedSearch();
    _customShortcuts["Ctrl+Plus"] = () => IncreaseFontSize();
            _customShortcuts["Ctrl+Minus"] = () => DecreaseFontSize();
     _customShortcuts["Ctrl+Shift+L"] = () => ToggleDarkMode();
        }

   private void SetupTypingTimer()
        {
            _typingTimer = new DispatcherTimer();
  _typingTimer.Interval = TimeSpan.FromMilliseconds(500);
            _typingTimer.Tick += TypingTimer_Tick;
    }

     private void SetupPomodoroTimer()
   {
            _pomodoroTimer = new DispatcherTimer();
        _pomodoroTimer.Interval = TimeSpan.FromSeconds(1);
            _pomodoroTimer.Tick += PomodoroTimer_Tick;
        }

        private void SetupWordCountTimer()
        {
            _wordCountTimer = new DispatcherTimer();
        _wordCountTimer.Interval = TimeSpan.FromSeconds(30);
 _wordCountTimer.Tick += (s, e) => UpdateWordCount();
          _wordCountTimer.Start();
        }

 private void SetupWritingStats()
   {
     LoadWritingStats();
            UpdateWordCount();
        }

        private void LoadUserPreferences()
        {
         // En una implementación real, cargaría preferencias desde archivo
            _originalFontSize = 14;
         _darkModeEnabled = false;
        }

        #endregion

      #region Funcionalidades de Voz

        private async void InitializeVoiceFeatures()
        {
            try
            {
         // Inicializar reconocimiento de voz
                _speechRecognizer = new SpeechRecognizer(SpeechRecognizer.SystemSpeechLanguage);
     
             // Configurar comandos de voz
     var listCommands = new List<string>
        {
         "nueva línea", "nuevo párrafo", "punto", "coma", "punto y coma",
              "abrir paréntesis", "cerrar paréntesis", "signo de interrogación",
        "mayúscula", "minúscula", "borrar palabra", "borrar línea",
            "guardar documento", "nuevo documento", "buscar", "reemplazar"
    };

  var listConstraint = new SpeechRecognitionListConstraint(listCommands, "commands");
           _speechRecognizer.Constraints.Add(listConstraint);
     await _speechRecognizer.CompileConstraintsAsync();

                // Inicializar síntesis de voz
    _speechSynthesizer = new SpeechSynthesizer();
       
     System.Diagnostics.Debug.WriteLine("Voice features initialized successfully");
     }
            catch (Exception ex)
      {
          System.Diagnostics.Debug.WriteLine($"Error initializing voice features: {ex.Message}");
            }
        }

 private async void StartVoiceRecognition()
        {
   if (_speechRecognizer == null || _isListening) return;

  try
   {
     _isListening = true;
                await ShowVoiceRecognitionUI(true);
           
       var result = await _speechRecognizer.RecognizeAsync();
         
     if (result.Status == SpeechRecognitionResultStatus.Success)
          {
          ProcessVoiceInput(result.Text);
          }
  }
            catch (Exception ex)
            {
              System.Diagnostics.Debug.WriteLine($"Voice recognition error: {ex.Message}");
}
        finally
        {
      _isListening = false;
 await ShowVoiceRecognitionUI(false);
            }
     }

  private void ProcessVoiceInput(string voiceText)
        {
 if (ViewModel.SelectedDocument == null) return;

 SaveToUndoStack();
            _lastVoiceInput = voiceText;

            var processedText = ProcessVoiceCommands(voiceText);
         
  if (!string.IsNullOrEmpty(processedText))
   {
    var currentContent = ViewModel.SelectedDocument.Content ?? "";
        var newContent = currentContent + processedText;
       
           _isUpdatingContent = true;
        ViewModel.SelectedDocument.Content = newContent;
         
       // Actualizar el TextEditor correctamente
    TextEditor.Text = newContent;
        
 UpdatePreviewContentImmediate(newContent);
     _isUpdatingContent = false;
          
     // Actualizar estadísticas
           UpdateWordCount();
      }
        }

        private string ProcessVoiceCommands(string voiceText)
  {
            var text = voiceText.ToLower();
            
  // Comandos de puntuación
            text = text.Replace("nueva línea", "\n");
    text = text.Replace("nuevo párrafo", "\n\n");
            text = text.Replace("punto", ".");
            text = text.Replace("coma", ",");
      text = text.Replace("punto y coma", ";");
            text = text.Replace("abrir paréntesis", "(");
          text = text.Replace("cerrar paréntesis", ")");
            text = text.Replace("signo de interrogación", "?");
     
        // Comandos de acción
   if (text.Contains("guardar documento"))
            {
        ViewModel.SaveCurrentDocumentCommand.Execute(null);
    return "";
            }
        
            if (text.Contains("nuevo documento"))
     {
     ViewModel.CreateNewDocumentCommand.Execute(null);
return "";
            }
            
  return text;
   }

  private async Task ShowVoiceRecognitionUI(bool isListening)
  {
      try
      {
          var message = isListening ? LocalizationService.Instance.GetString("Listening") : LocalizationService.Instance.GetString("VoiceRecognitionStopped");

          var dialog = new ContentDialog
          {
              Title = isListening ? LocalizationService.Instance.GetString("VoiceRecognition") : LocalizationService.Instance.GetString("VoiceRecognitionStopped"),
              Content = message,
              CloseButtonText = isListening ? LocalizationService.Instance.GetString("Cancel") : LocalizationService.Instance.GetString("OK"),
              XamlRoot = this.Content.XamlRoot
          };

          if (isListening)
          {
              // Auto-cerrar después de unos segundos para reconocimiento
              var timer = new DispatcherTimer();
              timer.Interval = TimeSpan.FromSeconds(3);
              timer.Tick += (s, e) => 
              {
                  timer.Stop();
                  dialog.Hide();
              };
              timer.Start();
          }

          await dialog.ShowAsync();
      }
      catch (Exception ex)
      {
          System.Diagnostics.Debug.WriteLine($"Error showing voice UI: {ex.Message}");
      }
  }

        private async void ReadSelectedText()
  {
    if (_isSpeaking || ViewModel.SelectedDocument == null) return;

         try
  {
       _isSpeaking = true;
      
          var textToRead = GetSelectedText() ?? ViewModel.SelectedDocument.Content ?? "";
   
     if (string.IsNullOrEmpty(textToRead))
                {
  _isSpeaking = false;
           return;
   }

  // Limpiar markdown para mejor lectura
  textToRead = CleanTextForSpeech(textToRead);
         
       // Crear stream de audio y reproducir
           var stream = await _speechSynthesizer.SynthesizeTextToStreamAsync(textToRead);
    
             System.Diagnostics.Debug.WriteLine($"Speaking: {textToRead.Substring(0, Math.Min(50, textToRead.Length))}...");
                
        // Simular duración basada en longitud del texto
    await Task.Delay(Math.Min(textToRead.Length * 50, 10000));
       _isSpeaking = false;
         
         System.Diagnostics.Debug.WriteLine("Speech completed");
    }
            catch (Exception ex)
            {
         System.Diagnostics.Debug.WriteLine($"Text-to-speech error: {ex.Message}");
     _isSpeaking = false;
      }
        }

        private string CleanTextForSpeech(string text)
 {
            // Remover markdown y caracteres especiales para mejor pronunciación
  text = Regex.Replace(text, @"#{1,6}\s*", ""); // Headers
      text = Regex.Replace(text, @"\*{1,2}(.*?)\*{1,2}", "$1"); // Bold/Italic
   text = Regex.Replace(text, @"`(.*?)`", "$1"); // Code
          text = Regex.Replace(text, @"\[(.*?)\]\(.*?\)", "$1"); // Links
 text = text.Replace("\n", ". "); // Line breaks
   
          return text;
        }

        private string GetSelectedText()
        {
            try
            {
    if (!string.IsNullOrEmpty(TextEditor?.SelectedText))
          {
     return TextEditor.SelectedText;
         }
          
 if (!string.IsNullOrEmpty(SplitModeEditor?.SelectedText))
          {
   return SplitModeEditor.SelectedText;
       }
     }
  catch (Exception ex)
      {
         System.Diagnostics.Debug.WriteLine($"Error getting selected text: {ex.Message}");
  }
            
        return null;
        }

        #endregion

        #region Funciones de Edición Avanzadas

        private void ChangeFontSize(double newSize)
        {
     try
            {
 // Cambiar tamaño de fuente en el editor actual
                if (TextEditor != null)
      {
         TextEditor.FontSize = newSize;
         }
  
                if (SplitModeEditor != null)
     {
      SplitModeEditor.FontSize = newSize;
             }
      
     // También actualizar TitleTextBox
       if (TitleTextBox != null)
              {
       TitleTextBox.FontSize = Math.Max(newSize + 4, 16); // Title slightly larger
      }
         
     System.Diagnostics.Debug.WriteLine($"Font size changed to: {newSize}");
     
                // Mostrar notificación del cambio
      ShowFontSizeNotification(newSize);
 }
            catch (Exception ex)
            {
             System.Diagnostics.Debug.WriteLine($"Error changing font size: {ex.Message}");
            }
        }

        private async void ShowFontSizeNotification(double fontSize)
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = LocalizationService.Instance.GetString("FontSize"),
                    Content = LocalizationService.Instance.GetString("CurrentSize", fontSize),
                    CloseButtonText = LocalizationService.Instance.GetString("OK"),
                    XamlRoot = this.Content.XamlRoot
                };

                // Auto-cerrar después de 1 segundo
                var timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += (s, e) => 
                {
                    timer.Stop();
                    dialog.Hide();
                };
                timer.Start();

                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing font size notification: {ex.Message}");
            }
        }

    private void IncreaseFontSize()
        {
      _originalFontSize = Math.Min(_originalFontSize + 2, 32);
    ChangeFontSize(_originalFontSize);
        }

   private void DecreaseFontSize()
 {
       _originalFontSize = Math.Max(_originalFontSize - 2, 8);
  ChangeFontSize(_originalFontSize);
        }

        private void InsertTextAtCursor(string text)
        {
            if (ViewModel.SelectedDocument == null) return;

            SaveToUndoStack();
            
            try
            {
  // Obtener el editor activo
        var activeEditor = GetActiveEditor();
  if (activeEditor != null)
    {
  var selectionStart = activeEditor.SelectionStart;
      
     // Insertar en la posición del cursor
      activeEditor.InsertTextAtSelection(text);
   
// Actualizar el documento
    var newContent = activeEditor.Text;
          _isUpdatingContent = true;
        ViewModel.SelectedDocument.Content = newContent;
      UpdatePreviewContentImmediate(newContent);
        _isUpdatingContent = false;
     
    // Posicionar cursor después del texto insertado
         activeEditor.SelectionStart = selectionStart + text.Length;
        }
            }
      catch (Exception ex)
    {
                System.Diagnostics.Debug.WriteLine($"Error inserting text: {ex.Message}");
   }
        }

        private Controls.RichTextEditor GetActiveEditor()
        {
       // Determinar qué editor está activo basado en el modo de vista
  return ViewModel.CurrentViewMode switch
   {
      ViewMode.Edit => TextEditor,
ViewMode.Split => SplitModeEditor ?? TextEditor,
  _ => TextEditor
     };
}

        private void ToggleFocusMode()
 {
            _isFocusMode = !_isFocusMode;
   ApplyFocusMode(_isFocusMode);
        }

        private void ApplyFocusMode(bool enabled)
        {
            if (enabled)
      {
    // Ocultar elementos de distracción
          if (MainNavigationView != null)
               MainNavigationView.IsPaneOpen = false;
           
         // Cambiar a modo oscuro si no está activado
   if (!_darkModeEnabled)
      {
         ToggleDarkMode();
    }
 
                // Aumentar tamaño de fuente
                ChangeFontSize(18);
     
     // Ocultar barra de herramientas secundaria
  HideNonEssentialUI(true);
    
                ShowFocusModeNotification(true);
 }
else
 {
                // Restaurar UI normal
      if (MainNavigationView != null)
       MainNavigationView.IsPaneOpen = true;
         
        // Restaurar tamaño de fuente
    ChangeFontSize(_originalFontSize);
      
         // Mostrar UI completa
      HideNonEssentialUI(false);
     
     ShowFocusModeNotification(false);
          }
       
 System.Diagnostics.Debug.WriteLine($"Focus mode: {(enabled ? "ON" : "OFF")}");
        }

    private async void ShowFocusModeNotification(bool enabled)
    {
        try
        {
            var title = enabled ? LocalizationService.Instance.GetString("FocusModeActivated") : LocalizationService.Instance.GetString("FocusModeDeactivated");
            var content = enabled 
                ? LocalizationService.Instance.GetString("FocusModeDescription")
                : LocalizationService.Instance.GetString("FocusModeRestored");

            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = LocalizationService.Instance.GetString("OK"),
                XamlRoot = this.Content.XamlRoot
            };

            // Auto-cerrar después de 2 segundos
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += (s, e) => 
            {
                timer.Stop();
                dialog.Hide();
            };
            timer.Start();

            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing focus mode notification: {ex.Message}");
        }
    }

        private void HideNonEssentialUI(bool hide)
        {
     var visibility = hide ? Visibility.Collapsed : Visibility.Visible;
            
       // Ocultar elementos no esenciales específicos
      try
            {
     // En una implementación real, ocultarías botones específicos de la toolbar
   System.Diagnostics.Debug.WriteLine($"Non-essential UI: {(hide ? "Hidden" : "Visible")}");
  }
            catch (Exception ex)
        {
      System.Diagnostics.Debug.WriteLine($"Error hiding UI elements: {ex.Message}");
            }
      }

      private void ToggleDarkMode()
        {
      _darkModeEnabled = !_darkModeEnabled;
     ApplyTheme(_darkModeEnabled);
 }

        private void ApplyTheme(bool darkMode)
        {
     var theme = darkMode ? ElementTheme.Dark : ElementTheme.Light;

         if (RootGrid != null)
    {
      RootGrid.RequestedTheme = theme;
   }
            
 System.Diagnostics.Debug.WriteLine($"Theme changed to: {(darkMode ? "Dark" : "Light")}");
        }

#endregion

        #region Funciones de Texto y Búsqueda

        private void SelectCurrentLine()
        {
            try
            {
       var activeEditor = GetActiveEditor();
      if (activeEditor != null)
       {
        var content = activeEditor.Text ?? "";
 var cursorPosition = activeEditor.SelectionStart;
         
 // Encontrar inicio y fin de la línea actual
       var lineStart = content.LastIndexOf('\n', Math.Max(0, cursorPosition - 1)) + 1;
      var lineEnd = content.IndexOf('\n', cursorPosition);
     if (lineEnd == -1) lineEnd = content.Length;

          // Seleccionar la línea completa
       activeEditor.SelectionStart = lineStart;
 activeEditor.SelectionLength = lineEnd - lineStart;
             activeEditor.SetFocus();
        
          System.Diagnostics.Debug.WriteLine($"Selected line from {lineStart} to {lineEnd}");
        }
   }
   catch (Exception ex)
            {
             System.Diagnostics.Debug.WriteLine($"Error selecting current line: {ex.Message}");
    }
      }

        private void DuplicateCurrentLine()
        {
          if (ViewModel.SelectedDocument == null) return;

  SaveToUndoStack();
  
        try
   {
     var activeEditor = GetActiveEditor();
       if (activeEditor != null)
         {
  var content = activeEditor.Text ?? "";
      var cursorPosition = activeEditor.SelectionStart;
         
     // Encontrar la línea actual
      var lineStart = content.LastIndexOf('\n', Math.Max(0, cursorPosition - 1)) + 1;
              var lineEnd = content.IndexOf('\n', cursorPosition);
         if (lineEnd == -1) lineEnd = content.Length;
                    
            var currentLine = content.Substring(lineStart, lineEnd - lineStart);
          var newContent = content.Insert(lineEnd, "\n" + currentLine);
   
            _isUpdatingContent = true;
    ViewModel.SelectedDocument.Content = newContent;
         activeEditor.Text = newContent;
           
         // Posicionar cursor en la línea duplicada
        activeEditor.SelectionStart = lineEnd + 1 + currentLine.Length;
        activeEditor.SetFocus();
   
       UpdatePreviewContentImmediate(newContent);
               _isUpdatingContent = false;
                 
System.Diagnostics.Debug.WriteLine($"Duplicated line: {currentLine}");
   }
            }
 catch (Exception ex)
     {
      System.Diagnostics.Debug.WriteLine($"Error duplicating line: {ex.Message}");
            }
        }

        private void FindNext(string searchText = "", bool caseSensitive = false)
        {
            try
            {
                if (ViewModel.SelectedDocument == null || string.IsNullOrEmpty(searchText)) return;

                var activeEditor = GetActiveEditor();
                if (activeEditor != null)
                {
                    var content = activeEditor.Text ?? "";
                    var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                    // Buscar desde la posición actual
                    var currentPosition = activeEditor.SelectionStart + activeEditor.SelectionLength;
                    var index = content.IndexOf(searchText, currentPosition, comparison);

                    // Si no se encuentra, buscar desde el principio
                    if (index == -1)
                    {
                        index = content.IndexOf(searchText, 0, comparison);
                    }

                    if (index >= 0)
                    {
                        activeEditor.SelectionStart = index;
                        activeEditor.SelectionLength = searchText.Length;
                        activeEditor.SetFocus();

                        System.Diagnostics.Debug.WriteLine($"Found '{searchText}' at position {index}");
                        ShowSearchResultNotification(LocalizationService.Instance.GetString("FoundAtPosition", index));
                    }
                    else
                    {
                        ShowSearchResultNotification(LocalizationService.Instance.GetString("NotFound", searchText));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in FindNext: {ex.Message}");
            }
        }

      private async void ShowSearchResultNotification(string message)
      {
          try
          {
              var dialog = new ContentDialog
              {
                  Title = LocalizationService.Instance.GetString("SearchResult"),
                  Content = message,
                  CloseButtonText = LocalizationService.Instance.GetString("OK"),
                  XamlRoot = this.Content.XamlRoot
              };

              // Auto-cerrar después de 2 segundos
              var timer = new DispatcherTimer();
              timer.Interval = TimeSpan.FromSeconds(2);
              timer.Tick += (s, e) => 
              {
                  timer.Stop();
                  dialog.Hide();
              };
              timer.Start();

              await dialog.ShowAsync();
          }
          catch (Exception ex)
          {
              System.Diagnostics.Debug.WriteLine($"Error showing search notification: {ex.Message}");
          }
      }

        #endregion

        // === CONTINÚA CON MÉTODOS RESTANTES ===
   
        #region Funciones Esenciales Faltantes

        private void SaveToUndoStack()
 {
            if (ViewModel.SelectedDocument?.Content != null)
   {
      _undoStack.Push(ViewModel.SelectedDocument.Content);
     _redoStack.Clear();
          
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

        private void UpdateWordCount()
        {
         if (ViewModel.SelectedDocument == null) return;

  var content = ViewModel.SelectedDocument.Content ?? "";
            var currentWordCount = CountWords(content);
          
            var wordsAdded = Math.Max(0, currentWordCount - _sessionWordCount);
            _sessionWordCount = currentWordCount;
          _dailyWordCount += wordsAdded;
         
      var today = DateTime.Today;
         _writingStats[today] = _dailyWordCount;
            
   SaveWritingStats();
  UpdateStatsDisplay();
   }

        private int CountWords(string text)
    {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            
      return text.Split(new char[] { ' ', '\n', '\r', '\t' }, 
        StringSplitOptions.RemoveEmptyEntries).Length;
  }

        private void UpdateStatsDisplay()
        {
        System.Diagnostics.Debug.WriteLine($"Session words: {_sessionWordCount}, Daily words: {_dailyWordCount}");
        }

        private void LoadWritingStats()
        {
            var today = DateTime.Today;
          _dailyWordCount = _writingStats.ContainsKey(today) ? _writingStats[today] : 0;
        }

        private void SaveWritingStats()
     {
    System.Diagnostics.Debug.WriteLine("Writing stats saved");
   }

        private void SaveUserPreferences()
      {
          System.Diagnostics.Debug.WriteLine("User preferences saved");
        }

        private void ProcessTextSnippets(string text)
        {
      try
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
                 if (SplitModeEditor != null)
       {
           SplitModeEditor.Text = newContent;
          }
     
        UpdatePreviewContentImmediate(newContent);
      _isUpdatingContent = false;

           System.Diagnostics.Debug.WriteLine($"Processed snippet: {snippet.Key} -> {snippet.Value.Substring(0, Math.Min(30, snippet.Value.Length))}...");
      break;
      }
        }
 }
      }
        catch (Exception ex)
    {
       System.Diagnostics.Debug.WriteLine($"Error processing text snippets: {ex.Message}");
      }
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
    AutoFormatContent();
        }

        private void ShowAutoSaveNotification()
        {
     System.Diagnostics.Debug.WriteLine("Document auto-saved");
        }

        private void AutoFormatContent()
        {
            if (ViewModel.SelectedDocument == null) return;

            var content = ViewModel.SelectedDocument.Content ?? "";
          var formatted = content;

         formatted = Regex.Replace(formatted, @"^(\s*)-\s*([^\s])", "$1- $2", RegexOptions.Multiline);
       formatted = Regex.Replace(formatted, @"(^#{1,6}.*)\n([^#\n])", "$1\n\n$2", RegexOptions.Multiline);

      if (formatted != content)
            {
    _isUpdatingContent = true;
    ViewModel.SelectedDocument.Content = formatted;
    TextEditor.Text = formatted;
      _isUpdatingContent = false;
}
        }

     private void StartPomodoroSession()
        {
    if (_pomodoroRunning) return;
            
            _pomodoroMinutes = 25;
_pomodoroSeconds = 0;
            _pomodoroRunning = true;
        _pomodoroTimer.Start();
    
System.Diagnostics.Debug.WriteLine("Pomodoro session started: 25 minutes");
        }

        private void PomodoroTimer_Tick(object sender, object e)
        {
            if (_pomodoroSeconds > 0)
      {
                _pomodoroSeconds--;
            }
            else if (_pomodoroMinutes > 0)
            {
      _pomodoroMinutes--;
     _pomodoroSeconds = 59;
   }
            else
            {
  _pomodoroTimer.Stop();
   _pomodoroRunning = false;
            OnPomodoroCompleted();
            }
  
            UpdatePomodoroDisplay();
        }

        private void OnPomodoroCompleted()
     {
    System.Diagnostics.Debug.WriteLine("Pomodoro session completed!");
          ShowPomodoroCompletionDialog();
        }

        private async void ShowPomodoroCompletionDialog()
        {
            var dialog = new ContentDialog
            {
                Title = LocalizationService.Instance.GetString("PomodoroCompleted"),
                Content = LocalizationService.Instance.GetString("PomodoroCompletedDescription"),
                PrimaryButtonText = LocalizationService.Instance.GetString("StartBreak"),
                SecondaryButtonText = LocalizationService.Instance.GetString("ContinueWriting"),
                CloseButtonText = LocalizationService.Instance.GetString("Close"),
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                StartBreakTimer();
            }
        }

     private void StartBreakTimer()
        {
            _pomodoroMinutes = 5;
            _pomodoroSeconds = 0;
            _pomodoroRunning = true;
            _pomodoroTimer.Start();
    
     System.Diagnostics.Debug.WriteLine("Break timer started: 5 minutes");
        }

 private void UpdatePomodoroDisplay()
        {
            var timeDisplay = $"{_pomodoroMinutes:D2}:{_pomodoroSeconds:D2}";
            System.Diagnostics.Debug.WriteLine($"Pomodoro: {timeDisplay}");
        }

        private async void ShowAdvancedSearch()
        {
            var dialog = new ContentDialog
            {
                Title = LocalizationService.Instance.GetString("AdvancedSearch"),
                PrimaryButtonText = LocalizationService.Instance.GetString("Search"),
                CloseButtonText = LocalizationService.Instance.GetString("Cancel"),
                XamlRoot = this.Content.XamlRoot
            };

            var stackPanel = new StackPanel { Spacing = 12 };

            var searchBox = new TextBox
            {
                PlaceholderText = LocalizationService.Instance.GetString("SearchFor"),
                Header = LocalizationService.Instance.GetString("SearchFor")
            };

            var searchInCombo = new ComboBox
            {
                Header = LocalizationService.Instance.GetString("SearchIn"),
                Items = { 
                    LocalizationService.Instance.GetString("CurrentDocument"), 
                    LocalizationService.Instance.GetString("AllDocuments"), 
                    LocalizationService.Instance.GetString("SelectedDocuments") 
                },
                SelectedIndex = 0
            };

            var regexCheck = new CheckBox
            {
                Content = LocalizationService.Instance.GetString("UseRegularExpressions"),
                IsChecked = false
            };

            var wholeWordsCheck = new CheckBox
            {
                Content = LocalizationService.Instance.GetString("WholeWordsOnly"),
                IsChecked = false
            };

            stackPanel.Children.Add(searchBox);
            stackPanel.Children.Add(searchInCombo);
            stackPanel.Children.Add(regexCheck);
            stackPanel.Children.Add(wholeWordsCheck);

            dialog.Content = stackPanel;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(searchBox.Text))
            {
                PerformAdvancedSearch(searchBox.Text, searchInCombo.SelectedIndex, 
                    regexCheck.IsChecked ?? false, 
                    wholeWordsCheck.IsChecked ?? false);
            }
        }

        private void PerformAdvancedSearch(string searchTerm, int searchScope, bool useRegex, bool wholeWords)
        {
    if (!_recentSearches.Contains(searchTerm))
            {
           _recentSearches.Insert(0, searchTerm);
                if (_recentSearches.Count > 10)
               _recentSearches.RemoveAt(10);
            }
     
            System.Diagnostics.Debug.WriteLine($"Advanced search: '{searchTerm}', Scope: {searchScope}, Regex: {useRegex}, Whole words: {wholeWords}");
        }

        private async void ShowFindReplaceDialog()
        {
            var dialog = new ContentDialog
            {
                Title = LocalizationService.Instance.GetString("FindAndReplace"),
                PrimaryButtonText = LocalizationService.Instance.GetString("ReplaceAll"),
                SecondaryButtonText = LocalizationService.Instance.GetString("FindNext"),
                CloseButtonText = LocalizationService.Instance.GetString("Close"),
                XamlRoot = this.Content.XamlRoot
            };

            var stackPanel = new StackPanel { Spacing = 12 };

            var findBox = new TextBox
            {
                PlaceholderText = LocalizationService.Instance.GetString("Find"),
                Header = LocalizationService.Instance.GetString("Find")
            };

            var replaceBox = new TextBox
            {
                PlaceholderText = LocalizationService.Instance.GetString("ReplaceWith"),
                Header = LocalizationService.Instance.GetString("ReplaceWith")
            };

            var caseSensitiveCheck = new CheckBox
            {
                Content = LocalizationService.Instance.GetString("CaseSensitive"),
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

    private void ReplaceAll(string findText, string replaceText, bool caseSensitive)
 {
   if (ViewModel.SelectedDocument == null || string.IsNullOrEmpty(findText)) return;

            SaveToUndoStack();
       
   try
    {
            var content = ViewModel.SelectedDocument.Content ?? "";
      var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
         
 string newContent;
    if (caseSensitive)
{
   newContent = content.Replace(findText, replaceText);
       }
    else
    {
            newContent = Regex.Replace(content, Regex.Escape(findText), replaceText, RegexOptions.IgnoreCase);
       }

      var originalMatches = Regex.Matches(content, Regex.Escape(findText), 
            caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase).Count;

  _isUpdatingContent = true;
           ViewModel.SelectedDocument.Content = newContent;
        
          TextEditor.Text = newContent;
 if (SplitModeEditor != null)
{
          SplitModeEditor.Text = newContent;
  }

      UpdatePreviewContentImmediate(newContent);
            _isUpdatingContent = false;
       
  ShowSearchResultNotification(LocalizationService.Instance.GetString("ReplacementsCompleted", originalMatches));
  System.Diagnostics.Debug.WriteLine($"Replaced {originalMatches} occurrences of '{findText}' with '{replaceText}'");
            }
            catch (Exception ex)
            {
    System.Diagnostics.Debug.WriteLine($"Error in ReplaceAll: {ex.Message}");
            }
        }

    #endregion

        // === MÉTODOS DE LA UI EXISTENTE ===

        #region Button Events
        private void CreateNewDocument_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CreateNewDocumentCommand.Execute(null);
        }

        private void SaveDocument_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SaveCurrentDocumentCommand.Execute(null);
        }

        private void ChangeLanguage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && menuItem.Tag is string languageCode)
            {
                try
                {
                    LocalizationService.Instance.SetLanguage(languageCode);
                    ShowLanguageChangedNotification(languageCode);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error changing language: {ex.Message}");
                }
            }
        }

        private async void ShowLanguageChangedNotification(string languageCode)
        {
            try
            {
                var languageName = LocalizationService.Instance.SupportedLanguages.TryGetValue(languageCode, out var name) ? name : languageCode;
                var message = LocalizationService.Instance.GetString("LanguageChanged", languageName);

                // Mostrar el mensaje de forma más prominente
                var infoBar = new InfoBar
                {
                    Title = "✓ " + LocalizationService.Instance.GetString("Language"),
                    Message = message,
                    Severity = InfoBarSeverity.Success,
                    IsOpen = true
                };

                // Si existe un contenedor para InfoBars, úsalo, si no, usa un diálogo
                var dialog = new ContentDialog
                {
                    Title = "✓ " + LocalizationService.Instance.GetString("Language"),
                    Content = message,
                    CloseButtonText = LocalizationService.Instance.GetString("OK"),
                    XamlRoot = this.Content.XamlRoot
                };

                // Auto-cerrar después de 1.5 segundos
                var timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(1500);
                timer.Tick += (s, e) => 
                {
                    timer.Stop();
                    dialog.Hide();
                };
                timer.Start();

                await dialog.ShowAsync();

                // Después de cerrar el diálogo, forzar actualización completa
                UpdateAllLocalizedTexts();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing language notification: {ex.Message}");
            }
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

        private async void SendEmail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewModel.SelectedDocument == null)
                {
                    var noDocDialog = new ContentDialog
                    {
                        Title = "⚠️ " + LocalizationService.Instance.GetString("Warning"),
                        Content = LocalizationService.Instance.GetString("NoDocumentSelected"),
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await noDocDialog.ShowAsync();
                    return;
                }

                var emailDialog = new Dialogs.SendEmailDialog(ViewModel.SelectedDocument);
                emailDialog.XamlRoot = this.Content.XamlRoot;

                // El diálogo ahora maneja el envío internamente
                await emailDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;

                var errorDialog = new ContentDialog
                {
                    Title = "❌ Error",
                    Content = $"Error: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await errorDialog.ShowAsync();

                System.Diagnostics.Debug.WriteLine($"Error in SendEmail_Click: {ex.Message}");
            }
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

        private async void PrintDocument_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewModel.SelectedDocument == null)
                {
                    var noDocDialog = new ContentDialog
                    {
                        Title = "⚠️ " + LocalizationService.Instance.GetString("Warning"),
                        Content = LocalizationService.Instance.GetString("NoDocumentSelected"),
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await noDocDialog.ShowAsync();
                    return;
                }

                var printService = new Services.PrintService();
                var success = await printService.PrintDocumentAsync(ViewModel.SelectedDocument);

                if (!success)
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "❌ Error",
                        Content = LocalizationService.Instance.GetString("PrintError"),
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error printing document: {ex.Message}");

                var errorDialog = new ContentDialog
                {
                    Title = "❌ Error",
                    Content = $"Error: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        private async void PrintDocumentFromMenu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is MenuFlyoutItem menuItem && menuItem.CommandParameter is Models.Document document)
                {
                    var printService = new Services.PrintService();
                    var success = await printService.PrintDocumentAsync(document);

                    if (!success)
                    {
                        var errorDialog = new ContentDialog
                        {
                            Title = "❌ Error",
                            Content = LocalizationService.Instance.GetString("PrintError"),
                            CloseButtonText = "OK",
                            XamlRoot = this.Content.XamlRoot
                        };
                        await errorDialog.ShowAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error printing document from menu: {ex.Message}");

                var errorDialog = new ContentDialog
                {
                    Title = "❌ Error",
                    Content = $"Error: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await errorDialog.ShowAsync();
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
 if (GitHubActionsPanel?.Visibility == Visibility.Visible)
    {
        GitHubActionsPanel.Visibility = Visibility.Collapsed;
       if (GitHubToggleButton != null)
      GitHubToggleButton.Content = new FontIcon { Glyph = "\uE70D", FontSize = 12 };
   }
  else
       {
    if (GitHubActionsPanel != null)
            GitHubActionsPanel.Visibility = Visibility.Visible;
    if (GitHubToggleButton != null)
              GitHubToggleButton.Content = new FontIcon { Glyph = "\uE70E", FontSize = 12 };
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
       ShowNoDocumentSelectedMessage();
     }
   }

      private async void ShowNoDocumentSelectedMessage()
      {
          try
          {
              var dialog = new ContentDialog
              {
                  Title = LocalizationService.Instance.GetString("NoDocumentSelected"),
                  Content = LocalizationService.Instance.GetString("NoDocumentSelectedDescription"),
                  CloseButtonText = LocalizationService.Instance.GetString("OK"),
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
 var timeSinceLastTyping = DateTime.Now - _lastTypingTime;
      if (timeSinceLastTyping.TotalSeconds > 2)
      {
         SaveToUndoStack();
        }
     _lastTypingTime = DateTime.Now;

ViewModel.SelectedDocument.Content = newText;
        ViewModel.SelectedDocument.ModifiedAt = DateTime.Now;
              
     if (sender == TextEditor && SplitModeEditor != null && 
        ViewModel.CurrentViewMode == ViewMode.Split)
             {
       _isUpdatingContent = true;
        SplitModeEditor.Text = newText;
         _isUpdatingContent = false;
                }
    else if (sender == SplitModeEditor && TextEditor != null)
      {
               _isUpdatingContent = true;
             TextEditor.Text = newText;
         _isUpdatingContent = false;
      }
     
     UpdatePreviewContentImmediate(newText);
        UpdateSplitModeContentImmediate(newText);
        UpdateDocumentIndex();

   ProcessTextSnippets(newText);

                _typingTimer.Stop();
    _typingTimer.Start();
       
        UpdateWordCount();
   }
   }

        private void TextEditor_DrawingModeChanged(object? sender, bool isDrawingMode)
        {
 System.Diagnostics.Debug.WriteLine($"Drawing mode changed: {isDrawingMode}");
   }

private void TextEditor_RequestFreeDrawing(object? sender, EventArgs e)
     {
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
   
_undoStack.Clear();
     _redoStack.Clear();
 _lastSavedContent = ViewModel.SelectedDocument?.Content ?? "";
    _sessionWordCount = 0;
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

        private void MainWindow_KeyDown(object sender, KeyRoutedEventArgs e)
        {
         var key = e.Key.ToString();
          var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
  var shift = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            string shortcut = "";
  if (ctrl && shift) shortcut = $"Ctrl+Shift+{key}";
  else if (ctrl) shortcut = $"Ctrl+{key}";
        else if (key == "F3") shortcut = "F3";
else if (key == "F11") shortcut = "F11";

if (_customShortcuts.ContainsKey(shortcut))
            {
        _customShortcuts[shortcut].Invoke();
     e.Handled = true;
            }
        
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
        #endregion

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
          
   _isUpdatingContent = true;
         
     if (TextEditor != null)
             {
        TextEditor.Text = newText;
       }
                
    _isUpdatingContent = false;
          
   UpdatePreviewContentImmediate(newText);
         UpdateDocumentIndex();
          
         UpdateWordCount();
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
                    if (GitHubStatusIndicator != null)
                        GitHubStatusIndicator.Fill = new SolidColorBrush(Microsoft.UI.Colors.Green);
                    if (GitHubStatusText != null)
                        GitHubStatusText.Text = LocalizationService.Instance.GetString("Connected");
                    if (SetupGitHubButton != null)
                        SetupGitHubButton.Visibility = Visibility.Collapsed;
                    if (GitHubActionsPanel != null)
                        GitHubActionsPanel.Visibility = Visibility.Visible;

                    if (QuickGitHubUploadButton != null)
                    {
                        QuickGitHubUploadButton.Opacity = 1.0;
                        QuickGitHubUploadButton.IsEnabled = true;
                    }
                }
                else
                {
                    if (GitHubStatusIndicator != null)
                        GitHubStatusIndicator.Fill = new SolidColorBrush(Microsoft.UI.Colors.Red);
                    if (GitHubStatusText != null)
                        GitHubStatusText.Text = LocalizationService.Instance.GetString("NotConnected");
                    if (SetupGitHubButton != null)
                        SetupGitHubButton.Visibility = Visibility.Visible;
                    if (GitHubActionsPanel != null)
                        GitHubActionsPanel.Visibility = Visibility.Collapsed;

                    if (QuickGitHubUploadButton != null)
                    {
                        QuickGitHubUploadButton.Opacity = 0.5;
                        QuickGitHubUploadButton.IsEnabled = false;
                    }
                }

                // Asegurar que los textos de los botones estén actualizados
                UpdateGitHubTexts();
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
    TextEditor.SetFocus();
        }

    private async void ShowDocumentStats()
    {
        if (ViewModel.SelectedDocument == null) return;

        try
        {
            var content = ViewModel.SelectedDocument.Content ?? "";
            var words = content.Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            var characters = content.Length;
            var charactersNoSpaces = content.Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", "").Length;
            var lines = content.Split('\n').Length;
            var paragraphs = content.Split(new string[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries).Length;

            var sentences = content.Split(new char[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries).Length;
            var avgWordsPerSentence = sentences > 0 ? Math.Round((double)words / sentences, 1) : 0;
            var readingTime = Math.Ceiling(words / 200.0);
            var speakingTime = Math.Ceiling(words / 150.0);

            var statsText = LocalizationService.Instance.GetString("DocumentStatistics") + "\n\n" +
                LocalizationService.Instance.GetString("Content") + "\n" +
                LocalizationService.Instance.GetString("Words", words) + "\n" +
                LocalizationService.Instance.GetString("Characters", characters) + "\n" +
                LocalizationService.Instance.GetString("CharactersNoSpaces", charactersNoSpaces) + "\n" +
                LocalizationService.Instance.GetString("Lines", lines) + "\n" +
                LocalizationService.Instance.GetString("Paragraphs", paragraphs) + "\n" +
                LocalizationService.Instance.GetString("Sentences", sentences) + "\n" +
                LocalizationService.Instance.GetString("AvgWordsPerSentence", avgWordsPerSentence) + "\n\n" +
                LocalizationService.Instance.GetString("EstimatedTime") + "\n" +
                LocalizationService.Instance.GetString("ReadingTime", readingTime) + "\n" +
                LocalizationService.Instance.GetString("SpeakingTime", speakingTime) + "\n\n" +
                LocalizationService.Instance.GetString("SessionStats") + "\n" +
                LocalizationService.Instance.GetString("WordsWrittenToday", _dailyWordCount) + "\n" +
                LocalizationService.Instance.GetString("WordsInSession", _sessionWordCount);

            var dialog = new ContentDialog
            {
                Title = LocalizationService.Instance.GetString("CompleteStatistics"),
                Content = new ScrollViewer
                {
                    Content = new TextBlock 
                    { 
                        Text = statsText, 
                        TextWrapping = TextWrapping.Wrap,
                        FontFamily = new FontFamily("Consolas")
                    },
                    MaxHeight = 400
                },
                CloseButtonText = LocalizationService.Instance.GetString("Close"),
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing document stats: {ex.Message}");
        }
    }

        // === FUNCIONES DE PLANTILLAS FALTANTES ===
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

        public void CleanupResources()
        {
            try
            {
       _speechRecognizer?.Dispose();
         _speechSynthesizer?.Dispose();
        _autoSaveTimer?.Stop();
    _pomodoroTimer?.Stop();
       _wordCountTimer?.Stop();
          _typingTimer?.Stop();

SaveUserPreferences();
      SaveWritingStats();

      System.Diagnostics.Debug.WriteLine("Resources cleaned up successfully");
   }
 catch (Exception ex)
   {
      System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
       }
   }

        #region Advanced Features Event Handlers

        // ============================================
        // 🔄 VERSION HISTORY
        // ============================================

        private async void VersionHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewModel.SelectedDocument == null)
                {
                    await ShowNoDocumentSelectedDialog();
                    return;
                }

                var dialog = new Dialogs.VersionHistoryDialog(
                    ViewModel.SelectedDocument,
                    ViewModel.DocumentService.VersionHistory
                );
                dialog.XamlRoot = this.Content.XamlRoot;

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary && dialog.SelectedVersionToRestore != null)
                {
                    await ViewModel.DocumentService.VersionHistory.RestoreVersionAsync(
                        ViewModel.SelectedDocument,
                        dialog.SelectedVersionToRestore
                    );

                    _isUpdatingContent = true;
                    TextEditor.Text = ViewModel.SelectedDocument.Content;
                    TitleTextBox.Text = ViewModel.SelectedDocument.Title;
                    _isUpdatingContent = false;

                    UpdatePreviewContent();
                    await ViewModel.DocumentService.SaveDocumentAsync(ViewModel.SelectedDocument);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error with version history: {ex.Message}");
            }
        }

        private async void VersionHistoryFromMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && menuItem.CommandParameter is Models.Document document)
            {
                var dialog = new Dialogs.VersionHistoryDialog(
                    document,
                    ViewModel.DocumentService.VersionHistory
                );
                dialog.XamlRoot = this.Content.XamlRoot;
                await dialog.ShowAsync();
            }
        }

        // ============================================
        // 🔐 ENCRYPTION
        // ============================================

        private async void EncryptDocument_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedDocument == null || ViewModel.SelectedDocument.IsEncrypted) return;

            var dialog = new Dialogs.EncryptionDialog(
                ViewModel.SelectedDocument,
                ViewModel.DocumentService.Encryption,
                Dialogs.EncryptionDialog.EncryptionMode.Encrypt
            );
            dialog.XamlRoot = this.Content.XamlRoot;

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                _isUpdatingContent = true;
                TextEditor.Text = ViewModel.SelectedDocument.Content;
                _isUpdatingContent = false;
                await ViewModel.DocumentService.SaveDocumentAsync(ViewModel.SelectedDocument);
            }
        }

        private async void UnlockDocument_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedDocument == null || !ViewModel.SelectedDocument.IsEncrypted) return;

            var dialog = new Dialogs.EncryptionDialog(
                ViewModel.SelectedDocument,
                ViewModel.DocumentService.Encryption,
                Dialogs.EncryptionDialog.EncryptionMode.Decrypt
            );
            dialog.XamlRoot = this.Content.XamlRoot;

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                _isUpdatingContent = true;
                TextEditor.Text = ViewModel.SelectedDocument.Content;
                _isUpdatingContent = false;
                UpdatePreviewContent();
            }
        }

        private async void LockDocument_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedDocument == null || !ViewModel.SelectedDocument.IsEncrypted) return;

            await ViewModel.DocumentService.Encryption.LockDocumentAsync(ViewModel.SelectedDocument, "");
            _isUpdatingContent = true;
            TextEditor.Text = ViewModel.SelectedDocument.Content;
            _isUpdatingContent = false;
            await ViewModel.DocumentService.SaveDocumentAsync(ViewModel.SelectedDocument);
        }

        // ============================================
        // ☁️ CLOUD SYNC
        // ============================================

        private async void CloudSync_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedDocument == null) return;

            var dialog = new Dialogs.CloudSyncDialog(
                ViewModel.SelectedDocument,
                ViewModel.DocumentService.CloudSync
            );
            dialog.XamlRoot = this.Content.XamlRoot;

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await ViewModel.DocumentService.SaveDocumentAsync(ViewModel.SelectedDocument);
            }
        }

        // ============================================
        // 📸 OCR
        // ============================================

        private async void OCR_Click(object sender, RoutedEventArgs e)
        {
            var ocrService = new OcrService();
            var dialog = new Dialogs.OcrDialog(ocrService);
            dialog.XamlRoot = this.Content.XamlRoot;

            if (await dialog.ShowAsync() == ContentDialogResult.Primary && !string.IsNullOrEmpty(dialog.ExtractedText))
            {
                if (ViewModel.SelectedDocument != null)
                {
                    InsertTextAtCursor("\n\n" + dialog.ExtractedText);
                }
                else
                {
                    ViewModel.CreateNewDocumentCommand.Execute(null);
                    await Task.Delay(100);
                    if (ViewModel.SelectedDocument != null)
                    {
                        ViewModel.SelectedDocument.Title = "OCR Extracted Text";
                        ViewModel.SelectedDocument.Content = dialog.ExtractedText;
                        TextEditor.Text = dialog.ExtractedText;
                    }
                }
            }
        }

        // ============================================
        // 🔗 DOCUMENT LINKS
        // ============================================

        private async void DocumentLinks_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedDocument == null) return;

            var allDocs = await ViewModel.DocumentService.LoadAllDocumentsAsync();
            var dialog = new Dialogs.DocumentLinksDialog(
                ViewModel.SelectedDocument,
                allDocs,
                ViewModel.DocumentService.DocumentLinks
            );
            dialog.XamlRoot = this.Content.XamlRoot;

            await dialog.ShowAsync();

            if (dialog.SelectedDocument != null)
            {
                ViewModel.SelectedDocument = dialog.SelectedDocument;
            }
        }

        private async void DocumentLinksFromMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && menuItem.CommandParameter is Models.Document document)
            {
                var allDocs = await ViewModel.DocumentService.LoadAllDocumentsAsync();
                var dialog = new Dialogs.DocumentLinksDialog(
                    document,
                    allDocs,
                    ViewModel.DocumentService.DocumentLinks
                );
                dialog.XamlRoot = this.Content.XamlRoot;
                await dialog.ShowAsync();
            }
        }

        // ============================================
        // 📎 ATTACHMENTS
        // ============================================

        private async void Attachments_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedDocument == null) return;

            var dialog = new Dialogs.AttachmentsDialog(
                ViewModel.SelectedDocument,
                ViewModel.DocumentService.Attachments
            );
            dialog.XamlRoot = this.Content.XamlRoot;

            await dialog.ShowAsync();
            await ViewModel.DocumentService.SaveDocumentAsync(ViewModel.SelectedDocument);
        }

        private async void AttachmentsFromMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && menuItem.CommandParameter is Models.Document document)
            {
                var dialog = new Dialogs.AttachmentsDialog(
                    document,
                    ViewModel.DocumentService.Attachments
                );
                dialog.XamlRoot = this.Content.XamlRoot;
                await dialog.ShowAsync();
                await ViewModel.DocumentService.SaveDocumentAsync(document);
            }
        }

        private async Task ShowNoDocumentSelectedDialog()
        {
            var dialog = new ContentDialog
            {
                Title = "⚠️ " + LocalizationService.Instance.GetString("Warning"),
                Content = LocalizationService.Instance.GetString("NoDocumentSelected"),
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        #endregion
    }

    public class DocumentHeading
    {
   public string Title { get; set; } = "";
        public int Level { get; set; }
    public int LineNumber { get; set; }
     public string IndentedTitle => new string(' ', (Level - 1) * 2) + Title;
    }
}