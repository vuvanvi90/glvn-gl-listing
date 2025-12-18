using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApp.Services;

namespace WpfApp.Views
{
    public partial class CustomMessageBox : Window
    {
        public string MessageText { get; set; } = string.Empty;
        public string MessageTitle { get; set; } = "Message";
        public MessageType MessageType { get; set; } = MessageType.Information;
        public MessageButtons MessageButtons { get; set; } = MessageButtons.OK;
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        public CustomMessageBox()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += CustomMessageBox_Loaded;
        }

        private void CustomMessageBox_Loaded(object sender, RoutedEventArgs e)
        {
            ConfigureIcon();
            ConfigureButtons();
        }

        private void ConfigureIcon()
        {
            PackIconKind iconKind;
            Brush iconBrush;

            switch (MessageType)
            {
                case MessageType.Information:
                    iconKind = PackIconKind.Information;
                    iconBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Blue
                    break;

                case MessageType.Warning:
                    iconKind = PackIconKind.Alert;
                    iconBrush = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                    break;

                case MessageType.Error:
                    iconKind = PackIconKind.AlertCircle;
                    iconBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
                    break;

                case MessageType.Success:
                    iconKind = PackIconKind.CheckCircle;
                    iconBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
                    break;

                case MessageType.Question:
                    iconKind = PackIconKind.HelpCircle;
                    iconBrush = new SolidColorBrush(Color.FromRgb(156, 39, 176)); // Purple
                    break;

                default:
                    iconKind = PackIconKind.Information;
                    iconBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                    break;
            }

            IconElement.Kind = iconKind;
            IconElement.Foreground = iconBrush;
        }

        private void ConfigureButtons()
        {
            ButtonPanel.Children.Clear();

            switch (MessageButtons)
            {
                case MessageButtons.OK:
                    AddButton("OK", MessageBoxResult.OK, isPrimary: true);
                    break;

                case MessageButtons.OKCancel:
                    AddButton("Cancel", MessageBoxResult.Cancel, isPrimary: false);
                    AddButton("OK", MessageBoxResult.OK, isPrimary: true);
                    break;

                case MessageButtons.YesNo:
                    AddButton("No", MessageBoxResult.No, isPrimary: false);
                    AddButton("Yes", MessageBoxResult.Yes, isPrimary: true);
                    break;

                case MessageButtons.YesNoCancel:
                    AddButton("Cancel", MessageBoxResult.Cancel, isPrimary: false);
                    AddButton("No", MessageBoxResult.No, isPrimary: false);
                    AddButton("Yes", MessageBoxResult.Yes, isPrimary: true);
                    break;
            }
        }

        private void AddButton(string content, MessageBoxResult result, bool isPrimary)
        {
            var button = new Button
            {
                Content = content,
                Width = 100,
                Height = 36,
                Margin = new Thickness(8, 0, 0, 0),
                Style = isPrimary 
                    ? (Style)FindResource("MaterialDesignRaisedButton")
                    : (Style)FindResource("MaterialDesignOutlinedButton")
            };

            button.Click += (s, e) =>
            {
                Result = result;
                Close();
            };

            ButtonPanel.Children.Add(button);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            Close();
        }

        // Allow dragging the window
        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }
    }
}