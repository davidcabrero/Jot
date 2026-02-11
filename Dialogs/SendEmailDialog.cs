using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Jot.Models;
using Jot.Services;

namespace Jot.Dialogs
{
    public sealed partial class SendEmailDialog : ContentDialog
    {
        private readonly Document _document;
        private readonly EmailService _emailService;

        private TextBox _toEmailTextBox;
        private ComboBox _methodComboBox;
        private ComboBox _formatComboBox;
        private StackPanel _smtpPanel;
        private TextBox _fromEmailTextBox;
        private PasswordBox _passwordBox;
        private TextBox _smtpServerTextBox;
        private TextBox _smtpPortTextBox;
        private CheckBox _useSslCheckBox;

        public SendEmailDialog(Document document)
        {
            _document = document;
            _emailService = new EmailService();

            this.Title = "📧 " + LocalizationService.Instance.GetString("SendEmail");
            this.PrimaryButtonText = LocalizationService.Instance.GetString("Send");
            this.SecondaryButtonText = LocalizationService.Instance.GetString("Cancel");
            this.DefaultButton = ContentDialogButton.Primary;

            SetupUI();

            this.PrimaryButtonClick += SendEmailDialog_PrimaryButtonClick;
        }

        private void SetupUI()
        {
            var mainStack = new StackPanel { Spacing = 12, Margin = new Thickness(0, 12, 0, 0) };

            // Documento a enviar
            var docInfo = new TextBlock
            {
                Text = $"📄 {_document.Title}",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 14
            };
            mainStack.Children.Add(docInfo);

            // Email destinatario
            mainStack.Children.Add(new TextBlock { Text = LocalizationService.Instance.GetString("ToEmail") + ":" });
            _toEmailTextBox = new TextBox
            {
                PlaceholderText = "ejemplo@correo.com",
                Text = ""
            };
            mainStack.Children.Add(_toEmailTextBox);

            // Método de envío
            mainStack.Children.Add(new TextBlock 
            { 
                Text = LocalizationService.Instance.GetString("SendMethod") + ":",
                Margin = new Thickness(0, 8, 0, 0)
            });
            _methodComboBox = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            _methodComboBox.Items.Add("📨 " + LocalizationService.Instance.GetString("DefaultEmailClient"));
            _methodComboBox.Items.Add("📧 Gmail");
            _methodComboBox.Items.Add("📧 Outlook");
            _methodComboBox.Items.Add("⚙️ " + LocalizationService.Instance.GetString("CustomSMTP"));
            _methodComboBox.SelectedIndex = 0;
            _methodComboBox.SelectionChanged += MethodComboBox_SelectionChanged;
            mainStack.Children.Add(_methodComboBox);

            // Formato
            mainStack.Children.Add(new TextBlock 
            { 
                Text = LocalizationService.Instance.GetString("EmailFormat") + ":",
                Margin = new Thickness(0, 8, 0, 0)
            });
            _formatComboBox = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            _formatComboBox.Items.Add("📝 Markdown (.md)");
            _formatComboBox.Items.Add("🌐 HTML (.html)");
            _formatComboBox.Items.Add("📄 " + LocalizationService.Instance.GetString("PlainText"));
            _formatComboBox.SelectedIndex = 1; // HTML por defecto
            mainStack.Children.Add(_formatComboBox);

            // Descripción del formato
            var formatDesc = new TextBlock
            {
                Text = "• Markdown: Archivo .md adjunto\n• HTML: Archivo .html adjunto con formato completo\n• Texto plano: Solo contenido en el cuerpo del mensaje\n\nNota: El cliente predeterminado abrirá un email con el documento adjunto listo para enviar.",
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.7,
                FontSize = 11,
                Margin = new Thickness(0, 4, 0, 0)
            };
            mainStack.Children.Add(formatDesc);

            // Panel SMTP (oculto por defecto)
            _smtpPanel = new StackPanel 
            { 
                Spacing = 8, 
                Visibility = Visibility.Collapsed,
                Margin = new Thickness(0, 8, 0, 0)
            };

            _smtpPanel.Children.Add(new TextBlock { Text = LocalizationService.Instance.GetString("FromEmail") + ":" });
            _fromEmailTextBox = new TextBox { PlaceholderText = "tu@correo.com" };
            _smtpPanel.Children.Add(_fromEmailTextBox);

            _smtpPanel.Children.Add(new TextBlock { Text = LocalizationService.Instance.GetString("Password") + ":" });
            _passwordBox = new PasswordBox { PlaceholderText = "••••••••" };
            _smtpPanel.Children.Add(_passwordBox);

            var smtpGrid = new Grid();
            smtpGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            smtpGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

            _smtpServerTextBox = new TextBox 
            { 
                PlaceholderText = "smtp.gmail.com",
                Text = "smtp.gmail.com"
            };
            _smtpPortTextBox = new TextBox 
            { 
                PlaceholderText = "587",
                Text = "587",
                Margin = new Thickness(8, 0, 0, 0)
            };

            Grid.SetColumn(_smtpServerTextBox, 0);
            Grid.SetColumn(_smtpPortTextBox, 1);
            smtpGrid.Children.Add(_smtpServerTextBox);
            smtpGrid.Children.Add(_smtpPortTextBox);

            _smtpPanel.Children.Add(new TextBlock { Text = LocalizationService.Instance.GetString("SMTPServer") + ":" });
            _smtpPanel.Children.Add(smtpGrid);

            _useSslCheckBox = new CheckBox 
            { 
                Content = "SSL/TLS",
                IsChecked = true
            };
            _smtpPanel.Children.Add(_useSslCheckBox);

            mainStack.Children.Add(_smtpPanel);

            // Advertencia
            var warning = new TextBlock
            {
                Text = "ℹ️ " + LocalizationService.Instance.GetString("EmailWarning"),
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.7,
                FontSize = 12,
                Margin = new Thickness(0, 12, 0, 0)
            };
            mainStack.Children.Add(warning);

            this.Content = new ScrollViewer 
            { 
                Content = mainStack,
                MaxHeight = 500
            };
            this.MaxWidth = 500;
        }

