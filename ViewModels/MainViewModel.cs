using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jot.Models;
using Jot.Services;
using System.Linq;

namespace Jot.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DocumentService _documentService;
        
        [ObservableProperty]
        private ObservableCollection<Document> documents = new();

        [ObservableProperty]
        private Document? selectedDocument;

        [ObservableProperty]
        private string searchText = "";

        [ObservableProperty]
        private bool isPaneOpen = true;

        [ObservableProperty]
        private ViewMode currentViewMode = ViewMode.Edit;

        private ObservableCollection<Document> _allDocuments = new();

        public MainViewModel()
        {
            _documentService = new DocumentService();
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
    }
}