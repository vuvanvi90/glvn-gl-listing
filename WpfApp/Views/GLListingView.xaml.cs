using System.Windows;
using System.Windows.Controls;
using WpfApp.ViewModels;

namespace WpfApp.Views
{
    public partial class GLListingView : Window
    {

        public GLListingView(GLListingViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            
        }
    }
}