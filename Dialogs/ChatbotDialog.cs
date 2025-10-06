using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using Jot.Services;
using Jot.Models;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using System.Linq;

namespace Jot.Dialogs
{
    public sealed partial class ChatbotDialog : ContentDialog
    {
        private ChatbotService _chatbotService;
        private List<Document> _documents;

        public ChatbotDialog(ChatbotService chatbotService, List<Document> documents)
        {
            _chatbotService = chatbotService;
            _documents = documents;
            
            this.Title = "🤖 AI Assistant";
            this.PrimaryButtonText = "Cerrar";
            this.DefaultButton = ContentDialogButton.Primary;
            
            SetupUI();
            LoadSuggestedQuestions();
        }

        private void SetupUI()
        {
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Chat area
            var chatScrollViewer = new ScrollViewer
            {
                Name = "ChatScrollViewer",
                Height = 400,
                Padding = new Thickness(16, 16, 16, 16),
                ZoomMode = ZoomMode.Disabled,
                HorizontalScrollMode = ScrollMode.Disabled,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var chatPanel = new StackPanel
            {
                Name = "ChatPanel",
                Spacing = 12
            };

            // Welcome message
            var welcomeBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(255, 100, 149, 237)),
                CornerRadius = new CornerRadius(12, 12, 12, 4),
                Padding = new Thickness(12, 8, 12, 8),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 300
            };

            var welcomeText = new TextBlock
            {
                Text = "¡Hola! Soy tu asistente de documentos. Puedes preguntarme sobre el contenido de tus documentos.",
                Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14
            };

            welcomeBorder.Child = welcomeText;
            chatPanel.Children.Add(welcomeBorder);
            chatScrollViewer.Content = chatPanel;
            Grid.SetRow(chatScrollViewer, 0);
            mainGrid.Children.Add(chatScrollViewer);

            // Suggestions area
            var suggestionsScroll = new ScrollViewer
            {
                HorizontalScrollMode = ScrollMode.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollMode = ScrollMode.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(0, 8, 0, 8)
            };

            var suggestionsPanel = new StackPanel
            {
                Name = "SuggestionsPanel",
                Orientation = Orientation.Horizontal,
                Spacing = 8
            };

            suggestionsScroll.Content = suggestionsPanel;
            Grid.SetRow(suggestionsScroll, 1);
            mainGrid.Children.Add(suggestionsScroll);

            // Input area
            var inputGrid = new Grid();
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var questionBox = new TextBox
            {
                Name = "QuestionTextBox",
                PlaceholderText = "Pregunta sobre tus documentos...",
                AcceptsReturn = false,
                MaxLength = 500,
                Margin = new Thickness(0, 0, 8, 0)
            };

            var sendButton = new Button
            {
                Name = "SendButton",
                Content = "Enviar",
                Style = Application.Current.Resources["AccentButtonStyle"] as Style,
                IsEnabled = false
            };

            questionBox.TextChanged += (s, e) =>
            {
                sendButton.IsEnabled = !string.IsNullOrWhiteSpace(questionBox.Text);
            };

            sendButton.Click += async (s, e) =>
            {
                await SendQuestion(questionBox.Text, chatPanel, questionBox, sendButton);
            };

            questionBox.KeyDown += async (s, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter && sendButton.IsEnabled)
                {
                    await SendQuestion(questionBox.Text, chatPanel, questionBox, sendButton);
                }
            };

            Grid.SetColumn(questionBox, 0);
            Grid.SetColumn(sendButton, 1);
            inputGrid.Children.Add(questionBox);
            inputGrid.Children.Add(sendButton);

            Grid.SetRow(inputGrid, 2);
            mainGrid.Children.Add(inputGrid);

