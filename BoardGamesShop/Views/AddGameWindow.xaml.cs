using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using BoardGamesShop.ViewModels;

namespace BoardGamesShop.Views
{
    public partial class AddGameWindow : Window
    {
        public AddGameWindow()
        {
            InitializeComponent();
            DataContext = new AddGameViewModel();
        }

        private void BrowseImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Obrázky|*.png;*.jpg;*.jpeg|Všechny soubory|*.*",
                Title = "Vybrat obrázek"
            };

            if (dlg.ShowDialog(this) == true)
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string p = dlg.FileName;

                if (p.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                    p = p.Substring(baseDir.Length).TrimStart(Path.DirectorySeparatorChar);

                p = p.Replace('\\', '/');

                if (DataContext is AddGameViewModel vm)
                    vm.ImagePath = p;
            }
        }
    }
}
