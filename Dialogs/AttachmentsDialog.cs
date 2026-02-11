using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using Windows.Storage;
using Windows.Storage.Pickers;
using Jot.Models;
using Jot.Services;

namespace Jot.Dialogs
{
    public sealed partial class AttachmentsDialog : ContentDialog
    {
        private readonly Document _document;
        private readonly AttachmentService _attachmentService;

        private ListView _attachmentsList;
        private TextBlock _statsTextBlock;
        private Button _addFileButton;
        private Button _addFromClipboardButton;
        private StackPanel _previewPanel;

        public AttachmentsDialog(Document document, AttachmentService attachmentService)
        {
            _document = document;
            _attachmentService = attachmentService;

            this.Title = "📎 " + LocalizationService.Instance.GetString("Attachments");
            this.CloseButtonText = LocalizationService.Instance.GetString("Close");
            this.DefaultButton = ContentDialogButton.Close;

            SetupUI();
            LoadAttachments();
        }

        private void SetupUI()
        {
            var mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Panel izquierdo - Lista
            var leftPanel = new StackPanel { Spacing = 12, Margin = new Thickness(0, 12, 12, 0) };

            // Header
            var header = new StackPanel { Spacing = 4 };
            var docTitle = new TextBlock
            {
                Text = $"📄 {_document.Title}",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 14
            };
            header.Children.Add(docTitle);

            _statsTextBlock = new TextBlock
            {
                Opacity = 0.7,
                FontSize = 11
            };
            header.Children.Add(_statsTextBlock);
            leftPanel.Children.Add(header);

            // Botones de acción
            var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 8, 0, 0) };

            _addFileButton = new Button
            {
                Content = "➕ " + LocalizationService.Instance.GetString("AddFile")
            };
            _addFileButton.Click += AddFileButton_Click;
            buttonsPanel.Children.Add(_addFileButton);

            _addFromClipboardButton = new Button
            {
                Content = "📋 " + LocalizationService.Instance.GetString("PasteImage")
            };
            _addFromClipboardButton.Click += AddFromClipboardButton_Click;
            buttonsPanel.Children.Add(_addFromClipboardButton);

            leftPanel.Children.Add(buttonsPanel);

            // Lista de adjuntos
            _attachmentsList = new ListView
            {
                SelectionMode = ListViewSelectionMode.Single,
                Height = 400
            };
            _attachmentsList.SelectionChanged += AttachmentsList_SelectionChanged;
            leftPanel.Children.Add(_attachmentsList);

            Grid.SetColumn(leftPanel, 0);
            mainGrid.Children.Add(leftPanel);

            // Panel derecho - Preview
            _previewPanel = new StackPanel { Spacing = 8, Margin = new Thickness(12, 12, 0, 0) };

            var previewHeader = new TextBlock
            {
                Text = LocalizationService.Instance.GetString("Preview"),
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 14
            };
            _previewPanel.Children.Add(previewHeader);