            this.Content = mainGrid;
        }

        private async Task SendQuestion(string question, StackPanel chatPanel, TextBox questionBox, Button sendButton)
        {
            if (string.IsNullOrWhiteSpace(question)) return;

            System.Diagnostics.Debug.WriteLine($"ChatbotDialog: Sending question '{question}'");

            // Add user message
            AddMessage(chatPanel, question, true);

            // Clear input and disable button
            questionBox.Text = "";
            sendButton.IsEnabled = false;

            try
            {
                // Get response from chatbot
                var response = await _chatbotService.AskQuestionAsync(question, _documents);

                System.Diagnostics.Debug.WriteLine($"ChatbotDialog: Received response: '{response.Answer}'");
                System.Diagnostics.Debug.WriteLine($"ChatbotDialog: Confidence: {response.Confidence:P2}");
                System.Diagnostics.Debug.WriteLine($"ChatbotDialog: Sources: {string.Join(", ", response.SourceDocuments)}");

                // Add bot response
                AddMessage(chatPanel, response.Answer, false, response);

                // Auto-scroll to bottom
                await ScrollToBottomAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChatbotDialog Error: {ex.Message}");
                AddMessage(chatPanel, $"Error: {ex.Message}", false);
            }
        }

        private async Task ScrollToBottomAsync()
        {
            try
            {
                // Find the chat scroll viewer by searching through the visual tree
                var mainGrid = this.Content as Grid;
                if (mainGrid != null)
                {
                    // The chat scroll viewer is the first child in row 0
                    var chatScrollViewer = mainGrid.Children.FirstOrDefault() as ScrollViewer;
                    if (chatScrollViewer != null)
                    {
                        // Small delay to ensure content is rendered
                        await Task.Delay(100);
                        
                        // Force update layout
                        chatScrollViewer.UpdateLayout();
                        
                        // Scroll to bottom
                        chatScrollViewer.ChangeView(null, chatScrollViewer.ScrollableHeight, null, false);
                        
                        System.Diagnostics.Debug.WriteLine($"ChatbotDialog: Scrolled to bottom. ScrollableHeight: {chatScrollViewer.ScrollableHeight}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("ChatbotDialog: Could not find chat scroll viewer");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error scrolling to bottom: {ex.Message}");
            }
        }

        private void AddMessage(StackPanel chatPanel, string message, bool isUser, ChatResponse? response = null)
        {
            System.Diagnostics.Debug.WriteLine($"ChatbotDialog: Adding message - User: {isUser}, Message: '{message}'");

            var messageBorder = new Border
            {
                CornerRadius = new CornerRadius(12, 12, isUser ? 4 : 12, isUser ? 12 : 4),
                Padding = new Thickness(12, 8, 12, 8),
                MaxWidth = isUser ? 250 : 350, // Increase max width for bot messages
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Margin = new Thickness(0, 4, 0, 4)
            };

            if (isUser)
            {
                messageBorder.Background = new SolidColorBrush(Color.FromArgb(255, 100, 149, 237));
            }
            else
            {
                messageBorder.Background = new SolidColorBrush(Color.FromArgb(60, 128, 128, 128)); // More visible background
            }

            var contentPanel = new StackPanel();

            // Ensure message is not empty
            var displayMessage = string.IsNullOrWhiteSpace(message) ? "[Sin respuesta]" : message;

            var messageText = new TextBlock
            {
                Text = displayMessage,
                Foreground = isUser ? new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)) : 
                                      new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)), // Force white text for bot too
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13, // Slightly smaller font
                MinHeight = 20, // Ensure minimum height
                LineHeight = 18,
                IsTextSelectionEnabled = true // Allow text selection
            };
            contentPanel.Children.Add(messageText);

            // Add confidence for bot responses
            if (!isUser && response != null && response.Confidence > 0)
            {
                var confidenceText = new TextBlock
                {
                    Text = $"Confianza: {response.Confidence:P0}",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                    Margin = new Thickness(0, 6, 0, 0)
                };
                contentPanel.Children.Add(confidenceText);

                if (response.SourceDocuments.Count > 0)
                {
                    var sourcesText = new TextBlock
                    {
                        Text = $"Fuentes: {string.Join(", ", response.SourceDocuments)}",
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                        Margin = new Thickness(0, 2, 0, 0)
                    };
                    contentPanel.Children.Add(sourcesText);
                }
            }

            messageBorder.Child = contentPanel;
            chatPanel.Children.Add(messageBorder);

            System.Diagnostics.Debug.WriteLine($"ChatbotDialog: Message added to chat panel. Total messages: {chatPanel.Children.Count}");
            System.Diagnostics.Debug.WriteLine($"ChatbotDialog: Message text length: {displayMessage.Length}");
        }

        private async void LoadSuggestedQuestions()
        {
            try
            {
                var suggestions = await _chatbotService.GetSuggestedQuestionsAsync(_documents);
                var suggestionsPanel = FindName("SuggestionsPanel") as StackPanel;
                
                if (suggestionsPanel != null)
                {
                    foreach (var suggestion in suggestions)
                    {
                        var button = new Button
                        {
                            Content = suggestion,
                            Margin = new Thickness(0, 0, 0, 0),
                            MaxWidth = 200,
                            Background = new SolidColorBrush(Color.FromArgb(40, 100, 149, 237)),
                            BorderBrush = new SolidColorBrush(Color.FromArgb(100, 100, 149, 237))
                        };

                        button.Click += async (s, e) =>
                        {
                            var questionBox = FindName("QuestionTextBox") as TextBox;
                            var sendButton = FindName("SendButton") as Button;
                            var chatPanel = FindName("ChatPanel") as StackPanel;
                            
                            if (questionBox != null && sendButton != null && chatPanel != null)
                            {
                                await SendQuestion(suggestion, chatPanel, questionBox, sendButton);
                            }
                        };

                        suggestionsPanel.Children.Add(button);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading suggestions: {ex.Message}");
            }
        }
    }
}