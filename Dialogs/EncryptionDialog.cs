using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Jot.Models;
using Jot.Services;

namespace Jot.Dialogs
{
    public sealed partial class EncryptionDialog : ContentDialog
    {
        private readonly Document _document;
        private readonly EncryptionService _encryptionService;
        private readonly EncryptionMode _mode;

        private PasswordBox _passwordBox;
        private PasswordBox _confirmPasswordBox;
        private TextBlock _statusTextBlock;
        private ProgressRing _progressRing;

        public enum EncryptionMode
        {
            Encrypt,
            Decrypt,
            ChangePassword
        }

        public string EnteredPassword { get; private set; }

        public EncryptionDialog(Document document, EncryptionService encryptionService, EncryptionMode mode)
        {
            _document = document;
            _encryptionService = encryptionService;
            _mode = mode;

            this.Title = GetTitle();
            this.PrimaryButtonText = GetPrimaryButtonText();
            this.SecondaryButtonText = LocalizationService.Instance.GetString("Cancel");
            this.DefaultButton = ContentDialogButton.Primary;

            this.PrimaryButtonClick += EncryptionDialog_PrimaryButtonClick;

            SetupUI();
        }

        private string GetTitle()
        {
            return _mode switch
            {
                EncryptionMode.Encrypt => "🔐 " + LocalizationService.Instance.GetString("EncryptDocument"),
                EncryptionMode.Decrypt => "🔓 " + LocalizationService.Instance.GetString("UnlockDocument"),
                EncryptionMode.ChangePassword => "🔑 " + LocalizationService.Instance.GetString("ChangePassword"),
                _ => "🔐 Encryption"
            };
        }

        private string GetPrimaryButtonText()
        {
            return _mode switch
            {
                EncryptionMode.Encrypt => LocalizationService.Instance.GetString("Encrypt"),
                EncryptionMode.Decrypt => LocalizationService.Instance.GetString("Unlock"),
                EncryptionMode.ChangePassword => LocalizationService.Instance.GetString("Change"),
                _ => "OK"
            };
        }

        private void SetupUI()
        {
            var mainStack = new StackPanel { Spacing = 12, Margin = new Thickness(0, 12, 0, 0) };

            // Icono y descripción
            var iconText = new TextBlock
            {
                Text = _mode == EncryptionMode.Encrypt ? "🔒" : (_mode == EncryptionMode.Decrypt ? "🔓" : "🔑"),
                FontSize = 48,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            mainStack.Children.Add(iconText);

            var description = new TextBlock
            {
                Text = GetDescription(),
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.8,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            };
            mainStack.Children.Add(description);

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

            // Password
            mainStack.Children.Add(new TextBlock 
            { 
                Text = _mode == EncryptionMode.ChangePassword 
                    ? LocalizationService.Instance.GetString("CurrentPassword") + ":"
                    : LocalizationService.Instance.GetString("Password") + ":" 
            });
            _passwordBox = new PasswordBox
            {
                PlaceholderText = "••••••••",
                MinWidth = 300
            };
            _passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
            mainStack.Children.Add(_passwordBox);

            // Confirm password (solo para Encrypt y ChangePassword)
            if (_mode == EncryptionMode.Encrypt || _mode == EncryptionMode.ChangePassword)
            {
                mainStack.Children.Add(new TextBlock 
                { 
                    Text = _mode == EncryptionMode.ChangePassword
                        ? LocalizationService.Instance.GetString("NewPassword") + ":"
                        : LocalizationService.Instance.GetString("ConfirmPassword") + ":",
                    Margin = new Thickness(0, 8, 0, 0)
                });
                _confirmPasswordBox = new PasswordBox
                {
                    PlaceholderText = "••••••••",
                    MinWidth = 300
                };
                _confirmPasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
                mainStack.Children.Add(_confirmPasswordBox);
            }

            // Status
            _statusTextBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 12, 0, 0),
                Visibility = Visibility.Collapsed
            };
            mainStack.Children.Add(_statusTextBlock);

            // Progress ring
            _progressRing = new ProgressRing
            {
                IsActive = false,
                Visibility = Visibility.Collapsed,
                Width = 40,
                Height = 40,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 12, 0, 0)
            };
            mainStack.Children.Add(_progressRing);

            // Advertencia de seguridad
            var warning = new InfoBar
            {
                Severity = InfoBarSeverity.Warning,
                IsOpen = true,
                Message = LocalizationService.Instance.GetString("EncryptionWarning"),
                Margin = new Thickness(0, 12, 0, 0)
            };
            mainStack.Children.Add(warning);

            this.Content = mainStack;
            this.MaxWidth = 450;
        }

        private string GetDescription()
        {
            return _mode switch
            {
                EncryptionMode.Encrypt => LocalizationService.Instance.GetString("EncryptDescription"),
                EncryptionMode.Decrypt => LocalizationService.Instance.GetString("UnlockDescription"),
                EncryptionMode.ChangePassword => LocalizationService.Instance.GetString("ChangePasswordDescription"),
                _ => ""
            };
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Validar que las contraseñas coincidan
            if (_confirmPasswordBox != null)
            {
                var match = _passwordBox.Password == _confirmPasswordBox.Password;
                this.IsPrimaryButtonEnabled = !string.IsNullOrEmpty(_passwordBox.Password) && match;
                
                if (!string.IsNullOrEmpty(_confirmPasswordBox.Password) && !match)
                {
                    _statusTextBlock.Text = "❌ " + LocalizationService.Instance.GetString("PasswordsDoNotMatch");
                    _statusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                    _statusTextBlock.Visibility = Visibility.Visible;
                }
                else
                {
                    _statusTextBlock.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                this.IsPrimaryButtonEnabled = !string.IsNullOrEmpty(_passwordBox.Password);
            }
        }

        private async void EncryptionDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var deferral = args.GetDeferral();

            try
            {
                _progressRing.IsActive = true;
                _progressRing.Visibility = Visibility.Visible;
                this.IsPrimaryButtonEnabled = false;

                EnteredPassword = _passwordBox.Password;
                var success = false;

                switch (_mode)
                {
                    case EncryptionMode.Encrypt:
                        success = await _encryptionService.EncryptDocumentAsync(_document, _passwordBox.Password);
                        break;

                    case EncryptionMode.Decrypt:
                        success = await _encryptionService.DecryptDocumentAsync(_document, _passwordBox.Password);
                        break;

                    case EncryptionMode.ChangePassword:
                        success = await _encryptionService.ChangePasswordAsync(_document, _passwordBox.Password, _confirmPasswordBox.Password);
                        break;
                }

                if (!success)
                {
                    args.Cancel = true;
                    _statusTextBlock.Text = "❌ " + LocalizationService.Instance.GetString("EncryptionError");
                    _statusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                    _statusTextBlock.Visibility = Visibility.Visible;
                    this.IsPrimaryButtonEnabled = true;
                }
            }
            catch (Exception ex)
            {
                args.Cancel = true;
                _statusTextBlock.Text = "❌ " + ex.Message;
                _statusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                _statusTextBlock.Visibility = Visibility.Visible;
                this.IsPrimaryButtonEnabled = true;
            }
            finally
            {
                _progressRing.IsActive = false;
                _progressRing.Visibility = Visibility.Collapsed;
                deferral.Complete();
            }
        }
    }
}