            var previewPlaceholder = new TextBlock
            {
                Text = LocalizationService.Instance.GetString("SelectAttachment"),
                Opacity = 0.6,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 100, 0, 0)
            };
            _previewPanel.Children.Add(previewPlaceholder);

            Grid.SetColumn(_previewPanel, 1);
            mainGrid.Children.Add(_previewPanel);

            this.Content = mainGrid;
            this.MaxWidth = 900;
            this.MaxHeight = 600;
        }

        private void LoadAttachments()
        {
            try
            {
                _attachmentsList.Items.Clear();

                foreach (var attachment in _document.Attachments)
                {
                    var item = CreateAttachmentListItem(attachment);
                    _attachmentsList.Items.Add(item);
                }

                if (_document.Attachments.Count == 0)
                {
                    _attachmentsList.Items.Add(new TextBlock
                    {
                        Text = LocalizationService.Instance.GetString("NoAttachments"),
                        Opacity = 0.6,
                        Margin = new Thickness(12)
                    });
                }

                // Stats
                var totalSize = _attachmentService.GetTotalAttachmentsSize(_document);
                var sizeText = _attachmentService.FormatFileSize(totalSize);
                _statsTextBlock.Text = $"{_document.Attachments.Count} {LocalizationService.Instance.GetString("Attachments")} • {sizeText}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading attachments: {ex.Message}");
            }
        }

        private ListViewItem CreateAttachmentListItem(DocumentAttachment attachment)
        {
            var icon = _attachmentService.GetAttachmentIcon(attachment.Type);
            var size = _attachmentService.FormatFileSize(attachment.SizeInBytes);

            var item = new ListViewItem
            {
                Tag = attachment
            };

            var panel = new Grid();
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var iconText = new TextBlock
            {
                Text = icon,
                FontSize = 24,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetColumn(iconText, 0);
            panel.Children.Add(iconText);

            var infoPanel = new StackPanel();
            var fileName = new TextBlock
            {
                Text = attachment.FileName,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };
            infoPanel.Children.Add(fileName);

            var details = new TextBlock
            {
                Text = $"{size} • {attachment.AttachedAt:dd/MM/yyyy HH:mm}",
                FontSize = 10,
                Opacity = 0.6
            };
            infoPanel.Children.Add(details);
            Grid.SetColumn(infoPanel, 1);
            panel.Children.Add(infoPanel);

            var actionsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };

            var openButton = new Button
            {
                Content = new SymbolIcon(Symbol.OpenFile),
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };
            openButton.Click += async (s, e) => await _attachmentService.OpenAttachmentAsync(attachment);
            actionsPanel.Children.Add(openButton);

            var deleteButton = new Button
            {
                Content = new SymbolIcon(Symbol.Delete),
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };
            deleteButton.Click += async (s, e) =>
            {
                await _attachmentService.RemoveAttachmentAsync(_document, attachment);
                LoadAttachments();
            };
            actionsPanel.Children.Add(deleteButton);

            Grid.SetColumn(actionsPanel, 2);
            panel.Children.Add(actionsPanel);

            item.Content = panel;
            return item;
        }

        private async void AddFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                picker.FileTypeFilter.Add("*");

                var files = await picker.PickMultipleFilesAsync();
                if (files != null && files.Count > 0)
                {
                    _addFileButton.IsEnabled = false;
                    await _attachmentService.AttachMultipleFilesAsync(_document, files);
                    LoadAttachments();
                    _addFileButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding file: {ex.Message}");
                _addFileButton.IsEnabled = true;
            }
        }

        private async void AddFromClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _addFromClipboardButton.IsEnabled = false;
                var attachment = await _attachmentService.AttachImageFromClipboardAsync(_document);
                
                if (attachment != null)
                {
                    LoadAttachments();
                }
                else
                {
                    var dialog = new ContentDialog
                    {
                        Title = "⚠️ " + LocalizationService.Instance.GetString("Warning"),
                        Content = LocalizationService.Instance.GetString("NoImageInClipboard"),
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                }

                _addFromClipboardButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding from clipboard: {ex.Message}");
                _addFromClipboardButton.IsEnabled = true;
            }
        }

        private void AttachmentsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_attachmentsList.SelectedItem is ListViewItem item && item.Tag is DocumentAttachment attachment)
            {
                ShowAttachmentPreview(attachment);
            }
        }

        private void ShowAttachmentPreview(DocumentAttachment attachment)
        {
            _previewPanel.Children.Clear();

            var header = new TextBlock
            {
                Text = LocalizationService.Instance.GetString("Preview"),
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 14
            };
            _previewPanel.Children.Add(header);

            var icon = new TextBlock
            {
                Text = _attachmentService.GetAttachmentIcon(attachment.Type),
                FontSize = 64,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 20)
            };
            _previewPanel.Children.Add(icon);

            var details = new StackPanel { Spacing = 8 };
            details.Children.Add(new TextBlock { Text = $"📝 {attachment.FileName}", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            details.Children.Add(new TextBlock { Text = $"📊 {_attachmentService.FormatFileSize(attachment.SizeInBytes)}", Opacity = 0.7 });
            details.Children.Add(new TextBlock { Text = $"📅 {attachment.AttachedAt:dd/MM/yyyy HH:mm}", Opacity = 0.7 });
            details.Children.Add(new TextBlock { Text = $"🏷️ {attachment.Type}", Opacity = 0.7 });

            _previewPanel.Children.Add(details);

            var openButton = new Button
            {
                Content = "🔓 " + LocalizationService.Instance.GetString("OpenFile"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 20, 0, 0)
            };
            openButton.Click += async (s, e) => await _attachmentService.OpenAttachmentAsync(attachment);
            _previewPanel.Children.Add(openButton);
        }
    }
}
