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

namespace Jot
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }
 private bool _isUpdatingContent = false;
        private ObservableCollection<DocumentHeading> _documentHeadings = new();

 public MainWindow()
        {
            this.InitializeComponent();
    
       ViewModel = new MainViewModel();
 
   SetupViewModeButtons();
        Title = "Jot - Modern Note Taking";
    this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1000, 700));
    LoadingOverlay.Visibility = Visibility.Collapsed;
  SetupBindings();
        }

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
       
UpdateViewMode();
     UpdateSelectedDocument();
UpdateDocumentIndex();
   UpdateGitHubConnectionStatus();
     UpdateGitHubButtonStates();
   }

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
    ViewModel.SelectedDocument.Content = newText;
         ViewModel.SelectedDocument.ModifiedAt = DateTime.Now;
       
       UpdatePreviewContentImmediate(newText);
       UpdateSplitModeContentImmediate(newText);
         UpdateDocumentIndex();
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