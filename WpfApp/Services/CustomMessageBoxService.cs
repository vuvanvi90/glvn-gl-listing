using System.Windows;
using WpfApp.Views;

namespace WpfApp.Services
{
    public enum MessageType
    {
        Information,
        Warning,
        Error,
        Success,
        Question
    }

    public enum MessageButtons
    {
        OK,
        OKCancel,
        YesNo,
        YesNoCancel
    }

    public interface IMessageBoxService
    {
        MessageBoxResult Show(string message, string title = "Message", MessageType type = MessageType.Information, MessageButtons buttons = MessageButtons.OK);
        
        void ShowInformation(string message, string title = "Information");
        void ShowWarning(string message, string title = "Warning");
        void ShowError(string message, string title = "Error");
        void ShowSuccess(string message, string title = "Success");
        bool ShowQuestion(string message, string title = "Question");
    }

    public class CustomMessageBoxService : IMessageBoxService
    {
        public MessageBoxResult Show(string message, string title = "Message", MessageType type = MessageType.Information, MessageButtons buttons = MessageButtons.OK)
        {
            var dialog = new CustomMessageBox
            {
                Owner = Application.Current.MainWindow,
                MessageText = message,
                MessageTitle = title,
                MessageType = type,
                MessageButtons = buttons
            };

            dialog.ShowDialog();
            return dialog.Result;
        }

        public void ShowInformation(string message, string title = "Information")
        {
            Show(message, title, MessageType.Information, MessageButtons.OK);
        }

        public void ShowWarning(string message, string title = "Warning")
        {
            Show(message, title, MessageType.Warning, MessageButtons.OK);
        }

        public void ShowError(string message, string title = "Error")
        {
            Show(message, title, MessageType.Error, MessageButtons.OK);
        }

        public void ShowSuccess(string message, string title = "Success")
        {
            Show(message, title, MessageType.Success, MessageButtons.OK);
        }

        public bool ShowQuestion(string message, string title = "Question")
        {
            var result = Show(message, title, MessageType.Question, MessageButtons.YesNo);
            return result == MessageBoxResult.Yes;
        }
    }
}