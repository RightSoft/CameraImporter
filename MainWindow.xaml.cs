using System.Windows;
using System.Windows.Controls;
using CameraImporter.ViewModel;

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
            //ViewModel.SettingsData.ArchiverName = ArchiverSelection.SelectedValue.ToString();
        }
    }
}