        private void MethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedIndex = _methodComboBox.SelectedIndex;

            // Mostrar panel SMTP solo para Gmail, Outlook o Custom
            _smtpPanel.Visibility = selectedIndex > 0 ? Visibility.Visible : Visibility.Collapsed;

            // Preconfigurar según el método
            switch (selectedIndex)
            {
                case 1: // Gmail
                    _smtpServerTextBox.Text = "smtp.gmail.com";
                    _smtpPortTextBox.Text = "587";
                    _useSslCheckBox.IsChecked = true;
                    break;
                case 2: // Outlook
                    _smtpServerTextBox.Text = "smtp-mail.outlook.com";
                    _smtpPortTextBox.Text = "587";
                    _useSslCheckBox.IsChecked = true;
                    break;
                case 3: // Custom
                    _smtpServerTextBox.Text = "";
                    _smtpPortTextBox.Text = "587";
                    break;
            }
        }

        private async void SendEmailDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Prevenir el cierre automático del diálogo
            var deferral = args.GetDeferral();

            try
            {
                var toEmail = _toEmailTextBox.Text.Trim();
                if (string.IsNullOrEmpty(toEmail))
                {
                    args.Cancel = true;
                    deferral.Complete();

                    // Mostrar error después de completar el deferral
                    await Task.Delay(100);
                    await ShowErrorAsync(LocalizationService.Instance.GetString("EnterEmail"));
                    return;
                }

                var method = _methodComboBox.SelectedIndex;
                var format = (EmailFormat)_formatComboBox.SelectedIndex;

                bool sendResult;

                // Método 0: Cliente predeterminado
                if (method == 0)
                {
                    sendResult = await _emailService.SendViaDefaultClientAsync(_document, toEmail);

                    // Permitir que el diálogo se cierre sin mostrar otro ContentDialog
                    // El usuario verá que Outlook/cliente de correo se abrió = éxito
                    if (!sendResult)
                    {
                        args.Cancel = true;
                    }
                    return;
                }

                // Métodos 1-3: SMTP
                var fromEmail = _fromEmailTextBox.Text.Trim();
                var password = _passwordBox.Password;
                var smtpServer = _smtpServerTextBox.Text.Trim();
                var smtpPort = int.TryParse(_smtpPortTextBox.Text, out var port) ? port : 587;
                var useSsl = _useSslCheckBox.IsChecked ?? true;

                if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(password))
                {
                    args.Cancel = true;
                    deferral.Complete();

                    // Mostrar error después de completar el deferral
                    await Task.Delay(100);
                    await ShowErrorAsync(LocalizationService.Instance.GetString("EnterCredentials"));
                    return;
                }

                // Deshabilitar el botón mientras se envía
                this.IsPrimaryButtonEnabled = false;

                sendResult = await _emailService.SendViaSmtpAsync(
                    _document,
                    toEmail,
                    fromEmail,
                    smtpServer,
                    smtpPort,
                    fromEmail,
                    password,
                    useSsl,
                    format
                );

                this.IsPrimaryButtonEnabled = true;

                // Permitir que el diálogo se cierre
                if (!sendResult)
                {
                    args.Cancel = true;
                    deferral.Complete();

                    // Mostrar error después de completar el deferral
                    await Task.Delay(100);
                    await ShowErrorAsync(LocalizationService.Instance.GetString("EmailSentError"));
                }
                // Si es exitoso, simplemente dejar que el diálogo se cierre
            }
            catch (Exception ex)
            {
                this.IsPrimaryButtonEnabled = true;
                args.Cancel = true;
                deferral.Complete();

                // Mostrar error después de completar el deferral
                await Task.Delay(100);
                await ShowErrorAsync($"Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error sending email: {ex.Message}");
            }
            finally
            {
                // Solo completar si no se ha completado ya
                try
                {
                    deferral.Complete();
                }
                catch
                {
                    // Ya se completó, ignorar
                }
            }
        }

        private async Task ShowErrorAsync(string message)
        {
            var errorDialog = new ContentDialog
            {
                Title = "❌ Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await errorDialog.ShowAsync();
        }

        private async Task ShowSuccessAsync(string message)
        {
            var successDialog = new ContentDialog
            {
                Title = "✅ " + LocalizationService.Instance.GetString("Success"),
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await successDialog.ShowAsync();
        }
    }
}
