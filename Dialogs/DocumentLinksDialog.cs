using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Jot.Models;
using Jot.Services;

namespace Jot.Dialogs
{
    public sealed partial class DocumentLinksDialog : ContentDialog
    {
        private readonly Document _document;
        private readonly List<Document> _allDocuments;
        private readonly DocumentLinksService _linksService;

        private ListView _linkedDocsList;
        private ListView _backlinksList;
        private ListView _relatedDocsList;
        private ListView _brokenLinksList;
        private TextBlock _statsTextBlock;

        public Document SelectedDocument { get; private set; }

        public DocumentLinksDialog(Document document, List<Document> allDocuments, DocumentLinksService linksService)
        {
            _document = document;
            _allDocuments = allDocuments;
            _linksService = linksService;

            this.Title = "🔗 " + LocalizationService.Instance.GetString("DocumentLinks");
            this.CloseButtonText = LocalizationService.Instance.GetString("Close");
            this.DefaultButton = ContentDialogButton.Close;

            SetupUI();
            LoadLinks();
        }

        private void SetupUI()
        {
            var mainGrid = new Grid { Margin = new Thickness(0, 12, 0, 0) };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Header con stats
            var headerPanel = new StackPanel { Spacing = 8, Margin = new Thickness(0, 0, 0, 12) };

            var docTitle = new TextBlock
            {
                Text = $"📄 {_document.Title}",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 16
            };
            headerPanel.Children.Add(docTitle);

            _statsTextBlock = new TextBlock
            {
                Opacity = 0.7,
                FontSize = 11
            };
            headerPanel.Children.Add(_statsTextBlock);

            var helpText = new TextBlock
            {
                Text = LocalizationService.Instance.GetString("WikiLinkHelp"),
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.6,
                FontSize = 10,
                Margin = new Thickness(0, 4, 0, 0)
            };
            headerPanel.Children.Add(helpText);

            Grid.SetRow(headerPanel, 0);
            mainGrid.Children.Add(headerPanel);

            // Tabs con diferentes vistas
            var tabView = new TabView();

            // Tab 1: Enlaces salientes
            var linksTab = new TabViewItem
            {
                Header = "📤 " + LocalizationService.Instance.GetString("OutgoingLinks"),
                IconSource = new SymbolIconSource { Symbol = Symbol.Link }
            };

            _linkedDocsList = new ListView
            {
                SelectionMode = ListViewSelectionMode.Single
            };
            _linkedDocsList.SelectionChanged += DocumentList_SelectionChanged;
            linksTab.Content = _linkedDocsList;
            tabView.TabItems.Add(linksTab);

            // Tab 2: Backlinks
            var backlinksTab = new TabViewItem
            {
                Header = "📥 " + LocalizationService.Instance.GetString("Backlinks"),
                IconSource = new SymbolIconSource { Symbol = Symbol.Back }
            };

            _backlinksList = new ListView
            {
                SelectionMode = ListViewSelectionMode.Single
            };
            _backlinksList.SelectionChanged += DocumentList_SelectionChanged;
            backlinksTab.Content = _backlinksList;
            tabView.TabItems.Add(backlinksTab);

            // Tab 3: Documentos relacionados
            var relatedTab = new TabViewItem
            {
                Header = "🔄 " + LocalizationService.Instance.GetString("RelatedDocuments"),
                IconSource = new SymbolIconSource { Symbol = Symbol.Globe }
            };

            _relatedDocsList = new ListView
            {
                SelectionMode = ListViewSelectionMode.Single
            };
            _relatedDocsList.SelectionChanged += DocumentList_SelectionChanged;
            relatedTab.Content = _relatedDocsList;
            tabView.TabItems.Add(relatedTab);

            // Tab 4: Enlaces rotos
            var brokenTab = new TabViewItem
            {
                Header = "⚠️ " + LocalizationService.Instance.GetString("BrokenLinks"),
                IconSource = new SymbolIconSource { Symbol = Symbol.Important }
            };

            _brokenLinksList = new ListView();
            brokenTab.Content = _brokenLinksList;
            tabView.TabItems.Add(brokenTab);

            Grid.SetRow(tabView, 1);
            mainGrid.Children.Add(tabView);

            this.Content = mainGrid;
            this.MaxWidth = 700;
            this.MaxHeight = 600;
        }

