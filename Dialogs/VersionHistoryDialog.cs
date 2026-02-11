using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jot.Models;
using Jot.Services;

namespace Jot.Dialogs
{
    public sealed partial class VersionHistoryDialog : ContentDialog
    {
        private readonly Document _document;
        private readonly VersionHistoryService _versionService;
        private List<DocumentVersion> _versions;
        private DocumentVersion _selectedVersion;

        private ListView _versionsListView;
        private TextBlock _versionDetailsTextBlock;
        private ScrollViewer _contentPreviewScroll;
        private TextBlock _contentPreviewText;

        public DocumentVersion SelectedVersionToRestore { get; private set; }

        public VersionHistoryDialog(Document document, VersionHistoryService versionService)
        {
            _document = document;
            _versionService = versionService;

            this.Title = "🔄 " + LocalizationService.Instance.GetString("VersionHistory");
            this.PrimaryButtonText = LocalizationService.Instance.GetString("Restore");
            this.SecondaryButtonText = LocalizationService.Instance.GetString("Close");
            this.DefaultButton = ContentDialogButton.Secondary;
            this.IsPrimaryButtonEnabled = false;

            SetupUI();
            LoadVersionsAsync();
        }

        private void SetupUI()
        {
            var mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Panel izquierdo - Lista de versiones
            var leftPanel = new StackPanel { Spacing = 8, Margin = new Thickness(0, 12, 12, 0) };

            var versionsHeader = new TextBlock
            {
                Text = LocalizationService.Instance.GetString("Versions"),
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 8)
            };
            leftPanel.Children.Add(versionsHeader);

            _versionsListView = new ListView
            {
                SelectionMode = ListViewSelectionMode.Single,
                Height = 400
            };
            _versionsListView.SelectionChanged += VersionsList_SelectionChanged;
            leftPanel.Children.Add(_versionsListView);

            Grid.SetColumn(leftPanel, 0);
            Grid.SetRow(leftPanel, 0);
            Grid.SetRowSpan(leftPanel, 2);
            mainGrid.Children.Add(leftPanel);

            // Panel derecho - Detalles y preview
            var rightPanel = new StackPanel { Spacing = 12, Margin = new Thickness(12, 12, 0, 0) };

            var detailsHeader = new TextBlock
            {
                Text = LocalizationService.Instance.GetString("Details"),
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 14
            };
            rightPanel.Children.Add(detailsHeader);

            _versionDetailsTextBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.8,
                FontSize = 12
            };
            rightPanel.Children.Add(_versionDetailsTextBlock);

            var previewHeader = new TextBlock
            {
                Text = LocalizationService.Instance.GetString("Preview"),
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 14,
                Margin = new Thickness(0, 12, 0, 0)
            };
            rightPanel.Children.Add(previewHeader);

            _contentPreviewScroll = new ScrollViewer
            {
                Height = 300,
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8)
            };

            _contentPreviewText = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                FontSize = 11
            };
            _contentPreviewScroll.Content = _contentPreviewText;
            rightPanel.Children.Add(_contentPreviewScroll);

            Grid.SetColumn(rightPanel, 1);
            Grid.SetRow(rightPanel, 0);
            Grid.SetRowSpan(rightPanel, 2);
            mainGrid.Children.Add(rightPanel);

            this.Content = mainGrid;
            this.MaxWidth = 900;
            this.MaxHeight = 600;
        }

        private async void LoadVersionsAsync()
        {
            try
            {
                _versions = await _versionService.GetVersionHistoryAsync(_document.Id);

                // Agregar versión actual como primera
                var currentVersion = new DocumentVersion
                {
                    Title = _document.Title,
                    Content = _document.Content,
                    CreatedAt = _document.ModifiedAt,
                    ChangeDescription = "Current version",
                    SizeInBytes = System.Text.Encoding.UTF8.GetByteCount(_document.Content ?? "")
                };
                _versions.Insert(0, currentVersion);

                // Poblar lista
                foreach (var version in _versions)
                {
                    var item = new ListViewItem
                    {
                        Content = new StackPanel
                        {
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = version.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                                },
                                new TextBlock
                                {
                                    Text = version.ChangeDescription,
                                    FontSize = 11,
                                    Opacity = 0.7
                                }
                            }
                        },
                        Tag = version
                    };
                    _versionsListView.Items.Add(item);
                }

                // Seleccionar primera versión
                if (_versionsListView.Items.Count > 0)
                {
                    _versionsListView.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading versions: {ex.Message}");
            }
        }

        private void VersionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_versionsListView.SelectedItem is ListViewItem item && item.Tag is DocumentVersion version)
            {
                _selectedVersion = version;
                this.IsPrimaryButtonEnabled = version.ChangeDescription != "Current version";

                // Mostrar detalles
                var sizeKB = version.SizeInBytes / 1024.0;
                _versionDetailsTextBlock.Text = $"📅 Created: {version.CreatedAt:dd/MM/yyyy HH:mm}\n" +
                                               $"📝 Description: {version.ChangeDescription}\n" +
                                               $"📊 Size: {sizeKB:0.00} KB\n" +
                                               $"👤 By: {version.CreatedBy}";

                // Mostrar preview del contenido
                _contentPreviewText.Text = version.Content;

                SelectedVersionToRestore = version;
            }
        }
    }
}
