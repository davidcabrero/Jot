using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;
using Jot.Models;

namespace Jot.Controls
{
    public sealed partial class QuizControl : UserControl
    {
        public static readonly DependencyProperty QuestionProperty =
            DependencyProperty.Register(nameof(Question), typeof(QuizQuestion), typeof(QuizControl),
                new PropertyMetadata(null, OnQuestionChanged));

        public QuizQuestion Question
        {
            get => (QuizQuestion)GetValue(QuestionProperty);
            set => SetValue(QuestionProperty, value);
        }

        private static void OnQuestionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is QuizControl control && e.NewValue is QuizQuestion question)
            {
                control.SetupQuestion(question);
            }
        }

        public QuizControl()
        {
            this.InitializeComponent();
        }

        private void SetupQuestion(QuizQuestion question)
        {
            QuestionText.Text = question.Question;
            OptionsPanel.Children.Clear();

            for (int i = 0; i < question.Options.Count; i++)
            {
                var radioButton = new RadioButton
                {
                    Content = question.Options[i],
                    GroupName = $"Quiz_{question.Id}",
                    Tag = i,
                    Margin = new Thickness(0, 4, 0, 4)
                };

                radioButton.Checked += RadioButton_Checked;
                OptionsPanel.Children.Add(radioButton);
            }

            SubmitButton.IsEnabled = false;
            ResultPanel.Visibility = Visibility.Collapsed;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            SubmitButton.IsEnabled = true;
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedOption = OptionsPanel.Children
                .OfType<RadioButton>()
                .FirstOrDefault(rb => rb.IsChecked == true);

            if (selectedOption != null && selectedOption.Tag is int selectedIndex)
            {
                bool isCorrect = selectedIndex == Question.CorrectAnswerIndex;
                
                ResultIcon.Glyph = isCorrect ? "&#xE73E;" : "&#xE711;"; // Checkmark or X
                ResultText.Text = isCorrect ? "Correct!" : "Incorrect";
                ExplanationText.Text = Question.Explanation;
                
                ResultPanel.Visibility = Visibility.Visible;
                SubmitButton.IsEnabled = false;

                // Disable all options
                foreach (var rb in OptionsPanel.Children.OfType<RadioButton>())
                {
                    rb.IsEnabled = false;
                    if ((int)rb.Tag == Question.CorrectAnswerIndex)
                    {
                        rb.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                            Windows.UI.Color.FromArgb(255, 0, 120, 215)); // Highlight correct answer
                    }
                }
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var rb in OptionsPanel.Children.OfType<RadioButton>())
            {
                rb.IsChecked = false;
                rb.IsEnabled = true;
                rb.ClearValue(ForegroundProperty);
            }

            ResultPanel.Visibility = Visibility.Collapsed;
            SubmitButton.IsEnabled = false;
        }
    }
}