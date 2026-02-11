using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;
using Jot.Models;
using Jot.Services;

namespace Jot.Dialogs
{
    public sealed partial class CloudSyncDialog : ContentDialog
    {
        private readonly Document _document;
        private readonly CloudSyncService _cloudSyncService;

        private ComboBox _providerComboBox;
        private TextBlock _folderPathText;
        private Button _selectFolderButton;
        private Button _detectButton;
        private ToggleSwitch _autoSyncToggle;
        private TextBlock _statusTextBlock;
        private TextBlock _lastSyncTextBlock;
        private Button _syncNowButton;
        private TextBlock _detectedFoldersText;

        public CloudProvider SelectedProvider { get; private set; }
        public bool AutoSyncEnabled { get; private set; }

        public CloudSyncDialog(Document document, CloudSyncService cloudSyncService)
        {
            _document = document;
            _cloudSyncService = cloudSyncService;

            this.Title = "☁️ " + LocalizationService.Instance.GetString("CloudSync");
            this.PrimaryButtonText = LocalizationService.Instance.GetString("Save");
            this.SecondaryButtonText = LocalizationService.Instance.GetString("Cancel");
            this.DefaultButton = ContentDialogButton.Primary;

            SetupUI();
            DetectCloudFolders();
        }

        private void SetupUI()
        {
            var mainStack = new StackPanel { Spacing = 12, Margin = new Thickness(0, 12, 0, 0) };

            // Icono
            var iconText = new TextBlock
            {
                Text = "☁️",
                FontSize = 48,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            mainStack.Children.Add(iconText);

            // Documento
            var docInfo = new TextBlock
            {
                Text = $"📄 {_document.Title}",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            };
            mainStack.Children.Add(docInfo);

            // Instrucciones
            var instructions = new TextBlock
            {
                Text = "Sincroniza este documento con tu carpeta de OneDrive, Google Drive o Dropbox.\nLos archivos se guardarán en formato Markdown (.md) y se sincronizarán automáticamente.",
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.8,
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 12)
            };
            mainStack.Children.Add(instructions);

            // Proveedor de nube
            mainStack.Children.Add(new TextBlock 
            { 
                Text = LocalizationService.Instance.GetString("CloudProvider") + ":",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });

            _providerComboBox = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            _providerComboBox.Items.Add(new ComboBoxItem 
            { 
                Content = "None",
                Tag = CloudProvider.None 
            });
            _providerComboBox.Items.Add(new ComboBoxItem 
            { 
                Content = "☁️ OneDrive",
                Tag = CloudProvider.OneDrive 
            });
            _providerComboBox.Items.Add(new ComboBoxItem 
            { 
                Content = "☁️ Google Drive",
                Tag = CloudProvider.GoogleDrive 
            });
            _providerComboBox.Items.Add(new ComboBoxItem 
            { 
                Content = "☁️ Dropbox",
                Tag = CloudProvider.Dropbox 
            });

            var currentIndex = (int)_document.CloudProvider;
            _providerComboBox.SelectedIndex = currentIndex;
            _providerComboBox.SelectionChanged += ProviderComboBox_SelectionChanged;
            mainStack.Children.Add(_providerComboBox);

            // Carpetas detectadas
            _detectedFoldersText = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.7,
                FontSize = 11,
                Margin = new Thickness(0, 4, 0, 8),
                Visibility = Visibility.Collapsed
            };
            mainStack.Children.Add(_detectedFoldersText);

            // Carpeta actual
            mainStack.Children.Add(new TextBlock 
            { 
                Text = "📁 Carpeta de Sincronización:",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(0, 8, 0, 0)
            });

            _folderPathText = new TextBlock
            {
                Text = "No configurada",
                Opacity = 0.7,
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap
            };
            mainStack.Children.Add(_folderPathText);