        private void LoadLinks()
        {
            try
            {
                // Actualizar enlaces
                _linksService.UpdateDocumentLinks(_document, _allDocuments);

                // Enlaces salientes
                var linkedDocs = _linksService.GetLinkedDocuments(_document, _allDocuments);
                foreach (var doc in linkedDocs)
                {
                    _linkedDocsList.Items.Add(CreateDocumentListItem(doc));
                }

                if (linkedDocs.Count == 0)
                {
                    _linkedDocsList.Items.Add(new TextBlock 
                    { 
                        Text = LocalizationService.Instance.GetString("NoLinksFound"),
                        Opacity = 0.6,
                        Margin = new Thickness(12)
                    });
                }

                // Backlinks
                var backlinks = _linksService.GetBacklinks(_document, _allDocuments);
                foreach (var doc in backlinks)
                {
                    _backlinksList.Items.Add(CreateDocumentListItem(doc));
                }

                if (backlinks.Count == 0)
                {
                    _backlinksList.Items.Add(new TextBlock 
                    { 
                        Text = LocalizationService.Instance.GetString("NoBacklinksFound"),
                        Opacity = 0.6,
                        Margin = new Thickness(12)
                    });
                }

                // Documentos relacionados
                var relatedDocs = _linksService.FindRelatedDocuments(_document, _allDocuments);
                foreach (var doc in relatedDocs)
                {
                    _relatedDocsList.Items.Add(CreateDocumentListItem(doc));
                }

                if (relatedDocs.Count == 0)
                {
                    _relatedDocsList.Items.Add(new TextBlock 
                    { 
                        Text = LocalizationService.Instance.GetString("NoRelatedFound"),
                        Opacity = 0.6,
                        Margin = new Thickness(12)
                    });
                }

                // Enlaces rotos
                var brokenLinks = _linksService.GetBrokenLinks(_document, _allDocuments);
                foreach (var link in brokenLinks)
                {
                    _brokenLinksList.Items.Add(new StackPanel
                    {
                        Margin = new Thickness(8),
                        Children =
                        {
                            new TextBlock
                            {
                                Text = $"⚠️ [[{link}]]",
                                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange)
                            },
                            new TextBlock
                            {
                                Text = LocalizationService.Instance.GetString("DocumentNotFound"),
                                FontSize = 10,
                                Opacity = 0.6
                            }
                        }
                    });
                }

                if (brokenLinks.Count == 0)
                {
                    _brokenLinksList.Items.Add(new TextBlock 
                    { 
                        Text = "✅ " + LocalizationService.Instance.GetString("NoBrokenLinks"),
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green),
                        Margin = new Thickness(12)
                    });
                }

                // Stats
                _statsTextBlock.Text = $"📤 {linkedDocs.Count} {LocalizationService.Instance.GetString("OutgoingLinks")} | " +
                                      $"📥 {backlinks.Count} {LocalizationService.Instance.GetString("Backlinks")} | " +
                                      $"⚠️ {brokenLinks.Count} {LocalizationService.Instance.GetString("BrokenLinks")}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading links: {ex.Message}");
            }
        }

        private ListViewItem CreateDocumentListItem(Document doc)
        {
            return new ListViewItem
            {
                Content = new StackPanel
                {
                    Margin = new Thickness(8),
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"📄 {doc.Title}",
                            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                        },
                        new TextBlock
                        {
                            Text = $"Modified: {doc.ModifiedAt:dd/MM/yyyy HH:mm}",
                            FontSize = 10,
                            Opacity = 0.6
                        }
                    }
                },
                Tag = doc
            };
        }

        private void DocumentList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as ListView)?.SelectedItem is ListViewItem item && item.Tag is Document doc)
            {
                SelectedDocument = doc;
            }
        }
    }
}
