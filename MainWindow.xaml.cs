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
                    SplitModeEditor.TextChanged += SplitEditor_TextChanged;
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
