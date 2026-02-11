using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Jot.Services;

namespace Jot.Dialogs
{
    public sealed partial class OcrDialog : ContentDialog
    {
        private readonly OcrService _ocrService;

        private TextBox _extractedTextBox;
        private ProgressRing _progressRing;
        private TextBlock _statusTextBlock;
        private Button _selectImageButton;
        private Button _clipboardButton;
        private ComboBox _languageComboBox;

        public string ExtractedText { get; private set; }

        public OcrDialog(OcrService ocrService)
        {
            _ocrService = ocrService;

            this.Title = "📸 " + LocalizationService.Instance.GetString("OCR");
            this.PrimaryButtonText = LocalizationService.Instance.GetString("InsertText");
            this.SecondaryButtonText = LocalizationService.Instance.GetString("Close");
            this.DefaultButton = ContentDialogButton.Secondary;
            this.IsPrimaryButtonEnabled = false;

            SetupUI();
            LoadLanguages();
        }

        private void SetupUI()
        {
            var mainStack = new StackPanel { Spacing = 12, Margin = new Thickness(0, 12, 0, 0) };

            // Icono
            var iconText = new TextBlock
            {
                Text = "📸",
                FontSize = 48,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            mainStack.Children.Add(iconText);

            var description = new TextBlock
            {
                Text = LocalizationService.Instance.GetString("OCRDescription"),
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.8,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            };
            mainStack.Children.Add(description);

            // Idioma
            mainStack.Children.Add(new TextBlock 
            { 
                Text = LocalizationService.Instance.GetString("OCRLanguage") + ":"
            });
            _languageComboBox = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            mainStack.Children.Add(_languageComboBox);

            // Botones de acción
            var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 8, 0, 0) };

            _selectImageButton = new Button
            {
                Content = "🖼️ " + LocalizationService.Instance.GetString("SelectImage"),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            _selectImageButton.Click += SelectImageButton_Click;
            buttonsPanel.Children.Add(_selectImageButton);

            _clipboardButton = new Button
            {
                Content = "📋 " + LocalizationService.Instance.GetString("FromClipboard"),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            _clipboardButton.Click += ClipboardButton_Click;
            buttonsPanel.Children.Add(_clipboardButton);

            mainStack.Children.Add(buttonsPanel);

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

            // Status
            _statusTextBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0),
                Visibility = Visibility.Collapsed
            };
            mainStack.Children.Add(_statusTextBlock);

            // Texto extraído
            mainStack.Children.Add(new TextBlock 
            { 
                Text = LocalizationService.Instance.GetString("ExtractedText") + ":",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(0, 12, 0, 0)
            });

            var scrollViewer = new ScrollViewer
            {
                Height = 300,
                BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                BorderThickness = new Thickness(1)
            };

            _extractedTextBox = new TextBox
            {
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                PlaceholderText = LocalizationService.Instance.GetString("NoTextExtracted"),
                MinHeight = 280
            };
            scrollViewer.Content = _extractedTextBox;
            mainStack.Children.Add(scrollViewer);

            this.Content = mainStack;
            this.MaxWidth = 600;
            this.MaxHeight = 700;

            this.PrimaryButtonClick += OcrDialog_PrimaryButtonClick;
        }

        private void LoadLanguages()
        {
            try
            {
                var languages = _ocrService.GetSupportedLanguages();
                foreach (var language in languages)
                {
                    _languageComboBox.Items.Add(language);
                }

                if (_languageComboBox.Items.Count > 0)
                {
                    _languageComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading OCR languages: {ex.Message}");
            }
        }

        private async void SelectImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".bmp");

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    await ProcessImageAsync(file.Path);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private async void ClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetProcessing(true);
                _statusTextBlock.Text = "⏳ " + LocalizationService.Instance.GetString("ExtractingText");
                _statusTextBlock.Visibility = Visibility.Visible;

                var text = await _ocrService.ExtractTextFromClipboardAsync();
                
                if (!string.IsNullOrEmpty(text) && !text.StartsWith("["))
                {
                    _extractedTextBox.Text = text;
                    this.IsPrimaryButtonEnabled = true;
                    ShowSuccess(LocalizationService.Instance.GetString("TextExtracted"));
                }
                else
                {
                    ShowError(LocalizationService.Instance.GetString("NoImageInClipboard"));
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                SetProcessing(false);
            }
        }

        private async Task ProcessImageAsync(string imagePath)
        {
            try
            {
                SetProcessing(true);
                _statusTextBlock.Text = "⏳ " + LocalizationService.Instance.GetString("ExtractingText");
                _statusTextBlock.Visibility = Visibility.Visible;

                var text = await _ocrService.ExtractTextFromImageAsync(imagePath);

                if (!string.IsNullOrEmpty(text))
                {
                    _extractedTextBox.Text = text;
                    this.IsPrimaryButtonEnabled = true;
                    ShowSuccess($"✅ {LocalizationService.Instance.GetString("TextExtracted")} ({text.Length} {LocalizationService.Instance.GetString("Characters")})");
                }
                else
                {
                    ShowError(LocalizationService.Instance.GetString("NoTextFound"));
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                SetProcessing(false);
            }
        }

        private void SetProcessing(bool processing)
        {
            _progressRing.IsActive = processing;
            _progressRing.Visibility = processing ? Visibility.Visible : Visibility.Collapsed;
            _selectImageButton.IsEnabled = !processing;
            _clipboardButton.IsEnabled = !processing;
        }

        private void ShowSuccess(string message)
        {
            _statusTextBlock.Text = message;
            _statusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
            _statusTextBlock.Visibility = Visibility.Visible;
        }

        private void ShowError(string message)
        {
            _statusTextBlock.Text = "❌ " + message;
            _statusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            _statusTextBlock.Visibility = Visibility.Visible;
        }

        private void OcrDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            ExtractedText = _extractedTextBox.Text;
        }
    }
}