            // Botones de configuración
            var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 8, 0, 0) };

            _detectButton = new Button
            {
                Content = "🔍 Detectar Automáticamente"
            };
            _detectButton.Click += DetectButton_Click;
            buttonsPanel.Children.Add(_detectButton);

            _selectFolderButton = new Button
            {
                Content = "📂 Seleccionar Carpeta"
            };
            _selectFolderButton.Click += SelectFolderButton_Click;
            buttonsPanel.Children.Add(_selectFolderButton);

            mainStack.Children.Add(buttonsPanel);

            // Auto-sync
            _autoSyncToggle = new ToggleSwitch
            {
                Header = LocalizationService.Instance.GetString("AutoSync"),
                OnContent = LocalizationService.Instance.GetString("Enabled"),
                OffContent = LocalizationService.Instance.GetString("Disabled"),
                IsOn = _document.IsSyncEnabled,
                Margin = new Thickness(0, 8, 0, 0)
            };
            mainStack.Children.Add(_autoSyncToggle);

            // Última sincronización
            _lastSyncTextBlock = new TextBlock
            {
                Text = _document.LastSyncedAt.HasValue
                    ? $"🕒 {LocalizationService.Instance.GetString("LastSync")}: {_document.LastSyncedAt.Value:dd/MM/yyyy HH:mm}"
                    : "🕒 " + LocalizationService.Instance.GetString("NeverSynced"),
                Opacity = 0.7,
                FontSize = 11,
                Margin = new Thickness(0, 8, 0, 0)
            };
            mainStack.Children.Add(_lastSyncTextBlock);

            // Sync now button
            _syncNowButton = new Button
            {
                Content = "🔄 " + LocalizationService.Instance.GetString("SyncNow"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 8, 0, 0)
            };
            _syncNowButton.Click += SyncNowButton_Click;
            mainStack.Children.Add(_syncNowButton);

            // Status
            _statusTextBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 12, 0, 0),
                Visibility = Visibility.Collapsed
            };
            mainStack.Children.Add(_statusTextBlock);

            // Info
            var infoBar = new InfoBar
            {
                Severity = InfoBarSeverity.Informational,
                IsOpen = true,
                Message = "Los documentos se guardarán en la subcarpeta 'Jot Documents' dentro de tu carpeta de nube seleccionada. El cliente de sincronización de OneDrive/Google Drive/Dropbox se encargará de subirlos a la nube automáticamente.",
                Margin = new Thickness(0, 12, 0, 0)
            };
            mainStack.Children.Add(infoBar);

            this.Content = mainStack;
            this.MaxWidth = 550;

            this.PrimaryButtonClick += CloudSyncDialog_PrimaryButtonClick;
        }

        private void DetectCloudFolders()
        {
            try
            {
                var detected = _cloudSyncService.DetectCloudFolders();

                if (detected.Count > 0)
                {
                    var info = "✅ Carpetas detectadas:\n";
                    foreach (var kvp in detected)
                    {
                        info += $"  • {kvp.Key}: {kvp.Value}\n";
                    }
                    _detectedFoldersText.Text = info;
                    _detectedFoldersText.Visibility = Visibility.Visible;
                }
                else
                {
                    _detectedFoldersText.Text = "⚠️ No se detectaron carpetas de OneDrive, Google Drive o Dropbox.\nPuedes seleccionar una carpeta manualmente.";
                    _detectedFoldersText.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error detecting folders: {ex.Message}");
            }
        }

        private void ProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_providerComboBox.SelectedItem is ComboBoxItem item && item.Tag is CloudProvider provider)
            {
                UpdateFolderDisplay(provider);
            }
        }

        private void UpdateFolderDisplay(CloudProvider provider)
        {
            if (provider == CloudProvider.None)
            {
                _folderPathText.Text = "No configurada";
                _selectFolderButton.IsEnabled = false;
                _detectButton.IsEnabled = false;
                return;
            }

            _selectFolderButton.IsEnabled = true;
            _detectButton.IsEnabled = true;

            var configuredFolder = _cloudSyncService.GetProviderFolder(provider);
            if (!string.IsNullOrEmpty(configuredFolder))
            {
                _folderPathText.Text = $"✅ {configuredFolder}\\Jot Documents";
            }
            else
            {
                _folderPathText.Text = "❌ No configurada - Haz clic en 'Detectar' o 'Seleccionar Carpeta'";
            }
        }

        private void DetectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_providerComboBox.SelectedItem is not ComboBoxItem item || item.Tag is not CloudProvider provider || provider == CloudProvider.None)
                {
                    ShowStatus("⚠️ Selecciona un proveedor primero", Microsoft.UI.Colors.Orange);
                    return;
                }

                var detected = _cloudSyncService.DetectCloudFolders();

                if (detected.ContainsKey(provider))
                {
                    _cloudSyncService.SetSyncFolder(provider, detected[provider]);
                    UpdateFolderDisplay(provider);
                    ShowStatus($"✅ Carpeta detectada y configurada: {detected[provider]}", Microsoft.UI.Colors.Green);
                }
                else
                {
                    ShowStatus($"❌ No se pudo detectar la carpeta de {provider}. Intenta seleccionarla manualmente.", Microsoft.UI.Colors.Red);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"❌ Error: {ex.Message}", Microsoft.UI.Colors.Red);
            }
        }

        private async void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_providerComboBox.SelectedItem is not ComboBoxItem item || item.Tag is not CloudProvider provider || provider == CloudProvider.None)
                {
                    ShowStatus("⚠️ Selecciona un proveedor primero", Microsoft.UI.Colors.Orange);
                    return;
                }

                var folderPath = await _cloudSyncService.SelectCloudFolderAsync();

                if (!string.IsNullOrEmpty(folderPath))
                {
                    _cloudSyncService.SetSyncFolder(provider, folderPath);
                    UpdateFolderDisplay(provider);
                    ShowStatus($"✅ Carpeta configurada: {folderPath}", Microsoft.UI.Colors.Green);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"❌ Error: {ex.Message}", Microsoft.UI.Colors.Red);
            }
        }

        private async void SyncNowButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _syncNowButton.IsEnabled = false;
                ShowStatus("⏳ " + LocalizationService.Instance.GetString("Syncing"), Microsoft.UI.Colors.Orange);

                var selectedItem = _providerComboBox.SelectedItem as ComboBoxItem;
                var provider = (CloudProvider)selectedItem.Tag;

                if (provider == CloudProvider.None)
                {
                    ShowStatus("❌ " + LocalizationService.Instance.GetString("SelectProvider"), Microsoft.UI.Colors.Red);
                    _syncNowButton.IsEnabled = true;
                    return;
                }

                if (!_cloudSyncService.IsProviderConfigured(provider))
                {
                    ShowStatus("❌ Configura la carpeta de sincronización primero", Microsoft.UI.Colors.Red);
                    _syncNowButton.IsEnabled = true;
                    return;
                }

                // Habilitar sync temporalmente
                var wasEnabled = _document.IsSyncEnabled;
                var originalProvider = _document.CloudProvider;

                _cloudSyncService.EnableAutoSync(_document, provider);
                var success = await _cloudSyncService.SyncDocumentAsync(_document);

                if (!wasEnabled)
                {
                    _cloudSyncService.DisableAutoSync(_document);
                    _document.CloudProvider = originalProvider;
                }

                if (success)
                {
                    ShowStatus("✅ " + LocalizationService.Instance.GetString("SyncSuccess"), Microsoft.UI.Colors.Green);
                    _lastSyncTextBlock.Text = $"🕒 {LocalizationService.Instance.GetString("LastSync")}: {DateTime.Now:dd/MM/yyyy HH:mm}";
                }
                else
                {
                    ShowStatus("❌ " + LocalizationService.Instance.GetString("SyncError"), Microsoft.UI.Colors.Red);
                }
            }
            catch (Exception ex)
            {
                ShowStatus("❌ " + ex.Message, Microsoft.UI.Colors.Red);
            }
            finally
            {
                _syncNowButton.IsEnabled = true;
            }
        }

        private void ShowStatus(string message, Windows.UI.Color color)
        {
            _statusTextBlock.Text = message;
            _statusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
            _statusTextBlock.Visibility = Visibility.Visible;
        }

        private void CloudSyncDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var selectedItem = _providerComboBox.SelectedItem as ComboBoxItem;
            SelectedProvider = (CloudProvider)selectedItem.Tag;
            AutoSyncEnabled = _autoSyncToggle.IsOn;

            if (SelectedProvider != CloudProvider.None && AutoSyncEnabled)
            {
                if (_cloudSyncService.IsProviderConfigured(SelectedProvider))
                {
                    _cloudSyncService.EnableAutoSync(_document, SelectedProvider);
                }
                else
                {
                    args.Cancel = true;
                    ShowStatus("❌ Configura la carpeta de sincronización antes de activar auto-sync", Microsoft.UI.Colors.Red);
                }
            }
            else
            {
                _cloudSyncService.DisableAutoSync(_document);
            }
        }
    }
}
