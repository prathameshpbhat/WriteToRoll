using App.ViewModels;
using MahApps.Metro.Controls;
using System.Windows;

namespace App.Host
{
    public partial class MainWindow : MetroWindow
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Editor.Focus();
            Editor.Document.Blocks.Clear();
        }
    }
}