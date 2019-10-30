using CameraImporter.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace CameraImporter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => (DataContext as MainViewModel);

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnArchiveChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SettingsData.Archiver = ViewModel.SelectedArchiverModel;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext != null)
            {
                ViewModel.SettingsData.Password = ((PasswordBox)sender).Password;
            }
        }
    }
}
