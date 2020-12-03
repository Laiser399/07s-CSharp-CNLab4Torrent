using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CNLab4_Client.GUI
{
    /// <summary>
    /// Логика взаимодействия для AddTorrentDialog.xaml
    /// </summary>
    public partial class AddTorrentDialog : Window
    {
        private AddTorrentDialogVM _viewModel;

        public string AccessCode
        {
            get => _viewModel.AccessCode;
            set => _viewModel.AccessCode = value;
        }

        public string Directory
        {
            get => _viewModel.Directory;
            set => _viewModel.Directory = value;
        }

        public AddTorrentDialog()
        {
            InitializeComponent();
            _viewModel = new AddTorrentDialogVM(this);
            DataContext = _viewModel;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (AccessCode.Length == 0)
            {
                MessageBox.Show("Access code is empty.");
                return;
            }
            if (!System.IO.Directory.Exists(Directory))
            {
                MessageBox.Show("Chosen directory does not exists.");
                return;
            }
            DialogResult = true;
        }
    }
}
