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

        public MainWindow()
        {
            this.InitializeComponent();
            ViewModel = new MainViewModel();
            
            // Setup bindings manually
            SetupBindings();
            
            // Set window properties
            Title = "Jot - Modern Note Taking";
            
            // Set minimum window size
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1000, 700));
        }

        private void SetupBindings()
        {
            // Setup simple bindings
            DocumentsList.ItemsSource = ViewModel.Documents;
            SearchBox.Text = ViewModel.SearchText;
            
            // Bind document list selection
            DocumentsList.SelectionChanged += DocumentsList_SelectionChanged;
            
            // Setup event handlers for buttons
            TogglePaneButton.Click += (s, e) => ViewModel.ToggleSidebarCommand.Execute(null);
            EditModeButton.Click += (s, e) => ViewModel.SetViewModeCommand.Execute(ViewMode.Edit);
            PreviewModeButton.Click += (s, e) => ViewModel.SetViewModeCommand.Execute(ViewMode.Preview);
            SplitModeButton.Click += (s, e) => ViewModel.SetViewModeCommand.Execute(ViewMode.Split);
            
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
                UpdateDocumentIndex();
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
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
                    TitleTextBox.Text = ViewModel.SelectedDocument.Title;
                }
                
                // Update content in all editors
                TextEditor.Text = ViewModel.SelectedDocument.Content ?? "";
                
                // Update split mode editors
                var splitEditors = GetSplitModeEditors();
                if (splitEditors.editor != null)
                {
                    splitEditors.editor.Text = ViewModel.SelectedDocument.Content ?? "";
                    splitEditors.editor.TextChanged += SplitEditor_TextChanged;
                }
                
                // Update previews
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
                TextEditor.Text = newText;
                UpdatePreviewContent();
                UpdateDocumentIndex();
            }
        }

        private void UpdatePreviewContent()
        {
            if (ViewModel.SelectedDocument != null)
            {
                MarkdownPreview.MarkdownText = ViewModel.SelectedDocument.Content ?? "";
                
                var splitEditors = GetSplitModeEditors();
                if (splitEditors.preview != null)
                {
                    splitEditors.preview.MarkdownText = ViewModel.SelectedDocument.Content ?? "";
                }
            }
        }

        private void UpdateSplitModeContent()
        {
            var splitEditors = GetSplitModeEditors();
            if (splitEditors.preview != null && ViewModel.SelectedDocument != null)
            {
                splitEditors.preview.MarkdownText = ViewModel.SelectedDocument.Content ?? "";
            }
        }

        private (Controls.RichTextEditor? editor, Controls.MarkdownPreview? preview) GetSplitModeEditors()
        {
            if (SplitModeGrid != null)
            {
                Controls.RichTextEditor? editor = null;
                Controls.MarkdownPreview? preview = null;
                
                foreach (var child in SplitModeGrid.Children)
                {
                    if (child is Controls.RichTextEditor richEditor)
                        editor = richEditor;
                    else if (child is Controls.MarkdownPreview markdownPreview)
                        preview = markdownPreview;
                }
                
                return (editor, preview);
            }
            
            return (null, null);
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
            if (ViewModel.SelectedDocument == null || DocumentIndexList == null)
            {
                if (DocumentIndexList != null)
                    DocumentIndexList.ItemsSource = null;
                return;
            }

            var content = ViewModel.SelectedDocument.Content ?? "";
            var headings = ExtractHeadings(content);
            
            DocumentIndexList.ItemsSource = headings;
        }

        private ObservableCollection<DocumentHeading> ExtractHeadings(string content)
        {
            var headings = new ObservableCollection<DocumentHeading>();
            var lines = content.Split('\n');
            int lineNumber = 0;

            foreach (var line in lines)
            {
                lineNumber++;
                var trimmedLine = line.Trim();
                
                if (trimmedLine.StartsWith("#"))
                {
                    var level = 0;
                    while (level < trimmedLine.Length && trimmedLine[level] == '#')
                        level++;
                    
                    if (level <= 6 && level < trimmedLine.Length && trimmedLine[level] == ' ')
                    {
                        var title = trimmedLine.Substring(level + 1).Trim();
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
            }

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
