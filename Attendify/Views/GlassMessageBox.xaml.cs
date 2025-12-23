using System.Windows;

namespace Attendify.Views
{
    public partial class GlassMessageBox : Window
    {
        public enum MessageBoxResult
        {
            OK,
            Cancel
        }

        public enum MessageType
        {
            Info,
            Success,
            Error
        }

        private MessageBoxResult _result = MessageBoxResult.Cancel;

        public GlassMessageBox(string message, string title = "Notification", bool showCancel = false, MessageType type = MessageType.Info)
        {
            InitializeComponent();
            MessageText.Text = message;
            TitleText.Text = title;

            // Auto-detect Error type based on title if not explicitly set
            if (type == MessageType.Info && 
                (title.Contains("Error", StringComparison.OrdinalIgnoreCase) || 
                 title.Contains("Failed", StringComparison.OrdinalIgnoreCase)))
            {
                type = MessageType.Error;
            }

            if (showCancel)
            {
                SecondaryButton.Visibility = Visibility.Visible;
            }

            ApplyTypeStyling(type);
        }

        private void ApplyTypeStyling(MessageType type)
        {
            var brushConverter = new System.Windows.Media.BrushConverter();
            switch (type)
            {
                case MessageType.Success:
                    TitleText.Foreground = (System.Windows.Media.Brush)brushConverter.ConvertFromString("#4CAF50");
                    PrimaryButton.Background = (System.Windows.Media.Brush)brushConverter.ConvertFromString("#4CAF50");
                    break;
                case MessageType.Error:
                    TitleText.Foreground = (System.Windows.Media.Brush)brushConverter.ConvertFromString("#F44336");
                    PrimaryButton.Background = (System.Windows.Media.Brush)brushConverter.ConvertFromString("#F44336");
                    break;
                case MessageType.Info:
                default:
                    // Keep default blue (#00A6FB)
                    break;
            }
        }

        public static MessageBoxResult Show(string message, string title = "Notification", bool showCancel = false, MessageType type = MessageType.Info)
        {
            var msgBox = new GlassMessageBox(message, title, showCancel, type);
            msgBox.ShowDialog();
            return msgBox._result;
        }

        private void PrimaryButton_Click(object sender, RoutedEventArgs e)
        {
            _result = MessageBoxResult.OK;
            this.Close();
        }

        private void SecondaryButton_Click(object sender, RoutedEventArgs e)
        {
            _result = MessageBoxResult.Cancel;
            this.Close();
        }
    }
}
